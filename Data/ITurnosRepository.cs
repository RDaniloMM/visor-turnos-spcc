using VisorTurnos.Domain;

namespace VisorTurnos.Data;

public interface ITurnosRepository
{
    Task<IReadOnlyList<TurnoRaw>> GetForDayAsync(
        int siteCode,
        DateTime dayStart,
        DateTime dayEndExclusive,
        int maxRows,
        CancellationToken cancellationToken);
}
