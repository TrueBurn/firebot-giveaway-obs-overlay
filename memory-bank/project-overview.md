# Firebot Giveaway OBS Overlay

## Project Overview

This project is an OBS overlay for Firebot giveaways. It provides a visual interface that can be added to OBS Studio as a browser source to display giveaway information during streams.

## Project Structure

The project is a .NET web application with the following structure:

- **FirebotGiveawayObsOverlay.WebApp**: The main web application
  - **Components**: Contains Razor components for the UI
    - **Layout**: Layout components
    - **Pages**: Page components including the main giveaway display
  - **Extensions**: Extension methods
  - **Helpers**: Helper classes including GiveAwayHelpers
  - **Services**: Services including TimerService, TwitchService, and GiveawayService
  - **Models**: Data models including TwitchSettings
  - **wwwroot**: Static assets
    - **giveaway.css**: Styling for the giveaway overlay
    - **app.css**: General application styling
    - **bootstrap/**: Bootstrap framework files
- **FirebotGiveawayObsOverlay.Tests**: Unit tests for the application

## Key Components

### GiveAway.razor

The main component that displays the giveaway information. It includes:
- Prize display
- Timer countdown
- Entry count
- Winner announcement overlay

### giveaway.css

Contains all the styling for the giveaway overlay, including:
- Container layout and styling
- Prize section styling
- Timer and entries display
- Winner overlay styling

### FireBotFileReader

A service that reads giveaway information from files:
- Prize information
- Entries list
- Winner information

### TimerService

Manages the countdown timer for the giveaway.

### TwitchService

Connects to Twitch chat and handles communication:
- Establishes and maintains connection to Twitch chat
- Handles authentication using the streamer's credentials
- Parses incoming messages
- Emits events for message and command handling

### CommandHandler

Processes commands from Twitch chat:
- Parses command syntax
- Validates user permissions
- Routes commands to appropriate handlers

### GiveawayService

Manages the giveaway state and operations:
- Handles giveaway state (active/inactive)
- Manages entries (add, remove, list)
- Selects random winners
- Updates the appropriate files

## Recent Modifications

### Twitch Chat Integration (April 14, 2025)

1. Added direct Twitch chat integration using TwitchLib
2. Implemented command handling for giveaway management
3. Created a configuration UI for Twitch settings
4. Added support for follower-only giveaways
5. Created comprehensive documentation and unit tests

### Winner Overlay Improvements (April 14, 2025)

1. Changed the winner overlay background from semi-transparent (`rgba(0, 0, 0, 0.8)`) to solid black (`rgb(0, 0, 0)`) to prevent distracting content from showing through.
2. Removed the trophy emojis that were displayed on both sides of the winner name for a cleaner look.

## Technical Details

- The application uses Blazor Server for interactive web UI
- It monitors files for changes to update the giveaway status
- It includes animations and styling for an engaging viewer experience
- The winner overlay appears when a winner is selected
- It can connect directly to Twitch chat for real-time interaction

## Future Considerations

Potential improvements could include:
- Additional customization options for colors and fonts
- Support for multiple simultaneous giveaways
- Enhanced animations and visual effects
- Configurable overlay size and position
- Mobile-responsive design for monitoring on different devices