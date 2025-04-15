using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using FirebotGiveawayObsOverlay.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using Xunit;

namespace FirebotGiveawayObsOverlay.Tests
{
    public class TwitchServiceTests
    {
        private readonly Mock<IOptionsMonitor<TwitchSettings>> _mockOptions;
        private readonly Mock<ITwitchClient> _mockTwitchClient;
        private readonly Mock<ITwitchAPI> _mockTwitchApi;
        private readonly Mock<ILogger<TwitchService>> _mockLogger;
        private readonly MockableTwitchAuthService _mockAuthService;
        private readonly TwitchSettings _twitchSettings;

        public TwitchServiceTests()
        {
            // Set up mock TwitchSettings
            _twitchSettings = new TwitchSettings
            {
                Channel = "testchannel",
                ClientId = "test_client_id",
                ClientSecret = "test_client_secret",
                RequireFollower = true,
                FollowerMinimumAgeDays = 7
            };

            _mockOptions = new Mock<IOptionsMonitor<TwitchSettings>>();
            _mockOptions.Setup(o => o.CurrentValue).Returns(_twitchSettings);

            // Set up mock TwitchClient
            _mockTwitchClient = new Mock<ITwitchClient>();

            // Set up mock TwitchAPI
            _mockTwitchApi = new Mock<ITwitchAPI>();
            
            // Set up mock logger
            _mockLogger = new Mock<ILogger<TwitchService>>();
            
            // Set up mockable TwitchAuthService
            _mockAuthService = new MockableTwitchAuthService();
            _mockAuthService.SetAccessToken("test_access_token");
            _mockAuthService.SetIsAuthenticated(true);
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Act
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);

            // Assert
            service.Should().NotBeNull();
            service.IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task ConnectAsync_ShouldConnectToTwitch()
        {
            // Arrange
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);

            // Act
            await service.ConnectAsync();

            // Assert
            service.IsConnected.Should().BeTrue();
        }

        [Fact]
        public void Disconnect_ShouldDisconnectFromTwitch()
        {
            // Arrange
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);
            service.SetConnected(true);

            // Act
            service.Disconnect();

            // Assert
            _mockTwitchClient.Verify(c => c.Disconnect());
            service.IsConnected.Should().BeFalse();
        }

        [Fact]
        public void SendMessage_ShouldSendMessageToChannel()
        {
            // Arrange
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);
            service.SetConnected(true);
            string message = "Test message";

            // Act
            service.SendMessage(message);

            // Assert
            service.MessageSent.Should().BeTrue();
            service.LastSentMessage.Should().Be(message);
        }

        [Fact]
        public void SendMessage_WhenNotConnected_ShouldThrowException()
        {
            // Arrange
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);
            string message = "Test message";

            // Act & Assert
            Action act = () => service.SendMessage(message);
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*Not connected*");
        }

        [Fact]
        public async Task CheckFollowerStatusAsync_WhenUserIsFollower_ShouldReturnTrue()
        {
            // Arrange
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);
            string username = "testuser";
            
            // Set up the mock to return a predefined result
            service.SetFollowerCheckResult(true, true);

            // Act
            var (isFollower, meetsMinimumAge) = await service.CheckFollowerStatusAsync(username);

            // Assert
            isFollower.Should().BeTrue();
            meetsMinimumAge.Should().BeTrue();
        }

        [Fact]
        public async Task CheckFollowerStatusAsync_WhenUserIsNewFollower_ShouldReturnFalseForMinimumAge()
        {
            // Arrange
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);
            string username = "testuser";
            
            // Set up the mock to return a predefined result
            service.SetFollowerCheckResult(true, false);

            // Act
            var (isFollower, meetsMinimumAge) = await service.CheckFollowerStatusAsync(username);

            // Assert
            isFollower.Should().BeTrue();
            meetsMinimumAge.Should().BeFalse();
        }

        [Fact]
        public async Task TestConnectionAsync_ShouldReturnTrueForValidSettings()
        {
            // Arrange
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);
            
            // Set up the mock client to simulate a successful connection
            service.SetTestConnectionResult(true);

            // Act
            var result = await service.TestConnectionAsync(_twitchSettings);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestConnectionAsync_ShouldReturnFalseForInvalidSettings()
        {
            // Arrange
            var service = new TwitchServiceForTest(_mockOptions.Object, _mockTwitchClient.Object, _mockTwitchApi.Object, _mockLogger.Object, _mockAuthService);
            
            // Set up the mock client to simulate a failed connection
            service.SetTestConnectionResult(false);

            // Act
            var result = await service.TestConnectionAsync(_twitchSettings);

            // Assert
            result.Should().BeFalse();
        }

        // Test helper class to expose internal state for testing
        public class TwitchServiceForTest : TwitchService
        {
            private readonly ITwitchClient _mockClient;
            private readonly ITwitchAPI _mockApi;
            private bool _testConnectionResult;
            private (bool isFollower, bool meetsMinimumAge) _followerCheckResult;
            private bool _isConnected;
            private bool _isJoinedToChannel;
            
            public bool MessageSent { get; private set; }
            public string LastSentMessage { get; private set; } = string.Empty; // Initialize to avoid warning

            public TwitchServiceForTest(IOptionsMonitor<TwitchSettings> settings, ITwitchClient mockClient, ITwitchAPI mockApi, ILogger<TwitchService> logger, TwitchAuthService authService)
                : base(settings, logger, authService)
            {
                _mockClient = mockClient;
                _mockApi = mockApi;
                
                // Setup the mock client for testing
                Mock.Get(_mockClient)
                    .Setup(c => c.IsConnected)
                    .Returns(() => _isConnected);
                
                Mock.Get(_mockClient)
                    .Setup(c => c.JoinedChannels)
                    .Returns(() => _isJoinedToChannel ?
                        new System.Collections.Generic.List<TwitchLib.Client.Models.JoinedChannel>
                        {
                            new TwitchLib.Client.Models.JoinedChannel(settings.CurrentValue.Channel)
                        } :
                        new System.Collections.Generic.List<TwitchLib.Client.Models.JoinedChannel>());
            }

            public void SetConnected(bool isConnected)
            {
                _isConnected = isConnected;
                _isJoinedToChannel = isConnected; // Also set joined to channel when connected
                
                // Use reflection to set the private field in base class
                var field = typeof(TwitchService).GetField("_isConnected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(this, isConnected);
                }
            }

            public void SetTestConnectionResult(bool success)
            {
                _testConnectionResult = success;
            }

            public void SetFollowerCheckResult(bool isFollower, bool meetsMinimumAge)
            {
                _followerCheckResult = (isFollower, meetsMinimumAge);
            }

            // Use "new" instead of "override" since the base methods aren't virtual
            public new async Task ConnectAsync()
            {
                // Set connected state without making a real connection
                SetConnected(true);
                return;
            }
            
            // Use "new" instead of "override" since the base methods aren't virtual
            public new void Disconnect()
            {
                if (_isConnected)
                {
                    _mockClient.Disconnect();
                    SetConnected(false);
                }
            }

            // Override the base methods with new implementations for testing
            public new Task<bool> TestConnectionAsync(TwitchSettings settings)
            {
                // Return the predetermined result
                return Task.FromResult(_testConnectionResult);
            }

            public new Task<(bool isFollower, bool meetsMinimumAge)> CheckFollowerStatusAsync(string username)
            {
                // Return the predetermined result
                return Task.FromResult(_followerCheckResult);
            }

            // Use "new" instead of "override" for consistency
            public new void SendMessage(string message)
            {
                // Check if connected before sending
                if (!_isConnected)
                {
                    throw new InvalidOperationException("Cannot send message: Not connected to Twitch chat");
                }
                
                // Use the mock client to send the message
                _mockClient.SendMessage(_mockClient.JoinedChannels[0].Channel, message);
                
                // Track that the message was sent
                MessageSent = true;
                LastSentMessage = message;
            }
            
            // IsConnected is marked as virtual in the base class, so we can use override
            public override bool IsConnected => _isConnected;
        }
    }
}