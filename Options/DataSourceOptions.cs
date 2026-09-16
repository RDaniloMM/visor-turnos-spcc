using System.ComponentModel.DataAnnotations;

namespace VisorTurnos.Options;

public enum TurnosDataSourceMode
{
    Disabled,
    Demo,
    Odbc
}

public sealed class DataSourceOptions
{
    public const string SectionName = "DataSource";

    [Required]
    public TurnosDataSourceMode Mode { get; init; } = TurnosDataSourceMode.Disabled;
}
