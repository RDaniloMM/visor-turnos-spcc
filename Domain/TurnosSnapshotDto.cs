namespace VisorTurnos.Domain;

public sealed record TurnosSnapshotDto(
    long Version,
    DateTimeOffset GeneratedAt,
    string SiteDisplayName,
    string Status,
    IReadOnlyList<TurnoPublicoDto> Items);
