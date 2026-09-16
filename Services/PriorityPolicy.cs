using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed class PriorityPolicy(
    IOptions<BusinessRulesOptions> options,
    IOptions<PriorityOptions> priorityOptions)
{
    public int GetTier(TurnoRaw turno)
    {
        if (turno.IsMedicalExam)
        {
            return priorityOptions.Value.MedicalExamTier;
        }

        if (turno.CitedTypeCode is not null &&
            options.Value.PriorityTierByCitedType.TryGetValue(turno.CitedTypeCode, out var tier))
        {
            return tier;
        }

        return priorityOptions.Value.DefaultTier;
    }

    public bool IsPreferential(TurnoRaw turno) =>
        turno.PatientTypeCode is not null &&
        options.Value.PreferentialPatientTypeCodes.Contains(
            turno.PatientTypeCode,
            StringComparer.OrdinalIgnoreCase);
}
