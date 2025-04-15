using FirebotGiveawayObsOverlay.WebApp.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Service for handling Twitch authentication using the Device Code Grant Flow
/// </summary>
public class TwitchAuthService : IDisposable
{
    private readonly IOptionsMonitor<TwitchSettings> _settingsMonitor;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TwitchAuthService> _logger;
    private readonly HttpClient _httpClient;
    private TwitchSettings _currentSettings;
    private Timer? _tokenRefreshTimer;
    private CancellationTokenSource? _authCancellationTokenSource;

    // Pre-registered application credentials for simple mode
    private const string DefaultClientId = "your-default-client-id";
    private const string DefaultClientSecret = "your-default-client-secret";

    // Twitch OAuth endpoints
    private const string TwitchOAuthBaseUrl = "https://id.twitch.tv/oauth2";
    private const string DeviceCodeEndpoint = "/device";
    private const string TokenEndpoint = "/token";
    private const string RevokeEndpoint = "/revoke";
    private const string ValidateEndpoint = "/validate";

    // Default scopes needed for the application
    private const string DefaultScopes = "chat:read chat:edit channel:read:subscriptions channel:read:predictions channel:read:polls";

    // Token storage keys
    private const string AccessTokenKey = "TwitchAccessToken";
    private const string RefreshTokenKey = "TwitchRefreshToken";
    private const string TokenExpiryKey = "TwitchTokenExpiry";
    private const string DeviceCodeKey = "TwitchDeviceCode";
    private const string UserCodeKey = "TwitchUserCode";
    private const string VerificationUriKey = "TwitchVerificationUri";
    private const string ExpiresInKey = "TwitchExpiresIn";
    private const string IntervalKey = "TwitchInterval";

    #region Events

    /// <summary>
    /// Event raised when authentication status changes
    /// </summary>
    public event EventHandler<AuthStatusChangedEventArgs>? AuthStatusChanged;

    /// <summary>
    /// Event raised when device code is received and user needs to authorize
    /// </summary>
    public event EventHandler<DeviceCodeReceivedEventArgs>? DeviceCodeReceived;

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether the user is currently authenticated
    /// </summary>
    public virtual bool IsAuthenticated { get; private set; }

    /// <summary>
    /// Gets a value indicating whether authentication is in progress
    /// </summary>
    public bool IsAuthenticating { get; private set; }

    /// <summary>
    /// Gets the current authentication mode (Simple or Advanced)
    /// </summary>
    public AuthMode CurrentAuthMode => 
        string.IsNullOrEmpty(_currentSettings.ClientId) || string.IsNullOrEmpty(_currentSettings.ClientSecret) 
        ? AuthMode.Simple 
        : AuthMode.Advanced;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitchAuthService"/> class
    /// </summary>
    /// <param name="settingsMonitor">The settings monitor</param>
    /// <param name="settingsService">The settings service</param>
    /// <param name="logger">The logger</param>
    public TwitchAuthService(
        IOptionsMonitor<TwitchSettings> settingsMonitor,
        ISettingsService settingsService,
        ILogger<TwitchAuthService> logger)
    {
        _settingsMonitor = settingsMonitor;
        _settingsService = settingsService;
        _logger = logger;
        _currentSettings = _settingsMonitor.CurrentValue;
        _httpClient = new HttpClient();

        // Subscribe to settings changes
        _settingsMonitor.OnChange(settings =>
        {
            _logger.LogInformation("Twitch settings changed");
            _currentSettings = settings;

            // If client ID or secret changed, we need to re-authenticate
            if (IsAuthenticated)
            {
                _ = ValidateTokenAsync();
            }
        });

        // Initialize authentication state
        _ = InitializeAuthStateAsync();
    }

    #region Public Methods

    /// <summary>
    /// Initiates the Device Code Grant Flow authentication process
    /// </summary>
    /// <param name="useDefaultCredentials">Whether to use default credentials (simple mode) or custom credentials (advanced mode)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InitiateAuthenticationAsync(bool useDefaultCredentials = false)
    {
        if (IsAuthenticating)
        {
            _logger.LogWarning("Authentication is already in progress");
            return;
        }

        try
        {
            IsAuthenticating = true;
            _authCancellationTokenSource = new CancellationTokenSource();

            // Get client ID and secret based on mode
            string clientId = useDefaultCredentials ? DefaultClientId : _currentSettings.ClientId;
            
            if (string.IsNullOrEmpty(clientId))
            {
                throw new InvalidOperationException("Client ID is not configured");
            }

            // Request device code
            var deviceCodeResponse = await RequestDeviceCodeAsync(clientId);
            
            // Store device code information
            await StoreAuthDataAsync(DeviceCodeKey, deviceCodeResponse.DeviceCode);
            await StoreAuthDataAsync(UserCodeKey, deviceCodeResponse.UserCode);
            await StoreAuthDataAsync(VerificationUriKey, deviceCodeResponse.VerificationUri);
            await StoreAuthDataAsync(ExpiresInKey, deviceCodeResponse.ExpiresIn.ToString());
            await StoreAuthDataAsync(IntervalKey, deviceCodeResponse.Interval.ToString());

            // Raise event with device code information
            DeviceCodeReceived?.Invoke(this, new DeviceCodeReceivedEventArgs(
                deviceCodeResponse.UserCode,
                deviceCodeResponse.VerificationUri,
                deviceCodeResponse.ExpiresIn));

            // Start polling for token
            _ = PollForTokenAsync(
                deviceCodeResponse.DeviceCode,
                useDefaultCredentials ? DefaultClientId : _currentSettings.ClientId,
                useDefaultCredentials ? DefaultClientSecret : _currentSettings.ClientSecret,
                deviceCodeResponse.Interval,
                _authCancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating authentication");
            IsAuthenticating = false;
            AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(false, ex.Message));
        }
    }

    /// <summary>
    /// Cancels the current authentication process
    /// </summary>
    public void CancelAuthentication()
    {
        if (!IsAuthenticating)
        {
            return;
        }

        _authCancellationTokenSource?.Cancel();
        IsAuthenticating = false;
        _logger.LogInformation("Authentication process cancelled");
        AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(false, "Authentication cancelled"));
    }

    /// <summary>
    /// Revokes the current access token
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task RevokeTokenAsync()
    {
        try
        {
            string? accessToken = await GetAuthDataAsync(AccessTokenKey);
            
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("No access token to revoke");
                return;
            }

            string clientId = string.IsNullOrEmpty(_currentSettings.ClientId) 
                ? DefaultClientId 
                : _currentSettings.ClientId;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["token"] = accessToken
            });

            var response = await _httpClient.PostAsync($"{TwitchOAuthBaseUrl}{RevokeEndpoint}", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Token revoked successfully");
                
                // Clear stored tokens
                await ClearAuthDataAsync();
                
                // Update state
                IsAuthenticated = false;
                
                // Stop refresh timer
                _tokenRefreshTimer?.Dispose();
                _tokenRefreshTimer = null;
                
                // Notify listeners
                AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(false, "Logged out"));
            }
            else
            {
                _logger.LogWarning("Failed to revoke token: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
        }
    }

    /// <summary>
    /// Gets the current access token
    /// </summary>
    /// <returns>The access token if authenticated, otherwise null</returns>
    public virtual async Task<string?> GetAccessTokenAsync()
    {
        if (!IsAuthenticated)
        {
            return null;
        }

        string? accessToken = await GetAuthDataAsync(AccessTokenKey);
        
        // Validate token if we have one
        if (!string.IsNullOrEmpty(accessToken))
        {
            bool isValid = await ValidateTokenAsync();
            if (!isValid)
            {
                return null;
            }
        }

        return accessToken;
    }

    /// <summary>
    /// Validates the current access token
    /// </summary>
    /// <returns>True if the token is valid, otherwise false</returns>
    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            string? accessToken = await GetAuthDataAsync(AccessTokenKey);
            
            if (string.IsNullOrEmpty(accessToken))
            {
                IsAuthenticated = false;
                return false;
            }

            // Set up request
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TwitchOAuthBaseUrl}{ValidateEndpoint}");
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);

            // Send request
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Token is valid");
                IsAuthenticated = true;
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("Token is invalid or expired, attempting to refresh");
                return await RefreshTokenAsync();
            }
            else
            {
                _logger.LogWarning("Token validation failed: {StatusCode}", response.StatusCode);
                IsAuthenticated = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            IsAuthenticated = false;
            return false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the authentication state from stored tokens
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task InitializeAuthStateAsync()
    {
        try
        {
            string? accessToken = await GetAuthDataAsync(AccessTokenKey);
            string? refreshToken = await GetAuthDataAsync(RefreshTokenKey);
            string? expiryString = await GetAuthDataAsync(TokenExpiryKey);

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogInformation("No stored tokens found");
                IsAuthenticated = false;
                return;
            }

            // Check if token is expired
            if (!string.IsNullOrEmpty(expiryString) && 
                DateTime.TryParse(expiryString, out DateTime expiry) && 
                expiry <= DateTime.UtcNow)
            {
                _logger.LogInformation("Stored token is expired, attempting to refresh");
                await RefreshTokenAsync();
            }
            else
            {
                // Validate token
                IsAuthenticated = await ValidateTokenAsync();
                
                if (IsAuthenticated && !string.IsNullOrEmpty(expiryString) && 
                    DateTime.TryParse(expiryString, out DateTime tokenExpiry))
                {
                    // Set up refresh timer
                    SetupTokenRefreshTimer(tokenExpiry);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing auth state");
            IsAuthenticated = false;
        }
    }

    /// <summary>
    /// Requests a device code from Twitch
    /// </summary>
    /// <param name="clientId">The client ID to use</param>
    /// <returns>The device code response</returns>
    private async Task<DeviceCodeResponse> RequestDeviceCodeAsync(string clientId)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scope"] = DefaultScopes
        });

        var response = await _httpClient.PostAsync($"{TwitchOAuthBaseUrl}{DeviceCodeEndpoint}", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var deviceCodeResponse = JsonSerializer.Deserialize<DeviceCodeResponse>(responseContent);

        if (deviceCodeResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize device code response");
        }

        _logger.LogInformation("Device code requested successfully");
        return deviceCodeResponse;
    }

    /// <summary>
    /// Polls for a token using the device code
    /// </summary>
    /// <param name="deviceCode">The device code</param>
    /// <param name="clientId">The client ID</param>
    /// <param name="clientSecret">The client secret</param>
    /// <param name="interval">The polling interval in seconds</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task PollForTokenAsync(
        string deviceCode, 
        string clientId, 
        string clientSecret, 
        int interval, 
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["device_code"] = deviceCode,
                        ["grant_type"] = "device_code"
                    });

                    var response = await _httpClient.PostAsync($"{TwitchOAuthBaseUrl}{TokenEndpoint}", content, cancellationToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                        if (tokenResponse == null)
                        {
                            throw new InvalidOperationException("Failed to deserialize token response");
                        }

                        // Calculate token expiry
                        var expiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

                        // Store tokens
                        await StoreAuthDataAsync(AccessTokenKey, tokenResponse.AccessToken);
                        await StoreAuthDataAsync(RefreshTokenKey, tokenResponse.RefreshToken);
                        await StoreAuthDataAsync(TokenExpiryKey, expiry.ToString("o"));

                        // Update state
                        IsAuthenticated = true;
                        IsAuthenticating = false;

                        // Set up refresh timer
                        SetupTokenRefreshTimer(expiry);

                        // Notify listeners
                        AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(true, "Authentication successful"));

                        _logger.LogInformation("Authentication successful");
                        return;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent);

                        if (errorResponse?.Error == "authorization_pending")
                        {
                            // User hasn't authorized yet, continue polling
                            _logger.LogDebug("Authorization pending, continuing to poll");
                        }
                        else if (errorResponse?.Error == "expired_token")
                        {
                            // Device code expired
                            _logger.LogWarning("Device code expired");
                            IsAuthenticating = false;
                            AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(false, "Authorization timed out"));
                            return;
                        }
                        else if (errorResponse?.Error == "access_denied")
                        {
                            // User denied authorization
                            _logger.LogWarning("User denied authorization");
                            IsAuthenticating = false;
                            AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(false, "Authorization denied"));
                            return;
                        }
                        else
                        {
                            // Other error
                            _logger.LogWarning("Error polling for token: {Error}", errorResponse?.Error);
                            IsAuthenticating = false;
                            AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(false, $"Error: {errorResponse?.Error}"));
                            return;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Polling was cancelled
                    _logger.LogInformation("Token polling cancelled");
                    IsAuthenticating = false;
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling for token");
                    IsAuthenticating = false;
                    AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(false, $"Error: {ex.Message}"));
                    return;
                }

                // Wait for the specified interval before polling again
                await Task.Delay(interval * 1000, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Polling was cancelled
            _logger.LogInformation("Token polling cancelled");
        }
        finally
        {
            IsAuthenticating = false;
        }
    }

    /// <summary>
    /// Refreshes the access token using the refresh token
    /// </summary>
    /// <returns>True if the token was refreshed successfully, otherwise false</returns>
    private async Task<bool> RefreshTokenAsync()
    {
        try
        {
            string? refreshToken = await GetAuthDataAsync(RefreshTokenKey);
            
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("No refresh token available");
                IsAuthenticated = false;
                return false;
            }

            string clientId = string.IsNullOrEmpty(_currentSettings.ClientId) 
                ? DefaultClientId 
                : _currentSettings.ClientId;
            
            string clientSecret = string.IsNullOrEmpty(_currentSettings.ClientSecret) 
                ? DefaultClientSecret 
                : _currentSettings.ClientSecret;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            });

            var response = await _httpClient.PostAsync($"{TwitchOAuthBaseUrl}{TokenEndpoint}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                if (tokenResponse == null)
                {
                    throw new InvalidOperationException("Failed to deserialize token response");
                }

                // Calculate token expiry
                var expiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

                // Store tokens
                await StoreAuthDataAsync(AccessTokenKey, tokenResponse.AccessToken);
                await StoreAuthDataAsync(RefreshTokenKey, tokenResponse.RefreshToken);
                await StoreAuthDataAsync(TokenExpiryKey, expiry.ToString("o"));

                // Update state
                IsAuthenticated = true;

                // Set up refresh timer
                SetupTokenRefreshTimer(expiry);

                _logger.LogInformation("Token refreshed successfully");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to refresh token: {StatusCode}", response.StatusCode);
                
                // If refresh fails, clear tokens and require re-authentication
                await ClearAuthDataAsync();
                IsAuthenticated = false;
                AuthStatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(false, "Session expired, please log in again"));
                
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            IsAuthenticated = false;
            return false;
        }
    }

    /// <summary>
    /// Sets up a timer to refresh the token before it expires
    /// </summary>
    /// <param name="expiry">The token expiry time</param>
    private void SetupTokenRefreshTimer(DateTime expiry)
    {
        // Dispose of existing timer
        _tokenRefreshTimer?.Dispose();

        // Calculate time until refresh (refresh 5 minutes before expiry)
        var timeUntilRefresh = expiry.AddMinutes(-5) - DateTime.UtcNow;
        
        // If token is already close to expiry, refresh immediately
        if (timeUntilRefresh <= TimeSpan.Zero)
        {
            _ = RefreshTokenAsync();
            return;
        }

        // Set up timer to refresh token
        _tokenRefreshTimer = new Timer(async _ =>
        {
            await RefreshTokenAsync();
        }, null, timeUntilRefresh, Timeout.InfiniteTimeSpan);

        _logger.LogInformation("Token refresh scheduled for {RefreshTime}", DateTime.UtcNow + timeUntilRefresh);
    }

    /// <summary>
    /// Stores authentication data
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task StoreAuthDataAsync(string key, string value)
    {
        // In a real implementation, this would use a secure storage mechanism
        // For now, we'll just use environment variables or a similar approach
        Environment.SetEnvironmentVariable(key, value);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets authentication data
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>The value if found, otherwise null</returns>
    private async Task<string?> GetAuthDataAsync(string key)
    {
        // In a real implementation, this would use a secure storage mechanism
        // For now, we'll just use environment variables or a similar approach
        var value = Environment.GetEnvironmentVariable(key);
        await Task.CompletedTask;
        return value;
    }

    /// <summary>
    /// Clears all authentication data
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task ClearAuthDataAsync()
    {
        await StoreAuthDataAsync(AccessTokenKey, string.Empty);
        await StoreAuthDataAsync(RefreshTokenKey, string.Empty);
        await StoreAuthDataAsync(TokenExpiryKey, string.Empty);
        await StoreAuthDataAsync(DeviceCodeKey, string.Empty);
        await StoreAuthDataAsync(UserCodeKey, string.Empty);
        await StoreAuthDataAsync(VerificationUriKey, string.Empty);
        await StoreAuthDataAsync(ExpiresInKey, string.Empty);
        await StoreAuthDataAsync(IntervalKey, string.Empty);
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the service
    /// </summary>
    public void Dispose()
    {
        _tokenRefreshTimer?.Dispose();
        _authCancellationTokenSource?.Dispose();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
    #region Nested Response Classes
    
    /// <summary>
    /// Response from the device code endpoint
    /// </summary>
    private class DeviceCodeResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = string.Empty;

        [JsonPropertyName("user_code")]
        public string UserCode { get; set; } = string.Empty;

        [JsonPropertyName("verification_uri")]
        public string VerificationUri { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("interval")]
        public int Interval { get; set; }
    }

    /// <summary>
    /// Response from the token endpoint
    /// </summary>
    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string[] Scope { get; set; } = Array.Empty<string>();

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Error response
    /// </summary>
    private class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
    
    #endregion
}

#region Helper Classes

/// <summary>
/// Authentication mode
/// </summary>
public enum AuthMode
{
    /// <summary>
    /// Simple mode uses pre-registered application credentials
    /// </summary>
    Simple,

    /// <summary>
    /// Advanced mode uses custom credentials
    /// </summary>
    Advanced
}

/// <summary>
/// Event arguments for authentication status changes
/// </summary>
public class AuthStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets a value indicating whether the user is authenticated
    /// </summary>
    public bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the status message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthStatusChangedEventArgs"/> class
    /// </summary>
    /// <param name="isAuthenticated">Whether the user is authenticated</param>
    /// <param name="message">The status message</param>
    public AuthStatusChangedEventArgs(bool isAuthenticated, string message)
    {
        IsAuthenticated = isAuthenticated;
        Message = message;
    }
}

/// <summary>
/// Event arguments for device code received
/// </summary>
public class DeviceCodeReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the user code
    /// </summary>
    public string UserCode { get; }

    /// <summary>
    /// Gets the verification URI
    /// </summary>
    public string VerificationUri { get; }

    /// <summary>
    /// Gets the expiry time in seconds
    /// </summary>
    public int ExpiresIn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceCodeReceivedEventArgs"/> class
    /// </summary>
    /// <param name="userCode">The user code</param>
    /// <param name="verificationUri">The verification URI</param>
    /// <param name="expiresIn">The expiry time in seconds</param>
    public DeviceCodeReceivedEventArgs(string userCode, string verificationUri, int expiresIn)
    {
        UserCode = userCode;
        VerificationUri = verificationUri;
        ExpiresIn = expiresIn;
    }
}

#endregion
