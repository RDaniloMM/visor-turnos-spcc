using Microsoft.Extensions.Options;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.Data;

public static class TurnosRepositoryFactory
{
    public static ITurnosRepository Create(IServiceProvider services, IConfiguration configuration)
    {
        var source = services.GetRequiredService<IOptions<DataSourceOptions>>().Value;
        var environment = services.GetRequiredService<IHostEnvironment>();
        if (source.Mode == TurnosSourceMode.DevelopmentSnapshot)
        {
            var snapshotGuard = services.GetRequiredService<DevelopmentSnapshotGuard>();
            if (!environment.IsDevelopment() || !snapshotGuard.IsEnabled)
            {
                throw new InvalidOperationException("El snapshot local solo puede habilitarse explícitamente en Development.");
            }

            return ActivatorUtilities.CreateInstance<OdbcTurnosRepository>(services, snapshotGuard.GetConnectionString());
        }

        var connectionString = configuration.GetConnectionString("LolcliOdbc") ?? string.Empty;
        if (!connectionString.Contains($"DSN={source.OdbcDsn}", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La conexion configurada debe usar exclusivamente el DSN ODBC autorizado.");
        }

        return ActivatorUtilities.CreateInstance<OdbcTurnosRepository>(services, connectionString);
    }
}
