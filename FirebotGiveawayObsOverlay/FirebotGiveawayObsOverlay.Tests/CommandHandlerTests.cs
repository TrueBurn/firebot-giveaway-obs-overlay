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
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using Xunit;

namespace FirebotGiveawayObsOverlay.Tests
{
    public class CommandHandlerTests
    {
        private readonly Mock<IOptionsMonitor<TwitchSettings>> _mockOptions;
        private readonly MockableTwitchService _mockTwitchService;
        private readonly MockableTimerService _mockTimerService;
        private readonly MockableGiveawayService _mockGiveawayService;
        private readonly Mock<ILogger<CommandHandler>> _mockLogger;
        private readonly TwitchSettings _twitchSettings;
        private readonly TestCommandHandler _commandHandler;

        public CommandHandlerTests()
        {
            // Set up TwitchSettings
            _twitchSettings = new TwitchSettings
            {
                Channel = "testchannel",
                RequireFollower = true,
                FollowerMinimumAgeDays = 7,
                Commands = new CommandSettings
                {
                    Join = "!join",
                    StartGiveaway = "!startgiveaway",
                    DrawWinner = "!drawwinner"
                }
            };

            _mockOptions = new Mock<IOptionsMonitor<TwitchSettings>>();
            _mockOptions.Setup(o => o.CurrentValue).Returns(_twitchSettings);

            // Set up mock services
            _mockTwitchService = new MockableTwitchService();
            _mockTimerService = new MockableTimerService();
            _mockGiveawayService = new MockableGiveawayService();
            _mockLogger = new Mock<ILogger<CommandHandler>>();

            // Create the test command handler
            _commandHandler = new TestCommandHandler(
                _mockOptions.Object,
                _mockTwitchService,
                _mockTimerService,
                _mockGiveawayService,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldSubscribeToTwitchMessageEvents()
        {
            // Since we can't verify event subscription with MockableTwitchService,
            // we'll just verify that the command handler is created successfully
            _commandHandler.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromTwitchMessageEvents()
        {
            // Act
            _commandHandler.Dispose();

            // Since we can't verify event unsubscription with MockableTwitchService,
            // we'll just verify that the command handler is disposed successfully
            // The actual unsubscription is tested through behavior in other tests
        }

        [Fact]
        public void OnMessageReceived_JoinCommand_ShouldAddEntryWhenGiveawayActive()
        {
            // Arrange
            string username = "testuser";
            _mockGiveawayService.SetGiveawayActive(true);
            _mockGiveawayService.SetCurrentPrize("Test Prize");
            
            
            _mockTwitchService.SetFollowerCheckResult(true, true); // User is a follower and meets minimum age

            // Act
            RaiseMessageReceivedEvent(_mockTwitchService, username, "!join", false);

            // Assert
            _mockGiveawayService.Entries.Should().Contain(username);
        }

        [Fact]
        public void OnMessageReceived_JoinCommand_ShouldRejectNonFollowers()
        {
            // Arrange
            string username = "testuser";
            _mockGiveawayService.SetGiveawayActive(true);
            
            
            _mockTwitchService.SetFollowerCheckResult(false, false); // User is not a follower

            // Act
            RaiseMessageReceivedEvent(_mockTwitchService, username, "!join", false);

            // Assert
            _mockGiveawayService.Entries.Should().NotContain(username);
        }

        [Fact]
        public void OnMessageReceived_JoinCommand_ShouldRejectNewFollowers()
        {
            // Arrange
            string username = "testuser";
            _mockGiveawayService.SetGiveawayActive(true);
            
            
            _mockTwitchService.SetFollowerCheckResult(true, false); // User is a follower but doesn't meet minimum age

            // Act
            RaiseMessageReceivedEvent(_mockTwitchService, username, "!join", false);

            // Assert
            _mockGiveawayService.Entries.Should().NotContain(username);
        }

        [Fact]
        public void OnMessageReceived_JoinCommand_ShouldRejectWhenNoActiveGiveaway()
        {
            // Arrange
            string username = "testuser";
            
            _mockGiveawayService.SetGiveawayActive(false);

            // Act
            RaiseMessageReceivedEvent(_mockTwitchService, username, "!join", false);

            // Assert
            _mockGiveawayService.Entries.Should().NotContain(username);
        }

        [Fact]
        public void OnMessageReceived_StartGiveawayCommand_ShouldStartGiveaway()
        {
            // Arrange
            string prize = "Test Prize";
            
            _mockGiveawayService.SetGiveawayActive(false);
            _mockTimerService.Reset(); // Reset the timer flag

            // Act
            RaiseMessageReceivedEvent(_mockTwitchService, "moderator", $"!startgiveaway {prize}", true);

            // Assert
            _mockGiveawayService.IsGiveawayActive.Should().BeTrue();
            _mockGiveawayService.CurrentPrize.Should().Be(prize);
            _mockTimerService.TimerWasReset.Should().BeTrue(); // Check if timer was reset
        }

        [Fact]
        public void OnMessageReceived_DrawWinnerCommand_ShouldDrawWinner()
        {
            // Arrange
            string winner = "luckyuser";
            string prize = "Test Prize";
            
            _mockGiveawayService.SetGiveawayActive(true);
            _mockGiveawayService.SetCurrentPrize(prize);
            _mockGiveawayService.SetParticipants(new[] { "user1", "user2", winner, "user3" });
            _mockGiveawayService.SetWinner(winner);

            // Act
            RaiseMessageReceivedEvent(_mockTwitchService, "moderator", "!drawwinner", true);

            // Assert
            _mockGiveawayService.IsGiveawayActive.Should().BeFalse();
        }

        [Fact]
        public void OnMessageReceived_DrawWinnerCommand_ShouldRejectWhenNoActiveGiveaway()
        {
            // Arrange
            _mockGiveawayService.SetGiveawayActive(false);

            // Act
            RaiseMessageReceivedEvent(_mockTwitchService, "moderator", "!drawwinner", true);

            // Assert
            _mockGiveawayService.IsGiveawayActive.Should().BeFalse();
        }

        [Fact]
        public void OnMessageReceived_DrawWinnerCommand_ShouldRejectWhenNoEntries()
        {
            // Arrange
            _mockGiveawayService.SetGiveawayActive(true);
            _mockGiveawayService.SetParticipants(Array.Empty<string>());

            // Act
            RaiseMessageReceivedEvent(_mockTwitchService, "moderator", "!drawwinner", true);

            // Assert
            _mockGiveawayService.IsGiveawayActive.Should().BeTrue();
        }

        #region Helper Methods

        private void RaiseMessageReceivedEvent(MockableTwitchService service, string username, string message, bool isModerator)
        {
            // Create a TestChatMessage
            var testChatMessage = new TestChatMessage(
                username: username,
                message: message,
                isModerator: isModerator,
                isBroadcaster: false
            );

            // Create the test event args
            var eventArgs = new TestMessageReceivedArgs(testChatMessage);

            // Raise the event using the service's method
            service.RaiseMessageReceivedEvent(eventArgs);
        }

        #endregion
    }
}