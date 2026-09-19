using Microsoft.AspNetCore.Mvc;
using VisorTurnos.Data;
using VisorTurnos.Domain;
using VisorTurnos.Services;

namespace VisorTurnos.Controllers;

[ApiController]
[Route("api/dev/simulador")]
public sealed class DevelopmentSimulationController(
    DevelopmentSnapshotGuard guard,
    DevelopmentSimulationRepository repository,
    TurnosQueue turnosQueue) : ControllerBase
{
    [HttpGet("turnos")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<IReadOnlyList<SimulationTurnDto>>> GetTurns(CancellationToken cancellationToken)
    {
        if (!guard.IsEnabled) return NotFound();
        return Ok(await repository.GetTurnsAsync(cancellationToken));
    }

    [HttpGet("cola-interna")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<IReadOnlyList<DevelopmentQueueEntryDto>> GetInternalQueue()
    {
        if (!guard.IsEnabled) return NotFound();
        return Ok(turnosQueue.GetDevelopmentSnapshot());
    }

    [HttpPost("citas-ficticias")]
    public Task<ActionResult<SimulationActionResultDto>> CreateFictitiousAppointment(
        [FromBody] CreateSimulationAppointmentRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => repository.CreateFictitiousAppointmentAsync(request, cancellationToken));

    [HttpPost("{invnum:int}/llamar")]
    public Task<ActionResult<SimulationActionResultDto>> OpenMedicalAct(int invnum, CancellationToken cancellationToken) =>
        ExecuteAsync(() => repository.OpenMedicalActAsync(invnum, cancellationToken));

    [HttpPost("{invnum:int}/crear-prefactura")]
    public Task<ActionResult<SimulationActionResultDto>> CreatePrefactura(int invnum, CancellationToken cancellationToken) =>
        ExecuteAsync(() => repository.CreatePrefacturaAsync(invnum, cancellationToken));

    [HttpPost("{invnum:int}/guardar-consulta")]
    public Task<ActionResult<SimulationActionResultDto>> SaveConsultation(int invnum, CancellationToken cancellationToken) =>
        ExecuteAsync(() => repository.SaveConsultationAsync(invnum, cancellationToken));

    [HttpPost("{invnum:int}/cerrar-cita")]
    public Task<ActionResult<SimulationActionResultDto>> CloseAppointment(int invnum, CancellationToken cancellationToken) =>
        ExecuteAsync(() => repository.CloseAppointmentAsync(invnum, cancellationToken));

    [HttpPost("{invnum:int}/restablecer")]
    public Task<ActionResult<SimulationActionResultDto>> ResetAppointment(int invnum, CancellationToken cancellationToken) =>
        ExecuteAsync(() => repository.ResetAppointmentAsync(invnum, cancellationToken));

    private async Task<ActionResult<SimulationActionResultDto>> ExecuteAsync(Func<Task<SimulationActionResultDto>> action)
    {
        if (!guard.IsEnabled) return NotFound();
        var result = await action();
        return result.Succeeded ? Ok(result) : Conflict(result);
    }
}
