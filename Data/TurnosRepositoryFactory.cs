using Microsoft.Extensions.Options;
using VisorTurnos.Options;

namespace VisorTurnos.Data;

public static class TurnosRepositoryFactory
{
    public static ITurnosRepository Create(IServiceProvider services, IConfiguration configuration)
    {
        var source = services.GetRequiredService<IOptions<DataSourceOptions>>().Value;
        var connectionString = configuration.GetConnectionString("LolcliOdbc") ?? string.Empty;
        if (!connectionString.Contains($"DSN={source.OdbcDsn}", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La conexion configurada debe usar exclusivamente el DSN ODBC autorizado.");
        }

        return ActivatorUtilities.CreateInstance<OdbcTurnosRepository>(services, connectionString);
    }
}
