using System.ComponentModel.DataAnnotations;

namespace VisorTurnos.Options;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public int Code { get; init; }

    [Required, MinLength(3)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    public string TimeZone { get; init; } = string.Empty;
}
