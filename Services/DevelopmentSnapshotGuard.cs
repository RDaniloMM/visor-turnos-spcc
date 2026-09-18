using Microsoft.Extensions.Options;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

/// <summary>
/// Evita que cualquier acción de simulación pueda ejecutarse fuera de la copia
/// local autorizada. No depende del nombre de la base de producción.
/// </summary>
public sealed class DevelopmentSnapshotGuard(
    IHostEnvironment environment,
    IOptions<DataSourceOptions> sourceOptions,
    IConfiguration configuration)
{
    public bool IsEnabled => environment.IsDevelopment() &&
        sourceOptions.Value.Mode == TurnosSourceMode.DevelopmentSnapshot &&
        sourceOptions.Value.EnableDevelopmentSnapshot &&
        IsLocalSnapshotConnection(configuration.GetConnectionString("DevelopmentSnapshotOdbc"));

    public static bool IsLocalSnapshotConnection(string? connectionString) =>
        !string.IsNullOrWhiteSpace(connectionString) &&
        connectionString.Contains("Server=(localdb)\\VisorTurnosDevelopment", StringComparison.OrdinalIgnoreCase) &&
        connectionString.Contains("Database=VisorTurnosDevelopment", StringComparison.OrdinalIgnoreCase);

    public string GetConnectionString()
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("El simulador solo se habilita en DevelopmentSnapshot explícito.");
        }

        return configuration.GetConnectionString("DevelopmentSnapshotOdbc")!;
    }
}
