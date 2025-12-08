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

// Add TimerService as a singleton
builder.Services.AddSingleton<TimerService>();

// Add ThemeService as a singleton
builder.Services.AddSingleton<ThemeService>();

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
    string fileBotFileFolder = app.Configuration.GetValue("AppSettings:FireBotFileFolder", "G:\\Giveaway") ?? "G:\\Giveaway";
    GiveAwayHelpers.SetFireBotFileFolder(fileBotFileFolder);
    
    // Initialize countdown timer settings from configuration
    bool countdownTimerEnabled = app.Configuration.GetValue<bool>("AppSettings:CountdownTimerEnabled", true);
    int countdownHours = app.Configuration.GetValue<int>("AppSettings:CountdownHours", 0);
    int countdownMinutes = app.Configuration.GetValue<int>("AppSettings:CountdownMinutes", 60);
    int countdownSeconds = app.Configuration.GetValue<int>("AppSettings:CountdownSeconds", 0);
    GiveAwayHelpers.SetCountdownTimerEnabled(countdownTimerEnabled);
    GiveAwayHelpers.SetCountdownTime(countdownHours, countdownMinutes, countdownSeconds);
    
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

    // Initialize theme settings from configuration
    var themeConfig = new ThemeConfig
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
    };
    GiveAwayHelpers.InitializeTheme(themeConfig);

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
