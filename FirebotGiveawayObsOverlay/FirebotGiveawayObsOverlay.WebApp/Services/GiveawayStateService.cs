using FirebotGiveawayObsOverlay.WebApp.Helpers;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Immutable view of the giveaway that overlays render.
/// </summary>
public sealed record GiveawaySnapshot(
    string Prize,
    string Winner,
    int EntryCount,
    bool TimerEnabled,
    TimeSpan Remaining,
    bool TimerExpired)
{
    public static readonly GiveawaySnapshot Empty = new(string.Empty, string.Empty, 0, true, TimeSpan.Zero, false);

    public bool IsRunning => !string.IsNullOrWhiteSpace(Prize);
    public bool HasWinner => !string.IsNullOrWhiteSpace(Winner);
}

/// <summary>
/// Single server-side source of truth for the giveaway: polls the Firebot files and runs the countdown.
/// <para>
/// Previously every connected overlay polled the files and ran its own countdown. That meant N× the I/O,
/// countdowns that drifted (decrement-per-tick) and a countdown that restarted from full whenever OBS reloaded
/// the browser source. Now one loop does the work and pushes a snapshot to subscribers only when something
/// visible changes; the countdown is computed from a deadline so it cannot drift and survives page reloads.
/// </para>
/// </summary>
public sealed class GiveawayStateService : BackgroundService
{
    /// <summary>Poll interval. Stat calls are cheap, so we poll fast for snappy updates.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly ISettingsService _settings;
    private readonly FireBotFileReader _reader;
    private readonly TimeProvider _time;
    private readonly ILogger<GiveawayStateService> _logger;
    private readonly Lock _lock = new();
    private readonly Lock _publishLock = new();

    // Countdown state (guarded by _lock)
    private bool _timerRunning;
    private DateTimeOffset _deadline;
    private TimeSpan _pausedRemaining;

    // Last observed file state (guarded by _lock)
    private FirebotFileData _files;
    private bool _initialized;
    private bool _timerEnabled;

    private volatile GiveawaySnapshot _current = GiveawaySnapshot.Empty;

    public GiveawayStateService(
        ISettingsService settings,
        FireBotFileReader reader,
        TimeProvider time,
        ILogger<GiveawayStateService> logger)
    {
        _settings = settings;
        _reader = reader;
        _time = time;
        _logger = logger;
        _timerEnabled = settings.Current.CountdownTimerEnabled;
        _settings.OnSettingsChanged += HandleSettingsChanged;
    }

    /// <summary>Latest snapshot. Safe to read from any thread.</summary>
    public GiveawaySnapshot Current => _current;

    /// <summary>
    /// Raised (on a background thread) whenever the visible snapshot changes.
    /// Handlers must be fast and must marshal to their own sync context (e.g. Blazor's InvokeAsync).
    /// </summary>
    public event Action<GiveawaySnapshot>? SnapshotChanged;

    /// <summary>Resets the countdown to the configured duration (restarts it if a giveaway is running).</summary>
    public void ResetTimer()
    {
        lock (_lock)
        {
            ResetTimerLocked();
            _logger.LogInformation("Timer reset manually");
        }
        Publish();
    }

    /// <summary>Runs one poll + countdown step. Exposed for tests; the background loop calls it.</summary>
    public void Tick()
    {
        var files = _reader.Read(_settings.Current.FireBotFileFolder);

        lock (_lock)
        {
            ApplyFilesLocked(files);
        }

        Publish();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Giveaway state service started");
        using var timer = new PeriodicTimer(PollInterval, _time);
        do
        {
            try
            {
                Tick();
            }
            catch (Exception ex)
            {
                // Never let the loop die — the overlay would freeze for the rest of the stream.
                _logger.LogError(ex, "Giveaway poll failed");
            }
        }
        while (await WaitNextAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitNextAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public override void Dispose()
    {
        _settings.OnSettingsChanged -= HandleSettingsChanged;
        base.Dispose();
    }

    private void ApplyFilesLocked(FirebotFileData files)
    {
        bool hadPrize = _initialized && !string.IsNullOrWhiteSpace(_files.Prize);
        bool hadWinner = _initialized && !string.IsNullOrWhiteSpace(_files.Winner);
        bool hasPrize = !string.IsNullOrWhiteSpace(files.Prize);
        bool hasWinner = !string.IsNullOrWhiteSpace(files.Winner);

        _files = files;
        _initialized = true;
        _timerEnabled = _settings.Current.CountdownTimerEnabled;

        if (!_timerEnabled)
        {
            return;
        }

        if (hasWinner && _timerRunning)
        {
            PauseLocked();
            _logger.LogInformation("Timer paused: winner detected");
        }
        else if (hadWinner && !hasWinner && hasPrize)
        {
            ResetTimerLocked();
            _logger.LogInformation("Timer reset: winner cleared, giveaway still running");
        }
        else if (!hadPrize && hasPrize && !hasWinner)
        {
            ResetTimerLocked();
            _logger.LogInformation("Timer reset: new giveaway started (prize detected)");
        }
        else if (!hasPrize && (_timerRunning || _pausedRemaining != ConfiguredDuration()))
        {
            ResetTimerLocked();
            _logger.LogDebug("Timer reset: giveaway not running");
        }
    }

    private void HandleSettingsChanged()
    {
        var s = _settings.Current;
        lock (_lock)
        {
            bool wasEnabled = _timerEnabled;
            _timerEnabled = s.CountdownTimerEnabled;

            if (wasEnabled && !_timerEnabled)
            {
                PauseLocked();
                _logger.LogDebug("Timer disabled via settings change");
            }
            else if (!wasEnabled && _timerEnabled)
            {
                ResetTimerLocked();
                _logger.LogDebug("Timer re-enabled via settings change");
            }
            else if (!_timerRunning && _initialized && string.IsNullOrWhiteSpace(_files.Prize))
            {
                // Idle: keep the displayed duration in sync with the configured one
                ResetTimerLocked();
            }
        }
        Publish();
    }

    private TimeSpan ConfiguredDuration()
    {
        var s = _settings.Current;
        var duration = new TimeSpan(s.CountdownHours, s.CountdownMinutes, s.CountdownSeconds);
        return duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }

    private void ResetTimerLocked()
    {
        var duration = ConfiguredDuration();
        bool shouldRun = _timerEnabled
                         && !string.IsNullOrWhiteSpace(_files.Prize)
                         && string.IsNullOrWhiteSpace(_files.Winner);

        _pausedRemaining = duration;
        _timerRunning = shouldRun;
        _deadline = _time.GetUtcNow() + duration;
    }

    private void PauseLocked()
    {
        if (!_timerRunning) return;
        _pausedRemaining = RemainingLocked();
        _timerRunning = false;
    }

    private TimeSpan RemainingLocked()
    {
        if (!_timerRunning) return _pausedRemaining;
        var remaining = _deadline - _time.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private void Publish()
    {
        // Serialize publishers (poll loop vs. settings/reset callers) so subscribers see snapshots in order.
        lock (_publishLock)
        {
            PublishLocked();
        }
    }

    private void PublishLocked()
    {
        GiveawaySnapshot next;
        lock (_lock)
        {
            var remaining = RemainingLocked();
            // Round up to whole seconds so "00:01" shows for the full final second and 0 means expired.
            var wholeSeconds = TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds));
            bool expired = _timerRunning && remaining <= TimeSpan.Zero;

            next = new GiveawaySnapshot(
                _files.Prize ?? string.Empty,
                _files.Winner ?? string.Empty,
                _files.EntryCount,
                _timerEnabled,
                wholeSeconds,
                expired);
        }

        if (next == _current) return;
        _current = next;

        var handlers = SnapshotChanged;
        if (handlers is null) return;

        // Invoke each subscriber in isolation so one broken circuit cannot starve the others.
        foreach (var handler in handlers.GetInvocationList().Cast<Action<GiveawaySnapshot>>())
        {
            try
            {
                handler(next);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Giveaway snapshot subscriber threw");
            }
        }
    }
}
