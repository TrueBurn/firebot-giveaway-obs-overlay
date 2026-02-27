using FirebotGiveawayObsOverlay.WebApp.Components;
using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Runtime.InteropServices;

// Create a LoggingLevelSwitch so we can change log level at runtime from the UI
var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/overlay-.log",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10_485_760,
        retainedFileCountLimit: 7,
        rollOnFileSizeLimit: true)
    .CreateLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();
builder.Services.AddSingleton(levelSwitch);

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Add TimerService as a singleton
builder.Services.AddSingleton<TimerService>();

// Add ThemeService as a singleton
builder.Services.AddSingleton<ThemeService>();

// Add VersionService as a singleton
builder.Services.AddSingleton<VersionService>();

// Add UserSettingsService as a singleton
builder.Services.AddSingleton<UserSettingsService>();

// Add settings persistence services for async/debounced saving
builder.Services.AddSingleton<SettingsPersistenceService>();
builder.Services.AddHostedService<BackgroundSettingsWriterService>();

// Add SettingsService as singleton (replaces GiveAwayHelpers for settings management)
builder.Services.AddSingleton<ISettingsService, SettingsService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Lifetime.ApplicationStarted.Register(() =>
{
    var settingsService = app.Services.GetRequiredService<ISettingsService>();

    // Build fallback defaults from appsettings.json
    var fallbackDefaults = new AppSettings
    {
        FireBotFileFolder = app.Configuration.GetValue("AppSettings:FireBotFileFolder", @"G:\Giveaway") ?? @"G:\Giveaway",
        CountdownTimerEnabled = app.Configuration.GetValue<bool>("AppSettings:CountdownTimerEnabled", true),
        CountdownHours = app.Configuration.GetValue<int>("AppSettings:CountdownHours", 0),
        CountdownMinutes = app.Configuration.GetValue<int>("AppSettings:CountdownMinutes", 60),
        CountdownSeconds = app.Configuration.GetValue<int>("AppSettings:CountdownSeconds", 0),
        PrizeSectionWidthPercent = app.Configuration.GetValue<int>("AppSettings:PrizeSectionWidthPercent", 75),
        PrizeFontSizeRem = app.Configuration.GetValue<double>("AppSettings:PrizeFontSizeRem", 3.5),
        TimerFontSizeRem = app.Configuration.GetValue<double>("AppSettings:TimerFontSizeRem", 3.0),
        EntriesFontSizeRem = app.Configuration.GetValue<double>("AppSettings:EntriesFontSizeRem", 2.5),
        Theme = new ThemeSettings
        {
            Name = app.Configuration.GetValue<string>("AppSettings:Theme:Name", "Warframe") ?? "Warframe",
            PrimaryColor = app.Configuration.GetValue<string>("AppSettings:Theme:PrimaryColor", "#00fff9") ?? "#00fff9",
            SecondaryColor = app.Configuration.GetValue<string>("AppSettings:Theme:SecondaryColor", "#ff00c8") ?? "#ff00c8",
            BackgroundStart = app.Configuration.GetValue<string>("AppSettings:Theme:BackgroundStart", "rgba(0, 0, 0, 0.9)") ?? "rgba(0, 0, 0, 0.9)",
            BackgroundEnd = app.Configuration.GetValue<string>("AppSettings:Theme:BackgroundEnd", "rgba(15, 25, 35, 0.98)") ?? "rgba(15, 25, 35, 0.98)",
            BorderGlowColor = app.Configuration.GetValue<string>("AppSettings:Theme:BorderGlowColor", "rgba(0, 255, 255, 0.15)") ?? "rgba(0, 255, 255, 0.15)",
            TextColor = app.Configuration.GetValue<string>("AppSettings:Theme:TextColor", "#ffffff") ?? "#ffffff",
            TimerExpiredColor = app.Configuration.GetValue<string>("AppSettings:Theme:TimerExpiredColor", "#ff3333") ?? "#ff3333",
            SeparatorColor = app.Configuration.GetValue<string>("AppSettings:Theme:SeparatorColor", "rgba(0, 255, 255, 0.5)") ?? "rgba(0, 255, 255, 0.5)"
        }
    };

    settingsService.LoadFromFile(fallbackDefaults);

    // Apply LoggingLevelSwitch from loaded settings
    var logLevelSwitch = app.Services.GetRequiredService<LoggingLevelSwitch>();
    logLevelSwitch.MinimumLevel = settingsService.Current.Logging.MinimumLevel;

    Log.Information("Application started - version {Version}",
        app.Services.GetRequiredService<VersionService>().GetDisplayVersion());

    // Launch browser with correct port
    string url = "http://localhost:5000/giveaway";
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
});

//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

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
