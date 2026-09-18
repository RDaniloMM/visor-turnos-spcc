using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace visor_turnos.Pages;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SimuladorModel(
    DevelopmentSnapshotGuard guard,
    IOptions<ScheduleOptions> scheduleOptions) : PageModel
{
    public int MorningStartHour => scheduleOptions.Value.MorningStartHour;
    public int RecessStartHour => scheduleOptions.Value.RecessStartHour;
    public int AfternoonStartHour => scheduleOptions.Value.AfternoonStartHour;
    public int DayEndHour => scheduleOptions.Value.DayEndHour;

    public IActionResult OnGet() => guard.IsEnabled ? Page() : NotFound();
}
