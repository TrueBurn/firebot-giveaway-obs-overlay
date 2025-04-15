using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace FirebotGiveawayObsOverlay.Tests.TestHelpers
{
    /// <summary>
    /// A testable version of TwitchService that can be easily mocked
    /// </summary>
    public class MockableTwitchService : TwitchService
    {
        private bool _isConnected;
        private (bool isFollower, bool meetsMinimumAge) _followerCheckResult = (true, true);

        // Original event for TwitchService compatibility
        public new event EventHandler<OnMessageReceivedArgs>? MessageReceived;
        
        // New event for test-specific message handling
        public event EventHandler<TestMessageReceivedArgs>? TestMessageReceived;

        public MockableTwitchService()
            : base(
                  CreateDefaultOptionsMonitor(),
                  CreateDefaultLogger(),
                  new MockableTwitchAuthService())
        {
        }

        public void SetConnected(bool isConnected)
        {
            _isConnected = isConnected;
        }

        public void SetFollowerCheckResult(bool isFollower, bool meetsMinimumAge)
        {
            _followerCheckResult = (isFollower, meetsMinimumAge);
        }

        public void RaiseMessageReceivedEvent(TestMessageReceivedArgs args)
        {
            // Raise the test-specific event
            TestMessageReceived?.Invoke(this, args);
        }

        public override bool IsConnected => _isConnected;

        public override void SendMessage(string message)
        {
            // Do nothing in the mock implementation
        }

        public override Task<(bool isFollower, bool meetsMinimumAge)> CheckFollowerStatusAsync(string username)
        {
            return Task.FromResult(_followerCheckResult);
        }

        private static IOptionsMonitor<TwitchSettings> CreateDefaultOptionsMonitor()
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

        private static ILogger<TwitchService> CreateDefaultLogger()
        {
            return new TestLogger<TwitchService>();
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
}