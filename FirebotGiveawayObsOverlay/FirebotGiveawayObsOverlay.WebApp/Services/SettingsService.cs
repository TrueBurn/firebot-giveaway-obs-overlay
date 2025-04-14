using FirebotGiveawayObsOverlay.WebApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

public class SettingsChangedEventArgs : EventArgs
{
    public string SectionName { get; }

    public SettingsChangedEventArgs(string sectionName)
    {
        SectionName = sectionName;
    }
}

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    TwitchSettings CurrentTwitchSettings { get; }

    event EventHandler<SettingsChangedEventArgs> SettingsChanged;

    Task UpdateAppSettingsAsync(AppSettings newSettings);
    Task UpdateTwitchSettingsAsync(TwitchSettings newSettings);

    // Helper methods to update specific settings
    Task UpdateCountdownTimeAsync(int minutes, int seconds);
    Task UpdateFireBotFileFolderAsync(string folderPath);
    Task UpdatePrizeSectionWidthAsync(int widthPercent);
    Task UpdateFontSizesAsync(double prizeFontSize, double timerFontSize, double entriesFontSize);
}

public class SettingsService : ISettingsService
{
    private readonly IOptionsMonitor<AppSettings> _appSettingsMonitor;
    private readonly IOptionsMonitor<TwitchSettings> _twitchSettingsMonitor;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SettingsService> _logger;
    private readonly object _fileLock = new();

    public AppSettings CurrentSettings => _appSettingsMonitor.CurrentValue;
    public TwitchSettings CurrentTwitchSettings => _twitchSettingsMonitor.CurrentValue;

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public SettingsService(
        IOptionsMonitor<AppSettings> appSettingsMonitor,
        IOptionsMonitor<TwitchSettings> twitchSettingsMonitor,
        IWebHostEnvironment environment,
        ILogger<SettingsService> logger)
    {
        _appSettingsMonitor = appSettingsMonitor;
        _twitchSettingsMonitor = twitchSettingsMonitor;
        _environment = environment;
        _logger = logger;

        // Subscribe to changes
        _appSettingsMonitor.OnChange(settings =>
        {
            _logger.LogInformation("AppSettings changed");
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs("AppSettings"));
        });

        _twitchSettingsMonitor.OnChange(settings =>
        {
            _logger.LogInformation("TwitchSettings changed");
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs("TwitchSettings"));
        });
    }

    public async Task UpdateAppSettingsAsync(AppSettings newSettings)
    {
        await UpdateSettingsInFileAsync("AppSettings", newSettings);
    }

    public async Task UpdateTwitchSettingsAsync(TwitchSettings newSettings)
    {
        await UpdateSettingsInFileAsync("TwitchSettings", newSettings);
    }

    public async Task UpdateCountdownTimeAsync(int minutes, int seconds)
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = CurrentSettings.FireBotFileFolder,
            Layout = CurrentSettings.Layout,
            Fonts = CurrentSettings.Fonts,
            Countdown = new CountdownSettings
            {
                Minutes = minutes,
                Seconds = seconds
            }
        };

        await UpdateAppSettingsAsync(settings);
    }

    public async Task UpdateFireBotFileFolderAsync(string folderPath)
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = folderPath,
            Countdown = CurrentSettings.Countdown,
            Layout = CurrentSettings.Layout,
            Fonts = CurrentSettings.Fonts
        };

        await UpdateAppSettingsAsync(settings);
    }

    public async Task UpdatePrizeSectionWidthAsync(int widthPercent)
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = CurrentSettings.FireBotFileFolder,
            Countdown = CurrentSettings.Countdown,
            Fonts = CurrentSettings.Fonts,
            Layout = new LayoutSettings
            {
                PrizeSectionWidthPercent = widthPercent
            }
        };

        await UpdateAppSettingsAsync(settings);
    }

    public async Task UpdateFontSizesAsync(double prizeFontSize, double timerFontSize, double entriesFontSize)
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = CurrentSettings.FireBotFileFolder,
            Countdown = CurrentSettings.Countdown,
            Layout = CurrentSettings.Layout,
            Fonts = new FontSettings
            {
                PrizeFontSizeRem = prizeFontSize,
                TimerFontSizeRem = timerFontSize,
                EntriesFontSizeRem = entriesFontSize
            }
        };

        await UpdateAppSettingsAsync(settings);
    }

    private async Task UpdateSettingsInFileAsync<T>(string sectionName, T newSettings)
    {
        string appSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");

        // Use a lock to prevent multiple simultaneous writes
        lock (_fileLock)
        {
            try
            {
                // Read current JSON
                string json = File.ReadAllText(appSettingsPath);
                using var jsonDocument = JsonDocument.Parse(json);

                // Create new JSON with updated settings
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();

                    // Copy all properties except the one being updated
                    foreach (var property in jsonDocument.RootElement.EnumerateObject())
                    {
                        if (property.Name != sectionName)
                        {
                            property.WriteTo(writer);
                        }
                    }

                    // Write the updated settings
                    writer.WritePropertyName(sectionName);
                    JsonSerializer.Serialize(writer, newSettings);

                    writer.WriteEndObject();
                }

                // Save back to file
                var jsonString = Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(appSettingsPath, jsonString);

                _logger.LogInformation("Updated {SectionName} in appsettings.json", sectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {SectionName} in appsettings.json", sectionName);
                throw;
            }
        }

        await Task.CompletedTask; // To make the method async
    }
}