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

### [2025-12-08] Event-Based Cross-Page Communication Pattern
**Pattern**: Singleton services with events for real-time cross-page state synchronization
**Implementation**:
- Service registered as singleton (ThemeService, TimerService)
- Service exposes Action event for state change notifications
- Source page calls service method to trigger notification
- Target page subscribes to event in OnInitializedAsync, unsubscribes in Dispose
- Event handler updates local state and calls InvokeAsync(StateHasChanged)
**Benefits**:
- Immediate updates without polling or page refresh
- Clean separation between pages with loose coupling
- Reusable pattern for any cross-page communication need
- Works with Blazor Server's SignalR architecture

### [2025-12-08] Inline Style Theme Application Pattern
**Pattern**: Dynamic inline styles for reliable theme color application
**Implementation**:
- Helper methods generate CSS style strings from theme configuration (GetContainerStyle, GetPrimarySpanStyle)
- Styles applied directly to elements via style attribute
- Color values embedded from currentTheme object properties
- StateHasChanged triggers re-render with updated style strings
**Benefits**:
- Guaranteed color application regardless of CSS cascade issues
- Predictable behavior across browsers and OBS browser source
- Immediate visual updates when theme changes
- No dependency on CSS custom property inheritance

### [2025-12-08] Preset with Custom Override Pattern
**Pattern**: Predefined options with ability to override individual properties
**Implementation**:
- ThemeConfig model defines all customizable properties
- ThemeConfig.Presets provides named preset configurations
- Dropdown allows preset selection or "Custom" option
- Custom selection reveals color pickers for individual property editing
- InitializeTheme checks if colors match preset to determine custom state
**Benefits**:
- Quick selection for common configurations via presets
- Full flexibility for personalized customization
- Clear UI distinction between preset and custom modes
- Configuration can be initialized from preset or custom values

### [2025-12-08] Delayed Input Binding Pattern
**Pattern**: Use onchange instead of oninput for form controls in Blazor Server
**Implementation**:
- Range sliders use @bind:event="onchange" instead of oninput
- Value updates only on input release, not during drag
- Prevents SignalR round-trip race conditions
**Benefits**:
- Eliminates glitchy slider behavior from race conditions
- Reduced server load during user input
- Stable user experience for form controls
- Appropriate for settings that don't need real-time preview

### [2026-01-17] File-Based Settings Persistence Pattern
**Pattern**: User settings persisted to JSON file separate from application defaults
**Implementation**:
- `AppSettings` model with non-nullable properties and sensible defaults
- `UserSettingsService` singleton for load/save operations
- Settings file (`usersettings.json`) stored in application directory
- Startup logic: load user settings if exists, fall back to appsettings.json
- Setup page triggers save after each setting change
**Benefits**:
- Settings persist across application restarts
- User settings survive application updates (git-ignored file)
- Single source of truth with non-nullable properties prevents drift
- Self-documenting JSON file users can manually edit if needed

### [2026-01-17] Centralized Settings Application Pattern
**Pattern**: Single method applies all settings from a settings object
**Implementation**:
- `GiveAwayHelpers.ApplySettings(AppSettings)` applies all settings at once
- `GiveAwayHelpers.GetCurrentSettings()` exports current state to AppSettings
- Startup uses ApplySettings for both user and default settings
- Eliminates code duplication between loading paths
**Benefits**:
- Consistent settings application regardless of source
- Easy to add new settings (single point of change)
- Simplifies testing and debugging of settings logic
- Reduces risk of missing settings during load/save

[2026-01-17 - Added settings persistence and centralized application patterns]
[2025-12-08 - Added theme system and cross-page communication patterns]
[2025-01-26 - Initial system patterns documentation]