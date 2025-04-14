# Technical Implementation Details

This document provides technical information about the Twitch chat integration feature in the Firebot Giveaway OBS Overlay application. It's intended for developers who need to understand, maintain, or extend the integration.

## Architecture Overview

The Twitch integration is built on a service-oriented architecture within the .NET Blazor web application. It uses the TwitchLib library to handle communication with Twitch's API and chat services.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  Firebot Giveaway OBS Overlay                │
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────────┐  │
│  │ TwitchSetup │    │ CommandHandler│   │ GiveawayService │  │
│  │   (UI)      │───▶│  (Processing)│───▶│  (Core Logic)   │  │
│  └─────────────┘    └─────────────┘    └─────────────────┘  │
│         │                  ▲                   │            │
│         │                  │                   │            │
│         ▼                  │                   ▼            │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────────┐  │
│  │TwitchSettings│    │TwitchService│    │   File System   │  │
│  │(Configuration)│◀──▶│ (Connection)│    │  (Integration)  │  │
│  └─────────────┘    └─────────────┘    └─────────────────┘  │
│                           │                                 │
└───────────────────────────┼─────────────────────────────────┘
                            │
                            ▼
                     ┌─────────────┐
                     │ Twitch API  │
                     │ & Chat      │
                     └─────────────┘
```

### Data Flow

1. User configures Twitch integration through the TwitchSetup UI
2. Configuration is stored in TwitchSettings
3. TwitchService establishes connection to Twitch chat
4. Incoming chat messages are processed by CommandHandler
5. Giveaway logic is handled by GiveawayService
6. Output files are written to the file system for OBS integration

## Key Classes and Responsibilities

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

2. **Rate Limiting**:
   - The application implements rate limiting to prevent Twitch timeouts
   - Default: 750 messages per 30-second period

3. **Permission Model**:
   - Moderator commands are restricted to users with moderator status
   - Follower requirements can be enabled to restrict giveaway participation

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