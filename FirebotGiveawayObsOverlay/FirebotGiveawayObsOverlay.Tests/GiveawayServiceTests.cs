using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace FirebotGiveawayObsOverlay.Tests
{
    public class GiveawayServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly string _testFolder;
        private readonly GiveawayService _giveawayService;

        public GiveawayServiceTests()
        {
            // Create a temporary test folder
            _testFolder = Path.Combine(Path.GetTempPath(), "FirebotGiveawayTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testFolder);

            // Set up mock configuration
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c.GetValue<string>("AppSettings:FireBotFileFolder", It.IsAny<string>()))
                .Returns(_testFolder);

            // Create the service with the mock configuration
            _giveawayService = new GiveawayService(_mockConfiguration.Object);
        }

        [Fact]
        public void StartGiveaway_ShouldSetGiveawayActive()
        {
            // Arrange
            string prize = "Test Prize";

            // Act
            bool result = _giveawayService.StartGiveaway(prize);

            // Assert
            result.Should().BeTrue();
            _giveawayService.IsGiveawayActive.Should().BeTrue();
            _giveawayService.CurrentPrize.Should().Be(prize);
            _giveawayService.EntryCount.Should().Be(0);

            // Verify files were created
            File.Exists(Path.Combine(_testFolder, "prize.txt")).Should().BeTrue();
            File.ReadAllText(Path.Combine(_testFolder, "prize.txt")).Should().Be(prize);
            File.Exists(Path.Combine(_testFolder, "winner.txt")).Should().BeTrue();
            File.ReadAllText(Path.Combine(_testFolder, "winner.txt")).Should().BeEmpty();
        }

        [Fact]
        public void StartGiveaway_WhenGiveawayAlreadyActive_ShouldReturnFalse()
        {
            // Arrange
            _giveawayService.StartGiveaway("First Prize");

            // Act
            bool result = _giveawayService.StartGiveaway("Second Prize");

            // Assert
            result.Should().BeFalse();
            _giveawayService.CurrentPrize.Should().Be("First Prize");
        }

        [Fact]
        public void AddEntry_ShouldAddParticipant()
        {
            // Arrange
            _giveawayService.StartGiveaway("Test Prize");
            string username = "testuser";

            // Act
            bool result = _giveawayService.AddEntry(username);

            // Assert
            result.Should().BeTrue();
            _giveawayService.EntryCount.Should().Be(1);
            _giveawayService.Entries.Should().Contain(username);

            // Verify file was updated
            File.Exists(Path.Combine(_testFolder, "giveaway.txt")).Should().BeTrue();
            File.ReadAllLines(Path.Combine(_testFolder, "giveaway.txt")).Should().Contain(username);
        }

        [Fact]
        public void AddEntry_WhenGiveawayNotActive_ShouldReturnFalse()
        {
            // Act
            bool result = _giveawayService.AddEntry("testuser");

            // Assert
            result.Should().BeFalse();
            _giveawayService.EntryCount.Should().Be(0);
        }

        [Fact]
        public void AddEntry_WhenUserAlreadyEntered_ShouldReturnFalse()
        {
            // Arrange
            _giveawayService.StartGiveaway("Test Prize");
            string username = "testuser";
            _giveawayService.AddEntry(username);

            // Act
            bool result = _giveawayService.AddEntry(username);

            // Assert
            result.Should().BeFalse();
            _giveawayService.EntryCount.Should().Be(1);
        }

        [Fact]
        public void DrawWinner_ShouldSelectRandomWinner()
        {
            // Arrange
            _giveawayService.StartGiveaway("Test Prize");
            _giveawayService.AddEntry("user1");
            _giveawayService.AddEntry("user2");
            _giveawayService.AddEntry("user3");

            // Act
            string? winner = _giveawayService.DrawWinner();

            // Assert
            winner.Should().NotBeNull();
            winner.Should().BeOneOf("user1", "user2", "user3");
            _giveawayService.IsGiveawayActive.Should().BeFalse();

            // Verify winner file was updated
            File.Exists(Path.Combine(_testFolder, "winner.txt")).Should().BeTrue();
            File.ReadAllText(Path.Combine(_testFolder, "winner.txt")).Should().Be(winner);
        }

        [Fact]
        public void DrawWinner_WhenNoEntries_ShouldReturnNull()
        {
            // Arrange
            _giveawayService.StartGiveaway("Test Prize");

            // Act
            string? winner = _giveawayService.DrawWinner();

            // Assert
            winner.Should().BeNull();
            _giveawayService.IsGiveawayActive.Should().BeTrue();
        }

        [Fact]
        public void DrawWinner_WhenGiveawayNotActive_ShouldReturnNull()
        {
            // Act
            string? winner = _giveawayService.DrawWinner();

            // Assert
            winner.Should().BeNull();
        }
    }
}