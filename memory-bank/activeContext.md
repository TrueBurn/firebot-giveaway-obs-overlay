# Active Context

## Current Focus

### [2026-01-22] Async Settings Persistence with Input Mode Toggle (v2.2.0)
- Implemented channel-based async settings persistence to eliminate slider lag/snap-back
- Created `SettingsPersistenceService` with System.Threading.Channels and 500ms debouncing
- Created `BackgroundSettingsWriterService` as IHostedService for non-blocking disk writes
- Added `SaveUserSettingsAsync()` method to `UserSettingsService` for async I/O
- Replaced synchronous `SaveSettings()` with `QueueSave()` pattern in Setup page
- Added slider/numeric input mode toggle for Prize Section Width and all font size settings
- Changed slider binding from `onchange` to `oninput` for real-time visual feedback (debouncing handles I/O)
- Implemented value clamping for numeric inputs with `Math.Clamp()`
- Graceful shutdown flushes pending settings before application stops
- Performance fix resolves UI blocking during rapid slider adjustments

### [2026-01-17] User-Specific Persistent Settings (v2.1.0)
- Implemented settings persistence across application restarts
- Created `AppSettings` model with non-nullable properties for type-safe configuration
- Created `UserSettingsService` singleton for loading/saving `usersettings.json`
- Added `ApplySettings()` method to `GiveAwayHelpers` for centralized settings application
- Settings saved to `usersettings.json` in application directory (git-ignored)
- Startup loads `usersettings.json` if exists, otherwise falls back to `appsettings.json`
- All Setup page changes automatically persist to `usersettings.json`
- Added Settings Management section to Setup page with:
  - Status indicator showing custom vs default settings
  - Collapsible diff view comparing current settings to defaults
  - Individual reset buttons for each changed setting
  - Full reset to defaults with confirmation

### [2025-12-22] .NET 10 Upgrade (v2.0.0)
- Upgraded from .NET 8.0 to .NET 10 (LTS) with C# 14 support
- Updated target framework to `net10.0` in project file
- Updated GitHub Actions workflow to use .NET 10.0.x SDK
- Updated dotnet-ef tool to version 10.0.0
- Migrated from legacy `.sln` to new `.slnx` XML solution format
- Version bumped to 2.0.0 to reflect major framework upgrade
- No code changes required - codebase already uses modern patterns
- Automatic Blazor 10 benefits: 76% smaller blazor.web.js, asset fingerprinting, improved reconnection

### [2025-12-21] GitHub Releases, Versioning, and Documentation
- Implemented automated GitHub Actions release workflow triggered by version bumps
- Added semantic versioning to .csproj (Version, AssemblyVersion, FileVersion, InformationalVersion)
- Created VersionService for runtime version access from assembly metadata
- Added version display footer to Setup page
- Created comprehensive documentation folder (/docs) with getting-started.md, usage.md, architecture.md
- Enhanced README.md with badges, quick start, system requirements, and documentation links
- Version bump in .csproj + push to main automatically creates release with win-x86 and win-x64 artifacts

### [2025-12-08] Customizable Theme System Implementation
- Successfully implemented comprehensive theme system with 7 preset themes
- Added custom color picker support for personalized themes
- Created ThemeConfig model and ThemeService for real-time theme updates
- Fixed slider glitchy behavior in Setup page
- Added theme configuration to appsettings.json for startup configuration
- Implemented event-based communication for immediate theme updates to overlay

### [2025-07-26] Comprehensive Timer Enhancement Implementation
- Successfully implemented hours support for countdown timer functionality
- Added configurable timer enable/disable feature for flexible giveaway types
- Enhanced user interface with complete timer control state management
- Updated configuration system to handle three-parameter time settings and timer state

## Recent Changes

### [2026-01-22] Async Settings Persistence Implementation (v2.2.0)
- Created `Services/SettingsPersistenceService.cs` with bounded channel (capacity 1, DropOldest mode)
- Implemented 500ms debounce timer using CancellationTokenSource pattern
- Created `Services/BackgroundSettingsWriterService.cs` inheriting from BackgroundService
- Background service consumes from channel via `ReadAllAsync()` and writes asynchronously
- Added `SaveUserSettingsAsync(AppSettings, CancellationToken)` to UserSettingsService
- Registered SettingsPersistenceService as singleton and BackgroundSettingsWriterService as hosted service in Program.cs
- Added `InputMode` enum (Slider, Numeric) to Setup.razor
- Added toggle buttons for each slider setting (Prize Width, Prize Font, Timer Font, Entries Font)
- Changed slider `@bind:event` from `onchange` to `oninput` for real-time feedback
- Added clamped setter methods (`SetPrizeSectionWidthClamped`, etc.) for numeric input validation
- Replaced all `SaveSettings()` calls with `QueueSettingsSave()` for non-blocking persistence
- Implemented `Flush()` method for graceful shutdown with pending settings completion
- Version bumped to 2.2.0

### [2026-01-17] User Settings Persistence System (v2.1.0)
- Created `Models/AppSettings.cs` with non-nullable properties and `ThemeSettings` helper class
- Added `GetDefaults()` and `GetDifferences()` methods to AppSettings for reset functionality
- Added `SettingsDiff` record for tracking setting changes
- Created `Services/UserSettingsService.cs` for JSON file-based settings persistence
- Added `DeleteUserSettings()` method for reset functionality
- Updated `GiveAwayHelpers.cs` with `GetCurrentSettings()` and `ApplySettings()` methods
- Modified `Program.cs` to load user settings first, fall back to appsettings.json
- Updated `Setup.razor` with comprehensive Settings Management UI:
  - Status badge showing "Custom settings active" or "Using defaults"
  - Collapsible diff table with color swatches for color values
  - Individual reset buttons (↻) for each changed setting
  - Full "Reset to Defaults" with two-step confirmation
- Added `usersettings.json` to `.gitignore` for user-specific settings exclusion
- Version bumped to 2.1.0

### [2025-12-22] .NET 10 Framework Upgrade
- Updated `.csproj` TargetFramework from `net8.0` to `net10.0`
- Updated version properties to 2.0.0 (major version bump for framework change)
- Updated `.github/workflows/release.yml` DOTNET_VERSION from `8.0.x` to `10.0.x`
- Updated `.config/dotnet-tools.json` dotnet-ef version from `8.0.2` to `10.0.0`
- Migrated `FirebotGiveawayObsOverlay.sln` to `FirebotGiveawayObsOverlay.slnx` (new XML format)
- Updated CLAUDE.md to reflect .NET 10 architecture
- No breaking changes affected this codebase (no cookie auth, no NavLink query matching)
- Blazor 10 automatic improvements: smaller JS bundle, asset fingerprinting, improved reconnection

### [2025-12-21] GitHub Releases and Documentation
- Created `.github/workflows/release.yml` for automated releases
- Added version properties to `.csproj`: Version, AssemblyVersion, FileVersion, InformationalVersion (starting at 1.0.0)
- Created `VersionService.cs` in Services/ for runtime version access
- Registered VersionService in Program.cs
- Added version footer to Setup.razor with badge display
- Created `/docs` folder with three documentation files:
  - getting-started.md: Installation and first run guide
  - usage.md: Configuration and daily use guide
  - architecture.md: Technical documentation
- Updated README.md with badges, quick start, system requirements, downloads section, and docs links
- Updated CLAUDE.md with complete versioning instructions (how to bump version for releases)
- Release workflow: bump version in .csproj → push to main → auto-creates tag and release with win-x86 + win-x64 ZIPs

### [2025-12-08] Customizable Theme System
- Created ThemeConfig model with 7 preset themes: Warframe, Cyberpunk, Neon, Classic, Ocean, Fire, Purple
- Implemented ThemeService singleton for cross-page theme change notifications
- Added theme selection dropdown and live preview to Setup.razor
- Added custom color pickers (Primary, Secondary, Timer Expired) that appear when "Custom" selected
- Fixed slider glitchy behavior by changing from oninput to onchange events (Blazor Server SignalR fix)
- Added theme configuration section to appsettings.json with all color properties
- Updated GiveAwayHelpers with theme management methods (InitializeTheme, SetPresetTheme, UpdateCustomColor)
- Modified GiveAway.razor to use inline styles for theme colors (fixes CSS variable inheritance issues)
- Implemented GetContainerStyle, GetPrimarySpanStyle, GetSeparatorStyle helper methods
- Theme changes apply immediately via ThemeService event-based communication
- Updated Program.cs to load theme settings from configuration at startup

### [2025-07-26] Complete Timer System Enhancement
- Added CountdownHours configuration to appsettings.json with default value 0
- Added CountdownTimerEnabled configuration to appsettings.json with default value true
- Enhanced GiveAwayHelpers class to support three-parameter time configuration and timer state management
- Modified GiveAway.razor timer logic and display formatting for hours support and conditional timer visibility
- Enhanced Setup.razor with hours input field, timer enable toggle, and comprehensive control state management

### April 14, 2025 - Winner Overlay Improvements
- Changed winner overlay background from semi-transparent to solid black
- Removed trophy emojis from winner display for cleaner appearance

## Open Questions/Issues

### Performance Optimization
- File monitoring efficiency with large numbers of entries
- Animation performance on lower-end streaming setups

### Feature Requests
- Multiple simultaneous giveaway support
- Integration with streaming platforms beyond file-based approach
- Animation speed customization

## Next Priorities

1. Test release workflow with first version bump
2. Consider additional theme customization options (background colors via UI)
3. Animation speed/disable configuration
4. Additional winner announcement styles

[2026-01-22 - Updated with async settings persistence and input mode toggle (v2.2.0)]
[2026-01-17 - Updated with user settings persistence system]
[2025-12-22 - Updated with .NET 10 upgrade (v2.0.0)]
[2025-12-21 - Updated with GitHub releases, versioning, and documentation implementation]
[2025-12-08 - Updated with theme system implementation]
[2025-07-26 - Updated with hours support implementation progress and current focus]
[2025-01-26 - Initial active context documentation]