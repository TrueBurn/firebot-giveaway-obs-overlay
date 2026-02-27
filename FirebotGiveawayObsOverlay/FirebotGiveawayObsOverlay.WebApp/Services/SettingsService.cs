using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Singleton in-memory settings store with event-driven change notification
/// and debounced async persistence via SettingsPersistenceService.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly UserSettingsService _userSettingsService;
    private readonly SettingsPersistenceService _persistenceService;
    private readonly ILogger<SettingsService> _logger;
    private readonly object _lock = new();
    private AppSettings _current = new();

    public SettingsService(
        UserSettingsService userSettingsService,
        SettingsPersistenceService persistenceService,
        ILogger<SettingsService> logger)
    {
        _userSettingsService = userSettingsService;
        _persistenceService = persistenceService;
        _logger = logger;
    }

    public AppSettings Current
    {
        get { lock (_lock) { return _current; } }
    }

    public event Action? OnSettingsChanged;

    public void Update(Action<AppSettings> mutator)
    {
        lock (_lock)
        {
            mutator(_current);
        }

        _logger.LogDebug("Settings updated in memory");

        // Apply to legacy GiveAwayHelpers (theme + file path that still use static state)
        ApplyToLegacyHelpers();

        // Fire change event for UI subscribers (GiveAway overlay, etc.)
        OnSettingsChanged?.Invoke();

        // Queue debounced async persistence
        _persistenceService.QueueSave(CloneCurrentSettings());

        _logger.LogDebug("Settings change queued for persistence");
    }

    public void ResetToDefaults()
    {
        lock (_lock)
        {
            _current = AppSettings.GetDefaults();
        }

        _logger.LogInformation("Settings reset to defaults");

        ApplyToLegacyHelpers();
        _userSettingsService.DeleteUserSettings();
        OnSettingsChanged?.Invoke();
    }

    public void LoadFromFile(AppSettings fallbackDefaults)
    {
        var loaded = _userSettingsService.LoadUserSettings();

        lock (_lock)
        {
            _current = loaded ?? fallbackDefaults;
        }

        if (loaded != null)
        {
            _logger.LogInformation("Loaded user settings from {Path}", _userSettingsService.GetUserSettingsPath());
        }
        else
        {
            _logger.LogInformation("No user settings found, using defaults from appsettings.json");
        }

        ApplyToLegacyHelpers();
    }

    /// <summary>
    /// Syncs current settings to GiveAwayHelpers static state.
    /// Needed because FireBotFileReader and theme logic still read from statics.
    /// This will be removed when those are also migrated to DI.
    /// </summary>
    private void ApplyToLegacyHelpers()
    {
        var s = Current;
        GiveAwayHelpers.ApplySettings(s);
    }

    private AppSettings CloneCurrentSettings()
    {
        lock (_lock)
        {
            // Create a fresh copy for persistence to avoid mutation during serialization
            return new AppSettings
            {
                FireBotFileFolder = _current.FireBotFileFolder,
                CountdownTimerEnabled = _current.CountdownTimerEnabled,
                CountdownHours = _current.CountdownHours,
                CountdownMinutes = _current.CountdownMinutes,
                CountdownSeconds = _current.CountdownSeconds,
                PrizeSectionWidthPercent = _current.PrizeSectionWidthPercent,
                PrizeFontSizeRem = _current.PrizeFontSizeRem,
                TimerFontSizeRem = _current.TimerFontSizeRem,
                EntriesFontSizeRem = _current.EntriesFontSizeRem,
                Theme = ThemeSettings.FromThemeConfig(
                    _current.Theme.ToThemeConfig()),
                Logging = new LoggingSettings
                {
                    MinimumLevel = _current.Logging.MinimumLevel,
                    LogFilePath = _current.Logging.LogFilePath,
                    EnableFileLogging = _current.Logging.EnableFileLogging,
                    EnableConsoleLogging = _current.Logging.EnableConsoleLogging,
                }
            };
        }
    }
}
