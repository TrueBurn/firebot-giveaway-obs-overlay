using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FirebotGiveawayObsOverlay.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly TempFolder _dir = new();
    private readonly FakeTimeProvider _time = new();
    private readonly UserSettingsService _userSettings;
    private readonly SettingsPersistenceService _persistence;
    private readonly SettingsService _sut;

    public SettingsTests()
    {
        _userSettings = new UserSettingsService(NullLogger<UserSettingsService>.Instance, _dir.File("usersettings.json"));
        _persistence = new SettingsPersistenceService(_time);
        _sut = new SettingsService(_userSettings, _persistence, NullLogger<SettingsService>.Instance);
        _sut.LoadFromFile(new AppSettings());
    }

    public void Dispose()
    {
        _persistence.Dispose();
        _dir.Dispose();
    }

    [Fact]
    public void Clone_IsDeep()
    {
        var a = new AppSettings();
        var b = a.Clone();
        b.Theme.PrimaryColor = "#000000";
        b.Logging.EnableFileLogging = false;
        Assert.NotEqual(a.Theme.PrimaryColor, b.Theme.PrimaryColor);
        Assert.True(a.Logging.EnableFileLogging);
        Assert.Empty(a.GetDifferences(a.Clone()));
    }

    [Fact]
    public void Update_PublishesNewSnapshot_OldSnapshotUnchanged()
    {
        var before = _sut.Current;
        _sut.Update(s => s.PrizeSectionWidthPercent = 60);

        Assert.Equal(75, before.PrizeSectionWidthPercent);
        Assert.Equal(60, _sut.Current.PrizeSectionWidthPercent);
        Assert.NotSame(before, _sut.Current);
    }

    [Fact]
    public void Update_FaultySubscriberDoesNotBreakOthers()
    {
        int calls = 0;
        _sut.OnSettingsChanged += () => throw new InvalidOperationException();
        _sut.OnSettingsChanged += () => calls++;

        _sut.Update(s => s.PrizeFontSizeRem = 2.0);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Update_IsDebounced_OnlyLatestReachesWriter()
    {
        _sut.Update(s => s.PrizeSectionWidthPercent = 60);
        _sut.Update(s => s.PrizeSectionWidthPercent = 61);
        _sut.Update(s => s.PrizeSectionWidthPercent = 62);

        Assert.False(_persistence.Reader.TryRead(out _));
        _time.Advance(TimeSpan.FromMilliseconds(SettingsPersistenceService.DebounceDelayMs));

        Assert.True(_persistence.Reader.TryRead(out var saved));
        Assert.Equal(62, saved.PrizeSectionWidthPercent);
        Assert.False(_persistence.Reader.TryRead(out _));
    }

    [Fact]
    public void ResetToDefaults_CancelsPendingSave()
    {
        _sut.Update(s => s.PrizeSectionWidthPercent = 60);
        _sut.ResetToDefaults();
        _time.Advance(TimeSpan.FromSeconds(5));

        Assert.False(_persistence.Reader.TryRead(out _));
        Assert.Equal(75, _sut.Current.PrizeSectionWidthPercent);
    }

    [Fact]
    public void ResetToDefaults_UsesAppSettingsJsonDefaults_NotHardcodedOnes()
    {
        _sut.LoadFromFile(new AppSettings { FireBotFileFolder = @"D:\Configured", CountdownMinutes = 59, CountdownSeconds = 59 });
        _sut.Update(s => { s.FireBotFileFolder = @"E:\Changed"; s.CountdownMinutes = 5; });

        _sut.ResetToDefaults();

        Assert.Equal(@"D:\Configured", _sut.Current.FireBotFileFolder);
        Assert.Equal(59, _sut.Current.CountdownMinutes);
        Assert.Empty(_sut.Current.GetDifferences(_sut.Defaults));
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsAtomically()
    {
        var settings = new AppSettings { PrizeFontSizeRem = 2.3, FireBotFileFolder = @"D:\Firebot" };
        settings.Theme.Name = "Custom";
        await _userSettings.SaveUserSettingsAsync(settings, TestContext.Current.CancellationToken);

        Assert.False(File.Exists(_userSettings.GetUserSettingsPath() + ".tmp"));
        var loaded = _userSettings.LoadUserSettings();
        Assert.NotNull(loaded);
        Assert.Empty(settings.GetDifferences(loaded));
    }

    [Fact]
    public void Load_CorruptFile_ReturnsNull()
    {
        File.WriteAllText(_userSettings.GetUserSettingsPath(), "{ not json");
        Assert.Null(_userSettings.LoadUserSettings());
    }
}
