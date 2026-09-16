using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed record ActiveCallSelection(long? StableId, bool ShouldAnnounce)
{
    public static ActiveCallSelection None { get; } = new(null, false);
}

/// <summary>
/// Coordina un único llamado público. Un paciente llamado sin prefactura se
/// reubica al final de la cola de su propio consultorio; nunca se escribe en
/// LOLCLI ni se elimina una cita desde el visor.
/// </summary>
public sealed class CalledTurnRotationPolicy(IOptions<QueueOptions> options)
{
    private readonly Dictionary<long, int> _attemptsByTurn = [];
    private long? _activeId;
    private string? _activeArea;
    private DateTimeOffset _activeSince;
    private bool _repeatAnnouncementSent;

    public ActiveCallSelection Select(
        IReadOnlyList<TurnoCandidate> orderedCandidates,
        IReadOnlyList<ClosedTurn> closedTurns,
        DateTimeOffset now)
    {
        var active = _activeId.HasValue
            ? orderedCandidates.FirstOrDefault(item => item.StableId == _activeId.Value)
            : null;

        if (active is not null)
        {
            // Una prefactura válida significa que el paciente está siendo atendido:
            // no se rota por tiempo; se espera el cierre confirmado en LOLCLI.
            if (active.Status == TurnoStatus.EnAtencion)
            {
                return new ActiveCallSelection(active.StableId, false);
            }

            var elapsed = now - _activeSince;
            if (!_repeatAnnouncementSent && elapsed >= TimeSpan.FromSeconds(options.Value.RepeatCallAnnouncementSeconds))
            {
                _repeatAnnouncementSent = true;
                return new ActiveCallSelection(active.StableId, true);
            }

            if (elapsed < TimeSpan.FromSeconds(options.Value.CalledDisplaySeconds))
            {
                return new ActiveCallSelection(active.StableId, false);
            }

            return StartNextInArea(orderedCandidates, active.AreaKey, now, active.StableId);
        }

        if (_activeId.HasValue)
        {
            var completed = closedTurns.Any(item => item.StableId == _activeId.Value);
            var areaKey = _activeArea;
            _attemptsByTurn.Remove(_activeId.Value);
            _activeId = null;
            _activeArea = null;

            return completed && !string.IsNullOrWhiteSpace(areaKey)
                ? StartNextInArea(orderedCandidates, areaKey, now, null)
                : ActiveCallSelection.None;
        }

        var databaseCall = orderedCandidates.FirstOrDefault(item => item.Status == TurnoStatus.EnAtencion);
        if (databaseCall is null)
        {
            return ActiveCallSelection.None;
        }

        Start(databaseCall, now, countAsAttempt: false);
        return new ActiveCallSelection(databaseCall.StableId, false);
    }

    private ActiveCallSelection StartNextInArea(
        IReadOnlyList<TurnoCandidate> orderedCandidates,
        string areaKey,
        DateTimeOffset now,
        long? timedOutId)
    {
        var next = orderedCandidates
            .Where(item => item.AreaKey == areaKey && item.Status != TurnoStatus.EnAtencion)
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
            _activeId = null;
            _activeArea = null;
            return ActiveCallSelection.None;
        }

        Start(next, now, countAsAttempt: true);
        return new ActiveCallSelection(next.StableId, true);
    }

    private void Start(TurnoCandidate candidate, DateTimeOffset now, bool countAsAttempt)
    {
        _activeId = candidate.StableId;
        _activeArea = candidate.AreaKey;
        _activeSince = now;
        _repeatAnnouncementSent = false;
        if (countAsAttempt)
        {
            _attemptsByTurn[candidate.StableId] = _attemptsByTurn.GetValueOrDefault(candidate.StableId) + 1;
        }
    }
}
