using VisorTurnos.Domain;

namespace VisorTurnos.Services;

public sealed record TurnosBuildResult(
    IReadOnlyList<TurnoPublicoDto> Items,
    IReadOnlyDictionary<long, TurnoStatus> States);
