using VisorTurnos.Domain;
using VisorTurnos.Services;

namespace VisorTurnos.UnitTests;

public sealed class TurnosChangeDetectorTests
{
    [Fact]
    public void FingerprintChangesWhenAnAnnouncementMustBeDelivered()
    {
        var item = new TurnoPublicoDto("T1", "C1", "Medico", "en-atencion", 100, false, false, null, null, true);
        var first = new TurnosSnapshotDto(1, DateTimeOffset.UnixEpoch, "Site", "live", [item]);
        var second = new TurnosSnapshotDto(9, DateTimeOffset.Now, "Site", "live", [item with { ShouldAnnounce = false }]);

        Assert.True(new TurnosChangeDetector().HasVisibleChange(first, second));
    }

    [Fact]
    public void PublicContractContainsNoRawPiiFields()
    {
        var names = typeof(TurnoPublicoDto).GetProperties().Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Pacnam", names);
        Assert.DoesNotContain("Pachis", names);
        Assert.DoesNotContain("PrefacturaNumber", names);
    }
}
