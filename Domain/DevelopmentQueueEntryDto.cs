namespace VisorTurnos.Domain;

/// <summary>
/// Diagnóstico de DevelopmentSnapshot. No se publica en la pantalla de TV ni
/// se habilita fuera del simulador local.
/// </summary>
public sealed record DevelopmentQueueEntryDto(
    long AppointmentId,
    string PatientName,
    string Consultorio,
    string Medico,
    DateTimeOffset ScheduledAt,
    int PositionInArea,
    string QueueState,
    bool IsEligibleForCall,
    int? PrefacturaNumber,
    bool HasMedicalConsultation,
    long? ConsultationId);
