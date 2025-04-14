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
  - **Services**: Services including TimerService
  - **wwwroot**: Static assets
    - **giveaway.css**: Styling for the giveaway overlay
    - **app.css**: General application styling
    - **bootstrap/**: Bootstrap framework files

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

## Recent Modifications

### Winner Overlay Improvements (April 14, 2025)

1. Changed the winner overlay background from semi-transparent (`rgba(0, 0, 0, 0.8)`) to solid black (`rgb(0, 0, 0)`) to prevent distracting content from showing through.
2. Removed the trophy emojis that were displayed on both sides of the winner name for a cleaner look.

## Technical Details

- The application uses Blazor Server for interactive web UI
- It monitors files for changes to update the giveaway status
- It includes animations and styling for an engaging viewer experience
- The winner overlay appears when a winner is selected

## Future Considerations

Potential improvements could include:
- Additional customization options for colors and fonts
- Integration with Twitch API for direct interaction
- Support for multiple simultaneous giveaways
- Enhanced animations and visual effects