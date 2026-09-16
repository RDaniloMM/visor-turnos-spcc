using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed class TurnosSnapshotBuilder(
    TurnoStatusPolicy statusPolicy,
    PriorityPolicy priorityPolicy,
    IOptions<SiteOptions> siteOptions)
{
    private readonly TimeZoneInfo _siteTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(siteOptions.Value.TimeZone);

    public TurnosBuildResult Build(
        IReadOnlyList<TurnoRaw> rawItems,
        IReadOnlyDictionary<long, TurnoStatus> previousStates,
        DateTimeOffset now)
    {
        var states = new Dictionary<long, TurnoStatus>();
        var candidates = new List<TurnoCandidate>();

        foreach (var raw in rawItems)
        {
            var status = statusPolicy.Normalize(raw, now);
            states[raw.StableId] = status;

            if (status == TurnoStatus.Cerrado || string.IsNullOrWhiteSpace(raw.PublicId))
            {
                continue;
            }

            var scheduled = ToSiteOffset(raw.ScheduledAt);
            DateTimeOffset? arrived = raw.ArrivedAt.HasValue ? ToSiteOffset(raw.ArrivedAt.Value) : null;
            candidates.Add(new TurnoCandidate(
                raw.StableId,
                raw.PublicId.Trim(),
                string.IsNullOrWhiteSpace(raw.Consultorio) ? "Por confirmar" : raw.Consultorio.Trim(),
                string.IsNullOrWhiteSpace(raw.Medico) ? "Médico por confirmar" : raw.Medico.Trim(),
                status,
                priorityPolicy.GetTier(raw),
                priorityPolicy.IsPreferential(raw),
                raw.IsMedicalExam,
                scheduled,
                arrived,
                arrived ?? scheduled));
        }

        var items = candidates
            .OrderBy(item => item.PriorityTier)
            .ThenByDescending(item => item.IsPreferential)
            .ThenBy(item => item.EligibilityTime)
            .ThenBy(item => item.ArrivedAt)
            .ThenBy(item => item.ScheduledAt)
            .ThenBy(item => item.StableId)
            .Select(item => new TurnoPublicoDto(
                item.PublicId,
                item.Consultorio,
                item.Medico,
                item.Status.ToPublicName(),
                item.PriorityTier,
                item.IsPreferential,
                item.IsMedicalExam,
                item.ScheduledAt,
                item.ArrivedAt,
                previousStates.TryGetValue(item.StableId, out var previous) &&
                    previous == TurnoStatus.EnEspera &&
                    item.Status == TurnoStatus.EnAtencion))
            .ToArray();

        return new TurnosBuildResult(items, states);
    }

    private DateTimeOffset ToSiteOffset(DateTime value)
    {
        var unspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, _siteTimeZone.GetUtcOffset(unspecified));
    }
}
