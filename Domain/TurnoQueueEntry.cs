namespace VisorTurnos.Domain;

/// <summary>
/// Estado interno, no expuesto al navegador, de una cita que ya abrió su acto
/// médico. Conserva solo los datos necesarios para coordinar los llamados y
/// las ausencias entre sondeos de LOLCLI.
/// </summary>
public sealed record TurnoQueueEntry(
    long AppointmentId,
    long? ConsultationId,
    int? PrefacturaNumber,
    string? ConsultationStatus,
    DateTimeOffset? ConsultationConnectedAt,
    DateTimeOffset? ConsultationCreatedAt,
    DateTimeOffset? ConsultationLastModifiedAt,
    TurnoCandidate Turno,
    int CallAttempts,
    bool IsAbsent,
    bool IsRequeueExpired,
    bool IsAwaitingClose);
