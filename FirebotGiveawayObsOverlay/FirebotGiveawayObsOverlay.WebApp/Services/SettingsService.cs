using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Singleton in-memory settings store with event-driven change notification
/// and debounced async persistence via SettingsPersistenceService.
/// <para>
/// Uses copy-on-write: <see cref="Current"/> always returns a snapshot that is never mutated afterwards,
/// so readers on other circuits/threads can never observe a half-applied update.
/// </para>
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly UserSettingsService _userSettingsService;
    private readonly SettingsPersistenceService _persistenceService;
    private readonly ILogger<SettingsService> _logger;
    private readonly Lock _lock = new();
    private volatile AppSettings _current = new();
    private volatile AppSettings _defaults = new();

    public SettingsService(
        UserSettingsService userSettingsService,
        SettingsPersistenceService persistenceService,
        ILogger<SettingsService> logger)
    {
        _userSettingsService = userSettingsService;
        _persistenceService = persistenceService;
        _logger = logger;
    }

    public AppSettings Current => _current;

    public AppSettings Defaults => _defaults;

    public event Action? OnSettingsChanged;

    public void Update(Action<AppSettings> mutator)
    {
        AppSettings updated;
        lock (_lock)
        {
            updated = _current.Clone();
            mutator(updated);
            _current = updated;
        }

        _logger.LogDebug("Settings updated in memory");

        // Apply to legacy GiveAwayHelpers (theme state that still uses static state)
        GiveAwayHelpers.ApplySettings(updated);

        RaiseChanged();

        // Queue debounced async persistence. The snapshot is immutable, so no extra copy is needed.
        _persistenceService.QueueSave(updated);
    }

    public void ResetToDefaults()
    {
        var defaults = _defaults.Clone();
        lock (_lock)
        {
            _current = defaults;
        }

        _logger.LogInformation("Settings reset to defaults");

        // Drop any debounced save first, otherwise it would re-create usersettings.json after we delete it.
        _persistenceService.CancelPending();
        _userSettingsService.DeleteUserSettings();

        GiveAwayHelpers.ApplySettings(defaults);
        RaiseChanged();
    }

    public void LoadFromFile(AppSettings fallbackDefaults)
    {
        _defaults = fallbackDefaults.Clone();
        var loaded = _userSettingsService.LoadUserSettings();
        var settings = loaded ?? fallbackDefaults.Clone();

        lock (_lock)
        {
            _current = settings;
        }

        if (loaded != null)
        {
            _logger.LogInformation("Loaded user settings from {Path}", _userSettingsService.GetUserSettingsPath());
        }
        else
        {
            _logger.LogInformation("No user settings found, using defaults from appsettings.json");
        }

        GiveAwayHelpers.ApplySettings(settings);
    }

    private void RaiseChanged()
    {
        var handlers = OnSettingsChanged;
        if (handlers is null) return;

        // Isolate subscribers: an exception in one overlay's handler must not break the Setup page that made the change.
        foreach (var handler in handlers.GetInvocationList().Cast<Action>())
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Settings change subscriber threw");
            }
        }
    }
}
