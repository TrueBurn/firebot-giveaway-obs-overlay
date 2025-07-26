# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ASP.NET Core 8.0 Blazor Server application that provides an OBS overlay for Firebot giveaways. The application monitors Firebot files and displays real-time giveaway information with Warframe-inspired styling and animations.

## Architecture

- **Framework**: ASP.NET Core 8.0 with Blazor Server
- **Frontend**: Razor components with Bootstrap 5.1 and custom CSS animations
- **File Monitoring**: Real-time file system monitoring for Firebot integration
- **Services**: Singleton TimerService for countdown management

### Key Components

- `GiveAway.razor`: Main overlay component displaying prize, timer, entries, and winner announcements
- `FireBotFileReader`: Monitors and reads Firebot giveaway files (prize, entries, winner data)
- `TimerService`: Manages countdown timer functionality
- `GiveAwayHelpers`: Static configuration helpers for runtime settings
- `giveaway.css`: Warframe-inspired styling with animations and gradients

### Project Structure

```
FirebotGiveawayObsOverlay.WebApp/
├── Components/
│   ├── Layout/          # Layout components (MainLayout, NoMenuLayout, NavMenu)
│   └── Pages/           # Page components (GiveAway, Setup, Home, Error)
├── Extensions/          # Extension methods (TimeSpanExtensions)
├── Helpers/            # Helper classes (GiveAwayHelpers, FireBotFileReader)
├── Services/           # Application services (TimerService)
└── wwwroot/           # Static assets and CSS
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

## Configuration

The application is configured through `appsettings.json` with the following key settings:

- `FireBotFileFolder`: Directory where Firebot stores giveaway files (default: "G:\\Giveaway")
- `CountdownHours/CountdownMinutes/CountdownSeconds`: Giveaway duration configuration
- `PrizeSectionWidthPercent`: Layout width distribution (50-90%)
- Font size settings: `PrizeFontSizeRem`, `TimerFontSizeRem`, `EntriesFontSizeRem`

Configuration is applied at application startup in `Program.cs:27-47` and managed through `GiveAwayHelpers` static methods.

### Timer Configuration
The countdown timer supports hours, minutes, and seconds configuration:
- `CountdownHours`: Default 0, supports extended giveaway durations
- `CountdownMinutes`: Default 59, range 0-59
- `CountdownSeconds`: Default 59, range 0-59

Display format automatically adapts:
- `HH:MM:SS` when hours > 0
- `MM:SS` when only minutes/seconds  
- `SS` when only seconds

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
- Warframe-inspired color schemes and gradients
- CSS animations for entry counters and winner announcements
- Responsive layout with configurable proportions
- Animated borders and pulsing text effects

Winner overlay uses solid black background (`rgb(0, 0, 0)`) without trophy emojis for clean appearance.

## Recent Project Changes

### July 26, 2025 - Timer Hours Support Enhancement
- Extended countdown timer to support hours in addition to minutes and seconds
- Added CountdownHours configuration parameter with default value of 0
- Enhanced Setup.razor with hours input field and proper validation (0-23 range)
- Implemented conditional display logic: HH:MM:SS, MM:SS, or SS based on duration
- Updated all timer calculation and configuration logic to handle three-parameter time settings
- Maintains backward compatibility with existing minute/second configurations

### April 14, 2025 - Winner Overlay Improvements
- Changed winner overlay background from semi-transparent to solid black for cleaner appearance
- Removed trophy emojis from winner display for minimal design

## Future Enhancement Ideas

Based on project roadmap:
- Customizable color schemes for the overlay
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