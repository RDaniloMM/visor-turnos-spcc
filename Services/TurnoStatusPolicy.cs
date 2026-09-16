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
        if (!string.IsNullOrEmpty(rawStatus) && options.Value.ClosedStatusCodes.Contains(rawStatus, StringComparer.OrdinalIgnoreCase))
        {
            return TurnoStatus.Cerrado;
        }

        var prefactura = prefacturaPolicy.Evaluate(turno.PrefacturaNumber);
        if (prefactura == PrefacturaPresence.Present)
        {
            return TurnoStatus.EnAtencion;
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
