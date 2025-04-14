# Command Reference

This document provides a detailed reference for all commands available in the Twitch chat integration feature of the Firebot Giveaway OBS Overlay application.

## Command Overview

The application supports three primary commands:

1. **Join Command**: Used by viewers to enter a giveaway
2. **Start Giveaway Command**: Used by moderators to start a new giveaway
3. **Draw Winner Command**: Used by moderators to select a winner

All commands are customizable through the Twitch Setup page.

## Viewer Commands

### Join Command

**Default**: `!join`

**Purpose**: Allows a viewer to enter the currently active giveaway.

**Usage**: 
```
!join
```

**Parameters**: None

**Requirements**:
- A giveaway must be currently active
- If follower-only mode is enabled:
  - User must be following the channel
  - User must meet the minimum follow age requirement (if set)

**Response**:
- Success: `@username, you have been entered into the giveaway for [prize]!`
- Already entered: `@username, you are already entered in the giveaway.`
- No active giveaway: `@username, there is no active giveaway at the moment.`
- Not a follower: `@username, you need to be a follower to join the giveaway.`
- Doesn't meet minimum follow age: `@username, you need to be a follower for at least [days] days to join the giveaway.`

**Notes**:
- Each user can only enter once per giveaway
- Usernames are stored case-insensitively to prevent duplicate entries

## Moderator Commands

These commands can only be used by the broadcaster or channel moderators.

### Start Giveaway Command

**Default**: `!startgiveaway`

**Purpose**: Starts a new giveaway with a specified prize.

**Usage**: 
```
!startgiveaway [prize]
```

**Parameters**:
- `[prize]`: (Required) The prize for the giveaway

**Examples**:
```
!startgiveaway Gaming Keyboard
!startgiveaway 1000 Channel Points
!startgiveaway "Steam Game Key for Cyberpunk 2077"
```

**Requirements**:
- User must be the broadcaster or a moderator
- No giveaway can be currently active

**Response**:
- Success: `Giveaway started for [prize]! Type !join to enter.`
- Already active: `A giveaway is already active. End it first with !drawwinner.`
- Missing prize: `Please specify a prize for the giveaway. Usage: !startgiveaway [prize]`

**System Actions**:
- Clears any previous entries
- Sets the current prize
- Updates the prize.txt file
- Clears the winner.txt file
- Marks the giveaway as active

### Draw Winner Command

**Default**: `!drawwinner`

**Purpose**: Randomly selects a winner from the current giveaway entries.

**Usage**: 
```
!drawwinner
```

**Parameters**: None

**Requirements**:
- User must be the broadcaster or a moderator
- A giveaway must be currently active
- At least one user must have entered the giveaway

**Response**:
- Success: `Congratulations @[winner]! You've won [prize]!`
- No active giveaway: `There is no active giveaway to draw a winner from.`
- No entries: `No one has entered the giveaway yet.`
- Error: `Failed to draw a winner. Please try again.`

**System Actions**:
- Randomly selects a winner from all entries
- Updates the winner.txt file with the winner's username
- Marks the giveaway as inactive

## Command Customization

All commands can be customized through the Twitch Setup page:

1. Navigate to the Twitch Setup page
2. Locate the "Command Settings" section
3. Modify the text for any command
4. Save the settings

**Important Notes**:
- Commands must start with a prefix character (typically `!`)
- Commands are case-insensitive (e.g., `!JOIN` and `!join` are treated the same)
- Avoid using spaces in command names
- Ensure commands don't conflict with other bots in your channel

## Command Permissions

The application uses Twitch's built-in permission system:

| Command | Broadcaster | Moderator | Regular Viewer |
|---------|------------|-----------|----------------|
| Join | ✓ | ✓ | ✓ |
| Start Giveaway | ✓ | ✓ | ✗ |
| Draw Winner | ✓ | ✓ | ✗ |

## Command Processing

When a message is received in chat, the application:

1. Checks if the message matches any of the configured commands
2. Verifies the user has permission to use the command
3. Executes the appropriate action based on the command
4. Sends a response message to the chat

For the Start Giveaway command, the application uses a regular expression to extract the prize:

```csharp
_startGiveawayRegex = new Regex(
    $@"^{Regex.Escape(_settings.Commands.StartGiveaway)}\s+(.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);
```

## Best Practices

1. **Choose Intuitive Commands**:
   - Use command names that are easy to remember and type
   - Consider your audience when choosing command names

2. **Announce Commands**:
   - Periodically remind viewers of available commands during the stream
   - Consider creating a command list in your channel description or as a chat command

3. **Avoid Command Conflicts**:
   - Check for conflicts with other bots in your channel
   - Use unique prefixes if necessary (e.g., `!gw-join` instead of `!join`)

4. **Moderate Effectively**:
   - Ensure all moderators know how to use the giveaway commands
   - Establish a process for handling giveaway issues during streams