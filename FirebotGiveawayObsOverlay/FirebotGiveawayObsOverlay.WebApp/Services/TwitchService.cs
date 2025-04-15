using FirebotGiveawayObsOverlay.WebApp.Models;
using Microsoft.Extensions.Options;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

public class TwitchService : IDisposable
{
    private readonly TwitchClient _client;
    private readonly IOptionsMonitor<TwitchSettings> _settingsMonitor;
    private TwitchSettings _currentSettings;
    private readonly TwitchAPI _api;
    private readonly TwitchAuthService _authService;
    private bool _isConnected;
    private readonly ILogger<TwitchService> _logger;

    public event EventHandler<OnMessageReceivedArgs>? MessageReceived;
    public event EventHandler<OnUserJoinedArgs>? UserJoined;
    public event EventHandler<OnUserLeftArgs>? UserLeft;
    public event EventHandler<OnConnectedArgs>? Connected;
    public event EventHandler<OnDisconnectedEventArgs>? Disconnected;

    public TwitchService(
        IOptionsMonitor<TwitchSettings> settingsMonitor,
        ILogger<TwitchService> logger,
        TwitchAuthService authService)
    {
        _settingsMonitor = settingsMonitor;
        _currentSettings = _settingsMonitor.CurrentValue;
        _logger = logger;
        _authService = authService;

        // Subscribe to settings changes
        _settingsMonitor.OnChange(settings =>
        {
            _logger.LogInformation("Twitch settings changed");
            _currentSettings = settings;

            // If connection state needs to change based on settings
            UpdateConnectionBasedOnSettings();
        });

        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };

        var webSocketClient = new WebSocketClient(clientOptions);
        _client = new TwitchClient(webSocketClient);

        // Initialize Twitch API
        _api = new TwitchAPI();
        
        // Configure API based on auth mode
        if (_currentSettings.AuthMode == AuthMode.Advanced &&
            !string.IsNullOrEmpty(_currentSettings.ClientId) &&
            !string.IsNullOrEmpty(_currentSettings.ClientSecret))
        {
            _api.Settings.ClientId = _currentSettings.ClientId;
            _api.Settings.Secret = _currentSettings.ClientSecret;
            _logger.LogInformation("Initialized Twitch API with advanced mode credentials");
        }
        else
        {
            // In Simple mode, we'll set the access token when connecting
            _logger.LogInformation("Initialized Twitch API for simple mode authentication");
        }

        // Set up event handlers
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnUserJoined += Client_OnUserJoined;
        _client.OnUserLeft += Client_OnUserLeft;
        _client.OnConnected += Client_OnConnected;
        _client.OnDisconnected += Client_OnDisconnected;
    }

    public async Task ConnectAsync()
    {
        if (_isConnected)
            return;

        try
        {
            ConnectionCredentials credentials;
            
            // Use different authentication methods based on the auth mode
            if (_currentSettings.AuthMode == AuthMode.Simple)
            {
                // Get access token from TwitchAuthService
                string? accessToken = await _authService.GetAccessTokenAsync();
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("No access token available. Please authenticate first.");
                    throw new InvalidOperationException("Authentication required. Please authenticate with Twitch first.");
                }
                
                // Create credentials using the access token
                credentials = new ConnectionCredentials(_currentSettings.Channel, accessToken);
                
                // Update API settings
                _api.Settings.AccessToken = accessToken;
                
                _logger.LogInformation("Using simple mode authentication with device code flow");
            }
            else
            {
                // Advanced mode - use the client secret directly
                credentials = new ConnectionCredentials(_currentSettings.Channel, _currentSettings.ClientSecret);
                _logger.LogInformation("Using advanced mode authentication");
            }
            
            _client.Initialize(credentials, _currentSettings.Channel);
            _logger.LogInformation("Connecting to Twitch channel: {Channel}", _currentSettings.Channel);
            _client.Connect();

            _isConnected = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to Twitch");
            throw;
        }
    }

    public void Disconnect()
    {
        if (!_isConnected)
            return;

        _client.Disconnect();
        _isConnected = false;
    }

    public virtual void SendMessage(string message)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Cannot send message: Not connected to Twitch chat");

        _client.SendMessage(_currentSettings.Channel, message);
        _logger.LogDebug("Sent message to Twitch chat: {Message}", message);
    }

    public virtual bool IsConnected => _isConnected;

    #region Event Handlers

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        MessageReceived?.Invoke(this, e);
    }

    private void Client_OnUserJoined(object? sender, OnUserJoinedArgs e)
    {
        UserJoined?.Invoke(this, e);
    }

    private void Client_OnUserLeft(object? sender, OnUserLeftArgs e)
    {
        UserLeft?.Invoke(this, e);
    }
    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        _logger.LogInformation("Connected to Twitch channel: {Channel}", _currentSettings.Channel);
        Connected?.Invoke(this, e);
    }

    private void Client_OnDisconnected(object? sender, OnDisconnectedEventArgs e)
    {
        _logger.LogInformation("Disconnected from Twitch");
        _isConnected = false;
        Disconnected?.Invoke(this, e);
    }

    private void UpdateConnectionBasedOnSettings()
    {
        // If settings changed from enabled to disabled, disconnect
        if (_isConnected && !_currentSettings.Enabled)
        {
            _logger.LogInformation("Disconnecting from Twitch due to settings change");
            Disconnect();
        }
        // If settings changed from disabled to enabled, connect
        else if (!_isConnected && _currentSettings.Enabled)
        {
            _logger.LogInformation("Connecting to Twitch due to settings change");
            _ = ConnectAsync();
        }
    }

    #endregion

    public void Dispose()
    {
        if (_isConnected)
        {
            Disconnect();
        }

        // Unsubscribe from events
        _client.OnMessageReceived -= Client_OnMessageReceived;
        _client.OnUserJoined -= Client_OnUserJoined;
        _client.OnUserLeft -= Client_OnUserLeft;
        _client.OnConnected -= Client_OnConnected;
        _client.OnDisconnected -= Client_OnDisconnected;
    }

    /// <summary>
    /// Tests the connection to Twitch using the provided settings
    /// </summary>
    /// <param name="settings">The Twitch settings to test</param>
    /// <returns>True if connection was successful, false otherwise</returns>
    public async Task<bool> TestConnectionAsync(TwitchSettings settings)
    {
        try
        {
            // Create a temporary client for testing
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            var webSocketClient = new WebSocketClient(clientOptions);
            var testClient = new TwitchClient(webSocketClient);

            // Set up a task completion source to track connection status
            var connectionTcs = new TaskCompletionSource<bool>();

            // Set up event handlers
            testClient.OnConnected += (sender, e) =>
            {
                connectionTcs.TrySetResult(true);
            };

            testClient.OnConnectionError += (sender, e) =>
            {
                connectionTcs.TrySetResult(false);
            };

            // Create credentials based on auth mode
            ConnectionCredentials credentials;
            
            if (settings.AuthMode == AuthMode.Simple)
            {
                // Get access token from TwitchAuthService for testing
                string? accessToken = await _authService.GetAccessTokenAsync();
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("No access token available for testing. Please authenticate first.");
                    return false;
                }
                
                credentials = new ConnectionCredentials(settings.Channel, accessToken);
                _logger.LogInformation("Testing connection with simple mode authentication");
            }
            else
            {
                // Advanced mode - use the client secret directly
                if (string.IsNullOrEmpty(settings.ClientSecret))
                {
                    _logger.LogWarning("No client secret provided for advanced mode authentication");
                    return false;
                }
                
                credentials = new ConnectionCredentials(settings.Channel, settings.ClientSecret);
                _logger.LogInformation("Testing connection with advanced mode authentication");
            }

            testClient.Initialize(credentials, settings.Channel);

            // Connect to Twitch
            testClient.Connect();

            // Wait for connection result with a timeout
            var connectionTask = connectionTcs.Task;
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

            var completedTask = await Task.WhenAny(connectionTask, timeoutTask);

            // Disconnect the test client
            testClient.Disconnect();

            // If the connection task completed, return its result
            if (completedTask == connectionTask)
            {
                return await connectionTask;
            }

            // If we timed out, return false
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Twitch connection: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Checks if a user is a follower of the channel and meets the minimum age requirement
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <returns>A tuple containing (isFollower, meetsMinimumAge)</returns>
    public virtual async Task<(bool isFollower, bool meetsMinimumAge)> CheckFollowerStatusAsync(string username)
    {
        // If follower check is not required, return true for both
        if (!_currentSettings.RequireFollower)
        {
            return (true, true);
        }
        
        // Ensure API is properly configured with access token if in simple mode
        if (_currentSettings.AuthMode == AuthMode.Simple)
        {
            string? accessToken = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("No access token available for follower check");
                return (true, true); // Default to allowing participation if we can't check
            }
            
            _api.Settings.AccessToken = accessToken;
        }

        try
        {
            // Get user ID from username
            var users = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { username });
            if (users.Users.Length == 0)
            {
                return (false, false);
            }

            var userId = users.Users[0].Id;

            // Get broadcaster ID
            var broadcasters = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { _currentSettings.Channel });
            if (broadcasters.Users.Length == 0)
            {
                _logger.LogWarning("Could not find broadcaster ID for channel: {Channel}", _currentSettings.Channel);
                return (false, false);
            }

            var broadcasterId = broadcasters.Users[0].Id;

            // Check if user follows the channel
            var follows = await _api.Helix.Users.GetUsersFollowsAsync(fromId: userId, toId: broadcasterId);

            if (follows.Follows.Length == 0)
            {
                return (false, false);
            }

            // Check if the follow meets the minimum age requirement
            var followDate = follows.Follows[0].FollowedAt;
            var followAge = DateTime.UtcNow - followDate;
            var meetsMinimumAge = followAge.TotalDays >= _currentSettings.FollowerMinimumAgeDays;

            _logger.LogDebug("User {Username} follow age: {FollowAgeDays} days, minimum required: {MinimumAgeDays} days",
                username, followAge.TotalDays, _currentSettings.FollowerMinimumAgeDays);

            return (true, meetsMinimumAge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking follower status for user {Username}", username);
            // In case of error, default to allowing the user to join
            return (true, true);
        }
    }
}