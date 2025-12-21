# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ASP.NET Core 8.0 Blazor Server application that provides an OBS overlay for Firebot giveaways. The application monitors Firebot files and displays real-time giveaway information with Warframe-inspired styling and animations.

## Architecture

- **Framework**: ASP.NET Core 8.0 with Blazor Server
- **Frontend**: Razor components with Bootstrap 5.1 and custom CSS animations
- **File Monitoring**: Real-time file system monitoring for Firebot integration
- **Services**: Singleton services for timer and theme management with event-based communication

### Key Components

- `GiveAway.razor`: Main overlay component displaying prize, timer, entries, and winner announcements with dynamic theming
- `Setup.razor`: Configuration page with theme selector, color pickers, and live preview
- `FireBotFileReader`: Monitors and reads Firebot giveaway files (prize, entries, winner data)
- `TimerService`: Manages countdown timer functionality with event notifications
- `ThemeService`: Manages theme change notifications between pages
- `VersionService`: Provides runtime access to assembly version information
- `GiveAwayHelpers`: Static configuration helpers for runtime settings including theme management
- `ThemeConfig`: Theme model with preset themes and custom color support
- `giveaway.css`: Base styling with CSS custom properties for theming

### Project Structure

```
FirebotGiveawayObsOverlay.WebApp/
├── Components/
│   ├── Layout/          # Layout components (MainLayout, NoMenuLayout, NavMenu)
│   └── Pages/           # Page components (GiveAway, Setup, Home, Error)
├── Extensions/          # Extension methods (TimeSpanExtensions)
├── Helpers/            # Helper classes (GiveAwayHelpers, FireBotFileReader)
├── Models/             # Data models (ThemeConfig)
├── Services/           # Application services (TimerService, ThemeService, VersionService)
└── wwwroot/           # Static assets and CSS

docs/                   # User documentation
├── getting-started.md  # Installation and first run guide
├── usage.md           # Configuration and daily use
└── architecture.md    # Technical documentation
```

## Development Commands

### Build and Run
```bash
# Navigate to the web application directory
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp

# Run the application (uses port 5000 for HTTP, 5243/7071 for HTTPS)
dotnet run

# Build the application
dotnet build

# Publish for deployment
dotnet publish -c Release
```

### Testing
```bash
# Run tests (if test project exists)
dotnet test

# Run tests from solution root
cd FirebotGiveawayObsOverlay
dotnet test
```

### Code Quality Validation
**IMPORTANT**: Always run build and test after making changes to .cs files to ensure no breaking changes:

```bash
# After modifying any .cs files, always run:
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build

# If tests exist, also run:
dotnet test
```

This validation step is mandatory for:
- All changes to C# source files (.cs)
- Component modifications (Razor files with code-behind)
- Service and helper class updates
- Extension method changes

## Versioning and Releases

### Version Location
The application version is defined in the `.csproj` file:
- **File**: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/FirebotGiveawayObsOverlay.WebApp.csproj`
- **Current Version**: Check the `<Version>` property in the file

### Version Properties
```xml
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
<InformationalVersion>1.0.0</InformationalVersion>
```

### How to Create a New Release

**When the user says "bump the version" or "create a new release":**

1. **Edit the .csproj file** to update ALL version properties:
   ```xml
   <Version>X.Y.Z</Version>
   <AssemblyVersion>X.Y.Z.0</AssemblyVersion>
   <FileVersion>X.Y.Z.0</FileVersion>
   <InformationalVersion>X.Y.Z</InformationalVersion>
   ```

2. **Version bump types** (Semantic Versioning):
   - **Patch** (1.0.0 → 1.0.1): Bug fixes, minor changes
   - **Minor** (1.0.0 → 1.1.0): New features, backward compatible
   - **Major** (1.0.0 → 2.0.0): Breaking changes

3. **Commit and push to main branch**
   - The GitHub Actions workflow automatically:
     - Detects the version change
     - Creates a git tag (v1.0.0)
     - Builds for win-x86 and win-x64
     - Creates a GitHub Release with both ZIP artifacts

### Automated Release Workflow

The workflow (`.github/workflows/release.yml`) triggers on:
- Push to `main` branch when `.csproj` version changes
- Manual dispatch from GitHub Actions UI

**Workflow behavior:**
- Reads version from `.csproj`
- Checks if tag already exists → skips if version unchanged
- Builds self-contained single-file executables for both win-x86 and win-x64
- Creates GitHub Release with auto-generated release notes

### Version Display
- Version is shown in the Setup page footer
- `VersionService` provides runtime version access via `Assembly.GetExecutingAssembly()`

### Release Artifacts
Each release includes:
- `FirebotGiveawayOverlay-{version}-win-x86.zip` - 32-bit Windows
- `FirebotGiveawayOverlay-{version}-win-x64.zip` - 64-bit Windows

## Configuration

The application is configured through `appsettings.json` with the following key settings:

- `FireBotFileFolder`: Directory where Firebot stores giveaway files (default: "G:\\Giveaway")
- `CountdownHours/CountdownMinutes/CountdownSeconds`: Giveaway duration configuration
- `PrizeSectionWidthPercent`: Layout width distribution (50-90%)
- Font size settings: `PrizeFontSizeRem`, `TimerFontSizeRem`, `EntriesFontSizeRem`
- `Theme`: Color scheme configuration with preset or custom colors

Configuration is applied at application startup in `Program.cs` and managed through `GiveAwayHelpers` static methods.

### Timer Configuration
The countdown timer supports hours, minutes, and seconds configuration with optional enable/disable:
- `CountdownTimerEnabled`: Default true, enables/disables countdown timer display and functionality
- `CountdownHours`: Default 0, supports extended giveaway durations
- `CountdownMinutes`: Default 59, range 0-59
- `CountdownSeconds`: Default 59, range 0-59

Timer behavior:
- When enabled: Display format automatically adapts (HH:MM:SS when hours > 0, MM:SS when only minutes/seconds, SS when only seconds)
- When disabled: Timer hidden from overlay, file monitoring continues for winner detection
- Setup page provides toggle control with disabled state styling for all timer inputs and reset button

### Theme Configuration
The overlay supports customizable color themes with 7 presets and custom color options:

**Preset Themes**: Warframe (default), Cyberpunk, Neon, Classic, Ocean, Fire, Purple

**Theme Properties**:
- `Name`: Preset theme name or "Custom" for custom colors
- `PrimaryColor`: Main gradient color (e.g., "#00fff9")
- `SecondaryColor`: Secondary gradient color (e.g., "#ff00c8")
- `BackgroundStart`/`BackgroundEnd`: Container background gradient colors
- `BorderGlowColor`: Border glow effect color
- `TextColor`: General text color
- `TimerExpiredColor`: Color when timer reaches zero
- `SeparatorColor`: Divider line between prize and info sections

Theme behavior:
- Themes can be changed at runtime via Setup page with immediate effect on overlay
- `ThemeService` notifies giveaway page of changes via event-based communication
- Setup page shows live preview of selected theme
- Custom colors section appears when "Custom" is selected from dropdown

## OBS Integration

The overlay is designed to be used as an OBS Browser Source:
- URL: `http://localhost:5243/giveaway` or `http://localhost:5000/giveaway`
- Recommended dimensions: 1200px x 300px
- Transparent background with custom CSS for OBS integration

## File Monitoring System

The application monitors Firebot files for:
- Prize information
- Real-time entry count updates
- Winner announcements
- Giveaway status changes

Files are read from the configured `FireBotFileFolder` directory.

## Styling and Animations

The overlay features:
- Customizable color themes with 7 presets and custom color support
- CSS animations for entry counters and winner announcements
- Responsive layout with configurable proportions
- Animated borders and pulsing text effects
- Theme colors applied via inline styles for reliable real-time updates

Winner overlay uses solid black background (`rgb(0, 0, 0)`) without trophy emojis for clean appearance.

## Recent Project Changes

### December 21, 2025 - GitHub Releases, Versioning, and Documentation
- Added automated GitHub Actions release workflow triggered by version bumps
- Implemented semantic versioning in .csproj (Version, AssemblyVersion, FileVersion, InformationalVersion)
- Created VersionService for runtime version access
- Added version display footer to Setup page
- Created comprehensive documentation folder (/docs):
  - getting-started.md: Installation and first run guide
  - usage.md: Configuration and daily use guide
  - architecture.md: Technical documentation
- Enhanced README.md with badges, quick start, system requirements, and documentation links
- Release workflow builds for both win-x86 and win-x64 platforms
- Releases are automatically created when version is bumped and pushed to main

### December 8, 2025 - Customizable Theme System
- Added comprehensive theme system with 7 preset themes (Warframe, Cyberpunk, Neon, Classic, Ocean, Fire, Purple)
- Implemented custom color picker support for personalized themes (Primary, Secondary, Timer Expired colors)
- Created ThemeConfig model and ThemeService for theme management and real-time updates
- Added theme configuration to appsettings.json for startup configuration
- Fixed slider glitchy behavior by changing from oninput to onchange events
- Implemented live theme preview on Setup page
- Theme changes apply immediately to giveaway overlay via event-based communication

### July 26, 2025 - Comprehensive Timer System Enhancement
- Extended countdown timer to support hours in addition to minutes and seconds
- Added configurable timer enable/disable feature for giveaways without explicit durations
- Added CountdownHours and CountdownTimerEnabled configuration parameters
- Enhanced Setup.razor with hours input field, timer toggle, and comprehensive control state management
- Implemented conditional display logic: HH:MM:SS, MM:SS, or SS based on duration
- Added disabled state styling for all timer controls when timer is turned off
- Updated all timer calculation and configuration logic while preserving file monitoring
- Maintains backward compatibility with existing configurations

### April 14, 2025 - Winner Overlay Improvements
- Changed winner overlay background from semi-transparent to solid black for cleaner appearance
- Removed trophy emojis from winner display for minimal design

## Future Enhancement Ideas

Based on project roadmap:
- Option to adjust animation speeds or disable specific animations
- Additional winner announcement styles
- Support for displaying multiple winners
- Integration with stream chat for real-time interaction
- Configurable overlay size and position
- Mobile-responsive design for monitoring on different devices

## Memory Bank Integration

This project uses an LLM agent memory bank system located in the `memory-bank/` directory to maintain context across sessions.

### Memory Bank Strategy

**Initialization Check:**
1. Always check if `memory-bank/` directory exists at session start
2. If memory bank exists, read all mandatory files before proceeding:
   - `productContext.md` - Project overview, goals, features, and constraints
   - `activeContext.md` - Current focus, recent changes, and open questions
   - `systemPatterns.md` - Architectural patterns and design approaches
   - `decisionLog.md` - Historical architectural decisions and rationale
   - `progress.md` - Task completion history and upcoming work
   - `project-overview.md` - Legacy project structure documentation
   - `tasks.md` - Legacy task tracking and future enhancement ideas
   - `README.md` - Memory bank overview and purpose
3. Set status to `[MEMORY BANK: ACTIVE]` and proceed with full context
4. If no memory bank exists, inform user and recommend creation

**Status Reporting:**
- Begin every response with either `[MEMORY BANK: ACTIVE]` or `[MEMORY BANK: INACTIVE]`

### Memory Bank Updates

Update memory bank files throughout chat sessions when significant changes occur:

**Standard Memory Bank File Updates:**

**decisionLog.md Updates:**
- **Trigger**: When significant architectural decisions are made (new components, data flow changes, technology choices)
- **Action**: Append new entries with timestamps, never overwrite existing entries
- **Format**: `[YYYY-MM-DD HH:MM:SS] - [Summary of Change/Focus/Issue]`

**productContext.md Updates:**
- **Trigger**: When high-level project description, goals, features, or overall architecture changes significantly
- **Action**: Append new information or modify existing entries with timestamps as footnotes
- **Format**: `[YYYY-MM-DD HH:MM:SS] - [Summary of Change]`

**systemPatterns.md Updates:**
- **Trigger**: When new architectural patterns are introduced or existing ones are modified
- **Action**: Append new patterns or modify existing entries with timestamps
- **Format**: `[YYYY-MM-DD HH:MM:SS] - [Description of Pattern/Change]`

**activeContext.md Updates:**
- **Trigger**: When current focus of work changes or significant progress is made
- **Action**: Append to relevant sections (Current Focus, Recent Changes, Open Questions/Issues) with timestamps
- **Format**: `[YYYY-MM-DD HH:MM:SS] - [Summary of Change/Focus/Issue]`

**progress.md Updates:**
- **Trigger**: When tasks begin, are completed, or have status changes
- **Action**: Append new entries with timestamps, never overwrite existing entries
- **Format**: `[YYYY-MM-DD HH:MM:SS] - [Summary of Change/Focus/Issue]`

**Legacy Files (maintained for compatibility):**
- **tasks.md**: Completed Tasks, Pending Tasks, Future Enhancement Ideas
- **project-overview.md**: Project structure, key components, technical details, recent modifications

### UMB (Update Memory Bank) Command

When user issues "Update Memory Bank" or "UMB":
1. **Halt current task** and acknowledge with `[MEMORY BANK: UPDATING]`
2. **Review complete chat history** to extract all relevant information
3. **Update all affected memory bank files** with session context
4. **Synchronize CLAUDE.md** if any architectural or process changes occurred
5. **Confirm completion** - "Memory Bank fully synchronized"

### CLAUDE.md Maintenance

**When to update CLAUDE.md:**
- After UMB updates that reveal new architectural patterns
- When development processes change
- When new configuration options are added
- When memory bank structure evolves

**Synchronization Process:**
1. After memory bank updates, review if CLAUDE.md needs updates
2. Keep CLAUDE.md aligned with current project state
3. Ensure both files complement each other without duplication