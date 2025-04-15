# Twitch Chat Integration for Firebot Giveaway OBS Overlay

This documentation provides comprehensive information about the Twitch chat integration feature in the Firebot Giveaway OBS Overlay application. The integration allows streamers to run giveaways directly through Twitch chat, making it easier to engage with viewers while maintaining a professional overlay for OBS. The integration offers two authentication modes: Simple (button-based) and Advanced (API credentials).

## Table of Contents

1. [User Guide](user-guide.md)
   - Setting Up Twitch Integration
   - Configuring the Integration
   - Available Commands
   - Permission Requirements
   - Follower-Only Mode

2. [Troubleshooting](troubleshooting.md)
   - Connection Problems
   - Authentication Errors
   - Command Issues
   - File Access Issues

3. [Testing Instructions](testing-instructions.md)
   - Testing the Connection
   - Simulating a Giveaway Workflow
   - Verifying Integration

4. [Technical Implementation](technical-details.md)
   - Architecture Overview
   - Key Classes and Responsibilities
   - File-Based Integration

## Documentation Structure

- **[User Guide](user-guide.md)**: Step-by-step instructions for setting up and using the Twitch integration
- **[Troubleshooting Guide](troubleshooting.md)**: Solutions for common issues
- **[Testing Instructions](testing-instructions.md)**: Procedures for testing the integration
- **[Technical Details](technical-details.md)**: Developer documentation for the implementation
- **[Command Reference](command-reference.md)**: Detailed information about available commands

## Quick Start

### Simple Authentication Mode (Recommended)

1. Navigate to the application's Twitch Setup page
2. Click the "Connect with Twitch" button
3. Authorize the application when redirected to Twitch
4. Test the connection to ensure everything is working
5. Start a giveaway using the `!startgiveaway` command (as moderator)
6. Have viewers join using the `!join` command
7. Draw a winner using the `!drawwinner` command (as moderator)

### Advanced Authentication Mode

1. Obtain Twitch API credentials from the [Twitch Developer Console](https://dev.twitch.tv/console/apps)
2. Configure the integration in the application's Twitch Setup page with your credentials
3. Test the connection to ensure everything is working
4. Start a giveaway using the `!startgiveaway` command (as moderator)
5. Have viewers join using the `!join` command
6. Draw a winner using the `!drawwinner` command (as moderator)

For detailed instructions, refer to the [User Guide](user-guide.md).