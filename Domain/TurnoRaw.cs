namespace VisorTurnos.Domain;

public sealed record TurnoRaw(
    long StableId,
    string? PublicId,
    string Consultorio,
    string Medico,
    DateTime ScheduledAt,
    DateTime? ArrivedAt,
    string? RawStatus,
    int? PrefacturaNumber,
    string? CitedTypeCode,
    string? PatientTypeCode,
    bool IsMedicalExam,
    bool HasMedicalConsultation = false,
    string? ConsultationStatus = null,
    DateTime? ConsultationConnectedAt = null,
    DateTime? ConsultationCreatedAt = null,
    DateTime? ConsultationLastModifiedAt = null,
    bool IsAmanecida = false,
    long? ConsultationId = null,
    int ConsultationAttemptCount = 0);
