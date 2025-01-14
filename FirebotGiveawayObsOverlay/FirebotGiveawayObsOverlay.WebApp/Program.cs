using FirebotGiveawayObsOverlay.WebApp.Components;
using FirebotGiveawayObsOverlay.WebApp.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

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
    int giveawayDuration = app.Configuration.GetValue<int>("AppSettings:GiveawayDuration", 69);
    GiveAwayHelpers.SetGiveawayDuration(giveawayDuration);

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
