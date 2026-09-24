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
    │   ├── GiveAwayHelpers.cs             # Static theme helpers
    │   └── FireBotFileReader.cs           # Change-detecting, shared-read file reader
    ├── Models/
    │   ├── ThemeConfig.cs                 # Theme configuration model
    │   └── AppSettings.cs                 # Settings model for persistence
    ├── Services/
    │   ├── GiveawayStateService.cs        # Single file poller + server-side countdown (hosted service)
    │   ├── ISettingsService.cs / SettingsService.cs  # Copy-on-write in-memory settings + change events
    │   ├── VersionService.cs              # Assembly version access
    │   ├── UserSettingsService.cs         # User settings persistence
    │   ├── SettingsPersistenceService.cs  # Debounced async persistence queue
    │   └── BackgroundSettingsWriterService.cs  # Background disk writer
    ├── Properties/
    │   ├── launchSettings.json            # Development launch settings
    │   └── PublishProfiles/               # Publish configurations
    ├── wwwroot/
    │   ├── app.css                        # Application styles
    │   ├── giveaway.css                   # Overlay-specific styles
    │   ├── overlay-reconnect.js           # Silent reconnect/self-heal for the OBS overlay
    │   └── fonts/                         # Self-hosted Orbitron (SIL OFL)
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
- Uses `NoMenuLayout` for clean OBS presentation (Blazor error/reconnect UI is never shown on stream)
- Thin view: subscribes to `GiveawayStateService.SnapshotChanged` and `ISettingsService.OnSettingsChanged`
- Re-renders only when something visible changes; theme/layout style strings are built once per settings change
- Timer digits are real elements (not `MarkupString`), so each tick only patches text nodes

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

### GiveawayStateService

Singleton `BackgroundService` and the single source of truth for what overlays show:

- Polls the Firebot folder every 250 ms with a `PeriodicTimer` (one poller for the whole app, no matter how many overlays are open)
- Runs the countdown from a **deadline** (`TimeProvider`), not by decrementing a counter per tick, so it cannot drift and it survives OBS browser-source reloads
- Publishes an immutable `GiveawaySnapshot` via `SnapshotChanged`, **only** when something visible changes (whole-second granularity)
- Each subscriber is invoked in isolation; an exception from one circuit is logged and does not affect others
- `ResetTimer()` is called by the Setup page's Reset button; settings changes (timer enable/disable) are observed through `ISettingsService.OnSettingsChanged`

Timer state machine (unchanged behaviour): new prize → reset + start; winner appears → pause; winner cleared while prize present → reset + start; prize removed → reset (idle at full duration); timer disabled → hidden/paused; re-enabled → reset.

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
- Instance class owned by `GiveawayStateService` (no static state)
- Change detection: a file is only re-read when its last-write time or length changes, so polling is a cheap stat call
- Opens files with `FileShare.ReadWrite | FileShare.Delete` so reads succeed while Firebot has the file open
- Entry count is computed by enumerating lines over a span (no per-entry string allocations); blank/whitespace lines are ignored
- Sticky cache: on I/O failure the last good value is kept; a warning is logged once per failure streak (no log spam)

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

3. **Server-side giveaway state**: One `GiveawayStateService` polls files and runs the countdown for all overlays; components are thin subscribers. (Replaced the per-component timers and the `TimerService`/`ThemeService` event relays.)

4. **Inline Styles for Themes**: Ensures theme changes apply immediately without CSS reload issues.

5. **Self-contained Deployment**: Users don't need .NET runtime installed, simplifying distribution.

6. **Version-triggered Releases**: Bumping version in .csproj automatically triggers release, reducing manual steps.

7. **Separate User Settings File**: User customizations stored in `usersettings.json` separate from shipped `appsettings.json`, surviving updates and avoiding git conflicts.

8. **Channel-Based Async Persistence with Debouncing**: Settings changes update memory immediately for instant UI feedback, while disk writes are debounced (500ms) and handled asynchronously by a background service. This eliminates slider lag caused by synchronous file I/O blocking the UI thread.

9. **Slider/Numeric Input Mode Toggle**: Provides both slider (visual feedback) and numeric input (precision) for range-based settings. Users can switch between modes with toggle buttons, combining ease of use with exact value entry when needed.

10. **Slider oninput Binding**: Changed from `onchange` to `oninput` for real-time visual feedback during slider drag. Safe to use with async persistence pattern, as debouncing prevents high-frequency disk writes.

11. **Copy-on-write settings**: `SettingsService.Update` clones, mutates and atomically swaps the settings object, so `Current` is always a consistent snapshot. Persistence writes to a temp file and atomically replaces `usersettings.json`. "Reset to Defaults" resets to the `appsettings.json` values and cancels any pending debounced save.

12. **Content root = exe folder for published builds**: wwwroot, `appsettings.json` and relative log paths resolve next to the executable, so the app works regardless of the working directory it was launched from.

13. **Runtime tuning**: Workstation GC (lower memory next to OBS/games), `InvariantGlobalization` (no ICU load; culture-proof CSS values), async Serilog sinks, `MapStaticAssets` (compressed, fingerprinted, cacheable assets), ReadyToRun release builds (faster startup).

## Testing

- **Unit tests** (`FirebotGiveawayObsOverlay.Tests`, xUnit v3 on Microsoft.Testing.Platform): file reader, countdown state machine (with `FakeTimeProvider`), settings persistence/debounce/reset. Run with `dotnet test` from `FirebotGiveawayObsOverlay/`.
- **E2E tests** (`e2e/`, Playwright): publish the app, start it against a sandbox Firebot folder, and drive the overlay and Setup page in Chromium — live file updates, countdown behaviour (incl. reload persistence), winner flow, live theme/layout changes across tabs, input clamping, reset flow, no third-party requests, no console errors.

