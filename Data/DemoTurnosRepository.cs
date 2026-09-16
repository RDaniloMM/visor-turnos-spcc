using VisorTurnos.Domain;

namespace VisorTurnos.Data;

public sealed class DemoTurnosRepository(TimeProvider timeProvider) : ITurnosRepository
{
    private readonly DateTime _anchor = timeProvider.GetLocalNow().DateTime;

    public Task<IReadOnlyList<TurnoRaw>> GetForDayAsync(
        int siteCode,
        DateTime dayStart,
        DateTime nextDayStart,
        int maxRows,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _anchor;
        IReadOnlyList<TurnoRaw> items =
        [
            new(1001, "DEMO-042", "Consultorio 03", "Dra. Andrea Vega", now.AddMinutes(-18), now.AddMinutes(-12), "N", 7812, null, null, true),
            new(1002, "DEMO-043", "Consultorio 01", "Dr. Luis Ramos", now.AddMinutes(-8), now.AddMinutes(-5), "N", null, null, null, false),
            new(1003, "DEMO-044", "Consultorio 05", "Dra. Carla Torres", now.AddMinutes(2), now.AddMinutes(-2), "N", 0, null, null, true),
            new(1004, "DEMO-045", "Consultorio 02", "Dr. Miguel Paredes", now.AddMinutes(12), null, "N", null, null, null, false),
            new(1005, "DEMO-046", "Consultorio 01", "Dr. Luis Ramos", now.AddMinutes(22), null, "N", null, null, null, false),
            new(1006, "DEMO-047", "Consultorio 05", "Dra. Carla Torres", now.AddMinutes(32), null, "N", null, null, null, false),
            new(1007, "DEMO-048", "Consultorio 02", "Dr. Miguel Paredes", now.AddMinutes(42), null, "N", null, null, null, false),
            new(1008, "DEMO-049", "Consultorio 01", "Dr. Luis Ramos", now.AddMinutes(52), null, "N", null, null, null, false)
        ];

        return Task.FromResult<IReadOnlyList<TurnoRaw>>(items.Take(maxRows).ToArray());
    }
}
