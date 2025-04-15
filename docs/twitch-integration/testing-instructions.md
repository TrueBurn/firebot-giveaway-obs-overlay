# Testing Instructions for Twitch Integration

This guide provides step-by-step instructions for testing the Twitch chat integration feature in the Firebot Giveaway OBS Overlay application. Following these procedures will help ensure that the integration is working correctly before using it in a live stream.

## Testing the Connection

### Prerequisites

- Completed Twitch integration setup (see [User Guide](user-guide.md))
- Access to the Twitch channel specified in the configuration

### Authentication Testing

#### Simple Authentication Mode (Button-Based)

1. **Login with Twitch Button Test**
   
   a. Navigate to the "Twitch Setup" page in the application
   b. Select "Simple Authentication" mode
   c. Click the "Login with Twitch" button
   d. Verify that a modal appears with a user code and verification URI
   e. Open the verification URI in a browser
   f. Enter the user code displayed in the application
   g. Authorize the application on Twitch
   h. Verify that the modal closes automatically and "Authenticated" appears next to the button
   i. Verify that the "Log Out" button appears

2. **Device Code Grant Flow Verification**
   
   a. During the authentication process, observe the progress bar in the modal
   b. Verify that the modal shows the remaining time before code expiration
   c. If you don't complete authentication before the timer expires, verify that an appropriate error message is displayed
   d. Test canceling the authentication by clicking the "Cancel" button in the modal
   e. Verify that the authentication process stops and no error messages appear

3. **Token Refresh and Authentication Persistence**
   
   a. After successfully authenticating, close the application
   b. Reopen the application and navigate to the "Twitch Setup" page
   c. Verify that you're still authenticated (the "Authenticated" status should appear)
   d. Leave the application running for several hours (or adjust system time if testing)
   e. Verify that the application automatically refreshes the token without requiring re-authentication
   f. Test the "Log Out" button and verify that it properly revokes the token and clears authentication status

#### Advanced Authentication Mode

1. **Custom Credentials Test**
   
   a. Navigate to the "Twitch Setup" page in the application
   b. Select "Advanced Authentication" mode
   c. Ensure all fields are filled in correctly:
      - Channel Name
      - Client ID
      - Client Secret
      - Redirect URI
   d. Click the "Test Connection" button
   e. Verify you receive a "Connection successful!" message

### Connection Test Procedure

1. **Basic Connection Test**

   a. Navigate to the "Twitch Setup" page in the application
   b. Ensure all fields are filled in correctly:
      - Channel Name
      - For Simple mode: Verify you're authenticated
      - For Advanced mode: Verify Client ID and Client Secret are filled in
   c. Click the "Test Connection" button
   d. Verify you receive a "Connection successful!" message

2. **Chat Connection Verification**

   a. Save your Twitch integration settings
   b. Open your Twitch channel in a browser
   c. Look for a system message indicating a bot has joined the chat
      - Note: This may not be visible depending on your Twitch chat settings
   d. If the application is configured to send a connection message, verify it appears in chat

3. **Permissions Verification**

   a. Ensure you're logged into Twitch as the broadcaster or a moderator
   b. Try sending a test message from the application:
      - In the application console or a test feature, send a message to the channel
      - Verify the message appears in your Twitch chat

## Simulating a Complete Giveaway Workflow

This test simulates a full giveaway from start to finish, verifying all components work together.

### Test Setup

1. **Prepare Testing Environment**

   a. Ensure the application is running and connected to Twitch
   b. Open your Twitch channel in a browser
   c. If possible, have a second Twitch account ready to simulate viewer participation
   d. Open OBS and add text sources for:
      - Current prize (`prize.txt`)
      - Current entries (`giveaway.txt`)
      - Winner (`winner.txt`)

2. **Verify Initial State**

   a. Confirm no giveaway is currently active
   b. Check that the winner and prize files are empty or contain default values

### Test Procedure

1. **Start a Giveaway**

   a. As a moderator or broadcaster, type in chat: `!startgiveaway Test Prize`
   b. Verify the application responds with a confirmation message
   c. Check that the prize file is updated with "Test Prize"
   d. Confirm the OBS overlay updates to show the new prize

2. **Join the Giveaway**

   a. Using a second account (or your main account if testing alone), type: `!join`
   b. Verify the application confirms your entry
   c. Check that the entries file is updated with your username
   d. Confirm the OBS overlay updates to show the new entry

3. **Test Follower Requirements** (if enabled)

   a. Using a non-follower account, type: `!join`
   b. Verify the application rejects the entry with an appropriate message
   c. Follow the channel with this account
   d. Try joining again and verify the entry is now accepted

4. **Draw a Winner**

   a. As a moderator or broadcaster, type: `!drawwinner`
   b. Verify the application announces a winner in chat
   c. Check that the winner file is updated with the winner's username
   d. Confirm the OBS overlay updates to show the winner
   e. Verify the giveaway is now marked as inactive

5. **Verify Post-Giveaway State**

   a. Try to join the giveaway by typing `!join`
   b. Verify the application responds that no giveaway is active
   c. Start another giveaway and confirm previous entries are cleared

## Verifying Integration with OBS

This test ensures that the file-based integration with OBS is working correctly.

### Test Setup

1. **Configure OBS Text Sources**

   a. In OBS, add three Text (GDI+) sources:
      - Prize Source: Set to read from `[FireBotFileFolder]/prize.txt`
      - Entries Source: Set to read from `[FireBotFileFolder]/giveaway.txt`
      - Winner Source: Set to read from `[FireBotFileFolder]/winner.txt`
   b. Style the text sources as desired for your overlay

   Note: Replace `[FireBotFileFolder]` with your configured folder path (default: `G:\Giveaway`)

2. **Verify File Paths**

   a. Check that the FireBot file folder path in the application matches the path used in OBS
   b. Ensure the application has write permissions to this folder
   c. Verify OBS has read permissions for this folder

### Test Procedure

1. **Real-time Updates Test**

   a. Start a giveaway as described in the workflow test
   b. Watch the OBS preview to confirm the prize text updates in real-time
   c. Have users join the giveaway and verify the entries update in OBS
   d. Draw a winner and confirm the winner display updates in OBS

2. **File Format Test**

   a. Check the formatting of each file:
      - prize.txt should contain only the prize text
      - giveaway.txt should contain a list of usernames, one per line
      - winner.txt should contain only the winner's username
   b. Verify that OBS is displaying the content correctly
   c. Adjust text source properties in OBS if necessary (word wrap, alignment, etc.)

3. **Stress Test** (for larger giveaways)

   a. Simulate multiple users joining (you can manually edit the giveaway.txt file)
   b. Check if OBS handles a large number of entries properly
   c. Verify the application can draw a winner from a large pool of entries

## Troubleshooting During Testing

If you encounter issues during testing, refer to the [Troubleshooting Guide](troubleshooting.md) for detailed solutions. Common testing issues include:

1. **Connection Problems**
   - Verify your internet connection
   - Check Twitch API status
   - Confirm your credentials are correct
   - For Simple mode: Try logging out and logging back in
   - For Advanced mode: Verify your Client ID and Client Secret

2. **File Access Issues**
   - Ensure the application has write permissions to the output folder
   - Verify OBS has read permissions for the same folder
   - Check if any other application is locking the files

3. **Command Recognition Issues**
   - Verify the command syntax matches your configuration
   - Ensure you have the appropriate permissions for the commands you're testing
   - Check that the application is properly connected to Twitch chat

4. **Authentication Issues**
   - For Simple mode: Try clearing browser cookies and cache before retrying
   - For Advanced mode: Verify your application is properly registered on the Twitch Developer Console
   - Check that your application has the required scopes (chat:read, chat:edit, etc.)
   - Verify that the device code hasn't expired during the authentication process

## Post-Testing Checklist

Before using the integration in a live stream, complete this final checklist:

- [ ] Connection to Twitch chat is stable
- [ ] Authentication works correctly in your chosen mode (Simple or Advanced)
- [ ] Token refresh works properly (test by leaving application running for extended period)
- [ ] All commands work as expected
- [ ] Follower requirements function correctly (if enabled)
- [ ] Files are being written to the correct location
- [ ] OBS overlay updates properly with giveaway information
- [ ] Winner selection works correctly
- [ ] Multiple giveaways can be run in succession

By thoroughly testing the integration using these instructions, you can ensure a smooth experience for both you and your viewers during live giveaways.