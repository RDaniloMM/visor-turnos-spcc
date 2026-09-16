using System.ComponentModel.DataAnnotations;

namespace VisorTurnos.Options;

public sealed class DataSourceOptions
{
    public const string SectionName = "DataSource";

    [Required]
    public string OdbcDsn { get; init; } = "LOLCLI9000";
}
