# Decision Log

## Architectural Decisions

### [2025-04-14] Winner Overlay Visual Design
**Decision**: Changed winner overlay from semi-transparent to solid black background and removed trophy emojis
**Rationale**: 
- Semi-transparent overlay caused visual distractions with content showing through
- Trophy emojis created visual clutter that detracted from winner name prominence
- Solid black provides clean, professional appearance that focuses attention on winner
**Implications**: 
- Improved readability and visual clarity
- More professional streaming appearance
- Easier integration with various stream backgrounds

### [Previous] File-Based Integration Approach
**Decision**: Use file system monitoring instead of direct Firebot API integration
**Rationale**:
- Firebot already outputs giveaway data to files
- No need to modify Firebot or create API dependencies
- Simpler integration path with existing Firebot workflow
- More reliable than potential API connectivity issues
**Implications**:
- Application depends on file system access to Firebot directory
- Real-time updates require efficient file monitoring
- Limited to data that Firebot outputs to files

### [Previous] ASP.NET Core with Blazor Server Architecture
**Decision**: Use Blazor Server instead of client-side Blazor or other web frameworks
**Rationale**:
- Real-time updates work naturally with SignalR
- Server-side processing for file monitoring
- Smaller client-side footprint for OBS browser source
- Familiar development experience for .NET developers
**Implications**:
- Requires local server running for overlay to function
- Network dependency for real-time updates
- Better suited for local development/streaming setup

### [Previous] Static Configuration Management
**Decision**: Use static helper class for configuration instead of dependency injection
**Rationale**:
- Simple access pattern for configuration values
- Avoids dependency injection complexity in Razor components
- Allows runtime configuration changes without restart
- Centralized configuration management
**Implications**:
- Global state management approach
- Thread safety considerations for configuration updates
- Easy access but less testable than DI approach

### [Previous] Warframe-Inspired Visual Theme
**Decision**: Adopt Warframe game aesthetic for overlay styling
**Rationale**:
- Appeals to gaming audience typical of Twitch streams
- Distinctive visual identity that stands out
- Rich color palette and animation opportunities
- Professional gaming aesthetic
**Implications**:
- Specific target audience appeal
- Requires custom CSS and animation work
- Theme consistency across all UI elements

### [2025-07-26] Timer Hours Support Enhancement
**Decision**: Extended countdown timer to support hours in addition to minutes and seconds
**Rationale**:
- User requirement for longer giveaway durations beyond 99 minutes
- Common use case for extended giveaways during special events or streams
- Consistent with standard time display conventions (HH:MM:SS format)
- Maintains backward compatibility with existing minute/second configurations
**Implementation Details**:
- Added `CountdownHours` to appsettings.json with default value of 0
- Updated `GiveAwayHelpers` to handle three-parameter time configuration
- Modified timer display logic to show hours only when > 0
- Enhanced Setup.razor with hours input field
- Updated all timer calculation logic to include hours
**Implications**:
- Improved flexibility for various giveaway durations
- Enhanced user experience with intuitive time format display
- Maintains clean UI by showing hours only when relevant
- Requires configuration migration for existing installations

### [2025-07-26] Configurable Timer Enable/Disable Feature
**Decision**: Implemented configurable countdown timer that can be enabled or disabled while preserving file monitoring
**Rationale**:
- User requirement for giveaways without explicit end times or durations
- Need to maintain file monitoring for winner detection regardless of timer state
- Consistent user experience with visual feedback for disabled states
- Improved flexibility for different giveaway types and streaming scenarios
**Implementation Details**:
- Added `CountdownTimerEnabled` to appsettings.json with default value true for backward compatibility
- Updated `GiveAwayHelpers` with timer enabled state management methods
- Modified `GiveAway.razor` to conditionally display timer while preserving file monitoring loop
- Enhanced `Setup.razor` with timer toggle positioned above timer inputs for logical flow
- Implemented automatic timer state management (stop when disabled, reset when re-enabled)
- Added disabled state styling for all timer-related inputs and reset button
- Included defensive programming to prevent timer operations when disabled
**Implications**:
- Backward compatibility maintained with timer enabled by default
- File monitoring continues independently of timer state for reliable winner detection
- Enhanced user interface with clear visual feedback for disabled controls
- Improved streaming flexibility for various giveaway formats and durations

### [2025-07-26] Enhanced Timer UI Control Consistency
**Decision**: Comprehensive timer control state management with disabled inputs and buttons
**Rationale**:
- Consistent user experience across all timer-related controls
- Clear visual indication of disabled functionality
- Prevention of user confusion about which controls are active
- Professional appearance matching standard UI conventions
**Implementation Details**:
- Timer inputs (hours, minutes, seconds) disabled when timer toggle is off
- Reset Timer button disabled when countdown timer is disabled
- Proper cursor styling (not-allowed) for disabled controls via Bootstrap
- Defensive method checks to prevent timer operations when disabled
- Clear user feedback messages for disabled state attempts
**Implications**:
- Improved usability with intuitive control states
- Reduced potential for user errors and confusion
- Professional appearance consistent with modern web applications
- Enhanced accessibility through proper disabled state handling

### [2025-12-08] Customizable Theme System with Preset and Custom Options
**Decision**: Implemented comprehensive theme system supporting both preset themes and custom color configuration
**Rationale**:
- User requirement for customizable overlay colors to match stream branding
- Preset themes provide quick selection for common aesthetic preferences
- Custom colors allow fine-tuned personalization for specific needs
- Configuration persistence in appsettings.json enables consistent startup experience
**Implementation Details**:
- Created ThemeConfig model with 7 preset themes (Warframe, Cyberpunk, Neon, Classic, Ocean, Fire, Purple)
- Added Theme section to appsettings.json with all color properties
- Implemented ThemeService singleton for cross-page event-based communication
- Setup page provides preset dropdown with live preview and custom color pickers
- Custom colors section conditionally displayed when "Custom" is selected
**Implications**:
- Enhanced user experience with immediate visual feedback
- Flexible theming system that can be extended with additional presets
- Configuration-driven initialization maintains user preferences across restarts

### [2025-12-08] Inline Styles for Theme Color Application
**Decision**: Applied theme colors via inline styles on elements rather than CSS custom properties
**Rationale**:
- CSS custom properties set on parent elements weren't reliably cascading to child elements in Blazor Server
- Inline styles guarantee immediate color application when StateHasChanged is called
- More predictable behavior across different browsers and OBS browser source
**Implementation Details**:
- Created GetContainerStyle(), GetPrimarySpanStyle(), GetSeparatorStyle() helper methods
- Inline styles directly embed color values from currentTheme object
- FormatTimerWithTheme() generates timer HTML with embedded color styles
**Implications**:
- Reliable theme updates without CSS cascade issues
- Slightly more verbose style generation but guaranteed functionality
- Theme changes apply immediately without page refresh

### [2025-12-08] Event-Based Theme Change Communication
**Decision**: Used ThemeService with event-based pattern for cross-page theme updates
**Rationale**:
- Consistent with existing TimerService pattern for timer reset notifications
- Blazor Server's SignalR infrastructure requires explicit state change notification
- Clean separation between Setup page (theme selection) and GiveAway page (theme display)
**Implementation Details**:
- ThemeService registered as singleton with OnThemeChanged event
- Setup.razor calls ThemeService.NotifyThemeChanged() when theme changes
- GiveAway.razor subscribes to OnThemeChanged and updates currentTheme + StateHasChanged
**Implications**:
- Immediate theme updates without polling or page refresh
- Established pattern can be reused for other cross-page notifications
- Clean architectural separation of concerns

### [2025-12-08] Slider Input Event Change (oninput to onchange)
**Decision**: Changed all range slider bindings from oninput to onchange event
**Rationale**:
- User-reported glitchy behavior with sliders jumping to unexpected values
- Blazor Server's SignalR round-trip creates race conditions with rapid input events
- oninput fires on every pixel movement, overwhelming the server with requests
**Implementation Details**:
- Changed @bind:event="oninput" to @bind:event="onchange" for all range inputs
- onchange only fires when user releases the slider, eliminating race conditions
**Implications**:
- Stable slider behavior without jumps or glitches
- Less real-time feedback during drag (value updates on release)
- Significantly reduced server load during slider adjustments

### [2025-12-21] Version-Bump Triggered Release Workflow
**Decision**: Implemented GitHub Actions workflow triggered by version changes in .csproj
**Rationale**:
- User preference for simple release process: edit version in .csproj, push to main
- Eliminates need for manual tag creation or separate release commands
- Version is single source of truth in the codebase
- Automatic skip when version unchanged prevents accidental duplicate releases
**Implementation Details**:
- Workflow triggers on push to main branch when .csproj changes
- Extracts version from .csproj using grep
- Checks if git tag v{version} already exists
- If new version: builds win-x86 and win-x64 artifacts, creates release
- If existing version: exits cleanly (silent skip)
**Implications**:
- Simple release process: edit version → commit → push = automatic release
- Version must be updated for every release (enforces versioning discipline)
- Both 32-bit and 64-bit Windows platforms supported

### [2025-12-21] VersionService for Runtime Version Access
**Decision**: Created VersionService singleton to provide assembly version at runtime
**Rationale**:
- Setup page needs to display current version to users
- Version information embedded in assembly via .csproj properties
- Consistent with existing service patterns (TimerService, ThemeService)
**Implementation Details**:
- Uses Assembly.GetExecutingAssembly() to read version metadata
- GetDisplayVersion() cleans up InformationalVersion (removes git hash suffix)
- Registered as singleton in Program.cs
- Injected into Setup.razor for display
**Implications**:
- Version automatically updates when .csproj version changes
- No hardcoded version strings anywhere in code
- Single source of truth for version information

### [2025-12-21] Comprehensive Documentation Structure
**Decision**: Created /docs folder with getting-started.md, usage.md, and architecture.md
**Rationale**:
- README.md was becoming too long for comprehensive documentation
- Separate files allow focused content for different audiences
- Supports GitHub Releases with clear user documentation
**Implementation Details**:
- getting-started.md: Quick start, prerequisites, download, first run
- usage.md: All configuration options, OBS integration, troubleshooting
- architecture.md: Technical documentation for developers
- README.md enhanced with quick start and links to full docs
**Implications**:
- Better user experience with organized documentation
- Easier to maintain and update individual sections
- Supports both end users and developers

[2025-12-21 - Added release workflow, versioning, and documentation decisions]
[2025-12-08 - Added theme system and slider fix architectural decisions]
[2025-01-26 - Initial decision log documentation with historical decisions]