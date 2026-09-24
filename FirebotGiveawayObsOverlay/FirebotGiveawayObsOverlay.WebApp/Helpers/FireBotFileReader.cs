using System.Text;

namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

/// <summary>
/// Snapshot of the three Firebot giveaway files.
/// </summary>
public readonly record struct FirebotFileData(string Prize, string Winner, int EntryCount);

/// <summary>
/// Reads Firebot giveaway files with change detection and sticky caching.
/// <list type="bullet">
/// <item>Files are only re-read when their last-write time or length changes, so polling is a cheap stat call.</item>
/// <item>Files are opened with <see cref="FileShare.ReadWrite"/> | <see cref="FileShare.Delete"/> so reads do not
/// fail (or block Firebot) while Firebot holds the file open for writing.</item>
/// <item>When a read fails, the last successfully read value is kept (sticky cache) so the overlay never flickers
/// and the countdown never resets because of a transient I/O error.</item>
/// </list>
/// Not thread-safe: intended to be owned by a single poller (<see cref="Services.GiveawayStateService"/>).
/// </summary>
public sealed class FireBotFileReader
{
    public const string PrizeFileName = "prize.txt";
    public const string WinnerFileName = "winner.txt";
    public const string EntriesFileName = "giveaway.txt";

    private readonly ILogger<FireBotFileReader> _logger;
    private readonly CachedFile _prize = new(PrizeFileName);
    private readonly CachedFile _winner = new(WinnerFileName);
    private readonly CachedFile _entries = new(EntriesFileName);
    private string _folder = string.Empty;

    public FireBotFileReader(ILogger<FireBotFileReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads all three files from <paramref name="folder"/>, re-reading only the ones that changed.
    /// </summary>
    public FirebotFileData Read(string folder)
    {
        if (!string.Equals(folder, _folder, StringComparison.Ordinal))
        {
            _logger.LogInformation("Firebot file folder set to: {FolderPath}", folder);
            _folder = folder;
            _prize.Invalidate();
            _winner.Invalidate();
            _entries.Invalidate();
        }

        var prize = Refresh(_prize, static text => (text.Trim(), 0));
        var winner = Refresh(_winner, static text => (text.Trim(), 0));
        var entries = Refresh(_entries, static text => (string.Empty, CountNonEmptyLines(text)));

        return new FirebotFileData(prize.Text, winner.Text, entries.Count);
    }

    /// <summary>
    /// Counts non-empty lines without allocating a string per entry.
    /// Handles \n, \r\n and \r line endings.
    /// </summary>
    public static int CountNonEmptyLines(ReadOnlySpan<char> text)
    {
        int count = 0;
        foreach (var line in text.EnumerateLines())
        {
            if (!line.IsWhiteSpace())
            {
                count++;
            }
        }
        return count;
    }

    private (string Text, int Count) Refresh(CachedFile file, Func<string, (string Text, int Count)> parse)
    {
        string path = Path.Combine(_folder, file.Name);
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                // Missing file is normal (no active giveaway / no winner yet)
                file.Set(DateTime.MinValue, -1, (string.Empty, 0));
                return file.Value;
            }

            if (file.IsCurrent(info.LastWriteTimeUtc, info.Length))
            {
                return file.Value;
            }

            string content = ReadShared(path);
            file.Set(info.LastWriteTimeUtc, info.Length, parse(content));
            _logger.LogDebug("Read {FileName}: {Length} chars", file.Name, content.Length);

            if (file.FailureLogged)
            {
                _logger.LogInformation("Read of {FileName} recovered", file.Name);
                file.FailureLogged = false;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // File locked / mid-write — keep last known good value. Log once per failure streak to avoid log spam.
            if (!file.FailureLogged)
            {
                _logger.LogWarning("Could not read {FileName} ({Message}); using cached value", file.Name, ex.Message);
                file.FailureLogged = true;
            }
        }
        catch (Exception ex)
        {
            if (!file.FailureLogged)
            {
                _logger.LogError(ex, "Unexpected error reading {FileName}; using cached value", file.Name);
                file.FailureLogged = true;
            }
        }

        return file.Value;
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private sealed class CachedFile(string name)
    {
        private DateTime _lastWriteUtc = DateTime.MaxValue;
        private long _length = long.MinValue;

        public string Name { get; } = name;
        public (string Text, int Count) Value { get; private set; } = (string.Empty, 0);
        public bool FailureLogged { get; set; }

        public bool IsCurrent(DateTime lastWriteUtc, long length) =>
            lastWriteUtc == _lastWriteUtc && length == _length;

        public void Set(DateTime lastWriteUtc, long length, (string Text, int Count) value)
        {
            _lastWriteUtc = lastWriteUtc;
            _length = length;
            Value = value;
        }

        public void Invalidate()
        {
            _lastWriteUtc = DateTime.MaxValue;
            _length = long.MinValue;
            Value = (string.Empty, 0);
            FailureLogged = false;
        }
    }
}
