using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.UnitTests;

public sealed class CalledTurnRotationPolicyTests
{
    [Fact]
    public void PrefacturaActivaElSiguienteTurnoDelMismoConsultorio()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var start = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
        var items = new[]
        {
            Candidate(1, TurnoStatus.EnAtencion),
            Candidate(2, TurnoStatus.EnEspera),
            Candidate(3, TurnoStatus.EnEspera)
        };

        var initial = policy.Select(items, [], start);
        var advanced = policy.Select(
            [Candidate(1, TurnoStatus.EnAtencion), Candidate(2, TurnoStatus.EnAtencion), Candidate(3, TurnoStatus.EnEspera)],
            [],
            start.AddSeconds(3));

        Assert.Equal(2, initial.Single().StableId);
        Assert.True(initial.Single().ShouldAnnounce);
        Assert.Equal(3, advanced.Single().StableId);
        Assert.True(advanced.Single().ShouldAnnounce);
    }

    [Fact]
    public void StartsFirstScheduledTurnWithoutAnInitialDatabaseCall()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var start = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
        var items = new[] { Candidate(2, TurnoStatus.PendienteLlegada) };

        var selection = policy.Select(items, [], start);

        Assert.Equal(2, selection.Single().StableId);
        Assert.True(selection.Single().ShouldAnnounce);
    }

    [Fact]
    public void DoesNotCallAPatientBeforeTheirScheduledTime()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var start = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
        var attending = Candidate(1, TurnoStatus.EnAtencion);
        var futureWaiting = Candidate(2, TurnoStatus.EnEspera) with { ScheduledAt = start.AddMinutes(1) };

        var tooEarly = policy.Select([attending, futureWaiting], [], start);
        var due = policy.Select([attending, futureWaiting], [], start.AddMinutes(1));

        Assert.Empty(tooEarly);
        Assert.Equal(futureWaiting.StableId, due.Single().StableId);
        Assert.True(due.Single().ShouldAnnounce);
    }

    [Fact]
    public void DoesNotStartCallForAnEarlierScheduledMinute()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var now = new DateTimeOffset(2026, 9, 17, 7, 40, 15, TimeSpan.Zero);
        var attending = Candidate(1, TurnoStatus.EnAtencion);
        var previousMinute = Candidate(2, TurnoStatus.PendienteLlegada) with { ScheduledAt = now.AddMinutes(-10) };

        var selection = policy.Select([attending, previousMinute], [], now);

        Assert.Empty(selection);
    }

    [Fact]
    public void CallsScheduledPatientWithoutArrivalTimestampWhenConsultorioAdvances()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var now = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
        var selection = policy.Select(
            [Candidate(1, TurnoStatus.EnAtencion), Candidate(2, TurnoStatus.PendienteLlegada)],
            [],
            now);

        Assert.Equal(2, selection.Single().StableId);
        Assert.True(selection.Single().ShouldAnnounce);
    }

    [Fact]
    public void MovesPatientToEndUntilFourthUnansweredCallThenStopsCalling()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var start = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
        var completed = Candidate(1, TurnoStatus.EnAtencion);
        var waiting = Candidate(2, TurnoStatus.EnEspera);

        var first = policy.Select([completed, waiting], [], start);
        var firstRepeat = policy.Select([waiting], [], start.AddSeconds(31));
        var second = policy.Select([waiting], [], start.AddSeconds(61));
        var secondRepeat = policy.Select([waiting], [], start.AddSeconds(91));
        var third = policy.Select([waiting], [], start.AddSeconds(121));
        var thirdRepeat = policy.Select([waiting], [], start.AddSeconds(151));
        var fourth = policy.Select([waiting], [], start.AddSeconds(181));
        var fourthRepeat = policy.Select([waiting], [], start.AddSeconds(211));
        var exhausted = policy.Select([waiting], [], start.AddSeconds(241));

        Assert.All([first, second, third, fourth], selections => Assert.Equal(waiting.StableId, selections.Single().StableId));
        Assert.True(firstRepeat.Single().ShouldAnnounce);
        Assert.True(secondRepeat.Single().ShouldAnnounce);
        Assert.True(thirdRepeat.Single().ShouldAnnounce);
        Assert.True(fourthRepeat.Single().ShouldAnnounce);
        Assert.Empty(exhausted);
    }

    [Fact]
    public void AdvancesIndependentQueuesForEachConsultorio()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var now = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
        var selections = policy.Select(
        [
            Candidate(1, TurnoStatus.EnAtencion, "C1"),
            Candidate(2, TurnoStatus.EnEspera, "C1"),
            Candidate(3, TurnoStatus.EnAtencion, "C2"),
            Candidate(4, TurnoStatus.EnEspera, "C2")
        ], [], now);

        Assert.Equal([2L, 4L], selections.Select(selection => selection.StableId!.Value).Order());
        Assert.All(selections, selection => Assert.True(selection.ShouldAnnounce));
    }

    private static TurnoCandidate Candidate(long id, TurnoStatus status, string consultorio = "C1") => new(
        id,
        $"T{id}",
        consultorio,
        "Medico",
        status,
        100,
        false,
        false,
        new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero));
}
