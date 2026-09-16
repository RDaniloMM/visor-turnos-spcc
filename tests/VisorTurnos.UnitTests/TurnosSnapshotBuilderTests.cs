using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.UnitTests;

public sealed class TurnosSnapshotBuilderTests
{
    [Fact]
    public void OrderingIsStableAndConfiguredPriorityWins()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true, PriorityTierByCitedType = new() { ["URG"] = 1 } });
        var at = new DateTime(2026, 9, 16, 9, 0, 0);
        TurnoRaw[] raw =
        [
            new(2, "B", "C2", "Medico B", at, at, "N", null, null, null, false),
            new(3, "URG", "C3", "Medico C", at.AddMinutes(5), at.AddMinutes(5), "N", null, "URG", null, false),
            new(1, "A", "C1", "Medico A", at, at, "N", null, null, null, false)
        ];

        var result = builder.Build(raw, new Dictionary<long, TurnoStatus>(), new DateTimeOffset(at, TimeSpan.Zero));

        Assert.Equal(["URG", "A", "B"], result.Items.Select(item => item.PublicId));
    }

    [Fact]
    public void TransitionFromWaitingToAttentionAnnouncesOnlyThatTransition()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 16, 9, 0, 0);
        TurnoRaw[] raw = [new(1, "DEMO", "C1", "Medico", at, at, "N", 20, null, null, false)];

        var transition = builder.Build(raw, new Dictionary<long, TurnoStatus> { [1] = TurnoStatus.EnEspera }, new DateTimeOffset(at, TimeSpan.Zero));
        var repeated = builder.Build(raw, transition.States, new DateTimeOffset(at, TimeSpan.Zero));

        Assert.True(transition.Items.Single().ShouldAnnounce);
        Assert.False(repeated.Items.Single().ShouldAnnounce);
    }

    [Fact]
    public void MissingPublicIdentifierIsNeverExposed()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 16, 9, 0, 0);

        var result = builder.Build([new(99, null, "C1", "Medico", at, at, "N", null, null, null, false)], new Dictionary<long, TurnoStatus>(), new DateTimeOffset(at, TimeSpan.Zero));

        Assert.Empty(result.Items);
    }

    [Fact]
    public void MedicalExamIsPriorityOneAndPrecedesEarlierRegularAppointment()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 16, 9, 0, 0);
        TurnoRaw[] raw =
        [
            new(1, "REGULAR", "C1", "Medico 1", at, at, "N", null, null, null, false),
            new(2, "EMA", "C2", "Medico 2", at.AddHours(1), at.AddMinutes(5), "N", null, null, null, true)
        ];

        var result = builder.Build(raw, new Dictionary<long, TurnoStatus>(), new DateTimeOffset(at, TimeSpan.Zero));

        Assert.Equal("EMA", result.Items[0].PublicId);
        Assert.Equal("Dr. Medico 2", result.Items[0].Medico);
        Assert.Equal(1, result.Items[0].PriorityTier);
        Assert.True(result.Items[0].IsMedicalExam);
    }

    [Fact]
    public void DoctorNameUsesPrefixWithoutDuplicatingExistingPrefix()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 16, 9, 0, 0);
        TurnoRaw[] raw =
        [
            new(1, "A", "C1", "Medico Uno", at, at, "N", null, null, null, false),
            new(2, "B", "C2", "Dr. Medico Dos", at, at, "N", null, null, null, false)
        ];

        var result = builder.Build(raw, new Dictionary<long, TurnoStatus>(), new DateTimeOffset(at, TimeSpan.Zero));

        Assert.Equal(["Dr. Medico Uno", "Dr. Medico Dos"], result.Items.Select(item => item.Medico));
    }

    [Fact]
    public void SnapshotRetainsAllQueriedItemsForAreaRotation()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 16, 9, 0, 0);
        var raw = Enumerable.Range(1, 9)
            .Select(index => new TurnoRaw(index, $"T{index}", "C1", "Medico", at.AddMinutes(index), at, "N", null, null, null, false))
            .ToArray();

        var result = builder.Build(raw, new Dictionary<long, TurnoStatus>(), new DateTimeOffset(at, TimeSpan.Zero));

        Assert.Equal(9, result.Items.Count);
    }

    private static TurnosSnapshotBuilder CreateBuilder(BusinessRulesOptions rules)
    {
        var ruleOptions = Microsoft.Extensions.Options.Options.Create(rules);
        var status = new TurnoStatusPolicy(new PrefacturaPolicy(ruleOptions), ruleOptions);
        return new TurnosSnapshotBuilder(
            status,
            new PrefacturaPolicy(ruleOptions),
            new PriorityPolicy(
                ruleOptions,
                Microsoft.Extensions.Options.Options.Create(new PriorityOptions())),
            new CalledTurnRotationPolicy(
                Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 })),
            Microsoft.Extensions.Options.Options.Create(new SiteOptions { Code = 1, DisplayName = "Test", TimeZone = "UTC" }));
    }
}
