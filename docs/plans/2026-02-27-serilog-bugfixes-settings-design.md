# Design: Serilog Logging, Bug Fixes, and Settings Architecture Refactor

**Date**: 2026-02-27
**Version target**: v2.3.0
**Scope**: Timer reset bug, slider jump bug, Serilog integration with UI config, settings service refactor

---

## 1. Timer Reset Bug Fix — Sticky Cached Reads

### Problem

`FireBotFileReader.GetFireBotFileAsync()` returns `string.Empty` when the prize/winner/entries file is locked by Firebot during a write. This causes `isGiveAwayRunning` in `GiveAway.razor` to momentarily flip to `false`, which triggers `ResetTimer()`. On the next 1s poll, the file reads successfully — but now `!hadPrizeBefore && hasPrizeNow` is true, triggering another `ResetTimer()`. Net result: timer resets to full duration randomly during a giveaway.

### Solution

Cache last-known-good values in `FireBotFileReader`:

- Add static fields: `_lastPrize`, `_lastWinner`, `_lastEntries`
- On successful file read: update cache, return new value
- On failure (exception, sharing violation): return cached value, log warning
- Only return empty/empty-array when no cached value exists (initial state before any successful read)

### Files Changed

- `Helpers/FireBotFileReader.cs`

---

## 2. Slider Jump Bug Fix — `oninput` + `onpointerup`

### Problem

Blazor Server sliders with `@bind:event="oninput"` send every drag-pixel as a SignalR round-trip. The server re-renders and sends DOM updates back, causing the slider to visually snap to stale server-acknowledged positions during drag.

### Solution

Pure Blazor approach (no JavaScript):

- Sliders keep `@bind:event="oninput"` for smooth visual feedback (updates local component field only)
- Remove `@bind:after` from all slider inputs — no settings sync during drag
- Add `@onpointerup` event handler to commit the final value to `ISettingsService` and queue persistence
- This means during drag: only the local field and display badge update (cheap, no round-trip overhead beyond the binding itself)
- On pointer release: settings are committed once

### Slider Tick Marks

Add HTML5 `<datalist>` elements to provide visual tick marks on sliders:

- Prize Section Width: ticks at 50, 55, 60, 65, 70, 75, 80, 85, 90
- Font sizes: ticks at 1.0, 2.0, 3.0, 4.0, 5.0, 6.0

### Files Changed

- `Components/Pages/Setup.razor`

---

## 3. Settings Architecture — `ISettingsService` Singleton

### Problem

Settings are currently managed via:
- `GiveAwayHelpers` static fields (in-memory state)
- `SettingsPersistenceService` (debounce + channel)
- `BackgroundSettingsWriterService` (async writer)
- `GiveAway.razor` polls `GiveAwayHelpers` getters every 1s to detect changes

This is scattered and the polling approach means settings changes aren't instant on the overlay.

### Solution

Consolidate into a proper `ISettingsService` singleton:

```csharp
public interface ISettingsService
{
    AppSettings Current { get; }
    event Action? OnSettingsChanged;
    void Update(Action<AppSettings> mutator);
    void ResetToDefaults();
    void LoadFromFile();
}
```

**Behavior**:
- `Current` — returns the in-memory settings snapshot (fast, no I/O)
- `Update(mutator)` — applies the mutation to in-memory settings, fires `OnSettingsChanged`, queues async persistence
- `ResetToDefaults()` — replaces in-memory settings with defaults, fires event, deletes user settings file
- `LoadFromFile()` — called at startup, loads `usersettings.json` or falls back to `appsettings.json`

**Implementation** (`SettingsService`):
- Registered as singleton in DI
- Holds `AppSettings _current` in memory
- `OnSettingsChanged` event fires immediately on any mutation (no file-roundtrip)
- Internally uses existing `SettingsPersistenceService` for debounced async file writes
- `GiveAway.razor` subscribes to `OnSettingsChanged` and calls `StateHasChanged()` — instant overlay updates
- `Setup.razor` calls `Update()` on pointer-up/change events

**What happens to `GiveAwayHelpers`**:
- Theme-specific logic (`InitializeTheme`, `SetPresetTheme`, `UpdateCustomColor`, etc.) stays for now — themes are complex enough to warrant their own helper
- All settings getter/setter methods are removed (replaced by `ISettingsService.Current`)
- The static fields for countdown, font sizes, etc. are removed

**What happens to existing services**:
- `SettingsPersistenceService` — kept, used internally by `SettingsService`
- `BackgroundSettingsWriterService` — kept, unchanged
- `UserSettingsService` — kept for file I/O methods, used by `SettingsService`
- `TimerService` — kept for timer reset events (separate concern from settings)
- `ThemeService` — kept for theme change events (or could be folded into `OnSettingsChanged`)

### Files Changed

- New: `Services/ISettingsService.cs`, `Services/SettingsService.cs`
- Modified: `Helpers/GiveAwayHelpers.cs` (remove settings fields/methods)
- Modified: `Components/Pages/GiveAway.razor` (subscribe to `OnSettingsChanged` instead of polling)
- Modified: `Components/Pages/Setup.razor` (use `ISettingsService.Update()`)
- Modified: `Program.cs` (register `ISettingsService`, call `LoadFromFile()`)

---

## 4. Serilog Logging Integration

### NuGet Packages

- `Serilog.AspNetCore` (includes core Serilog + ASP.NET Core integration)
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`

### Configuration

**`appsettings.json`** (default config):
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/overlay-.log",
          "rollingInterval": "Day",
          "fileSizeLimitBytes": 10485760,
          "retainedFileCountLimit": 7,
          "rollOnFileSizeLimit": true
        }
      }
    ]
  }
}
```

### Runtime Log Level Switching

- Register `Serilog.Core.LoggingLevelSwitch` as singleton
- `Program.cs` creates switch and passes to Serilog config
- Settings page dropdown changes switch value at runtime (instant, no restart)
- File path changes noted in UI as requiring restart

### Logging Settings in `AppSettings`

```csharp
public class LoggingSettings
{
    public string MinimumLevel { get; set; } = "Information";
    public string LogFilePath { get; set; } = "logs/overlay-.log";
    public bool EnableFileLogging { get; set; } = true;
    public bool EnableConsoleLogging { get; set; } = true;
}
```

Added to `AppSettings`:
```csharp
public LoggingSettings Logging { get; set; } = new();
```

### Where Logging Is Added

| Component | Level | What |
|-----------|-------|------|
| `FireBotFileReader` | Information | Successful file reads (Debug-level for routine) |
| `FireBotFileReader` | Warning | File read failures with cache fallback |
| `GiveAway.razor` | Information | Timer reset with reason |
| `GiveAway.razor` | Debug | File scan cycles, state transitions |
| `SettingsService` | Information | Settings changed (what changed) |
| `SettingsService` | Debug | Persistence queue/write operations |
| `BackgroundSettingsWriterService` | Information | Settings written to disk |
| `TimerService` | Information | Timer reset events |
| `Program.cs` | Information | Startup config source, version, settings loaded |

### Settings Page UI — Logging Section

New "Logging" section on the Setup page:

- **Log Level** dropdown: Verbose, Debug, Information, Warning, Error, Fatal
  - Changes take effect immediately via `LoggingLevelSwitch`
- **Log File Path** text input
  - Note displayed: "Changes take effect on next application restart"
- **Enable File Logging** toggle
- **Enable Console Logging** toggle
- All settings saved to `usersettings.json` via `ISettingsService`

### Files Changed

- Modified: `Program.cs` (Serilog bootstrap + `LoggingLevelSwitch`)
- Modified: `Models/AppSettings.cs` (add `LoggingSettings`)
- Modified: `Components/Pages/Setup.razor` (logging UI section)
- Modified: `Helpers/FireBotFileReader.cs` (add logging)
- Modified: `Components/Pages/GiveAway.razor` (add logging)
- Modified: `Services/SettingsService.cs` (add logging)
- Modified: `Services/BackgroundSettingsWriterService.cs` (replace Console.WriteLine with Serilog)
- Modified: `appsettings.json` (add Serilog config section)

---

## Summary of All Changes

### New Files
- `Services/ISettingsService.cs`
- `Services/SettingsService.cs`

### Modified Files
- `FirebotGiveawayObsOverlay.WebApp.csproj` (NuGet packages, version bump)
- `Program.cs` (Serilog + settings service registration)
- `Models/AppSettings.cs` (LoggingSettings)
- `Helpers/FireBotFileReader.cs` (cached reads + logging)
- `Helpers/GiveAwayHelpers.cs` (remove settings fields, keep theme logic)
- `Components/Pages/GiveAway.razor` (subscribe to settings events + logging)
- `Components/Pages/Setup.razor` (slider fix + logging UI + settings service)
- `Services/BackgroundSettingsWriterService.cs` (Serilog logging)
- `appsettings.json` (Serilog section)

### Removed
- Settings getter/setter methods from `GiveAwayHelpers` (moved to `ISettingsService`)
- Polling of `GiveAwayHelpers` in `GiveAway.razor` file scan loop (replaced by event subscription)
