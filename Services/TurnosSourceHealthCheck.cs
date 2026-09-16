using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace VisorTurnos.Services;

public sealed class TurnosSourceHealthCheck(TurnosSnapshotStore store) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = store.Get();
        var result = snapshot.Status switch
        {
            "live" => HealthCheckResult.Healthy("La fuente de turnos esta actualizada."),
            "stale" => HealthCheckResult.Degraded("Se conserva el ultimo snapshot valido."),
            _ => HealthCheckResult.Unhealthy("La fuente de turnos no tiene un snapshot valido.")
        };
        return Task.FromResult(result);
    }
}
