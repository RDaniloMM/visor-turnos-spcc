namespace VisorTurnos.Domain;

public sealed record TurnoPublicoDto(
    string PublicId,
    string Consultorio,
    string Medico,
    string Estado,
    int PriorityTier,
    bool IsPreferential,
    bool IsMedicalExam,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? ArrivedAt,
    bool ShouldAnnounce);
