using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using VisorTurnos.Options;

namespace visor_turnos.Pages;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TurnosModel(IOptions<QueueOptions> queueOptions) : PageModel
{
    public int StaleAfterSeconds { get; } = queueOptions.Value.StaleAfterSeconds;
    public int MaxVisibleRows { get; } = queueOptions.Value.MaxVisibleRows;
    public int AreaRotationSeconds { get; } = queueOptions.Value.AreaRotationSeconds;
}
