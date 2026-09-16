using Microsoft.Extensions.Options;
using VisorTurnos.Options;

namespace VisorTurnos.Data;

public static class TurnosRepositoryFactory
{
    public static ITurnosRepository Create(IServiceProvider services, IConfiguration configuration)
    {
        var mode = services.GetRequiredService<IOptions<DataSourceOptions>>().Value.Mode;
        return mode switch
        {
            TurnosDataSourceMode.Demo => ActivatorUtilities.CreateInstance<DemoTurnosRepository>(services),
            TurnosDataSourceMode.Odbc => ActivatorUtilities.CreateInstance<OdbcTurnosRepository>(
                services,
                configuration.GetConnectionString("LolcliOdbc") ?? string.Empty),
            _ => new DisabledTurnosRepository()
        };
    }
}
