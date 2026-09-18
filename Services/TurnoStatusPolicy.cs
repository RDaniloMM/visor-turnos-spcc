using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed class TurnoStatusPolicy(
    PrefacturaPolicy prefacturaPolicy,
    IOptions<BusinessRulesOptions> options)
{
    public TurnoStatus Normalize(TurnoRaw turno, DateTimeOffset now)
    {
        var rawStatus = turno.RawStatus?.Trim();
        var consultationStatus = turno.ConsultationStatus?.Trim();
        var prefactura = prefacturaPolicy.Evaluate(turno.PrefacturaNumber);

        // Una reapertura crea un numcon nuevo en T, pero LOLCLI puede conservar
        // citas.statte=S. El acto más reciente es la señal específica y debe
        // prevalecer sobre el estado histórico de la cita.
        if (prefactura == PrefacturaPresence.Present &&
            turno.ConsultationId.HasValue &&
            string.Equals(consultationStatus, "T", StringComparison.OrdinalIgnoreCase))
        {
            return TurnoStatus.EnEspera;
        }

        if (string.Equals(consultationStatus, "P", StringComparison.OrdinalIgnoreCase))
        {
            return TurnoStatus.Cerrado;
        }

        if (!string.IsNullOrEmpty(rawStatus) && options.Value.ClosedStatusCodes.Contains(rawStatus, StringComparer.OrdinalIgnoreCase))
        {
            return TurnoStatus.Cerrado;
        }

        if (prefactura == PrefacturaPresence.Present)
        {
            return TurnoStatus.Desconocido;
        }

        if (prefactura == PrefacturaPresence.Unknown)
        {
            return TurnoStatus.Desconocido;
        }

        if (turno.ArrivedAt.HasValue)
        {
            return TurnoStatus.EnEspera;
        }

        if (turno.ScheduledAt != default && turno.ScheduledAt.Date >= now.Date)
        {
            return TurnoStatus.PendienteLlegada;
        }

        return TurnoStatus.Desconocido;
    }
}
