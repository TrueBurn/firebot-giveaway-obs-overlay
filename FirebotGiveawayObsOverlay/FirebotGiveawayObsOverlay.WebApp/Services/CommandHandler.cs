using FirebotGiveawayObsOverlay.WebApp.Models;
using Microsoft.Extensions.Options;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

public class CommandHandler : IDisposable
{
    private readonly TwitchSettings _settings;
    private readonly TwitchService _twitchService;
    private readonly TimerService _timerService;
    private readonly GiveawayService _giveawayService;
    
    // Regular expression to parse the prize from the !startgiveaway command
    private readonly Regex _startGiveawayRegex;

    public CommandHandler(
        IOptions<TwitchSettings> settings,
        TwitchService twitchService,
        TimerService timerService,
        GiveawayService giveawayService)
    {
        _settings = settings.Value;
        _twitchService = twitchService;
        _timerService = timerService;
        _giveawayService = giveawayService;
        
        // Create regex to match the command and capture the prize
        _startGiveawayRegex = new Regex(
            $@"^{Regex.Escape(_settings.Commands.StartGiveaway)}\s+(.+)$",
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
        if (message.Equals(_settings.Commands.Join, StringComparison.OrdinalIgnoreCase))
        {
            await HandleJoinCommandAsync(username);
            return;
        }
        
        // Check for !drawwinner command (mod only)
        if (message.Equals(_settings.Commands.DrawWinner, StringComparison.OrdinalIgnoreCase) && isMod)
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
        if (_settings.RequireFollower)
        {
            var (isFollower, meetsMinimumAge) = await _twitchService.CheckFollowerStatusAsync(username);
            
            if (!isFollower)
            {
                _twitchService.SendMessage($"@{username}, you need to be a follower to join the giveaway.");
                return;
            }
            
            if (!meetsMinimumAge)
            {
                _twitchService.SendMessage($"@{username}, you need to be a follower for at least {_settings.FollowerMinimumAgeDays} days to join the giveaway.");
                return;
            }
        }

        // Add user to participants
        if (_giveawayService.AddEntry(username))
        {
            _twitchService.SendMessage($"@{username}, you have been entered into the giveaway for {_giveawayService.CurrentPrize}!");
        }
        else
        {
            _twitchService.SendMessage($"@{username}, you are already entered in the giveaway.");
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

    public void Dispose()
    {
        _twitchService.MessageReceived -= OnMessageReceived;
    }
}