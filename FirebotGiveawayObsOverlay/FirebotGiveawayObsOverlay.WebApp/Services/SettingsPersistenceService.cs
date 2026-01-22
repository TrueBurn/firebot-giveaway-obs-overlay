using System.Threading.Channels;
using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Service for queuing settings saves with debouncing.
/// UI updates memory immediately, then this service ensures eventual persistence to disk.
/// </summary>
public class SettingsPersistenceService : IDisposable
{
    private readonly Channel<AppSettings> _channel;
    private readonly object _debounceLock = new();
    private CancellationTokenSource? _debounceCts;
    private AppSettings? _pendingSettings;
    private bool _disposed;

    /// <summary>
    /// Debounce delay before writing to channel (milliseconds).
    /// </summary>
    public const int DebounceDelayMs = 500;

    public SettingsPersistenceService()
    {
        // Bounded channel with capacity 1, drop oldest - only latest settings matter
        _channel = Channel.CreateBounded<AppSettings>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Exposes the channel reader for the background writer service.
    /// </summary>
    public ChannelReader<AppSettings> Reader => _channel.Reader;

    /// <summary>
    /// Queues settings for persistence with debouncing.
    /// Multiple rapid calls will reset the timer; only the latest settings are saved.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    public void QueueSave(AppSettings settings)
    {
        if (_disposed) return;

        lock (_debounceLock)
        {
            // Store pending settings (always keep latest)
            _pendingSettings = settings;

            // Cancel any existing debounce timer
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();

            var cts = _debounceCts;

            // Start new debounce timer
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceDelayMs, cts.Token);

                    // If we weren't cancelled, write to channel
                    AppSettings? settingsToWrite;
                    lock (_debounceLock)
                    {
                        if (cts.Token.IsCancellationRequested || _pendingSettings == null)
                            return;

                        settingsToWrite = _pendingSettings;
                        _pendingSettings = null;
                    }

                    // Write to channel (non-blocking due to DropOldest)
                    _channel.Writer.TryWrite(settingsToWrite);
                }
                catch (OperationCanceledException)
                {
                    // Expected when debounce is reset
                }
            });
        }
    }

    /// <summary>
    /// Flushes any pending settings immediately to the channel.
    /// Called during graceful shutdown.
    /// </summary>
    public void Flush()
    {
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;

            if (_pendingSettings != null)
            {
                _channel.Writer.TryWrite(_pendingSettings);
                _pendingSettings = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Flush any pending settings before shutdown
        Flush();

        // Complete the channel
        _channel.Writer.TryComplete();

        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }
}
