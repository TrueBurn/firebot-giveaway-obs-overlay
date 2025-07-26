# System Patterns

## Architectural Patterns

### File-Based Integration Pattern
**Pattern**: Monitor external file system for data updates rather than API integration
**Implementation**: 
- `FireBotFileReader` class monitors Firebot output directory
- Real-time file system watching for immediate updates
- Parsed file content drives UI state changes
**Benefits**: 
- No direct API dependencies on Firebot
- Works with existing Firebot file output system
- Reliable and simple integration approach

### Configuration-Driven UI Pattern
**Pattern**: Runtime UI customization through configuration without code changes
**Implementation**:
- `GiveAwayHelpers` static class manages runtime settings
- `appsettings.json` configuration loaded at startup
- Static methods for accessing/updating configuration values
**Benefits**:
- Easy customization for streamers
- No recompilation needed for layout changes
- Centralized configuration management

### Singleton Service Pattern
**Pattern**: Single instance services for application-wide state management
**Implementation**:
- `TimerService` registered as singleton in DI container
- Manages countdown timer state across all components
- Provides consistent timer functionality throughout application
**Benefits**:
- Consistent state management
- Resource efficiency
- Simplified component interactions

## Component Patterns

### Real-Time Update Pattern
**Pattern**: Blazor Server components with automatic UI updates
**Implementation**:
- File system watchers trigger component state changes
- `StateHasChanged()` calls for immediate UI refresh
- Server-side rendering with SignalR for real-time updates
**Benefits**:
- Immediate visual feedback
- No client-side polling required
- Efficient bandwidth usage

### Layout Composition Pattern
**Pattern**: Flexible layout system with configurable proportions
**Implementation**:
- CSS Grid/Flexbox with dynamic percentage-based sizing
- Runtime adjustment of layout proportions
- Responsive design principles
**Benefits**:
- Adaptable to different stream layouts
- Easy customization for different screen sizes
- Professional appearance

## Styling Patterns

### Theme-Based Animation Pattern
**Pattern**: Consistent animation system with theme-specific styling
**Implementation**:
- CSS custom properties for theme colors
- Keyframe animations for consistent motion
- Warframe-inspired color schemes and effects
**Benefits**:
- Cohesive visual experience
- Easy theme modifications
- Professional streaming aesthetics

### State-Driven Visual Feedback Pattern
**Pattern**: Visual states reflect application data state
**Implementation**:
- Winner overlay appears based on winner data presence
- Entry counter animations on value changes
- Timer visual changes based on countdown status
**Benefits**:
- Clear visual communication
- Engaging viewer experience
- Intuitive state understanding

## Data Flow Patterns

### Unidirectional Data Flow Pattern
**Pattern**: Data flows from file system → services → components → UI
**Implementation**:
- File changes trigger service updates
- Services notify components of state changes
- Components update UI based on service state
**Benefits**:
- Predictable data flow
- Easy debugging and maintenance
- Clear separation of concerns

[2025-01-26 - Initial system patterns documentation]