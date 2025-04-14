using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using FluentAssertions;
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
        private readonly Mock<IOptions<TwitchSettings>> _mockOptions;
        private readonly Mock<TwitchService> _mockTwitchService;
        private readonly Mock<TimerService> _mockTimerService;
        private readonly Mock<GiveawayService> _mockGiveawayService;
        private readonly TwitchSettings _twitchSettings;
        private readonly CommandHandler _commandHandler;

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

            _mockOptions = new Mock<IOptions<TwitchSettings>>();
            _mockOptions.Setup(o => o.Value).Returns(_twitchSettings);

            // Set up mock services
            _mockTwitchService = new Mock<TwitchService>(MockBehavior.Loose, _mockOptions.Object);
            _mockTimerService = new Mock<TimerService>();
            _mockGiveawayService = new Mock<GiveawayService>(MockBehavior.Loose, It.IsAny<Microsoft.Extensions.Configuration.IConfiguration>());

            // Create the command handler
            _commandHandler = new CommandHandler(
                _mockOptions.Object,
                _mockTwitchService.Object,
                _mockTimerService.Object,
                _mockGiveawayService.Object);
        }

        [Fact]
        public void Constructor_ShouldSubscribeToTwitchMessageEvents()
        {
            // Verify that the constructor subscribes to the MessageReceived event
            _mockTwitchService.VerifyAdd(s => s.MessageReceived += It.IsAny<EventHandler<OnMessageReceivedArgs>>());
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromTwitchMessageEvents()
        {
            // Act
            _commandHandler.Dispose();

            // Verify that Dispose unsubscribes from the MessageReceived event
            _mockTwitchService.VerifyRemove(s => s.MessageReceived -= It.IsAny<EventHandler<OnMessageReceivedArgs>>());
        }

        [Fact]
        public void OnMessageReceived_JoinCommand_ShouldAddEntryWhenGiveawayActive()
        {
            // Arrange
            string username = "testuser";
            
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(true);
            _mockGiveawayService.Setup(g => g.CurrentPrize).Returns("Test Prize");
            _mockGiveawayService.Setup(g => g.AddEntry(username)).Returns(true);
            
            _mockTwitchService.Setup(t => t.CheckFollowerStatusAsync(username))
                .ReturnsAsync((true, true)); // User is a follower and meets minimum age

            // Act
            RaiseMessageReceivedEvent(username, "!join", false);

            // Assert
            _mockGiveawayService.Verify(g => g.AddEntry(username));
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains(username) && msg.Contains("entered into the giveaway"))));
        }

        [Fact]
        public void OnMessageReceived_JoinCommand_ShouldRejectNonFollowers()
        {
            // Arrange
            string username = "testuser";
            
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(true);
            
            _mockTwitchService.Setup(t => t.CheckFollowerStatusAsync(username))
                .ReturnsAsync((false, false)); // User is not a follower

            // Act
            RaiseMessageReceivedEvent(username, "!join", false);

            // Assert
            _mockGiveawayService.Verify(g => g.AddEntry(username), Times.Never);
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains(username) && msg.Contains("need to be a follower"))));
        }

        [Fact]
        public void OnMessageReceived_JoinCommand_ShouldRejectNewFollowers()
        {
            // Arrange
            string username = "testuser";
            
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(true);
            
            _mockTwitchService.Setup(t => t.CheckFollowerStatusAsync(username))
                .ReturnsAsync((true, false)); // User is a follower but doesn't meet minimum age

            // Act
            RaiseMessageReceivedEvent(username, "!join", false);

            // Assert
            _mockGiveawayService.Verify(g => g.AddEntry(username), Times.Never);
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains(username) && msg.Contains("need to be a follower for at least"))));
        }

        [Fact]
        public void OnMessageReceived_JoinCommand_ShouldRejectWhenNoActiveGiveaway()
        {
            // Arrange
            string username = "testuser";
            
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(false);

            // Act
            RaiseMessageReceivedEvent(username, "!join", false);

            // Assert
            _mockGiveawayService.Verify(g => g.AddEntry(username), Times.Never);
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains(username) && msg.Contains("no active giveaway"))));
        }

        [Fact]
        public void OnMessageReceived_StartGiveawayCommand_ShouldStartGiveaway()
        {
            // Arrange
            string prize = "Test Prize";
            
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(false);
            _mockGiveawayService.Setup(g => g.StartGiveaway(prize)).Returns(true);

            // Act
            RaiseMessageReceivedEvent("moderator", $"!startgiveaway {prize}", true);

            // Assert
            _mockGiveawayService.Verify(g => g.StartGiveaway(prize));
            _mockTimerService.Verify(t => t.ResetTimer());
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains("Giveaway started") && msg.Contains(prize))));
        }

        [Fact]
        public void OnMessageReceived_StartGiveawayCommand_ShouldRejectWhenGiveawayActive()
        {
            // Arrange
            string prize = "Test Prize";
            
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(true);

            // Act
            RaiseMessageReceivedEvent("moderator", $"!startgiveaway {prize}", true);

            // Assert
            _mockGiveawayService.Verify(g => g.StartGiveaway(prize), Times.Never);
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains("already active"))));
        }

        [Fact]
        public void OnMessageReceived_StartGiveawayCommand_ShouldRejectNonModerators()
        {
            // Arrange
            string prize = "Test Prize";

            // Act
            RaiseMessageReceivedEvent("regularuser", $"!startgiveaway {prize}", false);

            // Assert
            _mockGiveawayService.Verify(g => g.StartGiveaway(prize), Times.Never);
            // Non-moderators are silently ignored for mod-only commands
        }

        [Fact]
        public void OnMessageReceived_DrawWinnerCommand_ShouldDrawWinner()
        {
            // Arrange
            string winner = "luckyuser";
            string prize = "Test Prize";
            
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(true);
            _mockGiveawayService.Setup(g => g.EntryCount).Returns(10);
            _mockGiveawayService.Setup(g => g.CurrentPrize).Returns(prize);
            _mockGiveawayService.Setup(g => g.DrawWinner()).Returns(winner);

            // Act
            RaiseMessageReceivedEvent("moderator", "!drawwinner", true);

            // Assert
            _mockGiveawayService.Verify(g => g.DrawWinner());
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains("Congratulations") && msg.Contains(winner) && msg.Contains(prize))));
        }

        [Fact]
        public void OnMessageReceived_DrawWinnerCommand_ShouldRejectWhenNoActiveGiveaway()
        {
            // Arrange
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(false);

            // Act
            RaiseMessageReceivedEvent("moderator", "!drawwinner", true);

            // Assert
            _mockGiveawayService.Verify(g => g.DrawWinner(), Times.Never);
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains("no active giveaway"))));
        }

        [Fact]
        public void OnMessageReceived_DrawWinnerCommand_ShouldRejectWhenNoEntries()
        {
            // Arrange
            _mockGiveawayService.Setup(g => g.IsGiveawayActive).Returns(true);
            _mockGiveawayService.Setup(g => g.EntryCount).Returns(0);

            // Act
            RaiseMessageReceivedEvent("moderator", "!drawwinner", true);

            // Assert
            _mockGiveawayService.Verify(g => g.DrawWinner(), Times.Never);
            _mockTwitchService.Verify(t => t.SendMessage(It.Is<string>(msg => 
                msg.Contains("No one has entered"))));
        }

        #region Helper Methods

        private void RaiseMessageReceivedEvent(string username, string message, bool isModerator)
        {
            // Create a mock ChatMessage
            var mockChatMessage = new Mock<ChatMessage>();
            mockChatMessage.Setup(m => m.Username).Returns(username);
            mockChatMessage.Setup(m => m.Message).Returns(message);
            mockChatMessage.Setup(m => m.IsModerator).Returns(isModerator);
            mockChatMessage.Setup(m => m.IsBroadcaster).Returns(false);

            // Create the event args
            var eventArgs = new OnMessageReceivedArgs { ChatMessage = mockChatMessage.Object };

            // Raise the event
            _mockTwitchService.Raise(s => s.MessageReceived += null, eventArgs);
        }

        #endregion
    }
}