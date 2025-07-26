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
- `appsettings.json` configuration loaded at startup with structured parameters (hours, minutes, seconds)
- Static methods for accessing/updating configuration values with parameter validation
- Three-parameter time configuration supporting extended duration ranges
**Benefits**:
- Easy customization for streamers
- No recompilation needed for layout changes
- Centralized configuration management
- Flexible time duration configuration supporting hours for extended giveaways

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

## Development Quality Patterns

### [2025-07-26] Validation-First Development Pattern
**Pattern**: Mandatory build and test validation after any C# code changes
**Implementation**:
- Always run `dotnet build` after modifying .cs files
- Execute `dotnet test` if test project exists
- Verify compilation success before considering changes complete
- Apply to all C# source files, Razor components, services, and extensions
**Benefits**:
- Prevents breaking changes from reaching production
- Immediate feedback on compilation errors
- Maintains code quality and stability
- Reduces debugging time in development workflow

### [2025-07-26] Conditional Display Pattern
**Pattern**: Dynamic UI element visibility based on data state and value ranges
**Implementation**:
- Timer display format adapts based on duration: HH:MM:SS when hours > 0, MM:SS when only minutes/seconds, SS when only seconds
- Setup form fields validate input ranges (hours: 0-23, minutes: 0-59, seconds: 0-59)
- Configuration system maintains backward compatibility while supporting enhanced functionality
**Benefits**:
- Clean UI that shows only relevant information
- Intuitive user experience with familiar time format conventions
- Efficient screen space utilization
- Progressive enhancement maintaining existing functionality

### [2025-07-26] Configurable Feature Toggle Pattern
**Pattern**: Optional feature enablement with complete UI state management
**Implementation**:
- Boolean configuration parameter controls feature availability (`CountdownTimerEnabled`)
- UI elements conditionally rendered or disabled based on feature state
- Related controls (inputs, buttons) disabled when feature is off with proper visual feedback
- Background functionality (file monitoring) continues independently of feature state
- Automatic state management handles feature transitions (enable/disable with proper cleanup)
**Benefits**:
- Flexible user experience supporting different use cases
- Clear visual indication of feature availability
- Robust state management preventing inconsistent UI states
- Independent operation of core functionality regardless of optional features
- Professional appearance with standard disabled control styling

### [2025-07-26] Comprehensive Control State Management Pattern
**Pattern**: Coordinated enabling/disabling of related UI controls based on feature state
**Implementation**:
- Primary toggle control manages overall feature state
- Related input controls automatically disabled when feature is turned off
- Action buttons (reset, submit) disabled when their functionality is not available
- Visual feedback through Bootstrap disabled styling and cursor changes
- Defensive programming prevents operations when controls are disabled
**Benefits**:
- Consistent user experience across all related controls
- Prevention of user confusion about control availability
- Professional appearance matching standard UI conventions
- Improved accessibility through proper disabled state handling

[2025-01-26 - Initial system patterns documentation]