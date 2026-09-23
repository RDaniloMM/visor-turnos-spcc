using DotNetEnv;
using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace VisorTurnos.Configuration;

/// <summary>
/// Carga un archivo .env en la configuracion usando el paquete DotNetEnv.
///
/// Precedencia: variables de entorno reales > .env > appsettings*.json.
/// Se consigue insertando el source del .env justo antes del provider de
/// variables de entorno y usando LoadOptions que respeta los valores ya
/// presentes en el entorno (clobberExistingVars = false).
///
/// Las claves usan la convencion de variables de entorno de .NET:
/// "ConnectionStrings__LolcliOdbc" se convierte en la seccion
/// "ConnectionStrings:LolcliOdbc".
///
/// El archivo se busca en la carpeta de contenido de la aplicacion
/// (ContentRootPath), que en IIS es la carpeta fisica del sitio, y en la
/// carpeta base del proceso. Si no existe, se ignora sin error.
/// </summary>
public static class DotEnvLoader
{
    public static IConfigurationBuilder AddDotEnv(this IConfigurationBuilder builder, string? rootPath = null)
    {
        var dotEnvPath = FindDotEnv(rootPath ?? Directory.GetCurrentDirectory());
        if (dotEnvPath is null)
        {
            return builder;
        }

        var source = new EnvConfigurationSource(
            new[] { dotEnvPath },
            new LoadOptions(setEnvVars: false, clobberExistingVars: false));

        var sources = builder.Sources;
        var envIndex = sources
            .Select((item, index) => (item, index))
            .FirstOrDefault(item => item.item is EnvironmentVariablesConfigurationSource)
            .index;

        if (envIndex >= 0)
        {
            sources.Insert(envIndex, source);
        }
        else
        {
            builder.Add(source);
        }

        return builder;
    }

    internal static string? FindDotEnv(string rootPath)
    {
        var candidates = new[]
        {
            Path.Combine(rootPath, ".env"),
            Path.Combine(AppContext.BaseDirectory, ".env")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}