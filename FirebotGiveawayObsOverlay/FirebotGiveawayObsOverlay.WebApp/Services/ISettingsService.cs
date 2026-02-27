using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Singleton settings service that holds all application settings in memory,
/// fires change events for immediate UI updates, and queues async persistence.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Current in-memory settings. Fast read, no I/O.
    /// </summary>
    AppSettings Current { get; }

    /// <summary>
    /// Fired immediately when any setting is mutated.
    /// Subscribers (e.g. GiveAway overlay) use this for instant UI updates.
    /// </summary>
    event Action? OnSettingsChanged;

    /// <summary>
    /// Atomically mutates in-memory settings, fires OnSettingsChanged,
    /// and queues debounced async persistence to usersettings.json.
    /// </summary>
    void Update(Action<AppSettings> mutator);

    /// <summary>
    /// Replaces in-memory settings with defaults, fires OnSettingsChanged,
    /// and deletes usersettings.json.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Loads settings from usersettings.json (or falls back to provided defaults).
    /// Called once at startup.
    /// </summary>
    void LoadFromFile(AppSettings fallbackDefaults);
}
