using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using VisorTurnos.Options;

namespace visor_turnos.Pages;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TurnosModel(
    IOptions<QueueOptions> queueOptions,
    IOptions<ScheduleOptions> scheduleOptions) : PageModel
{
    public int StaleAfterSeconds { get; } = queueOptions.Value.StaleAfterSeconds;
    public int MaxVisibleRows { get; } = queueOptions.Value.MaxVisibleRows;
    public int AreaRotationSeconds { get; } = queueOptions.Value.AreaRotationSeconds;
    public int MorningStartHour { get; } = scheduleOptions.Value.MorningStartHour;
    public int RecessStartHour { get; } = scheduleOptions.Value.RecessStartHour;
    public int AfternoonStartHour { get; } = scheduleOptions.Value.AfternoonStartHour;
    public int DayEndHour { get; } = scheduleOptions.Value.DayEndHour;
}
