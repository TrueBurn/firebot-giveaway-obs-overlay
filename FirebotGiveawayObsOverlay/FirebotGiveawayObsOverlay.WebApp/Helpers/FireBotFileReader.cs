using Serilog;

namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

/// <summary>
/// Reads Firebot giveaway files with sticky caching.
/// When a file read fails (e.g. file locked by Firebot during write),
/// the last successfully read value is returned instead of empty string.
/// This prevents the giveaway timer from resetting on transient I/O failures.
/// </summary>
public static class FireBotFileReader
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(FireBotFileReader));

    private static string _fireBotFileFolder = @"G:\Giveaway";
    private static readonly string _prizeFile = "prize.txt";
    private static readonly string _winnerFile = "winner.txt";
    private static readonly string _entriesFile = "giveaway.txt";

    // Sticky cache: last-known-good values survive transient file read failures
    private static string _lastPrize = string.Empty;
    private static string _lastWinner = string.Empty;
    private static string[] _lastEntries = [];

    public static void SetFireBotFileFolder(string folderPath)
    {
        if (_fireBotFileFolder == folderPath) return;
        _fireBotFileFolder = folderPath;
        _logger.Information("Firebot file folder set to: {FolderPath}", folderPath);
    }

    public static string GetFireBotFileFolder() => _fireBotFileFolder;

    public static async Task<string> GetPrizeAsync()
    {
        var result = await GetFireBotFileAsync(_prizeFile);
        if (result != null)
        {
            _lastPrize = result;
            return result;
        }
        _logger.Warning("Prize file read failed, using cached value: '{CachedPrize}'", _lastPrize);
        return _lastPrize;
    }

    public static async Task<string> GetWinnerAsync()
    {
        var result = await GetFireBotFileAsync(_winnerFile);
        if (result != null)
        {
            _lastWinner = result;
            return result;
        }
        _logger.Warning("Winner file read failed, using cached value: '{CachedWinner}'", _lastWinner);
        return _lastWinner;
    }

    public static async Task<string[]> GetEntriesAsync()
    {
        var result = await GetFireBotFileAsync(_entriesFile);
        if (result != null)
        {
            _lastEntries = result.Split(
                [Environment.NewLine, "\n"],
                StringSplitOptions.RemoveEmptyEntries);
            return _lastEntries;
        }
        _logger.Warning("Entries file read failed, using cached value ({CachedCount} entries)", _lastEntries.Length);
        return _lastEntries;
    }

    /// <summary>
    /// Reads a Firebot file. Returns the content string on success, or null on failure.
    /// Null signals to callers that they should use the cached value.
    /// </summary>
    private static async Task<string?> GetFireBotFileAsync(string fileName)
    {
        string filePath = Path.Combine(_fireBotFileFolder, fileName);
        try
        {
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                _logger.Debug("Read {FileName}: {Length} chars", fileName, content.Length);
                return content;
            }

            // File doesn't exist - this is normal (e.g. no active giveaway)
            return string.Empty;
        }
        catch (IOException ex)
        {
            // File locked by Firebot during write - expected, use cache
            _logger.Warning("File I/O error reading {FileName}: {Message}", fileName, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error reading {FileName}", fileName);
            return null;
        }
    }
}
