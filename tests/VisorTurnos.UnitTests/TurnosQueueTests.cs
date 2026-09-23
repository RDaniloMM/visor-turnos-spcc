using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.UnitTests;

public sealed class TurnosQueueTests
{
    [Fact]
    public void RequiresANewNumconBeforeItCanCall()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var existing = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 101);

        var baseline = queue.SynchronizeAndSelect([existing], now);
        var unchanged = queue.SynchronizeAndSelect([existing], now.AddSeconds(3));

        Assert.Empty(baseline);
        Assert.Empty(unchanged);
    }

    [Fact]
    public void AnnouncesTwiceAtTwentySecondIntervalsAndEndsAtFortySeconds()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var turn = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 101);

        StartMonitoring(queue, [], now);
        var initial = queue.SynchronizeAndSelect([turn], now.AddSeconds(3));
        var beforeRepeat = queue.SynchronizeAndSelect([turn], now.AddSeconds(22));
        var repeated = queue.SynchronizeAndSelect([turn], now.AddSeconds(23));
        var beforeEnd = queue.SynchronizeAndSelect([turn], now.AddSeconds(42));
        var expired = queue.SynchronizeAndSelect([turn], now.AddSeconds(43));
        var entry = queue.GetDevelopmentSnapshot().Single();

        Assert.True(initial.Single().ShouldAnnounce);
        Assert.False(beforeRepeat.Single().ShouldAnnounce);
        Assert.True(repeated.Single().ShouldAnnounce);
        Assert.False(beforeEnd.Single().ShouldAnnounce);
        Assert.Empty(expired);
        Assert.Equal("llamado-finalizado", entry.QueueState);
        Assert.False(entry.IsEligibleForCall);
    }

    [Fact]
    public void EndingACallDoesNotClassifyThePatientAsAbsentOrHideTheAppointment()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var turn = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 101);

        StartMonitoring(queue, [], now);
        queue.SynchronizeAndSelect([turn], now.AddSeconds(3));
        queue.SynchronizeAndSelect([turn], now.AddSeconds(43));

        var entry = Assert.Single(queue.GetDevelopmentSnapshot());
        Assert.Equal(1, entry.AppointmentId);
        Assert.Equal("llamado-finalizado", entry.QueueState);
    }

    [Fact]
    public void AnOpenActAfterFortySecondsBlocksOnlyItsOwnArea()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var medicine = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 101, consultorio: "Medicina");
        var dentistry = Candidate(2, now, TurnoStatus.EnEspera, consultationId: 102, consultorio: "Odontología");

        StartMonitoring(queue, [], now);
        var initial = queue.SynchronizeAndSelect([medicine, dentistry], now.AddSeconds(3));
        var nextArea = queue.SynchronizeAndSelect([medicine, dentistry], now.AddSeconds(43));

        Assert.Equal(1, initial.Single().StableId);
        Assert.Equal(2, nextArea.Single().StableId);
    }

    [Fact]
    public void ANewNumconAfterAPriorClosedNonNoShowActDoesNotCallAutomatically()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        StartMonitoring(queue, [], now);

        var reopened = Candidate(
            1,
            now,
            TurnoStatus.EnEspera,
            consultationId: 102,
            hasPriorClosedActWithoutNoShow: true);
        var selection = queue.SynchronizeAndSelect([reopened], now.AddSeconds(3));
        var entry = Assert.Single(queue.GetDevelopmentSnapshot());

        Assert.Empty(selection);
        Assert.False(entry.IsEligibleForCall);
    }

    [Fact]
    public void NoShowDiagnosisDoesNotStartOrMaintainACall()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var eligible = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 101);
        var noShow = eligible with { HasNoShowDiagnosis = true };

        StartMonitoring(queue, [], now);
        var initialCall = queue.SynchronizeAndSelect([eligible], now.AddSeconds(3));
        var cancelled = queue.SynchronizeAndSelect([noShow], now.AddSeconds(6));
        var entry = Assert.Single(queue.GetDevelopmentSnapshot());

        Assert.Equal(1, initialCall.Single().StableId);
        Assert.Empty(cancelled);
        Assert.False(entry.IsEligibleForCall);
        Assert.Equal("proximo", entry.QueueState);
    }

    [Fact]
    public void LaterActWithoutNoShowDiagnosisCanCallTheSameAppointment()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var noShow = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 101, hasNoShowDiagnosis: true);
        var reopened = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 102);

        StartMonitoring(queue, [], now);
        Assert.Empty(queue.SynchronizeAndSelect([noShow], now.AddSeconds(3)));

        var call = queue.SynchronizeAndSelect([reopened], now.AddSeconds(6));

        Assert.Equal(1, call.Single().StableId);
    }

    [Fact]
    public void AClosedActReleasesTheAreaButNextPatientNeedsOwnNumcon()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var first = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 101);
        var second = Candidate(2, now.AddMinutes(15), TurnoStatus.PendienteLlegada);

        StartMonitoring(queue, [second], now);
        queue.SynchronizeAndSelect([first, second], now.AddSeconds(3));
        var afterClose = queue.SynchronizeAndSelect([Closed(1, now, 101), second], now.AddSeconds(6));
        var afterDoctorOpensNext = queue.SynchronizeAndSelect(
            [Closed(1, now, 101), Candidate(2, now.AddMinutes(15), TurnoStatus.EnEspera, consultationId: 102)],
            now.AddSeconds(9));

        Assert.Empty(afterClose);
        Assert.Equal(2, afterDoctorOpensNext.Single().StableId);
    }

    [Fact]
    public void EnabledMedicalExamPrecedesAnEnabledRegularTurn()
    {
        var queue = CreateQueue();
        var now = At(9, 0);
        var regular = Candidate(1, now, TurnoStatus.EnEspera, consultationId: 101, priorityTier: 100);
        var ema = Candidate(2, now.AddHours(2), TurnoStatus.EnEspera, consultationId: 102, priorityTier: 1);

        StartMonitoring(queue, [], now);
        var selection = queue.SynchronizeAndSelect([regular, ema], now.AddSeconds(3));

        Assert.Equal(2, selection.Single().StableId);
    }

    private static TurnosQueue CreateQueue() => new(
        Microsoft.Extensions.Options.Options.Create(new QueueOptions
        {
            RepeatCallAnnouncementSeconds = 20,
            CalledDisplaySeconds = 40
        }));

    private static void StartMonitoring(TurnosQueue queue, IReadOnlyList<TurnoCandidate> candidates, DateTimeOffset now) =>
        queue.SynchronizeAndSelect(candidates, now);

    private static TurnoCandidate Candidate(
        long id,
        DateTimeOffset scheduledAt,
        TurnoStatus status,
        long? consultationId = null,
        string consultorio = "C1",
        int priorityTier = 100,
        string? consultationStatus = "T",
        bool hasNoShowDiagnosis = false,
        bool hasPriorClosedActWithoutNoShow = false) => new(
        id,
        $"T{id}",
        consultorio,
        "Medico",
        status,
        priorityTier,
        false,
        priorityTier == 1,
        scheduledAt,
        null,
        scheduledAt,
        null,
        consultationId.HasValue,
        consultationId.HasValue ? consultationStatus : null,
        consultationId.HasValue ? scheduledAt : null,
        consultationId.HasValue ? scheduledAt : null,
        consultationId.HasValue ? scheduledAt : null,
        consultationId,
        consultationId.HasValue ? 1 : 0,
        false,
        hasNoShowDiagnosis,
        hasPriorClosedActWithoutNoShow);

    private static TurnoCandidate Closed(long id, DateTimeOffset scheduledAt, long consultationId) =>
        Candidate(id, scheduledAt, TurnoStatus.Cerrado, consultationId, consultationStatus: "P");

    private static DateTimeOffset At(int hour, int minute) =>
        new(2026, 9, 17, hour, minute, 0, TimeSpan.Zero);
}
