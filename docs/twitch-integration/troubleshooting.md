# Troubleshooting Twitch Integration

This guide helps you diagnose and resolve common issues with the Twitch chat integration in the Firebot Giveaway OBS Overlay application.

## Connection Problems

### Issue: Cannot Connect to Twitch Chat

**Symptoms:**
- "Connection failed" message when testing connection
- No messages from the application in Twitch chat
- Application shows disconnected status

**Possible Solutions:**

1. **Verify Internet Connection**
   - Ensure your computer has a stable internet connection
   - Try accessing Twitch.tv in a browser to confirm connectivity

2. **Check Credentials**
   - Verify your Channel Name is correct (without the @ symbol)
   - Ensure Client ID and Client Secret are entered correctly (for Advanced mode)
   - Re-generate a new Client Secret if necessary (for Advanced mode)

3. **Firewall or Network Issues**
   - Check if your firewall is blocking the application
   - If on a corporate or restricted network, ensure WebSocket connections are allowed

4. **Restart the Application**
   - Close and restart the Firebot Giveaway OBS Overlay application
   - Try the connection test again after restarting

5. **Twitch Service Status**
   - Check [Twitch Status](https://status.twitch.tv/) to see if there are any known issues

### Issue: Connection Drops Frequently

**Symptoms:**
- Application connects but disconnects after a short time
- Intermittent connection status

**Possible Solutions:**

1. **Check Internet Stability**
   - Run a speed test to check for packet loss or connection issues
   - Try a wired connection instead of Wi-Fi if possible

2. **Reduce Rate Limits**
   - The application is configured with rate limits to prevent Twitch timeouts
   - If you're experiencing disconnections, it might be due to exceeding these limits

3. **Check for Twitch Bans**
   - Ensure your bot account isn't temporarily banned from the channel

## Authentication Errors

### Issue: Simple Mode Authentication Problems

**Symptoms:**
- "Login with Twitch" button doesn't open the authentication modal
- Authentication modal appears but code verification fails
- "Authorization timed out" or "Authorization denied" messages

**Possible Solutions:**

1. **Browser Issues**
   - Ensure you're using a modern browser that supports popups
   - If the verification page doesn't open, try manually visiting the URL shown in the modal
   - Clear browser cookies and cache if you've previously authenticated

2. **Code Entry Problems**
   - Make sure you're entering the exact code shown in the modal
   - Codes are case-sensitive and must be entered exactly as shown
   - If the code expires (progress bar reaches 0%), click "Login with Twitch" again to get a new code

3. **Account Permissions**
   - Ensure you're logged into the correct Twitch account when authorizing
   - The account must have appropriate permissions for the channel you're trying to connect to

4. **Timeout Issues**
   - If you see "Authorization timed out", you didn't complete the authorization process in time
   - Click "Login with Twitch" again to restart the process with a new code

5. **Denied Authorization**
   - If you see "Authorization denied", you or Twitch rejected the authorization request
   - Check that you approved all requested permissions during the authorization process

### Issue: Invalid Client ID or Secret (Advanced Mode)

**Symptoms:**
- "Connection failed. Please check your credentials" message
- Authentication errors in logs

**Possible Solutions:**

1. **Verify Credentials**
   - Double-check that you've copied the Client ID and Client Secret correctly
   - Ensure there are no extra spaces before or after the credentials

2. **Regenerate Client Secret**
   - Go to the [Twitch Developer Console](https://dev.twitch.tv/console/apps)
   - Select your application and generate a new Client Secret
   - Update the application with the new secret

3. **Check Application Registration**
   - Ensure your application is properly registered on the Twitch Developer Console
   - Verify the redirect URI matches what you've configured in the application

### Issue: Authorization Scope Problems

**Symptoms:**
- Can connect to chat but certain features don't work
- Follower checks fail despite valid credentials

**Possible Solutions:**

1. **Simple Mode Scope Issues**
   - If using Simple mode, try logging out and logging back in
   - The pre-registered application should request all necessary scopes automatically

2. **Advanced Mode Scope Issues**
   - Recreate your application in the Twitch Developer Console
   - Ensure you've selected the appropriate category (Application Integration)
   - Add all required scopes: chat:read, chat:edit, channel:read:subscriptions, channel:read:predictions, channel:read:polls

3. **Token Refresh Issues**
   - If scopes were recently changed, you may need to revoke the token and re-authenticate
   - Click "Log Out" (in Simple mode) or clear your Client Secret and save, then re-enter it (in Advanced mode)

## Command Issues

### Issue: Commands Not Recognized

**Symptoms:**
- Typing commands in chat produces no response
- Commands work inconsistently

**Possible Solutions:**

1. **Check Command Settings**
   - Verify that the commands in your configuration match what you're typing in chat
   - Commands are case-insensitive, but must otherwise match exactly

2. **Verify Connection Status**
   - Ensure the application is connected to Twitch chat
   - Check the connection status in the application

3. **Check Permissions**
   - For moderator commands, ensure you have moderator status in the channel
   - The broadcaster always has permission to use all commands

### Issue: !join Command Not Working for Users

**Symptoms:**
- Users report that the !join command doesn't work
- No confirmation message when users try to join

**Possible Solutions:**

1. **Check if Giveaway is Active**
   - Ensure a giveaway has been started with the !startgiveaway command
   - Users can only join when a giveaway is active

2. **Verify Follower Requirements**
   - If follower-only mode is enabled, users must be followers
   - Check if the minimum follow age requirement is preventing users from joining

3. **Command Syntax**
   - Ensure users are typing just the command (e.g., `!join`) without additional text

## File Access Issues

### Issue: Cannot Write to Files

**Symptoms:**
- Error messages about file access
- Giveaway works but OBS overlay doesn't update

**Possible Solutions:**

1. **Check File Permissions**
   - Ensure the application has write permissions to the FireBot file folder
   - Default location is `G:\Giveaway` but can be configured in appsettings.json

2. **Verify Folder Path**
   - Check that the configured FireBot file folder exists
   - Create the folder manually if it doesn't exist

3. **Close Other Applications**
   - Ensure no other applications have the files open exclusively
   - Check if antivirus software might be blocking file access

### Issue: OBS Not Showing Updated Information

**Symptoms:**
- Giveaway runs correctly in chat but OBS overlay doesn't update
- Files are being written but not reflected in the overlay

**Possible Solutions:**

1. **Check OBS Text Source Configuration**
   - Ensure OBS text sources are pointing to the correct files
   - Verify that "Read from file" is enabled for each text source

2. **Refresh OBS Sources**
   - Right-click on the text sources in OBS and select "Properties"
   - Click "Browse" and reselect the same file to refresh

3. **File Path Issues**
   - Ensure the application and OBS are using the same file paths
   - Check for any path discrepancies in the configuration

## Advanced Troubleshooting

If you continue to experience issues after trying the solutions above:

1. **Check Application Logs**
   - Look for error messages in the application console
   - These can provide more specific information about what's going wrong

2. **Verify TwitchLib Version**
   - The application uses TwitchLib for Twitch integration
   - Ensure you're using a compatible version of the application

3. **Test with a Different Account**
   - Try connecting with a different Twitch account to rule out account-specific issues

4. **Clean Reinstall**
   - Uninstall the application completely
   - Delete any configuration files
   - Reinstall and reconfigure from scratch

## Device Code Authentication Troubleshooting (Simple Mode)

### Issue: Device Code Modal Doesn't Appear

**Symptoms:**
- Clicking "Login with Twitch" doesn't show the device code modal
- Application freezes after clicking the button

**Possible Solutions:**

1. **Check Browser Integration**
   - Ensure your system allows the application to open browser windows
   - Try manually opening a browser and navigating to the Twitch authorization page

2. **UI Rendering Issues**
   - Try resizing or minimizing/maximizing the application window
   - Restart the application and try again

### Issue: Device Code Authentication Times Out

**Symptoms:**
- "Authorization timed out" message appears
- Progress bar reaches 0% before authentication completes

**Possible Solutions:**

1. **Speed Up Authentication Process**
   - Have the Twitch verification page ready before requesting a code
   - Enter the code immediately after it appears in the modal
   - Ensure you're already logged into Twitch in your browser

2. **Check Network Delays**
   - If your network is slow, the code might expire before the authentication completes
   - Try using a more stable or faster internet connection

3. **Browser Issues**
   - Try using a different browser for the verification step
   - Ensure your browser isn't blocking cookies from Twitch

### Issue: Authentication Succeeds But Connection Fails

**Symptoms:**
- "Authentication successful" message appears
- Connection test still fails after authentication

**Possible Solutions:**

1. **Token Validation Issues**
   - The token might be valid but doesn't have the required scopes
   - Log out and log back in to refresh the token with proper scopes

2. **Channel Name Mismatch**
   - Ensure the Channel Name field matches the Twitch account you authenticated with
   - For bot accounts, the Channel Name should be the channel you want to connect to, not the bot's name

3. **Rate Limiting**
   - If you've made multiple authentication attempts, Twitch might be rate-limiting your requests
   - Wait a few minutes before trying again

4. **Token Storage Issues**
   - The application might have issues storing the authentication token
   - Restart the application and try authenticating again