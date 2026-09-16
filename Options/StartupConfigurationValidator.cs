using Microsoft.Extensions.Options;

namespace VisorTurnos.Options;

public sealed class StartupConfigurationValidator(
    IOptions<SiteOptions> siteOptions,
    IOptions<DataSourceOptions> dataSourceOptions,
    IOptions<BusinessRulesOptions> businessRulesOptions,
    IConfiguration configuration)
{
    public void Validate()
    {
        var site = siteOptions.Value;
        var source = dataSourceOptions.Value;
        var rules = businessRulesOptions.Value;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(site.TimeZone);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new OptionsValidationException(
                SiteOptions.SectionName,
                typeof(SiteOptions),
                [$"Site:TimeZone no existe en este servidor: {exception.Message}"]);
        }

        if (site.Code <= 0)
        {
            throw Invalid("Site:Code debe ser un codigo siscod validado mayor que cero.");
        }

        var failures = new List<string>();
        if (rules.ClosedStatusCodes.Length == 0)
        {
            failures.Add("BusinessRules:ClosedStatusCodes debe validarse antes de activar ODBC.");
        }

        if (!rules.ZeroPrefacturaMeansAbsent.HasValue)
        {
            failures.Add("BusinessRules:ZeroPrefacturaMeansAbsent debe validarse antes de activar ODBC.");
        }

        if (rules.PublicIdentifierMode == PublicIdentifierMode.Unconfigured)
        {
            failures.Add("BusinessRules:PublicIdentifierMode debe ser una fuente aprobada antes de activar ODBC.");
        }

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("LolcliOdbc")))
        {
            failures.Add("ConnectionStrings:LolcliOdbc es obligatorio en modo ODBC.");
        }

        if (!string.Equals(source.OdbcDsn, "LOLCLI9000", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("DataSource:OdbcDsn debe ser LOLCLI9000.");
        }

        if (failures.Count > 0)
        {
            throw new OptionsValidationException(DataSourceOptions.SectionName, typeof(DataSourceOptions), failures);
        }
    }

    private static OptionsValidationException Invalid(string message) =>
        new(DataSourceOptions.SectionName, typeof(DataSourceOptions), [message]);
}
