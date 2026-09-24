using DotNetEnv;
using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace VisorTurnos.Configuration;

/// <summary>
/// Carga un archivo .env en la configuracion usando el paquete DotNetEnv.
///
/// Precedencia general: variables de entorno reales > .env > appsettings*.json.
/// Las tres claves Site del .env son la excepcion: identifican la sede fija
/// de la carpeta IIS y prevalecen sobre variables de entorno heredadas.
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

        // Un valor Site__* global o heredado del servidor no debe convertir
        // las tres aplicaciones IIS en la misma sede. El .env fisico de cada
        // despliegue es la fuente autoritativa solo para estas tres claves.
        var siteSource = new EnvConfigurationSource(
            new[] { dotEnvPath },
            new LoadOptions(setEnvVars: false, clobberExistingVars: true));
        var siteFile = new ConfigurationBuilder().Add(siteSource).Build();
        var siteOverrides = new Dictionary<string, string?>();
        foreach (var key in new[] { "Site:Code", "Site:DisplayName", "Site:TimeZone" })
        {
            var value = siteFile[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                siteOverrides[key] = value;
            }
        }

        if (siteOverrides.Count > 0)
        {
            builder.AddInMemoryCollection(siteOverrides);
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
