# Architecture Documentation

Technical documentation for developers and contributors.

## Overview

The Firebot Giveaway OBS Overlay is built using:

- **Framework**: ASP.NET Core 10
- **UI**: Blazor Server with interactive components
- **Styling**: Bootstrap 5.1 + custom CSS animations
- **Deployment**: Self-contained single-file executable

## Project Structure

```
FirebotGiveawayObsOverlay/
├── FirebotGiveawayObsOverlay.sln          # Solution file
└── FirebotGiveawayObsOverlay.WebApp/      # Main web application
    ├── Components/
    │   ├── Layout/                         # Layout components
    │   │   ├── MainLayout.razor           # Standard layout with navigation
    │   │   ├── NoMenuLayout.razor         # Clean layout for overlay
    │   │   └── NavMenu.razor              # Navigation menu
    │   └── Pages/                          # Page components
    │       ├── GiveAway.razor             # Main overlay display
    │       ├── Setup.razor                # Configuration page
    │       ├── Home.razor                 # Landing page
    │       └── Error.razor                # Error handling
    ├── Extensions/
    │   └── TimeSpanExtensions.cs          # Time formatting helpers
    ├── Helpers/
    │   ├── GiveAwayHelpers.cs             # Static configuration management
    │   └── FireBotFileReader.cs           # File monitoring system
    ├── Models/
    │   ├── ThemeConfig.cs                 # Theme configuration model
    │   └── AppSettings.cs                 # Settings model for persistence
    ├── Services/
    │   ├── TimerService.cs                # Countdown timer management
    │   ├── ThemeService.cs                # Theme change notifications
    │   ├── VersionService.cs              # Assembly version access
    │   ├── UserSettingsService.cs         # User settings persistence
    │   ├── SettingsPersistenceService.cs  # Debounced async persistence queue
    │   └── BackgroundSettingsWriterService.cs  # Background disk writer
    ├── Properties/
    │   ├── launchSettings.json            # Development launch settings
    │   └── PublishProfiles/               # Publish configurations
    ├── wwwroot/
    │   ├── app.css                        # Application styles
    │   └── giveaway.css                   # Overlay-specific styles
    ├── Program.cs                         # Application entry point
    └── appsettings.json                   # Configuration file
```

## Core Components

### GiveAway.razor

The main overlay component responsible for:

- Displaying prize information
- Showing countdown timer (when enabled)
- Real-time entry count updates
- Winner announcement display
- Applying theme colors dynamically

**Key Features:**
- Uses `NoMenuLayout` for clean OBS presentation
- Subscribes to `ThemeService` for live theme updates
- Implements file polling for Firebot integration
- Handles timer state transitions (running, expired, disabled)

### Setup.razor

Configuration interface providing:

- Theme selection with live preview
- Custom color pickers (when Custom theme selected)
- Timer configuration (hours, minutes, seconds, enable/disable)
- Firebot file path input
- Layout and font size adjustments with slider/numeric input toggle
- Settings management with diff view and reset functionality
- Version display footer

**Patterns Used:**
- Two-way binding with `@bind` and `@bind:after`
- Conditional rendering for custom colors section and input modes
- Disabled state management for timer controls
- Async persistence via `SettingsPersistenceService` with debouncing
- Input mode toggle (slider vs numeric) for precise value entry

## Services

### TimerService

Singleton service managing countdown functionality:

```csharp
public class TimerService
{
    public event Action? OnTimerReset;
    public void ResetTimer() => OnTimerReset?.Invoke();
}
```

- Event-based communication between Setup and GiveAway pages
- Notifies overlay when timer is reset from Setup page

### ThemeService

Singleton service for theme change notifications:

```csharp
public class ThemeService
{
    public event Action? OnThemeChanged;
    public void NotifyThemeChanged() => OnThemeChanged?.Invoke();
}
```

- Enables real-time theme updates without page refresh
- Used by Setup page to notify GiveAway overlay

### VersionService

Singleton service providing runtime version information:

```csharp
public class VersionService
{
    public string GetDisplayVersion();
}
```

- Reads version from assembly metadata
- Cleans up version string for display (removes git hash suffix)

### UserSettingsService

Singleton service for persisting user settings:

```csharp
public class UserSettingsService
{
    public AppSettings? LoadUserSettings();
    public void SaveUserSettings(AppSettings settings);
    public Task SaveUserSettingsAsync(AppSettings settings, CancellationToken cancellationToken);
    public bool UserSettingsExist();
    public string GetUserSettingsPath();
    public bool DeleteUserSettings();
}
```

- Saves/loads settings to `usersettings.json` in application directory
- Uses System.Text.Json for serialization with camelCase naming
- Returns null if settings file doesn't exist or is invalid
- `DeleteUserSettings()` removes the file for reset functionality
- `SaveUserSettingsAsync()` provides non-blocking async file writes

### SettingsPersistenceService

Singleton service for debounced async settings persistence:

```csharp
public class SettingsPersistenceService : IDisposable
{
    public const int DebounceDelayMs = 500;
    public ChannelReader<AppSettings> Reader { get; }

    public void QueueSave(AppSettings settings);
    public void Flush();
}
```

- Uses `System.Threading.Channels` with bounded capacity 1 (DropOldest mode)
- Implements 500ms debounce timer that resets on each `QueueSave()` call
- Thread-safe with lock around CancellationTokenSource
- Only latest settings matter; older queued values are dropped
- `Flush()` immediately writes pending settings (used during shutdown)
- Exposes `ChannelReader` for background service consumption

### BackgroundSettingsWriterService

Hosted service for background settings persistence:

```csharp
public class BackgroundSettingsWriterService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken);
    public override async Task StopAsync(CancellationToken cancellationToken);
}
```

- Inherits from `BackgroundService` (built-in .NET hosted service)
- Consumes from `SettingsPersistenceService.Reader` via `ReadAllAsync()`
- Uses `UserSettingsService.SaveUserSettingsAsync()` for non-blocking I/O
- Registered via `AddHostedService<T>()` in Program.cs
- Graceful shutdown completes pending writes before stopping
- Logging for diagnostics and monitoring

### AppSettings Model

Settings model with comparison capabilities:

```csharp
public class AppSettings
{
    // Properties for all settings...

    public static AppSettings GetDefaults();
    public List<SettingsDiff> GetDifferences(AppSettings other);
}

public record SettingsDiff(string Name, string DefaultValue, string CurrentValue);
```

- `GetDefaults()` returns a new instance with default values
- `GetDifferences()` compares two settings objects and returns changed values
- Used by Setup page to show diff view and enable individual resets

## Configuration System

### Static Configuration (GiveAwayHelpers)

Central static class managing runtime configuration:

- `FireBotFileFolder`: Path to Firebot files
- `CountdownTime`: Timer duration (hours, minutes, seconds)
- `CountdownTimerEnabled`: Timer visibility toggle
- `PrizeSectionWidth`: Layout proportions
- Font sizes: Prize, Timer, Entries
- Theme configuration with preset and custom support

### Configuration Files

The application uses a two-file configuration approach:

1. **appsettings.json**: Ships with application, contains defaults
2. **usersettings.json**: User customizations, git-ignored, persists across updates

**Startup Load Order:**
```csharp
app.Lifetime.ApplicationStarted.Register(() =>
{
    var userSettings = userSettingsService.LoadUserSettings();
    if (userSettings != null)
    {
        GiveAwayHelpers.ApplySettings(userSettings);  // Use user settings
    }
    else
    {
        GiveAwayHelpers.ApplySettings(defaultSettings);  // Fall back to appsettings.json
    }
});
```

**Runtime Persistence (Async with Debouncing):**
- Setup page changes call `QueueSave()` via `SettingsPersistenceService`
- In-memory settings update immediately (instant UI feedback)
- Disk writes debounced with 500ms delay to prevent lag
- Multiple rapid changes result in single disk write after idle
- `BackgroundSettingsWriterService` handles async file writes
- Graceful shutdown ensures pending settings are written

**Persistence Flow:**
```
User changes slider → GiveAwayHelpers.Set*() [instant memory update]
                    → SettingsPersistenceService.QueueSave() [non-blocking]
                    → 500ms debounce timer [resets on each call]
                    → Channel.Writer.TryWrite() [when timer expires]
                    → BackgroundSettingsWriterService reads from channel
                    → UserSettingsService.SaveUserSettingsAsync() [async I/O]
                    → File.WriteAllTextAsync() to usersettings.json
```

This architecture eliminates slider lag by keeping UI updates synchronous (memory-only) while making disk I/O async and debounced.

## File Monitoring System

### FireBotFileReader

Monitors and reads Firebot-generated files:

- **Prize File**: Current giveaway prize text
- **Entries File**: Number of entries (updated in real-time)
- **Winner File**: Winner announcement data

**Implementation:**
- Polling-based file reading
- Handles file not found gracefully
- Parses various file formats

## Theming Architecture

### ThemeConfig Model

```csharp
public class ThemeConfig
{
    public string Name { get; set; }
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string BackgroundStart { get; set; }
    public string BackgroundEnd { get; set; }
    public string BorderGlowColor { get; set; }
    public string TextColor { get; set; }
    public string TimerExpiredColor { get; set; }
    public string SeparatorColor { get; set; }
}
```

### Preset Themes

7 built-in themes defined in `GiveAwayHelpers`:
- Warframe, Cyberpunk, Neon, Classic, Ocean, Fire, Purple

### Theme Application

Colors are applied via inline styles for reliable real-time updates:

```razor
<div style="background: linear-gradient(135deg, @theme.BackgroundStart, @theme.BackgroundEnd);">
```

CSS custom properties are defined in `giveaway.css` but inline styles take precedence for dynamic theming.

## Deployment

### Build Configuration

Self-contained single-file executable:

```xml
<PropertyGroup>
    <RuntimeIdentifier>win-x86</RuntimeIdentifier>  <!-- or win-x64 -->
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishTrimmed>false</PublishTrimmed>
</PropertyGroup>
```

### GitHub Actions Release

Automated release workflow (`.github/workflows/release.yml`):

1. Triggers on push to main when version changes in `.csproj`
2. Extracts version, checks if tag exists
3. Builds for win-x86 and win-x64 in parallel
4. Creates GitHub Release with ZIP artifacts

### Versioning

Version properties in `.csproj`:
- `Version`: NuGet package version (SemVer)
- `AssemblyVersion`: Assembly manifest version
- `FileVersion`: Windows file properties
- `InformationalVersion`: Human-readable version

## Development

### Running Locally

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet run
```

Application starts on port 5000 (HTTP).

### Building

```bash
dotnet build --configuration Release
```

### Publishing

```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

## Key Design Decisions

1. **Blazor Server over WebAssembly**: Chosen for simpler file system access and real-time updates without SignalR complexity.

2. **Static GiveAwayHelpers**: Provides simple global state management without dependency injection complexity for settings.

3. **Event-based Services**: TimerService and ThemeService use events for loose coupling between components.

4. **Inline Styles for Themes**: Ensures theme changes apply immediately without CSS reload issues.

5. **Self-contained Deployment**: Users don't need .NET runtime installed, simplifying distribution.

6. **Version-triggered Releases**: Bumping version in .csproj automatically triggers release, reducing manual steps.

7. **Separate User Settings File**: User customizations stored in `usersettings.json` separate from shipped `appsettings.json`, surviving updates and avoiding git conflicts.

8. **Channel-Based Async Persistence with Debouncing**: Settings changes update memory immediately for instant UI feedback, while disk writes are debounced (500ms) and handled asynchronously by a background service. This eliminates slider lag caused by synchronous file I/O blocking the UI thread.

9. **Slider/Numeric Input Mode Toggle**: Provides both slider (visual feedback) and numeric input (precision) for range-based settings. Users can switch between modes with toggle buttons, combining ease of use with exact value entry when needed.

10. **Slider oninput Binding**: Changed from `onchange` to `oninput` for real-time visual feedback during slider drag. Safe to use with async persistence pattern, as debouncing prevents high-frequency disk writes.
