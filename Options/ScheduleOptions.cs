using System.ComponentModel.DataAnnotations;

namespace VisorTurnos.Options;

public sealed class ScheduleOptions
{
    public const string SectionName = "Schedule";

    [Range(0, 23)] public int MorningStartHour { get; init; } = 7;
    [Range(1, 23)] public int RecessStartHour { get; init; } = 12;
    [Range(1, 23)] public int AfternoonStartHour { get; init; } = 14;
    [Range(1, 24)] public int DayEndHour { get; init; } = 18;
}
