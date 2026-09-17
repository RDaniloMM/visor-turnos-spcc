using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed record ActiveCallSelection(long? StableId, bool ShouldAnnounce)
{
    public static ActiveCallSelection None { get; } = new(null, false);
}

/// <summary>
/// Coordina una cola independiente por consultorio y médico. Un paciente llamado
/// sin prefactura se reubica al final de la cola de su propia área; nunca se
/// escribe en LOLCLI ni se elimina una cita desde el visor.
/// </summary>
public sealed class CalledTurnRotationPolicy(IOptions<QueueOptions> options)
{
    private readonly Dictionary<long, int> _attemptsByTurn = [];
    private readonly Dictionary<string, ActiveCallState> _activeByArea = [];

    public IReadOnlyList<ActiveCallSelection> Select(
        IReadOnlyList<TurnoCandidate> orderedCandidates,
        IReadOnlyList<ClosedTurn> closedTurns,
        DateTimeOffset now)
    {
        var selections = new List<ActiveCallSelection>();
        foreach (var area in orderedCandidates.GroupBy(item => item.AreaKey))
        {
            var candidates = area.ToArray();
            if (_activeByArea.TryGetValue(area.Key, out var activeState))
            {
                var active = candidates.FirstOrDefault(item => item.StableId == activeState.StableId);
                if (active is null)
                {
                    var wasClosed = closedTurns.Any(item => item.StableId == activeState.StableId);
                    _activeByArea.Remove(area.Key);
                    _attemptsByTurn.Remove(activeState.StableId);
                    if (wasClosed) AddIfPresent(selections, StartNextInArea(candidates, area.Key, now));
                    continue;
                }

                // Una prefactura válida libera solo la cola de este consultorio.
                if (active.Status == TurnoStatus.EnAtencion)
                {
                    _activeByArea.Remove(area.Key);
                    _attemptsByTurn.Remove(active.StableId);
                    AddIfPresent(selections, StartNextInArea(candidates, area.Key, now));
                    continue;
                }

                var elapsed = now - activeState.Since;
                if (!activeState.RepeatAnnouncementSent && elapsed >= TimeSpan.FromSeconds(options.Value.RepeatCallAnnouncementSeconds))
                {
                    _activeByArea[area.Key] = activeState with { RepeatAnnouncementSent = true };
                    selections.Add(new ActiveCallSelection(active.StableId, true));
                    continue;
                }

                if (elapsed < TimeSpan.FromSeconds(options.Value.CalledDisplaySeconds))
                {
                    selections.Add(new ActiveCallSelection(active.StableId, false));
                    continue;
                }

                AddIfPresent(selections, StartNextInArea(candidates, area.Key, now));
                continue;
            }

            // Tanto una prefactura detectada como el inicio de la jornada liberan
            // la cola de este consultorio. StartNextInArea decide si ya existe una
            // cita cuya hora programada se cumplió.
            AddIfPresent(selections, StartNextInArea(candidates, area.Key, now));
        }

        return selections;
    }

    private ActiveCallSelection StartNextInArea(
        IReadOnlyList<TurnoCandidate> orderedCandidates,
        string areaKey,
        DateTimeOffset now)
    {
        var next = orderedCandidates
            .Where(item => item.AreaKey == areaKey)
            .Where(item => item.Status is TurnoStatus.EnEspera or TurnoStatus.PendienteLlegada)
            .Where(item => IsScheduledForCurrentMinute(item.ScheduledAt, now) || _attemptsByTurn.ContainsKey(item.StableId))
            .Where(item => !_attemptsByTurn.TryGetValue(item.StableId, out var attempts) || attempts < 4)
            .OrderBy(item => _attemptsByTurn.ContainsKey(item.StableId) ? 1 : 0)
            .ThenBy(item => item.PriorityTier)
            .ThenByDescending(item => item.IsPreferential)
            .ThenBy(item => item.EligibilityTime)
            .ThenBy(item => item.ArrivedAt)
            .ThenBy(item => item.ScheduledAt)
            .ThenBy(item => item.StableId)
            .FirstOrDefault();

        if (next is null)
        {
            return ActiveCallSelection.None;
        }

        Start(next, now, countAsAttempt: true);
        return new ActiveCallSelection(next.StableId, true);
    }

    private void Start(TurnoCandidate candidate, DateTimeOffset now, bool countAsAttempt)
    {
        _activeByArea[candidate.AreaKey] = new ActiveCallState(candidate.StableId, now, false);
        if (countAsAttempt)
        {
            _attemptsByTurn[candidate.StableId] = _attemptsByTurn.GetValueOrDefault(candidate.StableId) + 1;
        }
    }

    private static void AddIfPresent(List<ActiveCallSelection> selections, ActiveCallSelection selection)
    {
        if (selection.StableId.HasValue) selections.Add(selection);
    }

    private sealed record ActiveCallState(long StableId, DateTimeOffset Since, bool RepeatAnnouncementSent);

    private static DateTimeOffset StartOfMinute(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Offset);

    private static bool IsScheduledForCurrentMinute(DateTimeOffset scheduledAt, DateTimeOffset now)
    {
        var minuteStart = StartOfMinute(now);
        return scheduledAt >= minuteStart && scheduledAt < minuteStart.AddMinutes(1);
    }
}
