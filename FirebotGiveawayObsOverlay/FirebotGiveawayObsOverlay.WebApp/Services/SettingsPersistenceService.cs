using System.Threading.Channels;
using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Service for queuing settings saves with debouncing.
/// UI updates memory immediately, then this service ensures eventual persistence to disk.
/// Uses a single reusable timer (no Task/CancellationTokenSource allocated per keystroke).
/// </summary>
public sealed class SettingsPersistenceService : IDisposable
{
    private readonly Channel<AppSettings> _channel;
    private readonly Lock _lock = new();
    private readonly ITimer _debounceTimer;
    private AppSettings? _pendingSettings;
    private bool _disposed;

    /// <summary>
    /// Debounce delay before writing to channel (milliseconds).
    /// </summary>
    public const int DebounceDelayMs = 500;

    public SettingsPersistenceService() : this(TimeProvider.System)
    {
    }

    public SettingsPersistenceService(TimeProvider timeProvider)
    {
        // Bounded channel with capacity 1, drop oldest - only latest settings matter
        _channel = Channel.CreateBounded<AppSettings>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        _debounceTimer = timeProvider.CreateTimer(_ => Flush(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Exposes the channel reader for the background writer service.
    /// </summary>
    public ChannelReader<AppSettings> Reader => _channel.Reader;

    /// <summary>
    /// Queues settings for persistence with debouncing.
    /// Multiple rapid calls restart the delay; only the latest settings are saved.
    /// </summary>
    public void QueueSave(AppSettings settings)
    {
        lock (_lock)
        {
            if (_disposed) return;
            _pendingSettings = settings;
            _debounceTimer.Change(TimeSpan.FromMilliseconds(DebounceDelayMs), Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Discards any pending (not yet flushed) save. Used when resetting to defaults.
    /// </summary>
    public void CancelPending()
    {
        lock (_lock)
        {
            _pendingSettings = null;
            _debounceTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            // Also drop anything already handed to the writer but not yet consumed
            while (_channel.Reader.TryRead(out _)) { }
        }
    }

    /// <summary>
    /// Flushes any pending settings immediately to the channel.
    /// Called by the debounce timer and during graceful shutdown.
    /// </summary>
    public void Flush()
    {
        lock (_lock)
        {
            _debounceTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            if (_pendingSettings != null)
            {
                _channel.Writer.TryWrite(_pendingSettings);
                _pendingSettings = null;
            }
        }
    }

    /// <summary>
    /// Flushes pending settings and completes the channel so the writer can drain and exit.
    /// </summary>
    public void Complete()
    {
        Flush();
        _channel.Writer.TryComplete();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        Complete();
        _debounceTimer.Dispose();
    }
}
