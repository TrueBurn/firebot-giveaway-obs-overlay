using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using FirebotGiveawayObsOverlay.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FirebotGiveawayObsOverlay.Tests
{
    public class SettingsServiceTests
    {
        private readonly Mock<IOptionsMonitor<AppSettings>> _mockAppSettingsMonitor;
        private readonly Mock<IOptionsMonitor<TwitchSettings>> _mockTwitchSettingsMonitor;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<ILogger<SettingsService>> _mockLogger;
        private readonly AppSettings _appSettings;
        private readonly TwitchSettings _twitchSettings;

        public SettingsServiceTests()
        {
            // Set up mock AppSettings
            _appSettings = new AppSettings
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

            // Set up mock TwitchSettings
            _twitchSettings = new TwitchSettings
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

            // Set up mocks
            _mockAppSettingsMonitor = new Mock<IOptionsMonitor<AppSettings>>();
            _mockAppSettingsMonitor.Setup(o => o.CurrentValue).Returns(_appSettings);

            _mockTwitchSettingsMonitor = new Mock<IOptionsMonitor<TwitchSettings>>();
            _mockTwitchSettingsMonitor.Setup(o => o.CurrentValue).Returns(_twitchSettings);

            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(e => e.ContentRootPath).Returns("test_content_root");

            _mockLogger = new Mock<ILogger<SettingsService>>();
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Act
            var service = new SettingsService(
                _mockAppSettingsMonitor.Object,
                _mockTwitchSettingsMonitor.Object,
                _mockEnvironment.Object,
                _mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
            service.CurrentSettings.Should().BeSameAs(_appSettings);
            service.CurrentTwitchSettings.Should().BeSameAs(_twitchSettings);
        }

        [Fact]
        public void CurrentSettings_ShouldReturnCurrentValue()
        {
            // Arrange
            var service = new SettingsService(
                _mockAppSettingsMonitor.Object,
                _mockTwitchSettingsMonitor.Object,
                _mockEnvironment.Object,
                _mockLogger.Object);

            // Act
            var settings = service.CurrentSettings;

            // Assert
            settings.Should().BeSameAs(_appSettings);
        }

        [Fact]
        public void CurrentTwitchSettings_ShouldReturnCurrentValue()
        {
            // Arrange
            var service = new SettingsService(
                _mockAppSettingsMonitor.Object,
                _mockTwitchSettingsMonitor.Object,
                _mockEnvironment.Object,
                _mockLogger.Object);

            // Act
            var settings = service.CurrentTwitchSettings;

            // Assert
            settings.Should().BeSameAs(_twitchSettings);
        }

        [Fact]
        public void SettingsChanged_ShouldBeRaisedWhenAppSettingsChange()
        {
            // Arrange
            Action<AppSettings, string>? onChangeCallback = null;
            _mockAppSettingsMonitor
                .Setup(o => o.OnChange(It.IsAny<Action<AppSettings, string>>()))
                .Callback<Action<AppSettings, string>>(callback => onChangeCallback = callback);

            var service = new SettingsService(
                _mockAppSettingsMonitor.Object,
                _mockTwitchSettingsMonitor.Object,
                _mockEnvironment.Object,
                _mockLogger.Object);

            var eventRaised = false;
            string? sectionName = null;
            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                sectionName = args.SectionName;
            };

            // Act
            onChangeCallback?.Invoke(_appSettings, "AppSettings");

            // Assert
            eventRaised.Should().BeTrue();
            sectionName.Should().Be("AppSettings");
        }

        [Fact]
        public void SettingsChanged_ShouldBeRaisedWhenTwitchSettingsChange()
        {
            // Arrange
            Action<TwitchSettings, string>? onChangeCallback = null;
            _mockTwitchSettingsMonitor
                .Setup(o => o.OnChange(It.IsAny<Action<TwitchSettings, string>>()))
                .Callback<Action<TwitchSettings, string>>(callback => onChangeCallback = callback);

            var service = new SettingsService(
                _mockAppSettingsMonitor.Object,
                _mockTwitchSettingsMonitor.Object,
                _mockEnvironment.Object,
                _mockLogger.Object);

            var eventRaised = false;
            string? sectionName = null;
            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                sectionName = args.SectionName;
            };

            // Act
            onChangeCallback?.Invoke(_twitchSettings, "TwitchSettings");

            // Assert
            eventRaised.Should().BeTrue();
            sectionName.Should().Be("TwitchSettings");
        }

        [Fact]
        public async Task UpdateAppSettingsAsync_ShouldUpdateSettingsInFile()
        {
            // Arrange
            var service = new MockableSettingsService();

            var newSettings = new AppSettings
            {
                FireBotFileFolder = "new_folder",
                Countdown = new CountdownSettings { Minutes = 10, Seconds = 30 },
                Layout = new LayoutSettings { PrizeSectionWidthPercent = 60 },
                Fonts = new FontSettings
                {
                    PrizeFontSizeRem = 4.0,
                    TimerFontSizeRem = 3.5,
                    EntriesFontSizeRem = 3.0
                }
            };

            var eventRaised = false;
            string? sectionName = null;
            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                sectionName = args.SectionName;
            };

            // Act
            await service.UpdateAppSettingsAsync(newSettings);

            // Assert
            service.CurrentSettings.FireBotFileFolder.Should().Be("new_folder");
            service.CurrentSettings.Countdown.Minutes.Should().Be(10);
            service.CurrentSettings.Countdown.Seconds.Should().Be(30);
            service.CurrentSettings.Layout.PrizeSectionWidthPercent.Should().Be(60);
            service.CurrentSettings.Fonts.PrizeFontSizeRem.Should().Be(4.0);
            service.CurrentSettings.Fonts.TimerFontSizeRem.Should().Be(3.5);
            service.CurrentSettings.Fonts.EntriesFontSizeRem.Should().Be(3.0);
            
            eventRaised.Should().BeTrue();
            sectionName.Should().Be("AppSettings");
        }

        [Fact]
        public async Task UpdateTwitchSettingsAsync_ShouldUpdateSettingsInFile()
        {
            // Arrange
            var service = new MockableSettingsService();

            var newSettings = new TwitchSettings
            {
                Channel = "new_channel",
                ClientId = "new_client_id",
                ClientSecret = "new_client_secret",
                Commands = new CommandSettings
                {
                    Join = "!enter",
                    StartGiveaway = "!start",
                    DrawWinner = "!draw"
                }
            };

            var eventRaised = false;
            string? sectionName = null;
            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                sectionName = args.SectionName;
            };

            // Act
            await service.UpdateTwitchSettingsAsync(newSettings);

            // Assert
            service.CurrentTwitchSettings.Channel.Should().Be("new_channel");
            service.CurrentTwitchSettings.ClientId.Should().Be("new_client_id");
            service.CurrentTwitchSettings.ClientSecret.Should().Be("new_client_secret");
            service.CurrentTwitchSettings.Commands.Join.Should().Be("!enter");
            service.CurrentTwitchSettings.Commands.StartGiveaway.Should().Be("!start");
            service.CurrentTwitchSettings.Commands.DrawWinner.Should().Be("!draw");
            
            eventRaised.Should().BeTrue();
            sectionName.Should().Be("TwitchSettings");
        }

        [Fact]
        public async Task UpdateCountdownTimeAsync_ShouldUpdateCountdownSettings()
        {
            // Arrange
            var service = new MockableSettingsService();
            int minutes = 15;
            int seconds = 45;

            var eventRaised = false;
            service.SettingsChanged += (sender, args) => eventRaised = true;

            // Act
            await service.UpdateCountdownTimeAsync(minutes, seconds);

            // Assert
            service.CurrentSettings.Countdown.Minutes.Should().Be(minutes);
            service.CurrentSettings.Countdown.Seconds.Should().Be(seconds);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateFireBotFileFolderAsync_ShouldUpdateFolderPath()
        {
            // Arrange
            var service = new MockableSettingsService();
            string newFolder = "new_folder_path";

            var eventRaised = false;
            service.SettingsChanged += (sender, args) => eventRaised = true;

            // Act
            await service.UpdateFireBotFileFolderAsync(newFolder);

            // Assert
            service.CurrentSettings.FireBotFileFolder.Should().Be(newFolder);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public async Task UpdatePrizeSectionWidthAsync_ShouldUpdateLayoutSettings()
        {
            // Arrange
            var service = new MockableSettingsService();
            int widthPercent = 80;

            var eventRaised = false;
            service.SettingsChanged += (sender, args) => eventRaised = true;

            // Act
            await service.UpdatePrizeSectionWidthAsync(widthPercent);

            // Assert
            service.CurrentSettings.Layout.PrizeSectionWidthPercent.Should().Be(widthPercent);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateFontSizesAsync_ShouldUpdateFontSettings()
        {
            // Arrange
            var service = new MockableSettingsService();
            double prizeFontSize = 4.5;
            double timerFontSize = 4.0;
            double entriesFontSize = 3.5;

            var eventRaised = false;
            service.SettingsChanged += (sender, args) => eventRaised = true;

            // Act
            await service.UpdateFontSizesAsync(prizeFontSize, timerFontSize, entriesFontSize);

            // Assert
            service.CurrentSettings.Fonts.PrizeFontSizeRem.Should().Be(prizeFontSize);
            service.CurrentSettings.Fonts.TimerFontSizeRem.Should().Be(timerFontSize);
            service.CurrentSettings.Fonts.EntriesFontSizeRem.Should().Be(entriesFontSize);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAppSettingsAsync_WhenFails_ShouldThrowException()
        {
            // Arrange
            var service = new MockableSettingsService();
            service.SetUpdateSettingsSuccess(false);

            var newSettings = new AppSettings
            {
                FireBotFileFolder = "new_folder"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAppSettingsAsync(newSettings));
        }

        [Fact]
        public async Task UpdateTwitchSettingsAsync_WhenFails_ShouldThrowException()
        {
            // Arrange
            var service = new MockableSettingsService();
            service.SetUpdateSettingsSuccess(false);

            var newSettings = new TwitchSettings
            {
                Channel = "new_channel"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateTwitchSettingsAsync(newSettings));
        }

    }
}