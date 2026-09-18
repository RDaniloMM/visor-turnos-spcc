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
/// verdad sigue siendo LOLCLI: cada sondeo sincroniza todas las citas abiertas,
/// y conserva únicamente el estado efímero de llamados y ausencias. No realiza
/// escrituras en la base.
/// </summary>
public sealed class TurnosQueue(
    IOptions<QueueOptions> options,
    IOptions<SiteOptions> siteOptions,
    IOptions<ScheduleOptions> scheduleOptions)
{
    private readonly object _sync = new();
    private readonly TimeZoneInfo _siteTimeZone = TimeZoneInfo.FindSystemTimeZoneById(siteOptions.Value.TimeZone);
    private readonly Dictionary<long, TurnoQueueEntry> _entriesByAppointment = [];
    private readonly Dictionary<string, ActiveCallState> _activeByArea = [];
    private readonly HashSet<long> _consumedConsultationIds = [];

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
                // La versión anterior podía conservar más de un llamado por
                // consultorio. Si el proceso se actualiza sin reiniciarse,
                // mantiene solamente el primero para restablecer el invariante.
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
                // El límite del llamado prevalece sobre la repetición. Así,
                // incluso si un sondeo se retrasa y no coincidió con los 30 s,
                // al minuto el paciente pasa a la regla de ausencia de forma
                // inmediata y no recibe un segundo aviso tardío.
                if (elapsed >= TimeSpan.FromSeconds(options.Value.CalledDisplaySeconds))
                {
                    _activeByArea.Remove(activeArea);
                    if (_entriesByAppointment.TryGetValue(activeEntry.AppointmentId, out var current))
                    {
                        _entriesByAppointment[activeEntry.AppointmentId] = current with
                        {
                            IsAbsent = true,
                            IsAwaitingClose = true,
                            IsRequeueExpired = current.CallAttempts >= options.Value.MaxCallAttempts || !CanRequeue(now)
                        };
                    }
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
                         .Where(entry => entry.Turno.Status != TurnoStatus.Cerrado || entry.CallAttempts > 0)
                         .GroupBy(entry => entry.Turno.AreaKey)
                         .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                var areaPosition = 0;
                foreach (var entry in area
                             .OrderBy(entry => activeAppointmentIds.Contains(entry.AppointmentId) ? 0 : 1)
                             .ThenBy(entry => IsUnconsumedOpenAct(entry) ? 0 : 1)
                             .ThenBy(entry => entry.IsAbsent ? 1 : 0)
                             .ThenBy(entry => entry.Turno.PriorityTier)
                             .ThenBy(entry => entry.CallAttempts > 0 ? 1 : 0)
                             .ThenByDescending(entry => entry.Turno.IsPreferential)
                             .ThenBy(entry => entry.Turno.ScheduledAt)
                             .ThenBy(entry => entry.AppointmentId))
                {
                    areaPosition++;
                    var isActive = activeAppointmentIds.Contains(entry.AppointmentId);
                    var queueState = isActive
                        ? "llamando"
                        : entry.IsAwaitingClose
                            ? "esperando-cierre"
                            : entry.Turno.Status == TurnoStatus.Cerrado && entry.CallAttempts > 0
                                ? "esperando-reactivacion"
                                : IsUnconsumedOpenAct(entry)
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
                        CanStartCall(entry, DateTimeOffset.UtcNow),
                        entry.CallAttempts,
                        entry.IsAbsent,
                        entry.IsRequeueExpired,
                        entry.CallAttempts >= options.Value.MaxCallAttempts,
                        entry.PrefacturaNumber,
                        entry.Turno.HasMedicalConsultation,
                        entry.ConsultationId,
                        entry.IsAwaitingClose));
                }
            }

            return snapshot;
        }
    }

    private void Synchronize(IReadOnlyList<TurnoCandidate> candidates)
    {
        var incoming = candidates
            .ToDictionary(candidate => candidate.StableId);

        foreach (var appointmentId in _entriesByAppointment.Keys.Except(incoming.Keys).ToArray())
        {
            _entriesByAppointment.Remove(appointmentId);
        }

        foreach (var candidate in incoming.Values)
        {
            if (_entriesByAppointment.TryGetValue(candidate.StableId, out var previous))
            {
                var completedAttempts = GetCompletedAttemptCount(candidate);
                _entriesByAppointment[candidate.StableId] = previous with
                {
                    ConsultationId = candidate.ConsultationId,
                    PrefacturaNumber = candidate.PrefacturaNumber,
                    ConsultationStatus = candidate.ConsultationStatus,
                    ConsultationConnectedAt = candidate.ConsultationConnectedAt,
                    ConsultationCreatedAt = candidate.ConsultationCreatedAt,
                    ConsultationLastModifiedAt = candidate.ConsultationLastModifiedAt,
                    Turno = candidate,
                    CallAttempts = Math.Max(previous.CallAttempts, completedAttempts),
                    IsAwaitingClose = previous.IsAwaitingClose && IsConsumedOpenAct(candidate)
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
                candidate,
                CallAttempts: GetCompletedAttemptCount(candidate),
                IsAbsent: false,
                IsRequeueExpired: false,
                IsAwaitingClose: false);
        }
    }

    private ActiveCallSelection StartNextGlobally(DateTimeOffset now)
    {
        // El médico habilita explícitamente cada llamado creando un numcon en
        // T. Las citas todavía no habilitadas no bloquean otra elección manual
        // del mismo consultorio. El altavoz sigue siendo único para toda la TV.
        var next = _entriesByAppointment.Values
            .GroupBy(entry => entry.Turno.AreaKey)
            .Where(group => !IsAreaBusy(group.Key))
            .Select(group => group
                    .Where(entry => CanStartCall(entry, now))
                    .OrderBy(entry => entry.Turno.PriorityTier)
                    .ThenBy(entry => entry.CallAttempts > 0 ? 1 : 0)
                    .ThenByDescending(entry => entry.Turno.IsPreferential)
                    .ThenBy(entry => entry.Turno.ConsultationCreatedAt)
                    .ThenBy(entry => entry.Turno.ScheduledAt)
                    .ThenBy(entry => entry.AppointmentId)
                    .FirstOrDefault())
            .Where(entry => entry is not null)
            .Select(entry => entry!)
            .OrderBy(entry => entry.Turno.PriorityTier)
            .ThenBy(entry => entry.CallAttempts > 0 ? 1 : 0)
            .ThenByDescending(entry => entry.Turno.IsPreferential)
            .ThenBy(entry => entry.Turno.ConsultationCreatedAt)
            .ThenBy(entry => entry.Turno.ScheduledAt)
            .ThenBy(entry => entry.AppointmentId)
            .FirstOrDefault();

        if (next is null || !CanStartCall(next, now) || !next.ConsultationId.HasValue)
        {
            return ActiveCallSelection.None;
        }

        _consumedConsultationIds.Add(next.ConsultationId.Value);
        _entriesByAppointment[next.AppointmentId] = next with
        {
            CallAttempts = next.CallAttempts + 1,
            IsAbsent = false,
            IsRequeueExpired = false,
            IsAwaitingClose = false
        };
        _activeByArea[next.Turno.AreaKey] = new ActiveCallState(
            next.AppointmentId,
            next.ConsultationId.Value,
            now,
            false);
        return new ActiveCallSelection(next.AppointmentId, true);
    }

    private bool CanRequeue(DateTimeOffset now)
    {
        var localTime = TimeZoneInfo.ConvertTime(now, _siteTimeZone).TimeOfDay;
        var morningEnd = TimeSpan.FromHours(scheduleOptions.Value.RecessStartHour);
        var afternoonStart = TimeSpan.FromHours(scheduleOptions.Value.AfternoonStartHour);
        var afternoonEnd = new TimeSpan(
            options.Value.AfternoonRequeueEndHour,
            options.Value.AfternoonRequeueEndMinute,
            0);

        return localTime < morningEnd || (localTime >= afternoonStart && localTime < afternoonEnd);
    }

    private bool IsUnconsumedOpenAct(TurnoQueueEntry entry) =>
        entry.Turno.Status == TurnoStatus.EnEspera &&
        entry.PrefacturaNumber is not null and not 0 &&
        entry.ConsultationId.HasValue &&
        string.Equals(entry.ConsultationStatus, "T", StringComparison.OrdinalIgnoreCase) &&
        !_consumedConsultationIds.Contains(entry.ConsultationId.Value);

    private bool CanStartCall(TurnoQueueEntry entry, DateTimeOffset now) =>
        entry.CallAttempts < options.Value.MaxCallAttempts &&
        !entry.IsRequeueExpired &&
        (entry.CallAttempts == 0 || CanRequeue(now)) &&
        IsUnconsumedOpenAct(entry);

    private bool IsAreaBusy(string areaKey) =>
        _entriesByAppointment.Values.Any(entry =>
            string.Equals(entry.Turno.AreaKey, areaKey, StringComparison.Ordinal) &&
            entry.ConsultationId.HasValue &&
            _consumedConsultationIds.Contains(entry.ConsultationId.Value) &&
            string.Equals(entry.ConsultationStatus, "T", StringComparison.OrdinalIgnoreCase));

    private static bool IsCurrentOpenAct(TurnoQueueEntry entry, long consultationId) =>
        entry.ConsultationId == consultationId &&
        string.Equals(entry.ConsultationStatus, "T", StringComparison.OrdinalIgnoreCase);

    private bool IsConsumedOpenAct(TurnoCandidate candidate) =>
        candidate.ConsultationId.HasValue &&
        _consumedConsultationIds.Contains(candidate.ConsultationId.Value) &&
        string.Equals(candidate.ConsultationStatus, "T", StringComparison.OrdinalIgnoreCase);

    private static int GetCompletedAttemptCount(TurnoCandidate candidate)
    {
        var currentActIsOpen = string.Equals(
            candidate.ConsultationStatus,
            "T",
            StringComparison.OrdinalIgnoreCase);
        return Math.Max(0, candidate.ConsultationAttemptCount - (currentActIsOpen ? 1 : 0));
    }

    private static IReadOnlyList<ActiveCallSelection> ToSelection(ActiveCallSelection selection) =>
        selection.StableId.HasValue ? [selection] : [];

    private sealed record ActiveCallState(
        long AppointmentId,
        long ConsultationId,
        DateTimeOffset Since,
        bool RepeatAnnouncementSent);
}
