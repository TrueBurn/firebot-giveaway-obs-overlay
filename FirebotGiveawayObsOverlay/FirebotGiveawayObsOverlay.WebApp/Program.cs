using FirebotGiveawayObsOverlay.WebApp.Components;
using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Runtime.InteropServices;

// Published builds: anchor the content root (wwwroot, appsettings.json, relative log paths) to the exe folder,
// not the current working directory. Otherwise launching from a shortcut with a different "Start in",
// a Stream Deck / Firebot "run program" action, or a script serves a blank app.
// (Build output has no wwwroot folder, so `dotnet run` keeps the default project-folder content root.)
var publishedContentRoot = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "wwwroot"))
    ? AppContext.BaseDirectory
    : null;

WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = publishedContentRoot,
});

// Defaults from appsettings.json (AppSettings section), overridden by usersettings.json when present.
// Loaded before the host starts so the very first request already sees the user's settings.
var fallbackDefaults = builder.Configuration.GetSection("AppSettings").Get<AppSettings>() ?? new AppSettings();
var userSettingsPath = builder.Configuration.GetValue<string>("UserSettingsPath");
var effectiveUserSettingsPath = string.IsNullOrWhiteSpace(userSettingsPath)
    ? Path.Combine(AppContext.BaseDirectory, UserSettingsService.DefaultFileName)
    : Path.GetFullPath(userSettingsPath);
var startupSettings = UserSettingsService.TryLoad(effectiveUserSettingsPath) ?? fallbackDefaults;

// LoggingLevelSwitch lets the Setup page change the log level at runtime.
// Sink/path options are applied here at startup (the UI notes they require a restart).
var levelSwitch = new LoggingLevelSwitch(startupSettings.Logging.MinimumLevel);
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext();

// Sinks run on a background thread so logging never blocks the poll loop or a Blazor circuit
// (Windows console writes in particular are slow).
loggerConfig.WriteTo.Async(sinks =>
{
    if (startupSettings.Logging.EnableConsoleLogging)
    {
        sinks.Console();
    }
    if (startupSettings.Logging.EnableFileLogging && !string.IsNullOrWhiteSpace(startupSettings.Logging.LogFilePath))
    {
        sinks.File(
            path: Path.Combine(builder.Environment.ContentRootPath, startupSettings.Logging.LogFilePath),
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 10_485_760,
            retainedFileCountLimit: 7,
            rollOnFileSizeLimit: true);
    }
});
Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSingleton(levelSwitch);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<VersionService>();
builder.Services.AddSingleton(sp => new UserSettingsService(
    sp.GetRequiredService<ILogger<UserSettingsService>>(), effectiveUserSettingsPath));

// Settings persistence: in-memory store + debounced async writer
builder.Services.AddSingleton<SettingsPersistenceService>();
builder.Services.AddHostedService<BackgroundSettingsWriterService>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();

// Single shared file poller + countdown for all overlays
builder.Services.AddSingleton<FireBotFileReader>();
builder.Services.AddSingleton<GiveawayStateService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GiveawayStateService>());

WebApplication app = builder.Build();

var settingsService = app.Services.GetRequiredService<ISettingsService>();
settingsService.LoadFromFile(fallbackDefaults);
levelSwitch.MinimumLevel = settingsService.Current.Logging.MinimumLevel;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.Lifetime.ApplicationStarted.Register(() =>
{
    Log.Information("Application started - version {Version}",
        app.Services.GetRequiredService<VersionService>().GetDisplayVersion());

    if (app.Configuration.GetValue("LaunchBrowser", true))
    {
        LaunchBrowser("http://localhost:5000/giveaway");
    }
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static void LaunchBrowser(string url)
{
    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to open browser");
    }
}

public partial class Program;
