using Microsoft.AspNetCore.Mvc;
using VisorTurnos.Domain;
using VisorTurnos.Services;

namespace VisorTurnos.Controllers;

[ApiController]
[Route("api/turnos")]
public sealed class TurnosController(TurnosSnapshotStore store) : ControllerBase
{
    [HttpGet("actuales")]
    [ProducesResponseType<TurnosSnapshotDto>(StatusCodes.Status200OK)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<TurnosSnapshotDto> GetCurrent() => Ok(store.Get());
}
