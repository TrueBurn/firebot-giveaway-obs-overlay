using System.Text.Json;
using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Service for loading and saving user-specific settings to usersettings.json.
/// Settings persist across application restarts and are git-ignored.
/// <para>
/// Writes are atomic (temp file + replace) so a crash or power loss mid-save can never leave a truncated
/// usersettings.json that would silently reset the user's configuration on next start.
/// </para>
/// </summary>
public class UserSettingsService
{
    public const string DefaultFileName = "usersettings.json";

    private readonly string _userSettingsPath;
    private readonly ILogger<UserSettingsService> _logger;
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UserSettingsService(ILogger<UserSettingsService> logger, string? userSettingsPath = null)
    {
        _logger = logger;
        // Default: store usersettings.json in the same directory as the executable
        _userSettingsPath = string.IsNullOrWhiteSpace(userSettingsPath)
            ? Path.Combine(AppContext.BaseDirectory, DefaultFileName)
            : Path.GetFullPath(userSettingsPath);
    }

    /// <summary>
    /// Checks if user settings file exists.
    /// </summary>
    public bool UserSettingsExist() => File.Exists(_userSettingsPath);

    /// <summary>
    /// Loads user settings from usersettings.json.
    /// </summary>
    /// <returns>AppSettings object or null if file doesn't exist or is invalid.</returns>
    public AppSettings? LoadUserSettings() => TryLoad(_userSettingsPath, _logger);

    /// <summary>
    /// Loads settings from a file without needing DI (used at startup before the logger/host exist).
    /// </summary>
    public static AppSettings? TryLoad(string path, ILogger? logger = null)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<AppSettings>(stream, JsonOptions);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to load user settings from {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Saves user settings to usersettings.json synchronously.
    /// </summary>
    public void SaveUserSettings(AppSettings settings)
    {
        _ioLock.Wait();
        try
        {
            var tempPath = _userSettingsPath + ".tmp";
            File.WriteAllBytes(tempPath, JsonSerializer.SerializeToUtf8Bytes(settings, JsonOptions));
            File.Move(tempPath, _userSettingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user settings to {Path}", _userSettingsPath);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    /// <summary>
    /// Saves user settings to usersettings.json asynchronously (atomic replace).
    /// Used by background writer service for non-blocking disk I/O.
    /// </summary>
    public async Task SaveUserSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            var tempPath = _userSettingsPath + ".tmp";
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
                             bufferSize: 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
            }
            File.Move(tempPath, _userSettingsPath, overwrite: true);
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw to allow proper cancellation handling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user settings to {Path}", _userSettingsPath);
        }
        finally
        {
            _ioLock.Release();
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
        _ioLock.Wait();
        try
        {
            File.Delete(_userSettingsPath); // No-op if missing
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user settings at {Path}", _userSettingsPath);
            return false;
        }
        finally
        {
            _ioLock.Release();
        }
    }
}
