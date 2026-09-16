namespace VisorTurnos.Domain;

public sealed record TurnoCandidate(
    long StableId,
    string PublicId,
    string Consultorio,
    string Medico,
    TurnoStatus Status,
    int PriorityTier,
    bool IsPreferential,
    bool IsMedicalExam,
    DateTimeOffset ScheduledAt,
    DateTimeOffset? ArrivedAt,
    DateTimeOffset EligibilityTime);
