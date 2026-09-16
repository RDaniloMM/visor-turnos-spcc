using System.ComponentModel.DataAnnotations;

namespace VisorTurnos.Options;

public sealed class PriorityOptions
{
    public const string SectionName = "PriorityRules";

    [Required, MinLength(1), MaxLength(20)]
    public string MedicalExamObservationCode { get; init; } = "EMA";

    [Range(1, 99)]
    public int MedicalExamTier { get; init; } = 1;

    [Range(2, 1000)]
    public int DefaultTier { get; init; } = 100;
}
