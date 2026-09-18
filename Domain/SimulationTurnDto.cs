namespace VisorTurnos.Domain;

public sealed record SimulationTurnDto(
    int Invnum,
    string MedicalCode,
    string PatientName,
    string Consultorio,
    string Medico,
    DateTime ScheduledAt,
    string? AppointmentStatus,
    string? AppointmentObservation,
    int? PrefacturaNumber,
    long? ConsultationId,
    bool HasMedicalConsultation,
    string? ConsultationStatus,
    DateTime? ConsultationConnectedAt,
    DateTime? ConsultationCreatedAt,
    DateTime? ConsultationLastModifiedAt);

public sealed record SimulationActionResultDto(
    bool Succeeded,
    string Message,
    int? AppointmentId = null);

/// <summary>
/// Entrada exclusiva del simulador LocalDB. No representa un contrato de
/// LOLCLI ni se admite fuera de DevelopmentSnapshot.
/// </summary>
public sealed record CreateSimulationAppointmentRequest(
    string? MedicalCode,
    DateTime? ScheduledAt,
    string? Priority);
