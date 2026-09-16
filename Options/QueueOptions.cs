using System.ComponentModel.DataAnnotations;

namespace VisorTurnos.Options;

public sealed class QueueOptions
{
    public const string SectionName = "Queue";

    [Range(1, 300)] public int PollingSeconds { get; init; } = 3;
    [Range(1, 60)] public int CommandTimeoutSeconds { get; init; } = 5;
    [Range(5, 3600)] public int StaleAfterSeconds { get; init; } = 15;
    [Range(1, 500)] public int MaxQueryRows { get; init; } = 100;
    [Range(1, 12)] public int MaxVisibleRows { get; init; } = 8;
    [Range(3, 60)] public int AreaRotationSeconds { get; init; } = 8;
    [Range(0, 240)] public int? EarlyArrivalMinutes { get; init; }
    [Range(0, 240)] public int? LateToleranceMinutes { get; init; }
    [Range(1, 299)] public int RepeatCallAnnouncementSeconds { get; init; } = 30;
    [Range(1, 300)] public int CalledDisplaySeconds { get; init; } = 60;
    [Range(0, 300)] public int ClosedRetentionSeconds { get; init; }
}
