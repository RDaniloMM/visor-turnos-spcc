using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed record ActiveCallSelection(long? StableId, bool ShouldAnnounce)
{
    public static ActiveCallSelection None { get; } = new(null, false);
}

/// <summary>
/// Cola en memoria con un único llamado activo para toda la TV. La fuente de
/// verdad sigue siendo LOLCLI: cada sondeo sincroniza las citas abiertas y
/// conserva únicamente el estado efímero del llamado. No escribe en la base
/// ni infiere si una persona asistió o no.
/// </summary>
public sealed class TurnosQueue(IOptions<QueueOptions> options)
{
    private readonly object _sync = new();
    private readonly Dictionary<long, TurnoQueueEntry> _entriesByAppointment = [];
    private readonly Dictionary<string, ActiveCallState> _activeByArea = [];
    private readonly HashSet<long> _consumedConsultationIds = [];
    private readonly HashSet<long> _newConsultationIds = [];
    private bool _hasInitialSnapshot;

    public IReadOnlyList<ActiveCallSelection> SynchronizeAndSelect(
        IReadOnlyList<TurnoCandidate> candidates,
        DateTimeOffset now)
    {
        lock (_sync)
        {
            Synchronize(candidates);

            var activeArea = _activeByArea.Keys.OrderBy(key => key, StringComparer.Ordinal).FirstOrDefault();
            if (activeArea is not null && _activeByArea.TryGetValue(activeArea, out var activeState))
            {
                // Si el proceso se actualiza sin reiniciarse, conserva un solo
                // llamado para restablecer el invariante global del altavoz.
                foreach (var areaKey in _activeByArea.Keys.Where(key => key != activeArea).ToArray())
                {
                    _activeByArea.Remove(areaKey);
                }

                if (!_entriesByAppointment.TryGetValue(activeState.AppointmentId, out var activeEntry) ||
                    !IsCurrentOpenAct(activeEntry, activeState.ConsultationId))
                {
                    _activeByArea.Remove(activeArea);
                    return ToSelection(StartNextGlobally(now));
                }

                var elapsed = now - activeState.Since;
                // Al finalizar los 40 segundos, solo desaparecen el banner y
                // la voz. El acto continúa siendo responsabilidad del médico.
                if (elapsed >= TimeSpan.FromSeconds(options.Value.CalledDisplaySeconds))
                {
                    _activeByArea.Remove(activeArea);
                    return ToSelection(StartNextGlobally(now));
                }

                if (!activeState.RepeatAnnouncementSent &&
                    elapsed >= TimeSpan.FromSeconds(options.Value.RepeatCallAnnouncementSeconds))
                {
                    _activeByArea[activeArea] = activeState with { RepeatAnnouncementSent = true };
                    return [new ActiveCallSelection(activeEntry.AppointmentId, true)];
                }

                return [new ActiveCallSelection(activeEntry.AppointmentId, false)];
            }

            return ToSelection(StartNextGlobally(now));
        }
    }

    public IReadOnlyList<DevelopmentQueueEntryDto> GetDevelopmentSnapshot()
    {
        lock (_sync)
        {
            var snapshot = new List<DevelopmentQueueEntryDto>();
            var activeAppointmentIds = _activeByArea.Values.Select(active => active.AppointmentId).ToHashSet();
            foreach (var area in _entriesByAppointment.Values
                         .Where(entry => entry.Turno.Status != TurnoStatus.Cerrado)
                         .GroupBy(entry => entry.Turno.AreaKey)
                         .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                var areaPosition = 0;
                foreach (var entry in area
                             .OrderBy(entry => activeAppointmentIds.Contains(entry.AppointmentId) ? 0 : 1)
                             .ThenBy(entry => IsUnconsumedConsultation(entry) ? 0 : 1)
                             .ThenBy(entry => entry.Turno.PriorityTier)
                             .ThenByDescending(entry => entry.Turno.IsPreferential)
                             .ThenBy(entry => entry.Turno.ScheduledAt)
                             .ThenBy(entry => entry.AppointmentId))
                {
                    areaPosition++;
                    var isActive = activeAppointmentIds.Contains(entry.AppointmentId);
                    var queueState = isActive
                        ? "llamando"
                        : IsConsumedCurrentConsultation(entry.Turno)
                            ? "llamado-finalizado"
                            : IsUnconsumedConsultation(entry)
                                ? "habilitado"
                                : "proximo";
                    snapshot.Add(new DevelopmentQueueEntryDto(
                        entry.AppointmentId,
                        entry.Turno.PublicId,
                        entry.Turno.Consultorio,
                        entry.Turno.Medico,
                        entry.Turno.ScheduledAt,
                        areaPosition,
                        queueState,
                        CanStartCall(entry),
                        entry.PrefacturaNumber,
                        entry.Turno.HasMedicalConsultation,
                        entry.ConsultationId));
                }
            }

            return snapshot;
        }
    }

    private void Synchronize(IReadOnlyList<TurnoCandidate> candidates)
    {
        var incoming = candidates.ToDictionary(candidate => candidate.StableId);
        var initialSnapshot = !_hasInitialSnapshot;

        foreach (var appointmentId in _entriesByAppointment.Keys.Except(incoming.Keys).ToArray())
        {
            _entriesByAppointment.Remove(appointmentId);
        }

        foreach (var candidate in incoming.Values)
        {
            if (!initialSnapshot && candidate.ConsultationId.HasValue)
            {
                var previousId = _entriesByAppointment.TryGetValue(candidate.StableId, out var priorEntry)
                    ? priorEntry.ConsultationId
                    : null;
                if (previousId != candidate.ConsultationId)
                {
                    _newConsultationIds.Add(candidate.ConsultationId.Value);
                }
            }

            if (_entriesByAppointment.TryGetValue(candidate.StableId, out var previous))
            {
                _entriesByAppointment[candidate.StableId] = previous with
                {
                    ConsultationId = candidate.ConsultationId,
                    PrefacturaNumber = candidate.PrefacturaNumber,
                    ConsultationStatus = candidate.ConsultationStatus,
                    ConsultationConnectedAt = candidate.ConsultationConnectedAt,
                    ConsultationCreatedAt = candidate.ConsultationCreatedAt,
                    ConsultationLastModifiedAt = candidate.ConsultationLastModifiedAt,
                    Turno = candidate
                };
                continue;
            }

            _entriesByAppointment[candidate.StableId] = new TurnoQueueEntry(
                candidate.StableId,
                candidate.ConsultationId,
                candidate.PrefacturaNumber,
                candidate.ConsultationStatus,
                candidate.ConsultationConnectedAt,
                candidate.ConsultationCreatedAt,
                candidate.ConsultationLastModifiedAt,
                candidate);
        }

        _hasInitialSnapshot = true;
    }

    private ActiveCallSelection StartNextGlobally(DateTimeOffset now)
    {
        // El médico habilita explícitamente cada llamado creando un numcon. La
        // TV nunca lo reutiliza ni crea uno nuevo por su cuenta.
        var next = _entriesByAppointment.Values
            .GroupBy(entry => entry.Turno.AreaKey)
            .Where(group => !IsAreaBusy(group.Key))
            .Select(group => group
                    .Where(CanStartCall)
                    .OrderBy(entry => entry.Turno.PriorityTier)
                    .ThenByDescending(entry => entry.Turno.IsPreferential)
                    .ThenBy(entry => entry.Turno.ConsultationCreatedAt)
                    .ThenBy(entry => entry.Turno.ScheduledAt)
                    .ThenBy(entry => entry.AppointmentId)
                    .FirstOrDefault())
            .Where(entry => entry is not null)
            .Select(entry => entry!)
            .OrderBy(entry => entry.Turno.PriorityTier)
            .ThenByDescending(entry => entry.Turno.IsPreferential)
            .ThenBy(entry => entry.Turno.ConsultationCreatedAt)
            .ThenBy(entry => entry.Turno.ScheduledAt)
            .ThenBy(entry => entry.AppointmentId)
            .FirstOrDefault();

        if (next is null || !CanStartCall(next) || !next.ConsultationId.HasValue)
        {
            return ActiveCallSelection.None;
        }

        _consumedConsultationIds.Add(next.ConsultationId.Value);
        _activeByArea[next.Turno.AreaKey] = new ActiveCallState(
            next.AppointmentId,
            next.ConsultationId.Value,
            now,
            false);
        return new ActiveCallSelection(next.AppointmentId, true);
    }

    private bool IsUnconsumedConsultation(TurnoQueueEntry entry) =>
        entry.Turno.Status == TurnoStatus.EnEspera &&
        entry.ConsultationId.HasValue &&
        _newConsultationIds.Contains(entry.ConsultationId.Value) &&
        !IsConsultationClosed(entry.ConsultationStatus) &&
        !entry.Turno.HasNoShowDiagnosis &&
        !entry.Turno.HasPriorClosedActWithoutNoShow &&
        !_consumedConsultationIds.Contains(entry.ConsultationId.Value);

    private bool CanStartCall(TurnoQueueEntry entry) => IsUnconsumedConsultation(entry);

    private bool IsAreaBusy(string areaKey) =>
        _entriesByAppointment.Values.Any(entry =>
            string.Equals(entry.Turno.AreaKey, areaKey, StringComparison.Ordinal) &&
            entry.ConsultationId.HasValue &&
            _consumedConsultationIds.Contains(entry.ConsultationId.Value) &&
            !IsConsultationClosed(entry.ConsultationStatus) &&
            !entry.Turno.HasNoShowDiagnosis &&
            !entry.Turno.HasPriorClosedActWithoutNoShow);

    private static bool IsCurrentOpenAct(TurnoQueueEntry entry, long consultationId) =>
        entry.ConsultationId == consultationId &&
        !IsConsultationClosed(entry.ConsultationStatus) &&
        !entry.Turno.HasNoShowDiagnosis &&
        !entry.Turno.HasPriorClosedActWithoutNoShow;

    private bool IsConsumedCurrentConsultation(TurnoCandidate candidate) =>
        candidate.ConsultationId.HasValue &&
        _consumedConsultationIds.Contains(candidate.ConsultationId.Value) &&
        !IsConsultationClosed(candidate.ConsultationStatus) &&
        !candidate.HasNoShowDiagnosis &&
        !candidate.HasPriorClosedActWithoutNoShow;

    private static bool IsConsultationClosed(string? status) =>
        string.Equals(status?.Trim(), "P", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<ActiveCallSelection> ToSelection(ActiveCallSelection selection) =>
        selection.StableId.HasValue ? [selection] : [];

    private sealed record ActiveCallState(
        long AppointmentId,
        long ConsultationId,
        DateTimeOffset Since,
        bool RepeatAnnouncementSent);
}
