using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using VisorTurnos.Data;
using VisorTurnos.Domain;
using VisorTurnos.Hubs;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed class TurnosPollingWorker(
    ITurnosRepository repository,
    TurnosSnapshotStore store,
    TurnosSnapshotBuilder snapshotBuilder,
    TurnosChangeDetector changeDetector,
    IHubContext<TurnosHub> hubContext,
    IOptions<SiteOptions> siteOptions,
    IOptions<QueueOptions> queueOptions,
    TimeProvider timeProvider,
    ILogger<TurnosPollingWorker> logger) : BackgroundService
{
    private static readonly int[] ErrorBackoffSeconds = [3, 5, 10, 20, 30];
    private IReadOnlyDictionary<long, TurnoStatus> _previousStates = new Dictionary<long, TurnoStatus>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consecutiveFailures = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var now = timeProvider.GetUtcNow();
                var siteZone = TimeZoneInfo.FindSystemTimeZoneById(siteOptions.Value.TimeZone);
                var localNow = TimeZoneInfo.ConvertTime(now, siteZone);
                var dayStart = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
                var rawItems = await repository.GetForDayAsync(
                    siteOptions.Value.Code,
                    dayStart,
                    dayStart.AddDays(1),
                    queueOptions.Value.MaxQueryRows,
                    stoppingToken);

                var buildResult = snapshotBuilder.Build(rawItems, _previousStates, localNow);
                var current = store.Get();
                var candidate = new TurnosSnapshotDto(
                    current.Version + 1,
                    now,
                    siteOptions.Value.DisplayName,
                    "live",
                    buildResult.Items);

                if (changeDetector.HasVisibleChange(current, candidate))
                {
                    store.Set(candidate);
                    await hubContext.Clients.All.SendAsync("TurnosActualizados", candidate, stoppingToken);
                    logger.LogInformation(
                        "Snapshot publicado; {RowCount} filas leidas; version {Version}; consulta {ElapsedMs} ms.",
                        rawItems.Count,
                        candidate.Version,
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    store.RefreshSuccessful(now);
                    logger.LogDebug(
                        "Sondeo sin cambios; {RowCount} filas leidas; consulta {ElapsedMs} ms.",
                        rawItems.Count,
                        stopwatch.ElapsedMilliseconds);
                }

                _previousStates = buildResult.States;
                consecutiveFailures = 0;
                await Task.Delay(TimeSpan.FromSeconds(queueOptions.Value.PollingSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                consecutiveFailures++;
                logger.LogError(
                    exception,
                    "Fallo de sondeo despues de {ElapsedMs} ms. Intento consecutivo {FailureCount}.",
                    stopwatch.ElapsedMilliseconds,
                    consecutiveFailures);
                await PublishFailureStatusAsync(stoppingToken);
                var backoffIndex = Math.Min(consecutiveFailures - 1, ErrorBackoffSeconds.Length - 1);
                await Task.Delay(TimeSpan.FromSeconds(ErrorBackoffSeconds[backoffIndex]), stoppingToken);
            }
        }
    }

    private async Task PublishFailureStatusAsync(CancellationToken cancellationToken)
    {
        var current = store.Get();
        var status = current.Items.Count > 0 ? "stale" : "unavailable";
        if (string.Equals(current.Status, status, StringComparison.Ordinal))
        {
            return;
        }

        var failed = current with { Version = current.Version + 1, Status = status };
        store.Set(failed);
        await hubContext.Clients.All.SendAsync("TurnosActualizados", failed, cancellationToken);
    }
}
