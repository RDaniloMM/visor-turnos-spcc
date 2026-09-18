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

        // numcon es la señal de que el médico seleccionó al paciente. No se
        // exige que la prefactura ya exista: el acto más reciente prevalece
        // sobre el estado histórico de citas mientras no haya sido guardado.
        if (turno.ConsultationId.HasValue &&
            string.Equals(consultationStatus, "P", StringComparison.OrdinalIgnoreCase))
        {
            return TurnoStatus.Cerrado;
        }

        if (turno.ConsultationId.HasValue)
        {
            return TurnoStatus.EnEspera;
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
