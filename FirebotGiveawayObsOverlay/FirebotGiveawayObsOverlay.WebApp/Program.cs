using FirebotGiveawayObsOverlay.WebApp.Components;
using FirebotGiveawayObsOverlay.WebApp.Configuration;
using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Diagnostics;
using System.Runtime.InteropServices;

// Configure Serilog first
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
    .CreateLogger();

try
{
    Log.Information("Starting Firebot Giveaway OBS Overlay");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // Log the environment
    Log.Information("Building application in environment: {Environment}", builder.Environment.EnvironmentName);

    // Set up dynamic configuration
    string appSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
    Log.Information("Adding dynamic configuration for: {Path}", appSettingsPath);
    builder.Configuration.AddDynamicJsonFile(appSettingsPath);
    
    // Also add dynamic configuration for appsettings.Development.json if in Development environment
    if (builder.Environment.IsDevelopment())
    {
        string devSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.Development.json");
        if (File.Exists(devSettingsPath))
        {
            Log.Information("Adding dynamic configuration for: {Path}", devSettingsPath);
            builder.Configuration.AddDynamicJsonFile(devSettingsPath);
        }
        else
        {
            Log.Warning("Development settings file not found: {Path}", devSettingsPath);
        }
    }

    // Add services to the container.
    builder.Services
        .AddRazorComponents()
        .AddInteractiveServerComponents();

    // Configure strongly typed settings
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    builder.Services.Configure<TwitchSettings>(builder.Configuration.GetSection("TwitchSettings"));

    // Register settings service
    builder.Services.AddSingleton<ISettingsService, SettingsService>();

    // Add services as singletons
    builder.Services.AddSingleton<TimerService>();
    builder.Services.AddSingleton<TwitchService>();
    builder.Services.AddSingleton<GiveawayService>();
    builder.Services.AddSingleton<CommandHandler>();

    WebApplication app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.Lifetime.ApplicationStarted.Register(async () =>
    {
        // Get the settings service
        var settingsService = app.Services.GetRequiredService<ISettingsService>();
    
        // Log the current environment and FireBotFileFolder path
        Log.Information("Current environment: {Environment}", app.Environment.EnvironmentName);
        Log.Information("FireBotFileFolder path: {Path}", settingsService.CurrentSettings.FireBotFileFolder);
        
        // Check if the folder exists
        bool folderExists = Directory.Exists(settingsService.CurrentSettings.FireBotFileFolder);
        Log.Information("FireBotFileFolder exists: {Exists}", folderExists);
        
        // Initialize FireBotFileReader with the current settings
        GiveAwayHelpers.SetFireBotFileFolder(settingsService.CurrentSettings.FireBotFileFolder);

        // Initialize countdown timer settings from configuration
        GiveAwayHelpers.SetCountdownTime(
            settingsService.CurrentSettings.Countdown.Minutes,
            settingsService.CurrentSettings.Countdown.Seconds);

        // Initialize prize section width from configuration
        GiveAwayHelpers.SetPrizeSectionWidth(
            settingsService.CurrentSettings.Layout.PrizeSectionWidthPercent);

        // Initialize font size settings from configuration
        GiveAwayHelpers.SetPrizeFontSize(settingsService.CurrentSettings.Fonts.PrizeFontSizeRem);
        GiveAwayHelpers.SetTimerFontSize(settingsService.CurrentSettings.Fonts.TimerFontSizeRem);
        GiveAwayHelpers.SetEntriesFontSize(settingsService.CurrentSettings.Fonts.EntriesFontSizeRem);

        // Subscribe to settings changes
        settingsService.SettingsChanged += (sender, e) =>
        {
            if (e.SectionName == "AppSettings")
            {
                var settings = settingsService.CurrentSettings;

                // Update helpers with new settings
                GiveAwayHelpers.SetFireBotFileFolder(settings.FireBotFileFolder);
                GiveAwayHelpers.SetCountdownTime(settings.Countdown.Minutes, settings.Countdown.Seconds);
                GiveAwayHelpers.SetPrizeSectionWidth(settings.Layout.PrizeSectionWidthPercent);
                GiveAwayHelpers.SetPrizeFontSize(settings.Fonts.PrizeFontSizeRem);
                GiveAwayHelpers.SetTimerFontSize(settings.Fonts.TimerFontSizeRem);
                GiveAwayHelpers.SetEntriesFontSize(settings.Fonts.EntriesFontSizeRem);

                Log.Information("Updated application settings from configuration changes");
            }
        };

        // Initialize Twitch connection if enabled
        bool enableTwitch = app.Configuration.GetValue<bool>("TwitchSettings:Enabled", false);
        if (enableTwitch)
        {
            try
            {
                var twitchService = app.Services.GetRequiredService<TwitchService>();
                await twitchService.ConnectAsync();
                Console.WriteLine("Connected to Twitch chat successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to Twitch: {ex.Message}");
            }
        }

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
            Console.WriteLine($"Failed to open browser: {ex.Message}");
        }
    });

    //app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    try
    {
        Log.Information("Starting web host");
        app.Run();
        return 0;
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Host terminated unexpectedly");
        return 1;
    }
    finally
    {
        Log.CloseAndFlush();
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
    return 1;
}
