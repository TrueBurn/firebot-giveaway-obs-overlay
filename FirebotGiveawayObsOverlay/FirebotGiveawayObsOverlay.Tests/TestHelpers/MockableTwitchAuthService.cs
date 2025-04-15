using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FirebotGiveawayObsOverlay.Tests.TestHelpers;

/// <summary>
/// A testable version of TwitchAuthService that can be easily mocked
/// </summary>
public class MockableTwitchAuthService : TwitchAuthService
{
    private string _accessToken = "test_access_token";
    private bool _isAuthenticated = true;

    public MockableTwitchAuthService()
        : base(
              CreateDefaultOptionsMonitor(),
              CreateMockSettingsService(),
              CreateDefaultLogger())
    {
    }

    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
    }

    public void SetIsAuthenticated(bool isAuthenticated)
    {
        _isAuthenticated = isAuthenticated;
    }

    public override Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult<string?>(_accessToken);
    }

    public override bool IsAuthenticated => _isAuthenticated;

    private static IOptionsMonitor<TwitchSettings> CreateDefaultOptionsMonitor()
    {
        var settings = new TwitchSettings
        {
            Channel = "test_channel",
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret"
        };

        var optionsMonitor = new TestOptionsMonitor<TwitchSettings>(settings);
        return optionsMonitor;
    }

    private static ISettingsService CreateMockSettingsService()
    {
        var mockSettings = new Mock<ISettingsService>();
        
        // Set up default properties
        var appSettings = new AppSettings();
        var twitchSettings = new TwitchSettings
        {
            Channel = "test_channel",
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret"
        };
        
        mockSettings.Setup(s => s.CurrentSettings).Returns(appSettings);
        mockSettings.Setup(s => s.CurrentTwitchSettings).Returns(twitchSettings);
        
        return mockSettings.Object;
    }

    private static ILogger<TwitchAuthService> CreateDefaultLogger()
    {
        return new TestLogger<TwitchAuthService>();
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