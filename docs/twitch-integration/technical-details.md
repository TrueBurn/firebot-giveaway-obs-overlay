# Technical Implementation Details

This document provides technical information about the Twitch chat integration feature in the Firebot Giveaway OBS Overlay application. It's intended for developers who need to understand, maintain, or extend the integration.

## Architecture Overview

The Twitch integration is built on a service-oriented architecture within the .NET Blazor web application. It uses the TwitchLib library to handle communication with Twitch's API and chat services, and implements the Device Code Grant Flow for authentication.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Firebot Giveaway OBS Overlay                        │
│                                                                         │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────────┐              │
│  │ TwitchSetup │    │ CommandHandler│   │ GiveawayService │              │
│  │   (UI)      │───▶│  (Processing)│───▶│  (Core Logic)   │              │
│  └─────────────┘    └─────────────┘    └─────────────────┘              │
│         │                  ▲                   │                        │
│         │                  │                   │                        │
│         ▼                  │                   ▼                        │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────────┐              │
│  │TwitchSettings│    │TwitchService│    │   File System   │              │
│  │(Configuration)│◀──▶│ (Connection)│    │  (Integration)  │              │
│  └─────────────┘    └─────────────┘    └─────────────────┘              │
│         ▲                  ▲                                            │
│         │                  │                                            │
│         │                  │                                            │
│         │           ┌─────────────┐                                     │
│         └───────────│TwitchAuthService                                  │
│                     │(Authentication)│                                   │
│                     └─────────────┘                                     │
│                           │                                             │
└───────────────────────────┼─────────────────────────────────────────────┘
                            │
                            ▼
                     ┌─────────────┐
                     │ Twitch API  │
                     │ & Chat      │
                     └─────────────┘
```

### Data Flow

1. User configures Twitch integration through the TwitchSetup UI
2. User authenticates with Twitch using the Device Code Grant Flow via TwitchAuthService
3. Authentication tokens are securely stored and managed by TwitchAuthService
4. Configuration is stored in TwitchSettings
5. TwitchService uses the access token from TwitchAuthService to establish connection to Twitch chat
6. Incoming chat messages are processed by CommandHandler
7. Giveaway logic is handled by GiveawayService
8. Output files are written to the file system for OBS integration

## Key Classes and Responsibilities

### TwitchAuthService

**Purpose**: Manages Twitch authentication using the Device Code Grant Flow and handles token lifecycle.

**Key Responsibilities**:
- Initiating the Device Code Grant Flow authentication process
- Managing access and refresh tokens
- Automatically refreshing tokens before they expire
- Validating token status
- Revoking tokens when logging out
- Providing authentication status updates

**Key Methods**:
- `InitiateAuthenticationAsync(bool useDefaultCredentials)`: Starts the authentication process
- `GetAccessTokenAsync()`: Retrieves the current access token
- `ValidateTokenAsync()`: Validates if the current token is valid
- `RefreshTokenAsync()`: Refreshes the access token using the refresh token
- `RevokeTokenAsync()`: Revokes the current token when logging out

**Events**:
- `AuthStatusChanged`: Fired when authentication status changes
- `DeviceCodeReceived`: Fired when a device code is received and user needs to authorize

**Authentication Modes**:
- Simple: Uses pre-registered application credentials
- Advanced: Uses custom application credentials provided by the user

### TwitchService

**Purpose**: Manages the connection to Twitch chat and provides methods for sending messages and checking follower status.

**Key Responsibilities**:
- Establishing and maintaining connection to Twitch chat
- Sending messages to the chat
- Checking follower status of users
- Providing events for chat messages and connection status

**Key Methods**:
- `ConnectAsync()`: Connects to Twitch chat
- `Disconnect()`: Disconnects from Twitch chat
- `SendMessage(string message)`: Sends a message to the chat
- `CheckFollowerStatusAsync(string username)`: Checks if a user is a follower and meets minimum age requirements
- `TestConnectionAsync(TwitchSettings settings)`: Tests connection with provided settings

**Events**:
- `MessageReceived`: Fired when a message is received in chat
- `UserJoined`: Fired when a user joins the channel
- `UserLeft`: Fired when a user leaves the channel
- `Connected`: Fired when successfully connected to Twitch
- `Disconnected`: Fired when disconnected from Twitch

### CommandHandler

**Purpose**: Processes commands from Twitch chat and triggers appropriate actions.

**Key Responsibilities**:
- Parsing commands from chat messages
- Validating user permissions for commands
- Executing command logic
- Responding to users in chat

**Key Methods**:
- `OnMessageReceived(object? sender, OnMessageReceivedArgs e)`: Entry point for processing chat messages
- `HandleJoinCommandAsync(string username)`: Processes the !join command
- `HandleStartGiveawayCommand(string prize)`: Processes the !startgiveaway command
- `HandleDrawWinnerCommand()`: Processes the !drawwinner command

### GiveawayService

**Purpose**: Manages the giveaway state, participants, and winner selection.

**Key Responsibilities**:
- Starting and ending giveaways
- Managing participant entries
- Selecting winners
- Writing giveaway data to files for OBS integration

**Key Methods**:
- `StartGiveaway(string prize)`: Starts a new giveaway
- `AddEntry(string username)`: Adds a user to the giveaway
- `DrawWinner()`: Selects a random winner from entries
- `UpdatePrizeFile()`, `UpdateWinnerFile()`, `UpdateEntriesFile()`: Write data to files for OBS

**Properties**:
- `CurrentPrize`: Gets the current prize
- `IsGiveawayActive`: Gets whether a giveaway is currently active
- `EntryCount`: Gets the current number of entries
- `Entries`: Gets all current entries

### TwitchSettings

**Purpose**: Stores configuration for the Twitch integration.

**Key Properties**:
- `Enabled`: Whether Twitch integration is enabled
- `Channel`: The Twitch channel name
- `ClientId`: The Twitch API Client ID
- `ClientSecret`: The Twitch API Client Secret
- `RedirectUri`: The OAuth redirect URI
- `Commands`: Custom command settings
- `RequireFollower`: Whether to require users to be followers
- `FollowerMinimumAgeDays`: Minimum follow age in days

### CommandSettings

**Purpose**: Stores customizable command text.

**Key Properties**:
- `Join`: The command for joining giveaways
- `StartGiveaway`: The command for starting giveaways
- `DrawWinner`: The command for drawing winners

## Dependencies

The Twitch integration relies on the following external dependencies:

1. **TwitchLib**: A .NET library for interacting with Twitch API and chat
   - TwitchLib.Client: For chat interaction
   - TwitchLib.Api: For API calls (follower checks)
   - TwitchLib.Communication: For WebSocket communication

2. **System.IO**: For file operations related to OBS integration

3. **System.Net.Http**: For HTTP requests to Twitch OAuth endpoints

4. **System.Text.Json**: For serializing and deserializing JSON responses

## File-Based Integration with OBS

The application uses a file-based approach to integrate with OBS:

### File Structure

- `prize.txt`: Contains the current giveaway prize
- `winner.txt`: Contains the username of the most recent winner
- `giveaway.txt`: Contains a list of all current giveaway entries

### File Location

The default location for these files is `G:\Giveaway`, but this can be configured in `appsettings.json` under the `AppSettings:FireBotFileFolder` key.

### OBS Integration

OBS reads these files using Text Source (GDI+) elements:
1. Add a Text (GDI+) source to your scene
2. Enable "Read from file"
3. Browse to the appropriate file in the FireBot file folder
4. Configure text formatting as desired

## Security Considerations

1. **API Credentials**:
   - Client Secret is stored in `appsettings.json` and should be protected
   - Consider using environment variables or secret management for production
   - Simple mode uses pre-registered application credentials
   - Advanced mode allows users to provide their own credentials

2. **Token Security**:
   - Access and refresh tokens are stored securely
   - Tokens are automatically refreshed before expiration
   - Tokens can be revoked when logging out

3. **Rate Limiting**:
   - The application implements rate limiting to prevent Twitch timeouts
   - Default: 750 messages per 30-second period

4. **Permission Model**:
   - Moderator commands are restricted to users with moderator status
   - Follower requirements can be enabled to restrict giveaway participation

5. **OAuth Scopes**:
   - The application requests only the necessary scopes:
     - chat:read - For reading chat messages
     - chat:edit - For sending messages to chat
     - channel:read:subscriptions - For checking subscription status
     - channel:read:predictions - For reading prediction data
     - channel:read:polls - For reading poll data

## Extension Points

The architecture allows for several extension points:

1. **Additional Commands**:
   - Extend the CommandHandler class to add new commands
   - Update the TwitchSettings model to include new command text

2. **Enhanced Follower Requirements**:
   - The follower checking system could be extended with additional criteria
   - Subscription status could be added as another requirement

3. **Advanced Winner Selection**:
   - The random selection algorithm could be replaced with weighted selection
   - Additional winner selection criteria could be implemented

4. **UI Enhancements**:
   - The TwitchSetup page could be extended with additional configuration options
   - Real-time status display could be added to show current connection state

## Implementation Notes

### Connection Handling

The TwitchService uses WebSocketClient from TwitchLib.Communication for reliable connection:

```csharp
var clientOptions = new ClientOptions
{
    MessagesAllowedInPeriod = 750,
    ThrottlingPeriod = TimeSpan.FromSeconds(30)
};

var webSocketClient = new WebSocketClient(clientOptions);
_client = new TwitchClient(webSocketClient);
```

### Command Parsing

Commands are parsed using regular expressions to extract parameters:

```csharp
_startGiveawayRegex = new Regex(
    $@"^{Regex.Escape(_settings.Commands.StartGiveaway)}\s+(.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);
```

### Follower Checking

Follower status is checked using the Twitch API:

```csharp
var follows = await _api.Helix.Users.GetUsersFollowsAsync(fromId: userId, toId: broadcasterId);
var followDate = follows.Follows[0].FollowedAt;
var followAge = DateTime.UtcNow - followDate;
var meetsMinimumAge = followAge.TotalDays >= _settings.FollowerMinimumAgeDays;
```

### Winner Selection

Winners are selected using a cryptographically secure random number generator:

```csharp
var winnerIndex = _random.Next(_participants.Count);
var winner = _participants.ElementAt(winnerIndex);
```

## Performance Considerations

1. **Memory Usage**:
   - Participant list is stored in memory using a HashSet for efficient lookups
   - For very large giveaways, consider implementing pagination or database storage

2. **API Rate Limits**:
   - Twitch API has rate limits that may affect follower checking
   - Consider implementing caching for follower status

3. **File I/O**:
   - File operations are performed synchronously and may block the UI thread
   - Consider implementing asynchronous file operations for better responsiveness

4. **Token Refresh**:
   - Tokens are refreshed 5 minutes before expiration to ensure continuous operation
   - Token refresh operations are performed asynchronously

## Device Code Grant Flow Implementation

The application implements the Device Code Grant Flow for Twitch authentication, which is particularly suitable for devices that lack a browser or have limited input capabilities.

### Authentication Flow

1. **Request Device Code**:
   - The application requests a device code from Twitch's OAuth endpoint
   - Twitch returns a device code, user code, verification URI, and expiration time

2. **User Authorization**:
   - The application displays the user code and verification URI to the user
   - The user navigates to the verification URI on a separate device and enters the user code
   - The user authorizes the application on Twitch's website

3. **Token Polling**:
   - The application polls Twitch's token endpoint at regular intervals
   - Once the user authorizes, Twitch returns an access token and refresh token

4. **Token Management**:
   - The access token is used for API requests and chat connection
   - The refresh token is used to obtain new access tokens when they expire
   - Tokens are securely stored and managed by the TwitchAuthService

### Token Storage and Management

1. **Storage Mechanism**:
   - Tokens are stored securely using environment variables (in the current implementation)
   - In a production environment, consider using a more secure storage mechanism

2. **Token Lifecycle**:
   - Access tokens expire after a set period (typically a few hours)
   - Refresh tokens are used to obtain new access tokens without requiring re-authentication
   - A timer is set to refresh tokens 5 minutes before they expire

3. **Token Validation**:
   - Tokens are validated before use to ensure they are still valid
   - If validation fails, the token is refreshed automatically

4. **Token Revocation**:
   - When logging out, tokens are revoked via Twitch's revoke endpoint
   - All stored token data is cleared