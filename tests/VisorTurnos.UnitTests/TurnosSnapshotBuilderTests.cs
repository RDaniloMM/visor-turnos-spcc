using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.UnitTests;

public sealed class TurnosSnapshotBuilderTests
{
    [Fact]
    public void ValidatedPriorityHierarchyPrecedesAppointmentTime()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 16, 9, 0, 0);
        TurnoRaw[] raw =
        [
            new(1, "REGULAR", "C1", "Medico A", at, at, "N", null, null, null, false),
            new(2, "AMANECIDA", "C2", "Medico B", at.AddMinutes(5), at.AddMinutes(5), "N", null, null, null, false, IsAmanecida: true),
            new(3, "EMA", "C3", "Medico C", at.AddMinutes(10), at.AddMinutes(10), "N", null, null, null, true)
        ];

        var result = builder.Build(raw, new Dictionary<long, TurnoStatus>(), new DateTimeOffset(at, TimeSpan.Zero));

        Assert.Equal(["EMA", "AMANECIDA", "REGULAR"], result.Items.Select(item => item.PublicId));
    }

    [Fact]
    public void TurnWithNumconAndNoPrefacturaIsExposedAsReady()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 16, 9, 0, 0);
        TurnoRaw[] raw = [new(1, "DEMO", "C1", "Medico", at, at, "N", 0, null, null, false, true, null, ConsultationId: 100)];

        var transition = builder.Build(raw, new Dictionary<long, TurnoStatus> { [1] = TurnoStatus.EnEspera }, new DateTimeOffset(at, TimeSpan.Zero));
        Assert.Equal(TurnoStatus.EnEspera, transition.States[1]);
        Assert.Single(transition.Items);
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
    public void ReadyMedicalExamCanBeCalledBeforeItsScheduledTime()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var now = new DateTime(2026, 9, 16, 9, 0, 0);
        TurnoRaw[] raw =
        [
            new(1, "REGULAR", "C1", "Medico 1", now, now, "N", 20, null, null, false, true, "T", ConsultationId: 100),
            new(2, "EMA", "C2", "Medico 2", now.AddHours(2), now, "N", 21, null, null, true, true, "T", ConsultationId: 101)
        ];

        builder.Build([], new Dictionary<long, TurnoStatus>(), new DateTimeOffset(now, TimeSpan.Zero));
        var result = builder.Build(raw, new Dictionary<long, TurnoStatus>(), new DateTimeOffset(now.AddSeconds(3), TimeSpan.Zero));

        var active = Assert.Single(result.Items, item => item.IsActiveCall);
        Assert.Equal("EMA", active.PublicId);
        Assert.True(active.ShouldAnnounce);
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

    [Fact]
    public void PastScheduledPatientMovesAfterTheUpcomingAppointmentsWithoutBeingDeleted()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var scheduled = new DateTime(2026, 9, 16, 9, 0, 0);
        var result = builder.Build(
            [
                new TurnoRaw(1, "VENCIDO-ANTERIOR", "C1", "Medico", scheduled, null, "N", null, null, null, false),
                new TurnoRaw(2, "VENCIDO-RECIENTE", "C1", "Medico", scheduled.AddMinutes(20), null, "N", null, null, null, false),
                new TurnoRaw(3, "PROXIMO", "C1", "Medico", scheduled.AddHours(1).AddMinutes(15), null, "N", null, null, null, false)
            ],
            new Dictionary<long, TurnoStatus>(),
            new DateTimeOffset(scheduled.AddMinutes(30), TimeSpan.Zero));

        Assert.Equal(["VENCIDO-RECIENTE", "VENCIDO-ANTERIOR", "PROXIMO"], result.Items.Select(item => item.PublicId));
    }

    [Fact]
    public void CurrentHourAppointmentsPrecedeTheNextHourAndEarlierHourBlocks()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 16, 9, 38, 0);
        var result = builder.Build(
            [
                new TurnoRaw(1, "OCHO-QUINCE", "C1", "Medico", at.AddHours(-1).AddMinutes(-23), null, "N", null, null, null, false),
                new TurnoRaw(2, "DIEZ", "C1", "Medico", at.AddMinutes(22), null, "N", null, null, null, false),
                new TurnoRaw(3, "NUEVE-QUINCE", "C1", "Medico", at.AddMinutes(-23), null, "N", null, null, null, false),
                new TurnoRaw(4, "NUEVE-TREINTA", "C1", "Medico", at.AddMinutes(-8), null, "N", null, null, null, false)
            ],
            new Dictionary<long, TurnoStatus>(),
            new DateTimeOffset(at, TimeSpan.Zero));

        Assert.Equal(["NUEVE-TREINTA", "NUEVE-QUINCE", "DIEZ", "OCHO-QUINCE"], result.Items.Select(item => item.PublicId));
    }

    [Fact]
    public void CalledPatientWithoutRegisteredArrivalIsRemovedFromThePublicUpcomingListsAfterTheMinute()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var scheduled = new DateTime(2026, 9, 16, 9, 0, 0);
        var raw = new TurnoRaw(
            1, "AUSENTE", "C1", "Medico", scheduled, null, "N", 0, null, null, false,
            HasMedicalConsultation: true,
            ConsultationStatus: "T",
            ConsultationId: 100);

        // La primera lectura es la línea base; el acto creado después inicia
        // el llamado y aún aparece durante sus 60 segundos reglamentarios.
        builder.Build([], new Dictionary<long, TurnoStatus>(), new DateTimeOffset(scheduled, TimeSpan.Zero));
        var duringCall = builder.Build([raw], new Dictionary<long, TurnoStatus>(), new DateTimeOffset(scheduled.AddSeconds(3), TimeSpan.Zero));
        var afterNoShow = builder.Build([raw], new Dictionary<long, TurnoStatus>(), new DateTimeOffset(scheduled.AddSeconds(63), TimeSpan.Zero));

        Assert.Single(duringCall.Items);
        Assert.Empty(afterNoShow.Items);
    }

    [Fact]
    public void NewOpenNumconCanReactivateAndCallAnAppointmentPreviouslyClosedInLolcli()
    {
        var builder = CreateBuilder(new BusinessRulesOptions { ClosedStatusCodes = ["S"], ZeroPrefacturaMeansAbsent = true });
        var at = new DateTime(2026, 9, 21, 11, 30, 0);
        var closed = new TurnoRaw(
            1, "REACTIVABLE", "C1", "Medico", at, at, "S", 0, null, null, false,
            HasMedicalConsultation: true,
            ConsultationStatus: "P",
            ConsultationId: 100);
        var reopenedByDoctor = closed with
        {
            // El médico abre nuevamente al paciente en LOLCLI. La cita puede
            // conservar S de forma temporal, pero el numcon nuevo T manda.
            ConsultationStatus = "T",
            ConsultationId = 101
        };

        var beforeReopen = builder.Build([closed], new Dictionary<long, TurnoStatus>(), new DateTimeOffset(at, TimeSpan.Zero));
        var afterReopen = builder.Build(
            [reopenedByDoctor],
            beforeReopen.States,
            new DateTimeOffset(at.AddSeconds(3), TimeSpan.Zero));

        Assert.Empty(beforeReopen.Items);
        var called = Assert.Single(afterReopen.Items);
        Assert.Equal("REACTIVABLE", called.PublicId);
        Assert.Equal("en-espera", called.Estado);
        Assert.True(called.IsActiveCall);
        Assert.True(called.ShouldAnnounce);
    }

    private static TurnosSnapshotBuilder CreateBuilder(BusinessRulesOptions rules)
    {
        var ruleOptions = Microsoft.Extensions.Options.Options.Create(rules);
        var status = new TurnoStatusPolicy(new PrefacturaPolicy(ruleOptions), ruleOptions);
        return new TurnosSnapshotBuilder(
            status,
            new PriorityPolicy(
                ruleOptions,
                Microsoft.Extensions.Options.Options.Create(new PriorityOptions())),
            new TurnosQueue(
                Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }),
                Microsoft.Extensions.Options.Options.Create(new SiteOptions { Code = 1, DisplayName = "Test", TimeZone = "UTC" }),
                Microsoft.Extensions.Options.Options.Create(new ScheduleOptions
                {
                    MorningStartHour = 7,
                    RecessStartHour = 12,
                    AfternoonStartHour = 14,
                    DayEndHour = 18
                })),
            Microsoft.Extensions.Options.Options.Create(new SiteOptions { Code = 1, DisplayName = "Test", TimeZone = "UTC" }));
    }
}
