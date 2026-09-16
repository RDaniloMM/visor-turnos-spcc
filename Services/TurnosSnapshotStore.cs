using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed class TurnosSnapshotStore
{
    private TurnosSnapshotDto _current;

    public TurnosSnapshotStore(IOptions<SiteOptions> siteOptions, TimeProvider timeProvider)
    {
        _current = new TurnosSnapshotDto(
            0,
            timeProvider.GetUtcNow(),
            siteOptions.Value.DisplayName,
            "unavailable",
            []);
    }

    public TurnosSnapshotDto Get() => Volatile.Read(ref _current);

    public void Set(TurnosSnapshotDto snapshot) => Volatile.Write(ref _current, snapshot);

    public void RefreshSuccessful(DateTimeOffset generatedAt)
    {
        while (true)
        {
            var current = Get();
            var refreshed = current with { GeneratedAt = generatedAt, Status = "live" };
            if (ReferenceEquals(Interlocked.CompareExchange(ref _current, refreshed, current), current))
            {
                return;
            }
        }
    }
}
