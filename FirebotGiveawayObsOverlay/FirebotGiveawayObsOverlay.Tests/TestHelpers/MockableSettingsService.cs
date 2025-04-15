using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FirebotGiveawayObsOverlay.Tests.TestHelpers;

/// <summary>
/// A testable version of SettingsService that can be easily mocked
/// </summary>
public class MockableSettingsService : SettingsService
{
    private AppSettings _currentSettings;
    private TwitchSettings _currentTwitchSettings;
    private bool _updateSettingsSuccess = true;

    public MockableSettingsService()
        : base(
              CreateDefaultAppSettingsMonitor(),
              CreateDefaultTwitchSettingsMonitor(),
              CreateDefaultWebHostEnvironment(),
              CreateDefaultLogger())
    {
        _currentSettings = new AppSettings
        {
            FireBotFileFolder = "test_folder",
            Countdown = new CountdownSettings { Minutes = 5, Seconds = 0 },
            Layout = new LayoutSettings { PrizeSectionWidthPercent = 75 },
            Fonts = new FontSettings 
            { 
                PrizeFontSizeRem = 3.5, 
                TimerFontSizeRem = 3.0, 
                EntriesFontSizeRem = 2.5 
            }
        };

        _currentTwitchSettings = new TwitchSettings
        {
            Channel = "test_channel",
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            Commands = new CommandSettings
            {
                Join = "!join",
                StartGiveaway = "!startgiveaway",
                DrawWinner = "!drawwinner"
            }
        };
    }

    public void SetUpdateSettingsSuccess(bool success)
    {
        _updateSettingsSuccess = success;
    }

    public void SetCurrentAppSettings(AppSettings settings)
    {
        _currentSettings = settings;
    }

    public void SetCurrentTwitchSettings(TwitchSettings settings)
    {
        _currentTwitchSettings = settings;
    }

    public new AppSettings CurrentSettings => _currentSettings;

    public new TwitchSettings CurrentTwitchSettings => _currentTwitchSettings;

    public new async Task UpdateAppSettingsAsync(AppSettings newSettings)
    {
        if (!_updateSettingsSuccess)
        {
            throw new InvalidOperationException("Failed to update app settings");
        }

        _currentSettings = newSettings;
        OnSettingsChanged("AppSettings");
        await Task.CompletedTask;
    }

    public new async Task UpdateTwitchSettingsAsync(TwitchSettings newSettings)
    {
        if (!_updateSettingsSuccess)
        {
            throw new InvalidOperationException("Failed to update Twitch settings");
        }

        _currentTwitchSettings = newSettings;
        OnSettingsChanged("TwitchSettings");
        await Task.CompletedTask;
    }

    public new async Task UpdateCountdownTimeAsync(int minutes, int seconds)
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = _currentSettings.FireBotFileFolder,
            Layout = _currentSettings.Layout,
            Fonts = _currentSettings.Fonts,
            Countdown = new CountdownSettings
            {
                Minutes = minutes,
                Seconds = seconds
            }
        };

        await UpdateAppSettingsAsync(settings);
    }

    public new async Task UpdateFireBotFileFolderAsync(string folderPath)
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = folderPath,
            Countdown = _currentSettings.Countdown,
            Layout = _currentSettings.Layout,
            Fonts = _currentSettings.Fonts
        };

        await UpdateAppSettingsAsync(settings);
    }

    public new async Task UpdatePrizeSectionWidthAsync(int widthPercent)
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = _currentSettings.FireBotFileFolder,
            Countdown = _currentSettings.Countdown,
            Fonts = _currentSettings.Fonts,
            Layout = new LayoutSettings
            {
                PrizeSectionWidthPercent = widthPercent
            }
        };

        await UpdateAppSettingsAsync(settings);
    }

    public new async Task UpdateFontSizesAsync(double prizeFontSize, double timerFontSize, double entriesFontSize)
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = _currentSettings.FireBotFileFolder,
            Countdown = _currentSettings.Countdown,
            Layout = _currentSettings.Layout,
            Fonts = new FontSettings
            {
                PrizeFontSizeRem = prizeFontSize,
                TimerFontSizeRem = timerFontSize,
                EntriesFontSizeRem = entriesFontSize
            }
        };

        await UpdateAppSettingsAsync(settings);
    }

    // Create a new event in the derived class
    public new event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    private void OnSettingsChanged(string sectionName)
    {
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(sectionName));
        // We don't call the base class method as it doesn't exist
    }

    private static IOptionsMonitor<AppSettings> CreateDefaultAppSettingsMonitor()
    {
        var settings = new AppSettings
        {
            FireBotFileFolder = "test_folder",
            Countdown = new CountdownSettings { Minutes = 5, Seconds = 0 },
            Layout = new LayoutSettings { PrizeSectionWidthPercent = 75 },
            Fonts = new FontSettings
            {
                PrizeFontSizeRem = 3.5,
                TimerFontSizeRem = 3.0,
                EntriesFontSizeRem = 2.5
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(settings);
        return optionsMonitor;
    }

    private static IOptionsMonitor<TwitchSettings> CreateDefaultTwitchSettingsMonitor()
    {
        var settings = new TwitchSettings
        {
            Channel = "test_channel",
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            Commands = new CommandSettings
            {
                Join = "!join",
                StartGiveaway = "!startgiveaway",
                DrawWinner = "!drawwinner"
            }
        };

        var optionsMonitor = new TestOptionsMonitor<TwitchSettings>(settings);
        return optionsMonitor;
    }

    private static IWebHostEnvironment CreateDefaultWebHostEnvironment()
    {
        return new TestWebHostEnvironment();
    }

    private static ILogger<SettingsService> CreateDefaultLogger()
    {
        return new TestLogger<SettingsService>();
    }

    private class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _currentValue;

        public TestOptionsMonitor(T currentValue)
        {
            _currentValue = currentValue;
        }

        public T CurrentValue => _currentValue;

        public T Get(string? name) => _currentValue;

        public IDisposable OnChange(Action<T, string> listener)
        {
            return new TestDisposable();
        }

        private class TestDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "test_webroot";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "test_contentroot";
        public string EnvironmentName { get; set; } = "Test";
        public IFileProvider WebRootFileProvider { get; set; } = new TestFileProvider();
        public IFileProvider ContentRootFileProvider { get; set; } = new TestFileProvider();
    }

    private class TestFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return new TestDirectoryContents();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return new TestFileInfo();
        }

        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter)
        {
            return new TestChangeToken();
        }
    }

    private class TestDirectoryContents : IDirectoryContents
    {
        public bool Exists => false;
        public IEnumerator<IFileInfo> GetEnumerator() => Enumerable.Empty<IFileInfo>().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class TestFileInfo : IFileInfo
    {
        public bool Exists => false;
        public long Length => 0;
        public string PhysicalPath => string.Empty;
        public string Name => string.Empty;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public bool IsDirectory => false;
        public Stream CreateReadStream() => Stream.Null;
    }

    private class TestChangeToken : Microsoft.Extensions.Primitives.IChangeToken
    {
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => new TestChangeDisposable();
        
        private class TestChangeDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    private class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => new TestDisposable();

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

        private class TestDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}