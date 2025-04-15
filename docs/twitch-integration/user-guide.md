# Twitch Integration User Guide

This guide provides detailed instructions on how to set up and use the Twitch chat integration feature in the Firebot Giveaway OBS Overlay application.

## Setting Up Twitch Integration

### Prerequisites

- A Twitch account
- Streamer or moderator permissions for the channel where you want to run giveaways
- Firebot Giveaway OBS Overlay application installed and running

### Authentication Modes

The application supports two authentication modes:

1. **Simple Authentication** - Uses a "Login with Twitch" button for quick and easy setup
2. **Advanced Authentication** - Requires your own Twitch API credentials for more control

#### Simple Authentication Mode

The Simple authentication mode is the easiest way to connect your Twitch account:

1. Launch the Firebot Giveaway OBS Overlay application
2. Navigate to the "Twitch Setup" page
3. Enable Twitch Integration by checking the "Enable Twitch Integration" checkbox
4. Select "Simple Authentication" mode
5. Enter your Twitch channel name (without the @ symbol)
6. Click the "Login with Twitch" button
7. A dialog will appear with:
   - A URL to visit (e.g., https://id.twitch.tv/oauth2/authorize)
   - A code to enter on that page
8. Open the URL in your web browser, log in to Twitch if prompted, and enter the code
9. Once authenticated, the application will show "Authenticated" with a green checkmark
10. Click "Test Connection" to verify everything is working correctly
11. Click "Save Settings" to complete the setup

If you need to log out or switch accounts, click the "Log Out" button next to the authentication status.

#### Advanced Authentication Mode

The Advanced authentication mode requires you to register your own application with Twitch:

##### Obtaining Twitch API Credentials

1. Go to the [Twitch Developer Console](https://dev.twitch.tv/console/apps)
2. Log in with your Twitch account
3. Click the "Register Your Application" button
4. Fill in the application details:
   - **Name**: Firebot Giveaway OBS Overlay (or any name you prefer)
   - **OAuth Redirect URLs**: http://localhost:3000/auth/callback (for local development) or your actual application URL
   - **Category**: Select "Application Integration"
5. Click "Create"
6. On the next screen, click "Manage" next to your newly created application
7. Note your **Client ID**
8. Click "New Secret" to generate a **Client Secret**
9. Copy both the Client ID and Client Secret - you'll need these for configuration

##### Configuring Advanced Authentication

1. Launch the Firebot Giveaway OBS Overlay application
2. Navigate to the "Twitch Setup" page
3. Enable Twitch Integration by checking the "Enable Twitch Integration" checkbox
4. Select "Advanced Authentication" mode
5. Fill in the following fields:
   - **Channel Name**: Your Twitch channel name (without the @ symbol)
   - **Client ID**: The Client ID you obtained from the Twitch Developer Console
   - **Client Secret**: The Client Secret you obtained from the Twitch Developer Console
   - **Redirect URI**: The same redirect URI you specified when registering your application

### Switching Between Authentication Modes

You can switch between Simple and Advanced authentication modes at any time:

1. Navigate to the "Twitch Setup" page
2. Select your preferred authentication mode (Simple or Advanced)
3. If switching from Advanced to Simple:
   - Click the "Login with Twitch" button to authenticate
4. If switching from Simple to Advanced:
   - Enter your Client ID, Client Secret, and Redirect URI
5. Click "Test Connection" to verify your settings
6. Click "Save Settings" to apply the changes

Note that switching authentication modes will require you to re-authenticate with Twitch.

### Command Settings

You can customize the commands used for the giveaway:

- **Join Command**: The command viewers use to enter the giveaway (default: `!join`)
- **Start Giveaway Command**: The command moderators use to start a giveaway (default: `!startgiveaway`)
- **Draw Winner Command**: The command moderators use to draw a winner (default: `!drawwinner`)

### Follower Requirements

You can restrict giveaway participation to followers only:

1. Check the "Require users to be followers" checkbox to enable follower-only mode
2. Set the "Minimum Follow Age (days)" to specify how long users must have been following the channel
   - Set to 0 for no minimum age requirement
   - Higher values can help prevent users from following just to enter the giveaway

### Testing the Connection

Before saving your settings, you can test the connection to ensure your credentials are correct:

1. Fill in all required fields (Channel Name and authentication details)
2. Click the "Test Connection" button
3. If successful, you'll see a "Connection successful!" message
4. If unsuccessful, verify your credentials and try again

### Saving Settings

Once you've configured the integration and tested the connection:

1. Click the "Save Settings" button
2. If Twitch integration is enabled, the application will attempt to connect to Twitch chat
3. You'll see a "Settings saved successfully!" message if everything is configured correctly

## Available Commands

### For Viewers

- **!join** (customizable): Allows viewers to enter the active giveaway
  - Usage: Type `!join` in the chat
  - Requirements: There must be an active giveaway, and the user must meet any follower requirements

### For Moderators and Broadcasters

- **!startgiveaway [prize]** (customizable): Starts a new giveaway with the specified prize
  - Usage: Type `!startgiveaway Gaming Keyboard` in the chat
  - Requirements: Moderator or broadcaster permissions, no active giveaway

- **!drawwinner** (customizable): Draws a random winner from the current giveaway entries
  - Usage: Type `!drawwinner` in the chat
  - Requirements: Moderator or broadcaster permissions, active giveaway with at least one entry

## Permission Requirements

The Twitch integration uses the following permission model:

- **Viewers**: Can only use the `!join` command to enter giveaways
- **Moderators and Broadcasters**: Can use all commands, including `!startgiveaway` and `!drawwinner`

The application automatically detects user roles based on Twitch's built-in permission system.

## Follower-Only Mode

When follower-only mode is enabled:

1. Only users who follow the channel can enter giveaways
2. If a minimum follow age is set, users must have been following for at least that many days
3. Users who don't meet these requirements will receive a message explaining why they can't enter

### Benefits of Follower-Only Mode

- Encourages viewers to follow your channel
- Reduces spam entries from non-followers
- Helps prevent users from creating multiple accounts to enter multiple times
- Can be used to reward loyal followers by setting a minimum follow age

### Limitations

- Requires valid Twitch API credentials to check follower status
- May exclude some viewers who don't want to follow
- API rate limits may affect performance with very large numbers of entries