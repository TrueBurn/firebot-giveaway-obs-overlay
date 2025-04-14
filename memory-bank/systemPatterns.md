# System Patterns

## Architectural Patterns

### Service-Based Architecture
The application follows a service-based architecture where different aspects of functionality are encapsulated in dedicated services:
- **TimerService**: Manages the countdown timer for giveaways
- **TwitchService**: Handles Twitch chat connection and communication
- **GiveawayService**: Manages giveaway state and operations
- **CommandHandler**: Processes commands from Twitch chat

### Component-Based UI
The UI is built using Blazor components, with a clear separation between:
- Layout components
- Page components
- Shared components

### File-Based Data Exchange
The application uses file monitoring for data exchange:
- Reads giveaway information from files
- Updates files when giveaway state changes
- Monitors files for changes to update the UI

## Design Patterns

### Observer Pattern
Used for event handling, particularly for:
- Twitch chat message events
- Timer tick events
- Giveaway state change events

### Dependency Injection
Used throughout the application for:
- Service registration and consumption
- Loose coupling between components
- Testability
- Configuration management

### Command Pattern
Implemented in the CommandHandler for processing chat commands:
- Commands are parsed and routed to appropriate handlers
- Each command has a specific handler method
- Permissions are validated before command execution

### Options Pattern
Implemented for configuration management:
- Strongly-typed settings classes
- IOptionsMonitor for dynamic updates
- Settings service for centralized management
- Change notifications for real-time updates

### Repository Pattern
Used for file operations:
- Abstracts file system interactions
- Provides a consistent interface for data access
- Improves testability

[2025-04-14 22:22:32] - Initial creation of systemPatterns.md based on project architecture
[2025-04-14 22:55:15] - Added Options Pattern and Repository Pattern sections