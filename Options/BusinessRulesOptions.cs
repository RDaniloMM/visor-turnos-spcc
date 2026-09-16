namespace VisorTurnos.Options;

public enum PublicIdentifierMode
{
    Unconfigured,
    Invnum,
    PatientName
}

public sealed class BusinessRulesOptions
{
    public const string SectionName = "BusinessRules";

    public string[] ClosedStatusCodes { get; init; } = [];
    public bool? ZeroPrefacturaMeansAbsent { get; init; }
    public PublicIdentifierMode PublicIdentifierMode { get; init; } = PublicIdentifierMode.Unconfigured;
    public string[] PreferentialPatientTypeCodes { get; init; } = [];
    public Dictionary<string, int> PriorityTierByCitedType { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
