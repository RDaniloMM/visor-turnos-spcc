using System.ComponentModel.DataAnnotations;

namespace VisorTurnos.Options;

public enum TurnosSourceMode
{
    LolcliOdbc,
    DevelopmentSnapshot
}

public sealed class DataSourceOptions
{
    public const string SectionName = "DataSource";

    [Required]
    public string OdbcDsn { get; init; } = "LOLCLI9000";
    public TurnosSourceMode Mode { get; init; } = TurnosSourceMode.LolcliOdbc;
    public bool EnableDevelopmentSnapshot { get; init; }
}
