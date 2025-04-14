using FirebotGiveawayObsOverlay.WebApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

public class CommandHandler : IDisposable
{
    private readonly IOptionsMonitor<TwitchSettings> _settingsMonitor;
    private TwitchSettings _currentSettings;
    private readonly TwitchService _twitchService;
    private readonly TimerService _timerService;
    private readonly GiveawayService _giveawayService;
    private readonly ILogger<CommandHandler> _logger;
    
    // Regular expression to parse the prize from the !startgiveaway command
    private Regex _startGiveawayRegex;

    public CommandHandler(
        IOptionsMonitor<TwitchSettings> settingsMonitor,
        TwitchService twitchService,
        TimerService timerService,
        GiveawayService giveawayService,
        ILogger<CommandHandler> logger)
    {
        _settingsMonitor = settingsMonitor;
        _currentSettings = _settingsMonitor.CurrentValue;
        _twitchService = twitchService;
        _timerService = timerService;
        _giveawayService = giveawayService;
        _logger = logger;
        
        // Create regex to match the command and capture the prize
        UpdateCommandRegex();
        
        // Subscribe to settings changes
        _settingsMonitor.OnChange(settings => {
            _logger.LogInformation("Twitch command settings changed");
            _currentSettings = settings;
            UpdateCommandRegex();
        });
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Subscribe to Twitch message events
        _twitchService.MessageReceived += OnMessageReceived;
    }

    private async void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var message = e.ChatMessage.Message.Trim();
        var username = e.ChatMessage.Username;
        var isMod = e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster;

        // Check for !join command
        if (message.Equals(_currentSettings.Commands.Join, StringComparison.OrdinalIgnoreCase))
        {
            await HandleJoinCommandAsync(username);
            return;
        }
        
        // Check for !drawwinner command (mod only)
        if (message.Equals(_currentSettings.Commands.DrawWinner, StringComparison.OrdinalIgnoreCase) && isMod)
        {
            HandleDrawWinnerCommand();
            return;
        }
        
        // Check for !startgiveaway command with prize (mod only)
        var startGiveawayMatch = _startGiveawayRegex.Match(message);
        if (startGiveawayMatch.Success && isMod)
        {
            var prize = startGiveawayMatch.Groups[1].Value.Trim();
            HandleStartGiveawayCommand(prize);
            return;
        }
    }

    private async Task HandleJoinCommandAsync(string username)
    {
        if (!_giveawayService.IsGiveawayActive)
        {
            _twitchService.SendMessage($"@{username}, there is no active giveaway at the moment.");
            return;
        }

        // Check if user is a follower if required
        if (_currentSettings.RequireFollower)
        {
            var (isFollower, meetsMinimumAge) = await _twitchService.CheckFollowerStatusAsync(username);
            
            _logger.LogDebug("User {Username} follower check: IsFollower={IsFollower}, MeetsMinimumAge={MeetsMinimumAge}",
                username, isFollower, meetsMinimumAge);
            
            if (!isFollower)
            {
                _twitchService.SendMessage($"@{username}, you need to be a follower to join the giveaway.");
                return;
            }
            
            if (!meetsMinimumAge)
            {
                _twitchService.SendMessage($"@{username}, you need to be a follower for at least {_currentSettings.FollowerMinimumAgeDays} days to join the giveaway.");
                _logger.LogInformation("User {Username} rejected from giveaway: does not meet minimum follower age requirement", username);
                return;
            }
        }

        // Add user to participants
        if (_giveawayService.AddEntry(username))
        {
            _twitchService.SendMessage($"@{username}, you have been entered into the giveaway for {_giveawayService.CurrentPrize}!");
            _logger.LogInformation("User {Username} entered the giveaway", username);
        }
        else
        {
            _twitchService.SendMessage($"@{username}, you are already entered in the giveaway.");
            _logger.LogDebug("User {Username} attempted to enter giveaway again", username);
        }
    }

    private void HandleStartGiveawayCommand(string prize)
    {
        if (_giveawayService.IsGiveawayActive)
        {
            _twitchService.SendMessage("A giveaway is already active. End it first with !drawwinner.");
            return;
        }

        if (string.IsNullOrWhiteSpace(prize))
        {
            _twitchService.SendMessage("Please specify a prize for the giveaway. Usage: !startgiveaway [prize]");
            return;
        }
        
        // Start the giveaway
        _giveawayService.StartGiveaway(prize);
        
        // Reset timer
        _timerService.ResetTimer();
        
        // Announce the giveaway
        _twitchService.SendMessage($"Giveaway started for {prize}! Type !join to enter.");
    }

    private void HandleDrawWinnerCommand()
    {
        if (!_giveawayService.IsGiveawayActive)
        {
            _twitchService.SendMessage("There is no active giveaway to draw a winner from.");
            return;
        }

        if (_giveawayService.EntryCount == 0)
        {
            _twitchService.SendMessage("No one has entered the giveaway yet.");
            return;
        }

        // Draw a winner
        var winner = _giveawayService.DrawWinner();
        
        if (winner != null)
        {
            // Announce winner
            _twitchService.SendMessage($"Congratulations @{winner}! You've won {_giveawayService.CurrentPrize}!");
        }
        else
        {
            _twitchService.SendMessage("Failed to draw a winner. Please try again.");
        }
    }

    private void UpdateCommandRegex()
    {
        _startGiveawayRegex = new Regex(
            $@"^{Regex.Escape(_currentSettings.Commands.StartGiveaway)}\s+(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        _logger.LogDebug("Updated command regex with StartGiveaway command: {Command}",
            _currentSettings.Commands.StartGiveaway);
    }
    
    public void Dispose()
    {
        _twitchService.MessageReceived -= OnMessageReceived;
    }
}