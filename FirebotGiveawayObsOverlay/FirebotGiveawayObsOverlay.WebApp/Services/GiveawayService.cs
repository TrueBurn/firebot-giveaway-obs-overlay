using FirebotGiveawayObsOverlay.WebApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FirebotGiveawayObsOverlay.WebApp.Services
{
    public class GiveawayService
    {
        private readonly IOptionsMonitor<AppSettings> _settings;
        private readonly ILogger<GiveawayService> _logger;
        private readonly HashSet<string> _participants = new(StringComparer.OrdinalIgnoreCase);
        private bool _giveawayActive = false;
        private string _currentPrize = string.Empty;
        private readonly Random _random = new();

        public GiveawayService(
            IOptionsMonitor<AppSettings> settings,
            ILogger<GiveawayService> logger)
        {
            _settings = settings;
            _logger = logger;
            
            // Subscribe to settings changes
            _settings.OnChange(updatedSettings => {
                _logger.LogInformation("Giveaway service detected settings change");
            });
        }

        /// <summary>
        /// Starts a new giveaway with the specified prize
        /// </summary>
        /// <param name="prize">The prize for the giveaway</param>
        /// <returns>True if the giveaway was started, false if a giveaway is already active</returns>
        public bool StartGiveaway(string prize)
        {
            if (_giveawayActive)
            {
                return false;
            }

            // Reset participants
            _participants.Clear();
            
            // Set prize
            _currentPrize = prize;
            
            // Update prize file
            UpdatePrizeFile();
            
            // Clear winner file
            UpdateWinnerFile(string.Empty);
            
            // Set giveaway as active
            _giveawayActive = true;
            
            return true;
        }

        /// <summary>
        /// Adds a user to the giveaway
        /// </summary>
        /// <param name="username">The username to add</param>
        /// <returns>True if the user was added, false if they were already entered or the giveaway is not active</returns>
        public bool AddEntry(string username)
        {
            if (!_giveawayActive)
            {
                return false;
            }

            // Add user to participants
            if (_participants.Add(username))
            {
                UpdateEntriesFile();
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Draws a winner from the entries
        /// </summary>
        /// <returns>The winner's username, or null if there are no entries or the giveaway is not active</returns>
        public string? DrawWinner()
        {
            if (!_giveawayActive || _participants.Count == 0)
            {
                return null;
            }

            // Select a random winner
            var winnerIndex = _random.Next(_participants.Count);
            var winner = _participants.ElementAt(winnerIndex);

            // Update winner file
            UpdateWinnerFile(winner);

            // End giveaway
            _giveawayActive = false;

            return winner;
        }

        /// <summary>
        /// Gets the current prize
        /// </summary>
        public string CurrentPrize => _currentPrize;

        /// <summary>
        /// Gets whether a giveaway is currently active
        /// </summary>
        public bool IsGiveawayActive => _giveawayActive;

        /// <summary>
        /// Gets the current number of entries
        /// </summary>
        public int EntryCount => _participants.Count;

        /// <summary>
        /// Gets all current entries
        /// </summary>
        public IEnumerable<string> Entries => _participants.ToList();

        private void UpdatePrizeFile()
        {
            try
            {
                string filePath = Path.Combine(_settings.CurrentValue.FireBotFileFolder, "prize.txt");
                File.WriteAllText(filePath, _currentPrize);
                _logger.LogInformation("Updated prize file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating prize file: {ex.Message}");
            }
        }

        private void UpdateWinnerFile(string winner)
        {
            try
            {
                string filePath = Path.Combine(_settings.CurrentValue.FireBotFileFolder, "winner.txt");
                File.WriteAllText(filePath, winner);
                _logger.LogInformation("Updated winner file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating winner file: {ex.Message}");
            }
        }

        private void UpdateEntriesFile()
        {
            try
            {
                string filePath = Path.Combine(_settings.CurrentValue.FireBotFileFolder, "giveaway.txt");
                File.WriteAllLines(filePath, _participants);
                _logger.LogInformation("Updated entries file: {FilePath} with {Count} entries", filePath, _participants.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating entries file: {ex.Message}");
            }
        }
    }
}