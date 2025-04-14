# Product Context

## Project Overview

The Firebot Giveaway OBS Overlay is a web application that provides a visual interface for displaying giveaway information during streams. It can be added to OBS Studio as a browser source.

## Core Features

- Visual display of giveaway information (prize, timer, entry count)
- Winner announcement overlay with animations
- Twitch chat integration for giveaway management
- Command handling for giveaway operations
- Configuration UI for Twitch settings
- Support for follower-only giveaways

## Technical Architecture

- Built as a .NET web application using Blazor Server
- Monitors files for changes to update giveaway status
- Connects directly to Twitch chat using TwitchLib
- Includes animations and styling for an engaging viewer experience

## Project Structure

- **FirebotGiveawayObsOverlay.WebApp**: The main web application
  - **Components**: Contains Razor components for the UI
    - **Layout**: Layout components
    - **Pages**: Page components including the main giveaway display
  - **Extensions**: Extension methods
  - **Helpers**: Helper classes including GiveAwayHelpers
  - **Services**: Services including TimerService, TwitchService, GiveawayService, and SettingsService
  - **Models**: Data models including TwitchSettings and AppSettings
  - **Configuration**: Configuration providers including DynamicConfigurationProvider
  - **wwwroot**: Static assets
    - **giveaway.css**: Styling for the giveaway overlay
    - **app.css**: General application styling
    - **bootstrap/**: Bootstrap framework files
- **FirebotGiveawayObsOverlay.Tests**: Unit tests for the application

## Future Considerations

- Additional customization options for colors and fonts
- Support for multiple simultaneous giveaways
- Enhanced animations and visual effects
- Configurable overlay size and position
- Mobile-responsive design for monitoring on different devices

[2025-04-14 22:21:48] - Initial creation of productContext.md from existing project-overview.md
[2025-04-14 22:56:00] - Updated project structure to include new components for IOptions and Serilog implementation