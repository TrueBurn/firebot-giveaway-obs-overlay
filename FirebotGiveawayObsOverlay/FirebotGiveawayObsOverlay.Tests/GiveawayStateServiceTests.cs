using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FirebotGiveawayObsOverlay.Tests;

public sealed class GiveawayStateServiceTests : IDisposable
{
    private readonly TempFolder _dir = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private readonly SettingsService _settings;
    private readonly GiveawayStateService _sut;

    public GiveawayStateServiceTests()
    {
        var userSettings = new UserSettingsService(NullLogger<UserSettingsService>.Instance, _dir.File("usersettings.json"));
        _settings = new SettingsService(userSettings, new SettingsPersistenceService(_time), NullLogger<SettingsService>.Instance);
        _settings.LoadFromFile(new AppSettings
        {
            FireBotFileFolder = _dir.Path,
            CountdownHours = 0,
            CountdownMinutes = 1,
            CountdownSeconds = 0,
        });
        _sut = new GiveawayStateService(_settings, new FireBotFileReader(NullLogger<FireBotFileReader>.Instance), _time,
            NullLogger<GiveawayStateService>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
        _dir.Dispose();
    }

    private void Advance(TimeSpan by)
    {
        _time.Advance(by);
        _sut.Tick();
    }

    [Fact]
    public void NoPrize_NotRunning_ShowsFullDuration()
    {
        _sut.Tick();
        Assert.False(_sut.Current.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(1), _sut.Current.Remaining);
    }

    [Fact]
    public void PrizeAppears_CountdownStartsAndCountsDownFromDeadline()
    {
        _dir.Write(FireBotFileReader.PrizeFileName, "PS5");
        _dir.Write(FireBotFileReader.EntriesFileName, "a\nb\n");
        _sut.Tick();

        Assert.True(_sut.Current.IsRunning);
        Assert.Equal(2, _sut.Current.EntryCount);
        Assert.Equal(TimeSpan.FromSeconds(60), _sut.Current.Remaining);

        Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(TimeSpan.FromSeconds(50), _sut.Current.Remaining);

        // A long gap (e.g. thread pool starvation) must not cause drift
        Advance(TimeSpan.FromSeconds(30.5));
        Assert.Equal(TimeSpan.FromSeconds(20), _sut.Current.Remaining);
    }

    [Fact]
    public void Countdown_ExpiresAtZero()
    {
        _dir.Write(FireBotFileReader.PrizeFileName, "PS5");
        _sut.Tick();
        Advance(TimeSpan.FromSeconds(59.5));
        Assert.False(_sut.Current.TimerExpired);
        Assert.Equal(TimeSpan.FromSeconds(1), _sut.Current.Remaining);

        Advance(TimeSpan.FromSeconds(1));
        Assert.True(_sut.Current.TimerExpired);
        Assert.Equal(TimeSpan.Zero, _sut.Current.Remaining);
    }

    [Fact]
    public void Winner_PausesCountdown_ClearingWinnerResets()
    {
        _dir.Write(FireBotFileReader.PrizeFileName, "PS5");
        _sut.Tick();
        Advance(TimeSpan.FromSeconds(15));

        _dir.Write(FireBotFileReader.WinnerFileName, "CoolStreamer42");
        _sut.Tick();
        Assert.True(_sut.Current.HasWinner);
        Assert.Equal("CoolStreamer42", _sut.Current.Winner);
        var paused = _sut.Current.Remaining;

        Advance(TimeSpan.FromSeconds(20));
        Assert.Equal(paused, _sut.Current.Remaining);

        _dir.Write(FireBotFileReader.WinnerFileName, "");
        _sut.Tick();
        Assert.Equal(TimeSpan.FromSeconds(60), _sut.Current.Remaining);
        Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.FromSeconds(55), _sut.Current.Remaining);
    }

    [Fact]
    public void PrizeCleared_ResetsCountdown()
    {
        _dir.Write(FireBotFileReader.PrizeFileName, "PS5");
        _sut.Tick();
        Advance(TimeSpan.FromSeconds(30));

        File.Delete(_dir.File(FireBotFileReader.PrizeFileName));
        _sut.Tick();
        Assert.False(_sut.Current.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(60), _sut.Current.Remaining);
    }

    [Fact]
    public void ManualReset_RestartsFromConfiguredDuration()
    {
        _dir.Write(FireBotFileReader.PrizeFileName, "PS5");
        _sut.Tick();
        Advance(TimeSpan.FromSeconds(30));

        _settings.Update(s => s.CountdownMinutes = 2);
        _sut.ResetTimer();
        Assert.Equal(TimeSpan.FromMinutes(2), _sut.Current.Remaining);
    }

    [Fact]
    public void DisablingTimer_HidesAndFreezes_ReEnablingRestarts()
    {
        _dir.Write(FireBotFileReader.PrizeFileName, "PS5");
        _sut.Tick();
        Advance(TimeSpan.FromSeconds(10));

        _settings.Update(s => s.CountdownTimerEnabled = false);
        Assert.False(_sut.Current.TimerEnabled);

        Advance(TimeSpan.FromSeconds(10));
        _settings.Update(s => s.CountdownTimerEnabled = true);
        Assert.True(_sut.Current.TimerEnabled);
        Assert.Equal(TimeSpan.FromSeconds(60), _sut.Current.Remaining);
    }

    [Fact]
    public void SnapshotChanged_FiresOnlyOnVisibleChange_AndIsolatesFaultySubscribers()
    {
        var received = new List<GiveawaySnapshot>();
        _sut.SnapshotChanged += _ => throw new InvalidOperationException("broken circuit");
        _sut.SnapshotChanged += received.Add;

        _dir.Write(FireBotFileReader.PrizeFileName, "PS5");
        _sut.Tick();
        int afterStart = received.Count;
        Assert.True(afterStart >= 1);

        _time.Advance(TimeSpan.FromMilliseconds(100));
        _sut.Tick(); // same whole second, nothing changed
        Assert.Equal(afterStart, received.Count);

        Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(afterStart + 1, received.Count);
    }

    [Fact]
    public void HoursDurationFormatsCorrectly()
    {
        _settings.Update(s => { s.CountdownHours = 25; s.CountdownMinutes = 0; });
        _dir.Write(FireBotFileReader.PrizeFileName, "PS5");
        _sut.Tick();
        Assert.Equal(TimeSpan.FromHours(25), _sut.Current.Remaining);
    }
}
