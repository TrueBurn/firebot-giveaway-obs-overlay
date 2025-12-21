# Active Context

## Current Focus

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

[2025-12-21 - Updated with GitHub releases, versioning, and documentation implementation]
[2025-12-08 - Updated with theme system implementation]
[2025-07-26 - Updated with hours support implementation progress and current focus]
[2025-01-26 - Initial active context documentation]