using VisorTurnos.Data;
using VisorTurnos.Hubs;
using VisorTurnos.Options;
using VisorTurnos.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 16 * 1024);
builder.Services.AddHealthChecks()
    .AddCheck<TurnosSourceHealthCheck>("turnos-source", tags: ["ready"]);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<SiteOptions>()
    .Bind(builder.Configuration.GetSection(SiteOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<QueueOptions>()
    .Bind(builder.Configuration.GetSection(QueueOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<DataSourceOptions>()
    .Bind(builder.Configuration.GetSection(DataSourceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<BusinessRulesOptions>()
    .Bind(builder.Configuration.GetSection(BusinessRulesOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<PriorityOptions>()
    .Bind(builder.Configuration.GetSection(PriorityOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<StartupConfigurationValidator>();
builder.Services.AddSingleton<TurnosSnapshotStore>();
builder.Services.AddSingleton<PrefacturaPolicy>();
builder.Services.AddSingleton<TurnoStatusPolicy>();
builder.Services.AddSingleton<PriorityPolicy>();
builder.Services.AddSingleton<CalledTurnRotationPolicy>();
builder.Services.AddSingleton<TurnosSnapshotBuilder>();
builder.Services.AddSingleton<TurnosChangeDetector>();
builder.Services.AddSingleton<ITurnosRepository>(services =>
    TurnosRepositoryFactory.Create(services, builder.Configuration));
builder.Services.AddHostedService<TurnosPollingWorker>();

var app = builder.Build();
app.Services.GetRequiredService<StartupConfigurationValidator>().Validate();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-store";
        }
    }
});

app.UseRouting();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self' ws: wss:; img-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
    await next();
});

app.MapControllers();
app.MapHub<TurnosHub>("/hubs/turnos");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapRazorPages();

app.Run();

public partial class Program;
