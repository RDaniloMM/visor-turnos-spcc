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
    bool IsMedicalExam);
