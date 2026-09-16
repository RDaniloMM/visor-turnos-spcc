using VisorTurnos.Domain;

namespace VisorTurnos.Data;

public sealed class DisabledTurnosRepository : ITurnosRepository
{
    public Task<IReadOnlyList<TurnoRaw>> GetForDayAsync(
        int siteCode,
        DateTime dayStart,
        DateTime nextDayStart,
        int maxRows,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TurnoRaw>>([]);
}
