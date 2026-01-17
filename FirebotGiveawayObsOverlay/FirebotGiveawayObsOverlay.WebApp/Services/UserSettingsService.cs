using System.Text.Json;
using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Service for loading and saving user-specific settings to usersettings.json.
/// Settings persist across application restarts and are git-ignored.
/// </summary>
public class UserSettingsService
{
    private readonly string _userSettingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UserSettingsService()
    {
        // Store usersettings.json in the same directory as the executable
        var baseDirectory = AppContext.BaseDirectory;
        _userSettingsPath = Path.Combine(baseDirectory, "usersettings.json");
    }

    /// <summary>
    /// Checks if user settings file exists.
    /// </summary>
    public bool UserSettingsExist() => File.Exists(_userSettingsPath);

    /// <summary>
    /// Loads user settings from usersettings.json.
    /// </summary>
    /// <returns>AppSettings object or null if file doesn't exist or is invalid.</returns>
    public AppSettings? LoadUserSettings()
    {
        try
        {
            if (!File.Exists(_userSettingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(_userSettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load user settings: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves user settings to usersettings.json.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    public void SaveUserSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_userSettingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save user settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the path to the user settings file.
    /// </summary>
    public string GetUserSettingsPath() => _userSettingsPath;

    /// <summary>
    /// Deletes the user settings file, effectively resetting to defaults.
    /// </summary>
    /// <returns>True if file was deleted or didn't exist, false on error.</returns>
    public bool DeleteUserSettings()
    {
        try
        {
            if (File.Exists(_userSettingsPath))
            {
                File.Delete(_userSettingsPath);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete user settings: {ex.Message}");
            return false;
        }
    }
}
