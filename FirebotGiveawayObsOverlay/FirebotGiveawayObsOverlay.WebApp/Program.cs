using FirebotGiveawayObsOverlay.WebApp.Components;
using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Twitch settings
builder.Services.Configure<TwitchSettings>(builder.Configuration.GetSection("TwitchSettings"));

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
    string fileBotFileFolder = app.Configuration.GetValue("AppSettings:FireBotFileFolder", "G:\\Giveaway") ?? "G:\\Giveaway";
    GiveAwayHelpers.SetFireBotFileFolder(fileBotFileFolder);
    
    // Initialize countdown timer settings from configuration
    int countdownMinutes = app.Configuration.GetValue<int>("AppSettings:CountdownMinutes", 60);
    int countdownSeconds = app.Configuration.GetValue<int>("AppSettings:CountdownSeconds", 0);
    GiveAwayHelpers.SetCountdownTime(countdownMinutes, countdownSeconds);
    
    // Initialize prize section width from configuration
    int prizeSectionWidth = app.Configuration.GetValue<int>("AppSettings:PrizeSectionWidthPercent", 75);
    GiveAwayHelpers.SetPrizeSectionWidth(prizeSectionWidth);
    
    // Initialize font size settings from configuration
    double prizeFontSize = app.Configuration.GetValue<double>("AppSettings:PrizeFontSizeRem", 3.5);
    double timerFontSize = app.Configuration.GetValue<double>("AppSettings:TimerFontSizeRem", 3.0);
    double entriesFontSize = app.Configuration.GetValue<double>("AppSettings:EntriesFontSizeRem", 2.5);
    GiveAwayHelpers.SetPrizeFontSize(prizeFontSize);
    GiveAwayHelpers.SetTimerFontSize(timerFontSize);
    GiveAwayHelpers.SetEntriesFontSize(entriesFontSize);
    
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

app.Run();
