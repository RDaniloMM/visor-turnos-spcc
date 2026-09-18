using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.UnitTests;

public sealed class TurnosQueueTests
{
    [Fact]
    public void CallsTheFirstEnabledPatientWithoutBeingBlockedByAnUnopenedAppointment()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var unopened = Candidate(1, now, TurnoStatus.PendienteLlegada);
        var selectedByDoctor = Candidate(2, now.AddMinutes(15), TurnoStatus.EnEspera, 42, 102);

        var selection = queue.SynchronizeAndSelect([unopened, selectedByDoctor], now);

        Assert.Equal(2, selection.Single().StableId);
        Assert.True(selection.Single().ShouldAnnounce);
    }

    [Fact]
    public void RequiresANumconInTInAdditionToThePrefactura()
    {
        var queue = CreateQueue();
        var now = At(9, 0);

        var selection = queue.SynchronizeAndSelect(
            [Candidate(1, now, TurnoStatus.Desconocido, prefactura: 42)],
            now);

        Assert.Empty(selection);
    }

    [Fact]
    public void RepeatsOnceAndNeverReusesTheSameNumconAfterTheMinute()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var turn = Candidate(1, now, TurnoStatus.EnEspera, 41, 101);

        var initial = queue.SynchronizeAndSelect([turn], now);
        var repeated = queue.SynchronizeAndSelect([turn], now.AddSeconds(30));
        var expired = queue.SynchronizeAndSelect([turn], now.AddSeconds(60));
        var laterPoll = queue.SynchronizeAndSelect([turn], now.AddSeconds(120));
        var entry = queue.GetDevelopmentSnapshot().Single();

        Assert.True(initial.Single().ShouldAnnounce);
        Assert.True(repeated.Single().ShouldAnnounce);
        Assert.Empty(expired);
        Assert.Empty(laterPoll);
        Assert.Equal(1, entry.CallAttempts);
        Assert.True(entry.IsAwaitingClose);
        Assert.False(entry.IsEligibleForCall);
    }

    [Fact]
    public void AnOpenActAfterItsMinuteBlocksOnlyItsOwnArea()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var medicine = Candidate(1, now, TurnoStatus.EnEspera, 41, 101, "Medicina", "Medico A");
        var dentistry = Candidate(2, now, TurnoStatus.EnEspera, 42, 102, "Odontología", "Medico B");

        var initial = queue.SynchronizeAndSelect([medicine, dentistry], now);
        var nextArea = queue.SynchronizeAndSelect([medicine, dentistry], now.AddSeconds(60));

        Assert.Equal(1, initial.Single().StableId);
        Assert.Equal(2, nextArea.Single().StableId);
    }

    [Fact]
    public void PSReleasesTheAreaButTheNextPatientStillNeedsTheirOwnT()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var first = Candidate(1, now, TurnoStatus.EnEspera, 41, 101);
        var secondWithoutAct = Candidate(2, now.AddMinutes(15), TurnoStatus.PendienteLlegada);

        queue.SynchronizeAndSelect([first, secondWithoutAct], now);
        var afterClose = queue.SynchronizeAndSelect(
            [Closed(1, now, 41, 101), secondWithoutAct],
            now.AddSeconds(3));
        var afterDoctorOpensNext = queue.SynchronizeAndSelect(
            [Closed(1, now, 41, 101), Candidate(2, now.AddMinutes(15), TurnoStatus.EnEspera, 42, 102)],
            now.AddSeconds(6));

        Assert.Empty(afterClose);
        Assert.Equal(2, afterDoctorOpensNext.Single().StableId);
    }

    [Fact]
    public void ANewNumconReactivatesTheSameAppointmentAndCountsANewAttempt()
    {
        var queue = CreateQueue();
        var now = At(9, 0);

        queue.SynchronizeAndSelect([Candidate(1, now, TurnoStatus.EnEspera, 41, 101)], now);
        var closed = queue.SynchronizeAndSelect([Closed(1, now, 41, 101)], now.AddSeconds(3));
        var reopened = queue.SynchronizeAndSelect(
            [Candidate(1, now, TurnoStatus.EnEspera, 41, 102)],
            now.AddSeconds(6));
        var entry = queue.GetDevelopmentSnapshot().Single();

        Assert.Empty(closed);
        Assert.Equal(1, reopened.Single().StableId);
        Assert.True(reopened.Single().ShouldAnnounce);
        Assert.Equal(2, entry.CallAttempts);
        Assert.Equal(102, entry.ConsultationId);
    }

    [Fact]
    public void NeverCallsAFifthDistinctNumconForTheSameAppointment()
    {
        var queue = CreateQueue();
        var now = At(9, 0);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var consultationId = 100 + attempt;
            var openedAt = now.AddMinutes(attempt - 1);
            var selection = queue.SynchronizeAndSelect(
                [Candidate(1, now, TurnoStatus.EnEspera, 41, consultationId)],
                openedAt);
            Assert.Equal(1, selection.Single().StableId);

            queue.SynchronizeAndSelect(
                [Closed(1, now, 41, consultationId)],
                openedAt.AddSeconds(3));
        }

        var fifth = queue.SynchronizeAndSelect(
            [Candidate(1, now, TurnoStatus.EnEspera, 41, 105)],
            now.AddMinutes(5));
        var entry = queue.GetDevelopmentSnapshot().Single();

        Assert.Empty(fifth);
        Assert.Equal(4, entry.CallAttempts);
        Assert.True(entry.HasReachedMaxAttempts);
    }

    [Fact]
    public void ReconstructsTheFourPreviousAttemptsFromLolcliAfterARestart()
    {
        var queue = CreateQueue();
        var now = At(9, 0);

        var fifth = queue.SynchronizeAndSelect(
            [Candidate(1, now, TurnoStatus.EnEspera, 41, 105, attemptCount: 5)],
            now);
        var entry = queue.GetDevelopmentSnapshot().Single();

        Assert.Empty(fifth);
        Assert.Equal(4, entry.CallAttempts);
        Assert.True(entry.HasReachedMaxAttempts);
    }

    [Fact]
    public void AReattemptAfterTheSessionCutoffIsNotCalled()
    {
        var queue = CreateQueue();
        var firstAt = At(17, 28);

        queue.SynchronizeAndSelect([Candidate(1, firstAt, TurnoStatus.EnEspera, 41, 101)], firstAt);
        queue.SynchronizeAndSelect([Closed(1, firstAt, 41, 101)], firstAt.AddSeconds(3));
        var lateReopening = queue.SynchronizeAndSelect(
            [Candidate(1, firstAt, TurnoStatus.EnEspera, 41, 102)],
            At(17, 31));

        Assert.Empty(lateReopening);
    }

    [Fact]
    public void EnabledMedicalExamPrecedesAnEnabledRegularTurn()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var regular = Candidate(1, now, TurnoStatus.EnEspera, 41, 101, "C1", "Medico A", 100);
        var ema = Candidate(2, now.AddHours(2), TurnoStatus.EnEspera, 42, 102, "C2", "Medico B", 1);

        var selection = queue.SynchronizeAndSelect([regular, ema], now);

        Assert.Equal(2, selection.Single().StableId);
    }

    [Fact]
    public void SerializesSimultaneousActsAcrossAreas()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var medicine = Candidate(1, now, TurnoStatus.EnEspera, 41, 101, "Medicina", "Medico A");
        var dentistry = Candidate(2, now, TurnoStatus.EnEspera, 42, 102, "Odontología", "Medico B");

        var initial = queue.SynchronizeAndSelect([medicine, dentistry], now);
        var continued = queue.SynchronizeAndSelect([medicine, dentistry], now.AddSeconds(3));

        Assert.Single(initial);
        Assert.Equal(initial.Single().StableId, continued.Single().StableId);
        Assert.False(continued.Single().ShouldAnnounce);
        Assert.Single(queue.GetDevelopmentSnapshot(), entry => entry.QueueState == "llamando");
    }

    private static TurnosQueue CreateQueue() => new(
        Microsoft.Extensions.Options.Options.Create(new QueueOptions
        {
            RepeatCallAnnouncementSeconds = 30,
            CalledDisplaySeconds = 60,
            MaxCallAttempts = 4,
            AfternoonRequeueEndHour = 17,
            AfternoonRequeueEndMinute = 30
        }),
        Microsoft.Extensions.Options.Options.Create(new SiteOptions { Code = 1, DisplayName = "Prueba", TimeZone = "UTC" }),
        Microsoft.Extensions.Options.Options.Create(new ScheduleOptions
        {
            MorningStartHour = 7,
            RecessStartHour = 12,
            AfternoonStartHour = 14,
            DayEndHour = 18
        }));

    private static TurnoCandidate Candidate(
        long id,
        DateTimeOffset scheduledAt,
        TurnoStatus status,
        int? prefactura = null,
        long? consultationId = null,
        string consultorio = "C1",
        string medico = "Medico",
        int priorityTier = 100,
        string? consultationStatus = "T",
        int attemptCount = 1) => new(
        id,
        $"T{id}",
        consultorio,
        medico,
        status,
        priorityTier,
        false,
        priorityTier == 1,
        scheduledAt,
        null,
        scheduledAt,
        prefactura,
        consultationId.HasValue,
        consultationId.HasValue ? consultationStatus : null,
        consultationId.HasValue ? scheduledAt : null,
        consultationId.HasValue ? scheduledAt : null,
        consultationId.HasValue ? scheduledAt : null,
        consultationId,
        consultationId.HasValue ? attemptCount : 0);

    private static TurnoCandidate Closed(long id, DateTimeOffset scheduledAt, int prefactura, long consultationId) =>
        Candidate(id, scheduledAt, TurnoStatus.Cerrado, prefactura, consultationId, consultationStatus: "P");

    private static DateTimeOffset At(int hour, int minute) =>
        new(2026, 9, 17, hour, minute, 0, TimeSpan.Zero);
}
