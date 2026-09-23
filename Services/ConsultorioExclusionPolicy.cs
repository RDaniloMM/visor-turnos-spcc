using Microsoft.Extensions.Options;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

/// <summary>
/// Centraliza los consultorios que no participan en el visor ni en la
/// simulación. La comparación ignora mayúsculas y espacios repetidos.
/// </summary>
public sealed class ConsultorioExclusionPolicy(IOptions<QueueOptions> queueOptions)
{
    private readonly HashSet<string> _excludedConsultorios = queueOptions.Value.ExcludedConsultorios
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(Normalize)
        .ToHashSet(StringComparer.Ordinal);

    public bool IsExcluded(string? consultorio) =>
        !string.IsNullOrWhiteSpace(consultorio) &&
        _excludedConsultorios.Contains(Normalize(consultorio));

    private static string Normalize(string value) =>
        string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
}
