using VisorTurnos.Configuration;
using Microsoft.Extensions.Configuration;

namespace VisorTurnos.UnitTests;

public sealed class DotEnvLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _envPath;

    public DotEnvLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "visor-turnos-dotenv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _envPath = Path.Combine(_tempDir, ".env");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void MapsDoubleUnderscoreToSectionDelimiter()
    {
        File.WriteAllText(_envPath, "ConnectionStrings__LolcliOdbc=DSN=LOLCLI9000;Trusted_Connection=Yes;");

        var config = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddDotEnv(_tempDir)
            .Build();

        Assert.Equal("DSN=LOLCLI9000;Trusted_Connection=Yes;", config["ConnectionStrings:LolcliOdbc"]);
    }

    [Fact]
    public void ExistingEnvironmentVariableWinsOverDotEnvFile()
    {
        var key = "VISORTURNOS_TEST_PRECEDENCE_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key, "from-env");
        try
        {
            File.WriteAllText(_envPath, $"{key}=from-file");

            var config = new ConfigurationBuilder()
                .SetBasePath(_tempDir)
                .AddDotEnv(_tempDir)
                .Build();

            Assert.Equal("from-env", config[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public void SiteIdentityFromDotEnvWinsOverInheritedEnvironment()
    {
        const string codeKey = "Site__Code";
        const string nameKey = "Site__DisplayName";
        const string zoneKey = "Site__TimeZone";
        var oldCode = Environment.GetEnvironmentVariable(codeKey);
        var oldName = Environment.GetEnvironmentVariable(nameKey);
        var oldZone = Environment.GetEnvironmentVariable(zoneKey);

        try
        {
            Environment.SetEnvironmentVariable(codeKey, "1");
            Environment.SetEnvironmentVariable(nameKey, "Hospital SPCC Cuajone");
            Environment.SetEnvironmentVariable(zoneKey, "Wrong zone");
            File.WriteAllText(_envPath,
                "Site__Code=3\nSite__DisplayName=Hospital SPCC Toquepala\nSite__TimeZone=SA Pacific Standard Time\n");

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddDotEnv(_tempDir)
                .Build();

            Assert.Equal("3", config["Site:Code"]);
            Assert.Equal("Hospital SPCC Toquepala", config["Site:DisplayName"]);
            Assert.Equal("SA Pacific Standard Time", config["Site:TimeZone"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(codeKey, oldCode);
            Environment.SetEnvironmentVariable(nameKey, oldName);
            Environment.SetEnvironmentVariable(zoneKey, oldZone);
        }
    }

    [Fact]
    public void MissingDotEnvFileIsIgnored()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddDotEnv(_tempDir)
            .Build();

        Assert.Equal("fallback", config["Site:DisplayName"] ?? "fallback");
    }
}
