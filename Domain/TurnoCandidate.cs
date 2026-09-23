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
    DateTimeOffset EligibilityTime,
    int? PrefacturaNumber = null,
    bool HasMedicalConsultation = false,
    string? ConsultationStatus = null,
    DateTimeOffset? ConsultationConnectedAt = null,
    DateTimeOffset? ConsultationCreatedAt = null,
    DateTimeOffset? ConsultationLastModifiedAt = null,
    long? ConsultationId = null,
    int ConsultationAttemptCount = 0,
    bool IsAmanecida = false,
    bool HasNoShowDiagnosis = false,
    bool HasPriorClosedActWithoutNoShow = false)
{
    public string AreaKey => $"{Consultorio}\u001f{Medico}";
}
