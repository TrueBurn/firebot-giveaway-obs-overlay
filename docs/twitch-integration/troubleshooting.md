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
   - Ensure Client ID and Client Secret are entered correctly
   - Re-generate a new Client Secret if necessary

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

### Issue: Invalid Client ID or Secret

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

1. **Recreate Application with Proper Scopes**
   - The application requires specific OAuth scopes for full functionality
   - Recreate your application in the Twitch Developer Console
   - Ensure you've selected the appropriate category (Application Integration)

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