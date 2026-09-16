using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.UnitTests;

public sealed class CalledTurnRotationPolicyTests
{
    [Fact]
    public void WaitsForConfirmedClosureBeforeCallingNextTurnInSameArea()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var start = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
        var items = new[]
        {
            Candidate(1, TurnoStatus.EnAtencion),
            Candidate(2, TurnoStatus.EnEspera)
        };

        var initial = policy.Select(items, [], start);
        var afterLimit = policy.Select(items, [], start.AddSeconds(60));
        var advanced = policy.Select(
            [Candidate(2, TurnoStatus.EnEspera)],
            [new ClosedTurn(1, Candidate(1, TurnoStatus.EnAtencion).AreaKey)],
            start.AddSeconds(61));

        Assert.Equal(1, initial.StableId);
        Assert.False(initial.ShouldAnnounce);
        Assert.Equal(1, afterLimit.StableId);
        Assert.Equal(2, advanced.StableId);
        Assert.True(advanced.ShouldAnnounce);
    }

    [Fact]
    public void DoesNotStartAutomaticCallsWithoutAnInitialDatabaseCall()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var items = new[] { Candidate(2, TurnoStatus.EnEspera) };

        var selection = policy.Select(items, [], DateTimeOffset.UtcNow);

        Assert.Null(selection.StableId);
    }

    [Fact]
    public void MovesPatientToEndUntilFourthUnansweredCallThenStopsCalling()
    {
        var policy = new CalledTurnRotationPolicy(
            Microsoft.Extensions.Options.Options.Create(new QueueOptions { CalledDisplaySeconds = 60 }));
        var start = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);
        var completed = Candidate(1, TurnoStatus.EnAtencion);
        var waiting = Candidate(2, TurnoStatus.EnEspera);

        policy.Select([completed], [], start);
        var first = policy.Select([waiting], [new ClosedTurn(completed.StableId, completed.AreaKey)], start.AddSeconds(1));
        var firstRepeat = policy.Select([waiting], [], start.AddSeconds(31));
        var second = policy.Select([waiting], [], start.AddSeconds(61));
        var secondRepeat = policy.Select([waiting], [], start.AddSeconds(91));
        var third = policy.Select([waiting], [], start.AddSeconds(121));
        var thirdRepeat = policy.Select([waiting], [], start.AddSeconds(151));
        var fourth = policy.Select([waiting], [], start.AddSeconds(181));
        var fourthRepeat = policy.Select([waiting], [], start.AddSeconds(211));
        var exhausted = policy.Select([waiting], [], start.AddSeconds(241));

        Assert.All([first, second, third, fourth], selection => Assert.Equal(waiting.StableId, selection.StableId));
        Assert.True(firstRepeat.ShouldAnnounce);
        Assert.True(secondRepeat.ShouldAnnounce);
        Assert.True(thirdRepeat.ShouldAnnounce);
        Assert.True(fourthRepeat.ShouldAnnounce);
        Assert.Null(exhausted.StableId);
    }

    private static TurnoCandidate Candidate(long id, TurnoStatus status) => new(
        id,
        $"T{id}",
        "C1",
        "Medico",
        status,
        100,
        false,
        false,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch);
}
