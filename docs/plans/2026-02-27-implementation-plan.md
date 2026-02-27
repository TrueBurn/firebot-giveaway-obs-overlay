# Serilog Logging, Bug Fixes, and Settings Refactor — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix timer reset and slider bugs, add Serilog logging with UI configuration, and consolidate settings into a proper singleton service.

**Architecture:** Replace scattered static fields in `GiveAwayHelpers` with an `ISettingsService` singleton that holds settings in memory, fires change events, and queues debounced async persistence. Add Serilog with `LoggingLevelSwitch` for runtime level changes. Fix `FireBotFileReader` to cache last-known-good values so transient file-lock failures don't reset the giveaway timer.

**Tech Stack:** .NET 10, Blazor Server, Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File, System.Threading.Channels

**Design doc:** `docs/plans/2026-02-27-serilog-bugfixes-settings-design.md`

---

## Phase 1: Foundation (Serilog + Model Updates)

### Task 1: Add Serilog NuGet Packages and Bootstrap

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/FirebotGiveawayObsOverlay.WebApp.csproj`
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Program.cs`

**Step 1: Add NuGet packages**

Run from `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/`:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

**Step 2: Add Serilog bootstrap to Program.cs**

Add at the very top of `Program.cs`, before `WebApplication.CreateBuilder`:

```csharp
using Serilog;
using Serilog.Core;
using Serilog.Events;

// Create a LoggingLevelSwitch so we can change log level at runtime from the UI
var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/overlay-.log",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10_485_760,
        retainedFileCountLimit: 7,
        rollOnFileSizeLimit: true)
    .CreateLogger();
```

After `WebApplication.CreateBuilder(args)`, add:

```csharp
builder.Host.UseSerilog();
builder.Services.AddSingleton(levelSwitch);
```

At the very end of Program.cs, wrap `app.Run()` in try/finally:

```csharp
try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

**Step 3: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

Expected: Build succeeded.

**Step 4: Commit**

```bash
git add -A && git commit -m "feat: Add Serilog NuGet packages and bootstrap logging in Program.cs"
```

---

### Task 2: Add LoggingSettings to AppSettings Model

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Models/AppSettings.cs`

**Step 1: Add LoggingSettings class and property**

Add below the `ThemeSettings` class in `Models/AppSettings.cs`:

```csharp
/// <summary>
/// Logging configuration settings for Serilog.
/// </summary>
public class LoggingSettings
{
    public string MinimumLevel { get; set; } = "Information";
    public string LogFilePath { get; set; } = "logs/overlay-.log";
    public bool EnableFileLogging { get; set; } = true;
    public bool EnableConsoleLogging { get; set; } = true;
}
```

Add to `AppSettings` class (after `Theme` property):

```csharp
public LoggingSettings Logging { get; set; } = new();
```

Add logging comparison to `GetDifferences()` method in `AppSettings`:

```csharp
if (Logging.MinimumLevel != other.Logging.MinimumLevel)
    diffs.Add(new("Log Level", other.Logging.MinimumLevel, Logging.MinimumLevel));

if (Logging.LogFilePath != other.Logging.LogFilePath)
    diffs.Add(new("Log File Path", other.Logging.LogFilePath, Logging.LogFilePath));

if (Logging.EnableFileLogging != other.Logging.EnableFileLogging)
    diffs.Add(new("File Logging", other.Logging.EnableFileLogging.ToString(), Logging.EnableFileLogging.ToString()));

if (Logging.EnableConsoleLogging != other.Logging.EnableConsoleLogging)
    diffs.Add(new("Console Logging", other.Logging.EnableConsoleLogging.ToString(), Logging.EnableConsoleLogging.ToString()));
```

**Step 2: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

Expected: Build succeeded.

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: Add LoggingSettings model with defaults for Serilog configuration"
```

---

### Task 3: Add Serilog Configuration Section to appsettings.json

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/appsettings.json`

**Step 1: Replace the existing `"Logging"` section with Serilog config**

Replace the existing `"Logging": { ... }` block with:

```json
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
```

Note: Keep `"AllowedHosts": "*"` and the `"AppSettings"` section as-is.

**Step 2: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

**Step 3: Commit**

```bash
git add -A && git commit -m "chore: Replace default Logging config with Serilog section in appsettings.json"
```

---

## Phase 2: Core Services

### Task 4: Create ISettingsService Interface and SettingsService Implementation

**Files:**
- Create: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Services/ISettingsService.cs`
- Create: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Services/SettingsService.cs`

**Step 1: Create the interface**

Create `Services/ISettingsService.cs`:

```csharp
using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Singleton settings service that holds all application settings in memory,
/// fires change events for immediate UI updates, and queues async persistence.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Current in-memory settings. Fast read, no I/O.
    /// </summary>
    AppSettings Current { get; }

    /// <summary>
    /// Fired immediately when any setting is mutated.
    /// Subscribers (e.g. GiveAway overlay) use this for instant UI updates.
    /// </summary>
    event Action? OnSettingsChanged;

    /// <summary>
    /// Atomically mutates in-memory settings, fires OnSettingsChanged,
    /// and queues debounced async persistence to usersettings.json.
    /// </summary>
    void Update(Action<AppSettings> mutator);

    /// <summary>
    /// Replaces in-memory settings with defaults, fires OnSettingsChanged,
    /// and deletes usersettings.json.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Loads settings from usersettings.json (or falls back to provided defaults).
    /// Called once at startup.
    /// </summary>
    void LoadFromFile(AppSettings fallbackDefaults);
}
```

**Step 2: Create the implementation**

Create `Services/SettingsService.cs`:

```csharp
using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Services;

/// <summary>
/// Singleton in-memory settings store with event-driven change notification
/// and debounced async persistence via SettingsPersistenceService.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly UserSettingsService _userSettingsService;
    private readonly SettingsPersistenceService _persistenceService;
    private readonly ILogger<SettingsService> _logger;
    private readonly object _lock = new();
    private AppSettings _current = new();

    public SettingsService(
        UserSettingsService userSettingsService,
        SettingsPersistenceService persistenceService,
        ILogger<SettingsService> logger)
    {
        _userSettingsService = userSettingsService;
        _persistenceService = persistenceService;
        _logger = logger;
    }

    public AppSettings Current
    {
        get { lock (_lock) { return _current; } }
    }

    public event Action? OnSettingsChanged;

    public void Update(Action<AppSettings> mutator)
    {
        lock (_lock)
        {
            mutator(_current);
        }

        _logger.LogDebug("Settings updated in memory");

        // Apply to legacy GiveAwayHelpers (theme + file path that still use static state)
        ApplyToLegacyHelpers();

        // Fire change event for UI subscribers (GiveAway overlay, etc.)
        OnSettingsChanged?.Invoke();

        // Queue debounced async persistence
        _persistenceService.QueueSave(CloneCurrentSettings());

        _logger.LogDebug("Settings change queued for persistence");
    }

    public void ResetToDefaults()
    {
        lock (_lock)
        {
            _current = AppSettings.GetDefaults();
        }

        _logger.LogInformation("Settings reset to defaults");

        ApplyToLegacyHelpers();
        _userSettingsService.DeleteUserSettings();
        OnSettingsChanged?.Invoke();
    }

    public void LoadFromFile(AppSettings fallbackDefaults)
    {
        var loaded = _userSettingsService.LoadUserSettings();

        lock (_lock)
        {
            _current = loaded ?? fallbackDefaults;
        }

        if (loaded != null)
        {
            _logger.LogInformation("Loaded user settings from {Path}", _userSettingsService.GetUserSettingsPath());
        }
        else
        {
            _logger.LogInformation("No user settings found, using defaults from appsettings.json");
        }

        ApplyToLegacyHelpers();
    }

    /// <summary>
    /// Syncs current settings to GiveAwayHelpers static state.
    /// Needed because FireBotFileReader and theme logic still read from statics.
    /// This will be removed when those are also migrated to DI.
    /// </summary>
    private void ApplyToLegacyHelpers()
    {
        var s = Current;
        GiveAwayHelpers.ApplySettings(s);
    }

    private AppSettings CloneCurrentSettings()
    {
        lock (_lock)
        {
            // Create a fresh copy for persistence to avoid mutation during serialization
            return new AppSettings
            {
                FireBotFileFolder = _current.FireBotFileFolder,
                CountdownTimerEnabled = _current.CountdownTimerEnabled,
                CountdownHours = _current.CountdownHours,
                CountdownMinutes = _current.CountdownMinutes,
                CountdownSeconds = _current.CountdownSeconds,
                PrizeSectionWidthPercent = _current.PrizeSectionWidthPercent,
                PrizeFontSizeRem = _current.PrizeFontSizeRem,
                TimerFontSizeRem = _current.TimerFontSizeRem,
                EntriesFontSizeRem = _current.EntriesFontSizeRem,
                Theme = ThemeSettings.FromThemeConfig(
                    _current.Theme.ToThemeConfig()),
                Logging = new LoggingSettings
                {
                    MinimumLevel = _current.Logging.MinimumLevel,
                    LogFilePath = _current.Logging.LogFilePath,
                    EnableFileLogging = _current.Logging.EnableFileLogging,
                    EnableConsoleLogging = _current.Logging.EnableConsoleLogging,
                }
            };
        }
    }
}
```

**Step 3: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

Expected: Build succeeded.

**Step 4: Commit**

```bash
git add -A && git commit -m "feat: Create ISettingsService singleton with event-driven change notification"
```

---

### Task 5: Fix FireBotFileReader with Sticky Cached Reads and Logging

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Helpers/FireBotFileReader.cs`

**Step 1: Rewrite FireBotFileReader with caching and Serilog**

Replace the entire file content with:

```csharp
namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

/// <summary>
/// Reads Firebot giveaway files with sticky caching.
/// When a file read fails (e.g. file locked by Firebot during write),
/// the last successfully read value is returned instead of empty string.
/// This prevents the giveaway timer from resetting on transient I/O failures.
/// </summary>
public static class FireBotFileReader
{
    private static readonly ILogger _logger = LoggerFactory
        .Create(b => b.AddSerilog())
        .CreateLogger(typeof(FireBotFileReader));

    private static string _fireBotFileFolder = @"G:\Giveaway";
    private static readonly string _prizeFile = "prize.txt";
    private static readonly string _winnerFile = "winner.txt";
    private static readonly string _entriesFile = "giveaway.txt";

    // Sticky cache: last-known-good values survive transient file read failures
    private static string _lastPrize = string.Empty;
    private static string _lastWinner = string.Empty;
    private static string[] _lastEntries = [];

    public static void SetFireBotFileFolder(string folderPath)
    {
        _fireBotFileFolder = folderPath;
        _logger.LogInformation("Firebot file folder set to: {FolderPath}", folderPath);
    }

    public static string GetFireBotFileFolder() => _fireBotFileFolder;

    public static async Task<string> GetPrizeAsync()
    {
        var result = await GetFireBotFileAsync(_prizeFile);
        if (result != null)
        {
            _lastPrize = result;
            return result;
        }
        _logger.LogWarning("Prize file read failed, using cached value: '{CachedPrize}'", _lastPrize);
        return _lastPrize;
    }

    public static async Task<string> GetWinnerAsync()
    {
        var result = await GetFireBotFileAsync(_winnerFile);
        if (result != null)
        {
            _lastWinner = result;
            return result;
        }
        _logger.LogWarning("Winner file read failed, using cached value: '{CachedWinner}'", _lastWinner);
        return _lastWinner;
    }

    public static async Task<string[]> GetEntriesAsync()
    {
        var result = await GetFireBotFileAsync(_entriesFile);
        if (result != null)
        {
            _lastEntries = result.Split(
                [Environment.NewLine, "\n"],
                StringSplitOptions.RemoveEmptyEntries);
            return _lastEntries;
        }
        _logger.LogWarning("Entries file read failed, using cached value ({CachedCount} entries)", _lastEntries.Length);
        return _lastEntries;
    }

    /// <summary>
    /// Reads a Firebot file. Returns the content string on success, or null on failure.
    /// Null signals to callers that they should use the cached value.
    /// </summary>
    private static async Task<string?> GetFireBotFileAsync(string fileName)
    {
        string filePath = Path.Combine(_fireBotFileFolder, fileName);
        try
        {
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                _logger.LogDebug("Read {FileName}: {Length} chars", fileName, content.Length);
                return content;
            }

            // File doesn't exist — this is normal (e.g. no active giveaway)
            return string.Empty;
        }
        catch (IOException ex)
        {
            // File locked by Firebot during write — expected, use cache
            _logger.LogWarning("File I/O error reading {FileName}: {Message}", fileName, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reading {FileName}", fileName);
            return null;
        }
    }
}
```

Note: The static logger uses `LoggerFactory.Create(b => b.AddSerilog())`. This requires adding `using Serilog;` and `using Microsoft.Extensions.Logging;` at the top. Since this is a static class that can't use DI, this is the standard Serilog pattern for statics.

Actually, for a static class the simplest approach is to use Serilog's static `Log.Logger` directly:

```csharp
using Serilog;

namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

public static class FireBotFileReader
{
    private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(FireBotFileReader));
    // ... rest of file using _logger.Warning(), _logger.Information(), etc.
```

Use `Log.ForContext(typeof(FireBotFileReader))` — this is the idiomatic Serilog pattern for static classes.

**Step 2: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

Expected: Build succeeded.

**Step 3: Commit**

```bash
git add -A && git commit -m "fix: Add sticky cached reads to FireBotFileReader to prevent timer reset on file lock"
```

---

## Phase 3: Refactor Consumers

### Task 6: Update Program.cs for New Service Registration and Startup

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Program.cs`

**Step 1: Register ISettingsService and update startup logic**

In the service registration section, add:

```csharp
// Add SettingsService as singleton (replaces GiveAwayHelpers for settings management)
builder.Services.AddSingleton<ISettingsService, SettingsService>();
```

Replace the `app.Lifetime.ApplicationStarted.Register(() => { ... })` block. The new block uses `ISettingsService.LoadFromFile()`:

```csharp
app.Lifetime.ApplicationStarted.Register(() =>
{
    var settingsService = app.Services.GetRequiredService<ISettingsService>();

    // Build fallback defaults from appsettings.json
    var fallbackDefaults = new AppSettings
    {
        FireBotFileFolder = app.Configuration.GetValue("AppSettings:FireBotFileFolder", @"G:\Giveaway") ?? @"G:\Giveaway",
        CountdownTimerEnabled = app.Configuration.GetValue<bool>("AppSettings:CountdownTimerEnabled", true),
        CountdownHours = app.Configuration.GetValue<int>("AppSettings:CountdownHours", 0),
        CountdownMinutes = app.Configuration.GetValue<int>("AppSettings:CountdownMinutes", 60),
        CountdownSeconds = app.Configuration.GetValue<int>("AppSettings:CountdownSeconds", 0),
        PrizeSectionWidthPercent = app.Configuration.GetValue<int>("AppSettings:PrizeSectionWidthPercent", 75),
        PrizeFontSizeRem = app.Configuration.GetValue<double>("AppSettings:PrizeFontSizeRem", 3.5),
        TimerFontSizeRem = app.Configuration.GetValue<double>("AppSettings:TimerFontSizeRem", 3.0),
        EntriesFontSizeRem = app.Configuration.GetValue<double>("AppSettings:EntriesFontSizeRem", 2.5),
        Theme = new ThemeSettings
        {
            Name = app.Configuration.GetValue<string>("AppSettings:Theme:Name", "Warframe") ?? "Warframe",
            PrimaryColor = app.Configuration.GetValue<string>("AppSettings:Theme:PrimaryColor", "#00fff9") ?? "#00fff9",
            SecondaryColor = app.Configuration.GetValue<string>("AppSettings:Theme:SecondaryColor", "#ff00c8") ?? "#ff00c8",
            BackgroundStart = app.Configuration.GetValue<string>("AppSettings:Theme:BackgroundStart", "rgba(0, 0, 0, 0.9)") ?? "rgba(0, 0, 0, 0.9)",
            BackgroundEnd = app.Configuration.GetValue<string>("AppSettings:Theme:BackgroundEnd", "rgba(15, 25, 35, 0.98)") ?? "rgba(15, 25, 35, 0.98)",
            BorderGlowColor = app.Configuration.GetValue<string>("AppSettings:Theme:BorderGlowColor", "rgba(0, 255, 255, 0.15)") ?? "rgba(0, 255, 255, 0.15)",
            TextColor = app.Configuration.GetValue<string>("AppSettings:Theme:TextColor", "#ffffff") ?? "#ffffff",
            TimerExpiredColor = app.Configuration.GetValue<string>("AppSettings:Theme:TimerExpiredColor", "#ff3333") ?? "#ff3333",
            SeparatorColor = app.Configuration.GetValue<string>("AppSettings:Theme:SeparatorColor", "rgba(0, 255, 255, 0.5)") ?? "rgba(0, 255, 255, 0.5)"
        }
    };

    settingsService.LoadFromFile(fallbackDefaults);

    // Apply LoggingLevelSwitch from loaded settings
    var logLevelSwitch = app.Services.GetRequiredService<LoggingLevelSwitch>();
    if (Enum.TryParse<LogEventLevel>(settingsService.Current.Logging.MinimumLevel, true, out var level))
    {
        logLevelSwitch.MinimumLevel = level;
    }

    Log.Information("Application started — version {Version}",
        app.Services.GetRequiredService<VersionService>().GetDisplayVersion());

    // Launch browser with correct port
    string url = "http://localhost:5000/giveaway";
    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to open browser");
    }
});
```

Remove the old `Console.WriteLine` calls — Serilog handles all logging now.

**Step 2: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: Wire up ISettingsService and Serilog LoggingLevelSwitch in Program.cs"
```

---

### Task 7: Refactor GiveAway.razor — Subscribe to ISettingsService Events

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Components/Pages/GiveAway.razor`

**Key changes:**
1. Inject `ISettingsService` instead of reading from `GiveAwayHelpers` statics
2. Subscribe to `OnSettingsChanged` for instant settings updates from Setup page
3. Remove polling of `GiveAwayHelpers` getters from `GetLatestGiveAwayDetails()` (lines 196-219)
4. Add Serilog logging for timer reset reasons and state transitions

**Step 1: Update the @inject directives**

Add at the top (after existing injects):

```razor
@inject ISettingsService SettingsService
```

**Step 2: Update OnInitializedAsync**

Read initial settings from `ISettingsService.Current` instead of `GiveAwayHelpers`:

```csharp
protected override async Task OnInitializedAsync()
{
    // Load initial settings from singleton service
    var settings = SettingsService.Current;
    prizeSectionWidth = settings.PrizeSectionWidthPercent;
    prizeFontSize = settings.PrizeFontSizeRem;
    timerFontSize = settings.TimerFontSizeRem;
    entriesFontSize = settings.EntriesFontSizeRem;
    isTimerEnabled = settings.CountdownTimerEnabled;
    currentTheme = settings.Theme.ToThemeConfig();

    await GetLatestGiveAwayDetails();
    fileScanTimer.Elapsed += new ElapsedEventHandler(HandleFileScanTimer);
    countdownTimer.Elapsed += new ElapsedEventHandler(HandleCountdownTimer);
    StartFileScanTimer();

    if (isTimerEnabled)
    {
        StartCountdownTimer();
    }

    TimerService.OnTimerReset += HandleTimerReset;
    ThemeService.OnThemeChanged += HandleThemeChanged;
    SettingsService.OnSettingsChanged += HandleSettingsChanged;
}
```

**Step 3: Add HandleSettingsChanged handler**

```csharp
private async void HandleSettingsChanged()
{
    var settings = SettingsService.Current;

    // Update display settings immediately
    prizeSectionWidth = settings.PrizeSectionWidthPercent;
    prizeFontSize = settings.PrizeFontSizeRem;
    timerFontSize = settings.TimerFontSizeRem;
    entriesFontSize = settings.EntriesFontSizeRem;
    currentTheme = settings.Theme.ToThemeConfig();

    // Handle timer enabled/disabled changes
    bool wasTimerEnabled = isTimerEnabled;
    isTimerEnabled = settings.CountdownTimerEnabled;

    if (wasTimerEnabled && !isTimerEnabled)
    {
        countdownTimer.Stop();
        Log.Debug("Timer disabled via settings change");
    }
    else if (!wasTimerEnabled && isTimerEnabled && isGiveAwayRunning && !doesGiveawayHaveWinner)
    {
        ResetTimer();
        Log.Debug("Timer re-enabled via settings change");
    }

    await InvokeAsync(StateHasChanged);
}
```

**Step 4: Simplify GetLatestGiveAwayDetails**

Remove lines 196-219 (the settings polling block). The file scan loop should now ONLY handle file data (prize, entries, winner) and timer management — settings come via `HandleSettingsChanged`. Also add logging for timer resets:

In the timer management section (lines 171-194), add log messages:

```csharp
// Timer management - only if timer is enabled
if (isTimerEnabled)
{
    if (doesGiveawayHaveWinner && countdownTimer.Enabled)
    {
        countdownTimer.Stop();
        Log.Information("Timer paused: winner detected");
    }
    else if (hadWinnerBefore && !doesGiveawayHaveWinner && isGiveAwayRunning)
    {
        ResetTimer();
        Log.Information("Timer reset: winner cleared, giveaway still running");
    }
    else if (!hadPrizeBefore && hasPrizeNow && !doesGiveawayHaveWinner)
    {
        ResetTimer();
        Log.Information("Timer reset: new giveaway started (prize detected)");
    }
    else if (!isGiveAwayRunning)
    {
        ResetTimer();
        Log.Debug("Timer reset: giveaway not running");
    }
}
```

Delete the old settings polling block entirely (the section that reads `GiveAwayHelpers.GetPrizeSectionWidth()` etc. and the timer enabled state check).

**Step 5: Update field initializers**

Change these field initializers at the top of `@code`:

```csharp
// Old (remove):
private int prizeSectionWidth = GiveAwayHelpers.GetPrizeSectionWidth();
private double prizeFontSize = GiveAwayHelpers.GetPrizeFontSize();
// etc.

// New:
private int prizeSectionWidth;
private double prizeFontSize;
private double timerFontSize;
private double entriesFontSize;
private bool isTimerEnabled;
private ThemeConfig currentTheme = new();
```

These get set in `OnInitializedAsync` from `SettingsService.Current`.

**Step 6: Update Dispose to unsubscribe**

```csharp
public void Dispose()
{
    fileScanTimer.Dispose();
    countdownTimer.Dispose();
    TimerService.OnTimerReset -= HandleTimerReset;
    ThemeService.OnThemeChanged -= HandleThemeChanged;
    SettingsService.OnSettingsChanged -= HandleSettingsChanged;
}
```

**Step 7: Add using for Serilog at top of @code block**

At the top of the file, ensure there is `@using Serilog` (or use `Log.` directly since Serilog's `Log` is a global static).

**Step 8: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

**Step 9: Commit**

```bash
git add -A && git commit -m "refactor: GiveAway.razor subscribes to ISettingsService events instead of polling"
```

---

### Task 8: Refactor Setup.razor — Slider Fix + ISettingsService + Tick Marks

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Components/Pages/Setup.razor`

This is the largest task. Key changes:

1. **Inject `ISettingsService`** — replace `SettingsPersistenceService` and direct `GiveAwayHelpers` calls
2. **Slider fix** — remove `@bind:after` from sliders, add `@onpointerup` for commit-on-release
3. **Add `<datalist>` tick marks** on all sliders
4. **Replace all `GiveAwayHelpers` calls** with `SettingsService.Update()`
5. **Simplify `@code` block** — remove `BuildCurrentSettings()`, `QueueSettingsSave()`, many setter methods

**Step 1: Update @inject directives**

Replace:
```razor
@inject UserSettingsService UserSettingsService
@inject SettingsPersistenceService PersistenceService
```

With:
```razor
@inject ISettingsService SettingsService
```

Keep `TimerService`, `ThemeService`, `VersionService` injections.

**Step 2: Fix all slider inputs — remove `@bind:after`, add `@onpointerup` and `list` attribute**

Example for Prize Section Width slider. Change from:

```razor
<input type="range"
       class="form-range"
       min="50"
       max="90"
       step="5"
       @bind="prizeSectionWidth"
       @bind:event="oninput"
       @bind:after="SetPrizeSectionWidth" />
```

To:

```razor
<input type="range"
       class="form-range"
       min="50"
       max="90"
       step="5"
       list="widthTicks"
       @bind="prizeSectionWidth"
       @bind:event="oninput"
       @onpointerup="CommitPrizeSectionWidth" />
<datalist id="widthTicks">
    @for (int i = 50; i <= 90; i += 5)
    {
        <option value="@i"></option>
    }
</datalist>
```

Apply the same pattern to all four sliders:

- Prize Font Size: `list="prizeFontTicks"`, `@onpointerup="CommitPrizeFontSize"`
- Timer Font Size: `list="timerFontTicks"`, `@onpointerup="CommitTimerFontSize"`
- Entries Font Size: `list="entriesFontTicks"`, `@onpointerup="CommitEntriesFontSize"`

Font size datalists:
```razor
<datalist id="prizeFontTicks">
    @for (double d = 1.0; d <= 6.0; d += 1.0)
    {
        <option value="@d.ToString("0.0")"></option>
    }
</datalist>
```

**Step 3: Numeric inputs — change `@bind:after` to use `SettingsService`**

Numeric inputs (which fire on `onchange` / blur) can keep `@bind:after` but the handler calls `SettingsService.Update()`:

```razor
<input type="number"
       class="form-control"
       min="50" max="90" step="5"
       inputmode="numeric"
       @bind="prizeSectionWidth"
       @bind:event="onchange"
       @bind:after="CommitPrizeSectionWidthClamped" />
```

**Step 4: Rewrite @code block methods**

Replace the settings methods with:

```csharp
// === Commit methods (called on pointer-up for sliders, or on change for other inputs) ===

private void CommitPrizeSectionWidth()
{
    SettingsService.Update(s => s.PrizeSectionWidthPercent = prizeSectionWidth);
    UpdateSettingsDiff();
}

private void CommitPrizeSectionWidthClamped()
{
    prizeSectionWidth = Math.Clamp(prizeSectionWidth, 50, 90);
    CommitPrizeSectionWidth();
}

private void CommitPrizeFontSize()
{
    SettingsService.Update(s => s.PrizeFontSizeRem = prizeFontSize);
    UpdateSettingsDiff();
}

private void CommitPrizeFontSizeClamped()
{
    prizeFontSize = Math.Clamp(prizeFontSize, 1.0, 6.0);
    CommitPrizeFontSize();
}

private void CommitTimerFontSize()
{
    SettingsService.Update(s => s.TimerFontSizeRem = timerFontSize);
    UpdateSettingsDiff();
}

private void CommitTimerFontSizeClamped()
{
    timerFontSize = Math.Clamp(timerFontSize, 1.0, 6.0);
    CommitTimerFontSize();
}

private void CommitEntriesFontSize()
{
    SettingsService.Update(s => s.EntriesFontSizeRem = entriesFontSize);
    UpdateSettingsDiff();
}

private void CommitEntriesFontSizeClamped()
{
    entriesFontSize = Math.Clamp(entriesFontSize, 1.0, 6.0);
    CommitEntriesFontSize();
}

private void SetFirebotFilePath()
{
    SettingsService.Update(s => s.FireBotFileFolder = firebotFilePath);
    UpdateSettingsDiff();
}

private void SetCountdownTime()
{
    SettingsService.Update(s =>
    {
        s.CountdownHours = countdownHours;
        s.CountdownMinutes = countdownMinutes;
        s.CountdownSeconds = countdownSeconds;
    });
    UpdateSettingsDiff();
}

private void SetCountdownTimerEnabled()
{
    bool wasEnabled = SettingsService.Current.CountdownTimerEnabled;
    SettingsService.Update(s => s.CountdownTimerEnabled = countdownTimerEnabled);

    if (!wasEnabled && countdownTimerEnabled)
    {
        TimerService.ResetTimer();
    }

    UpdateSettingsDiff();
}
```

**Step 5: Update OnInitialized to read from ISettingsService**

```csharp
protected override void OnInitialized()
{
    versionDisplay = VersionService.GetDisplayVersion();

    var settings = SettingsService.Current;
    countdownHours = settings.CountdownHours;
    countdownMinutes = settings.CountdownMinutes;
    countdownSeconds = settings.CountdownSeconds;
    countdownTimerEnabled = settings.CountdownTimerEnabled;
    firebotFilePath = settings.FireBotFileFolder;
    prizeSectionWidth = settings.PrizeSectionWidthPercent;
    prizeFontSize = settings.PrizeFontSizeRem;
    timerFontSize = settings.TimerFontSizeRem;
    entriesFontSize = settings.EntriesFontSizeRem;

    currentTheme = GiveAwayHelpers.GetCurrentTheme();
    selectedThemeName = GiveAwayHelpers.IsUsingCustomTheme() ? "Custom" : currentTheme.Name;
    customPrimaryColor = currentTheme.PrimaryColor;
    customSecondaryColor = currentTheme.SecondaryColor;
    customTimerExpiredColor = currentTheme.TimerExpiredColor;

    UpdateSettingsDiff();
}
```

**Step 6: Update theme methods to use SettingsService**

```csharp
private void OnPresetThemeChanged()
{
    if (selectedThemeName == "Custom")
    {
        GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.PrimaryColor), customPrimaryColor);
        GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.SecondaryColor), customSecondaryColor);
        GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.TimerExpiredColor), customTimerExpiredColor);
        currentTheme = GiveAwayHelpers.GetCurrentTheme();
    }
    else
    {
        GiveAwayHelpers.SetPresetTheme(selectedThemeName);
        currentTheme = GiveAwayHelpers.GetCurrentTheme();
        customPrimaryColor = currentTheme.PrimaryColor;
        customSecondaryColor = currentTheme.SecondaryColor;
        customTimerExpiredColor = currentTheme.TimerExpiredColor;
    }
    ThemeService.NotifyThemeChanged();
    SettingsService.Update(s => s.Theme = ThemeSettings.FromThemeConfig(currentTheme));
    UpdateSettingsDiff();
}
```

**Step 7: Update UpdateSettingsDiff and ConfirmReset**

```csharp
private void UpdateSettingsDiff()
{
    var defaults = AppSettings.GetDefaults();
    var current = SettingsService.Current;
    settingsDiff = current.GetDifferences(defaults);
    hasCustomSettings = settingsDiff.Count > 0;
}

private void ConfirmReset()
{
    SettingsService.ResetToDefaults();

    // Reload local UI state from fresh defaults
    var defaults = SettingsService.Current;
    firebotFilePath = defaults.FireBotFileFolder;
    countdownTimerEnabled = defaults.CountdownTimerEnabled;
    countdownHours = defaults.CountdownHours;
    countdownMinutes = defaults.CountdownMinutes;
    countdownSeconds = defaults.CountdownSeconds;
    prizeSectionWidth = defaults.PrizeSectionWidthPercent;
    prizeFontSize = defaults.PrizeFontSizeRem;
    timerFontSize = defaults.TimerFontSizeRem;
    entriesFontSize = defaults.EntriesFontSizeRem;

    currentTheme = GiveAwayHelpers.GetCurrentTheme();
    selectedThemeName = currentTheme.Name;
    customPrimaryColor = currentTheme.PrimaryColor;
    customSecondaryColor = currentTheme.SecondaryColor;
    customTimerExpiredColor = currentTheme.TimerExpiredColor;

    ThemeService.NotifyThemeChanged();
    showResetConfirm = false;
    showDiff = false;
    UpdateSettingsDiff();
}
```

**Step 8: Update ResetIndividualSetting to use SettingsService**

Replace `GiveAwayHelpers.SetXxx()` + `QueueSettingsSave()` with `SettingsService.Update()`:

```csharp
private void ResetIndividualSetting(string settingName)
{
    var defaults = AppSettings.GetDefaults();

    switch (settingName)
    {
        case "Firebot File Path":
            firebotFilePath = defaults.FireBotFileFolder;
            SettingsService.Update(s => s.FireBotFileFolder = firebotFilePath);
            break;

        case "Timer Enabled":
            countdownTimerEnabled = defaults.CountdownTimerEnabled;
            SettingsService.Update(s => s.CountdownTimerEnabled = countdownTimerEnabled);
            break;

        case "Countdown Hours":
            countdownHours = defaults.CountdownHours;
            SettingsService.Update(s => s.CountdownHours = countdownHours);
            break;

        case "Countdown Minutes":
            countdownMinutes = defaults.CountdownMinutes;
            SettingsService.Update(s => s.CountdownMinutes = countdownMinutes);
            break;

        case "Countdown Seconds":
            countdownSeconds = defaults.CountdownSeconds;
            SettingsService.Update(s => s.CountdownSeconds = countdownSeconds);
            break;

        case "Prize Section Width":
            prizeSectionWidth = defaults.PrizeSectionWidthPercent;
            SettingsService.Update(s => s.PrizeSectionWidthPercent = prizeSectionWidth);
            break;

        case "Prize Font Size":
            prizeFontSize = defaults.PrizeFontSizeRem;
            SettingsService.Update(s => s.PrizeFontSizeRem = prizeFontSize);
            break;

        case "Timer Font Size":
            timerFontSize = defaults.TimerFontSizeRem;
            SettingsService.Update(s => s.TimerFontSizeRem = timerFontSize);
            break;

        case "Entries Font Size":
            entriesFontSize = defaults.EntriesFontSizeRem;
            SettingsService.Update(s => s.EntriesFontSizeRem = entriesFontSize);
            break;

        case "Theme":
            GiveAwayHelpers.SetPresetTheme(defaults.Theme.Name);
            currentTheme = GiveAwayHelpers.GetCurrentTheme();
            selectedThemeName = currentTheme.Name;
            customPrimaryColor = currentTheme.PrimaryColor;
            customSecondaryColor = currentTheme.SecondaryColor;
            customTimerExpiredColor = currentTheme.TimerExpiredColor;
            ThemeService.NotifyThemeChanged();
            SettingsService.Update(s => s.Theme = ThemeSettings.FromThemeConfig(currentTheme));
            break;

        case "Primary Color":
        case "Secondary Color":
        case "Timer Expired Color":
            // Reset the specific color
            if (settingName == "Primary Color") customPrimaryColor = defaults.Theme.PrimaryColor;
            if (settingName == "Secondary Color") customSecondaryColor = defaults.Theme.SecondaryColor;
            if (settingName == "Timer Expired Color") customTimerExpiredColor = defaults.Theme.TimerExpiredColor;
            GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.PrimaryColor), customPrimaryColor);
            GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.SecondaryColor), customSecondaryColor);
            GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.TimerExpiredColor), customTimerExpiredColor);
            currentTheme = GiveAwayHelpers.GetCurrentTheme();
            ThemeService.NotifyThemeChanged();
            SettingsService.Update(s => s.Theme = ThemeSettings.FromThemeConfig(currentTheme));
            break;

        case "Log Level":
            SettingsService.Update(s => s.Logging.MinimumLevel = defaults.Logging.MinimumLevel);
            break;
        case "Log File Path":
            SettingsService.Update(s => s.Logging.LogFilePath = defaults.Logging.LogFilePath);
            break;
        case "File Logging":
            SettingsService.Update(s => s.Logging.EnableFileLogging = defaults.Logging.EnableFileLogging);
            break;
        case "Console Logging":
            SettingsService.Update(s => s.Logging.EnableConsoleLogging = defaults.Logging.EnableConsoleLogging);
            break;
    }

    UpdateSettingsDiff();
}
```

**Step 9: Remove dead code**

Delete these methods and fields that are no longer needed:
- `QueueSettingsSave()`
- `BuildCurrentSettings()`
- `SaveSettingsSync()`
- `SetPrizeSectionWidth()`, `SetPrizeFontSize()`, `SetTimerFontSize()`, `SetEntriesFontSize()` (replaced by `CommitXxx()` methods)
- `SetPrizeSectionWidthClamped()`, `SetPrizeFontSizeClamped()`, etc. (replaced by `CommitXxxClamped()` methods)

**Step 10: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

**Step 11: Commit**

```bash
git add -A && git commit -m "refactor: Setup.razor uses ISettingsService, slider fix with onpointerup, add tick marks"
```

---

### Task 9: Add Logging Configuration UI Section to Setup.razor

> **REQUIRED SUB-SKILL:** Use @frontend-design for this UI section.

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Components/Pages/Setup.razor`

**Step 1: Add logging fields to @code block**

```csharp
// Logging settings fields
private string logLevel = "Information";
private string logFilePath = "logs/overlay-.log";
private bool enableFileLogging = true;
private bool enableConsoleLogging = true;
```

Initialize them in `OnInitialized()`:
```csharp
// Initialize logging fields
var loggingSettings = SettingsService.Current.Logging;
logLevel = loggingSettings.MinimumLevel;
logFilePath = loggingSettings.LogFilePath;
enableFileLogging = loggingSettings.EnableFileLogging;
enableConsoleLogging = loggingSettings.EnableConsoleLogging;
```

**Step 2: Inject LoggingLevelSwitch**

Add to the inject directives:
```razor
@inject Serilog.Core.LoggingLevelSwitch LogLevelSwitch
```

**Step 3: Add logging handler methods**

```csharp
private void SetLogLevel()
{
    // Apply runtime log level switch immediately
    if (Enum.TryParse<Serilog.Events.LogEventLevel>(logLevel, true, out var level))
    {
        LogLevelSwitch.MinimumLevel = level;
    }
    SettingsService.Update(s => s.Logging.MinimumLevel = logLevel);
    UpdateSettingsDiff();
}

private void SetLogFilePath()
{
    SettingsService.Update(s => s.Logging.LogFilePath = logFilePath);
    UpdateSettingsDiff();
}

private void SetEnableFileLogging()
{
    SettingsService.Update(s => s.Logging.EnableFileLogging = enableFileLogging);
    UpdateSettingsDiff();
}

private void SetEnableConsoleLogging()
{
    SettingsService.Update(s => s.Logging.EnableConsoleLogging = enableConsoleLogging);
    UpdateSettingsDiff();
}
```

**Step 4: Add Logging UI section to the markup**

Insert this before the Settings Management section (before line 397 `<hr />`), after the Font Sizes section:

```razor
<hr />

<div class="mb-3">
    <label class="form-label">Logging</label>

    <div class="row g-3 mb-3">
        <div class="col-md-6">
            <label class="form-label">Log Level</label>
            <select class="form-select"
                    @bind="logLevel"
                    @bind:after="SetLogLevel">
                <option value="Verbose">Verbose</option>
                <option value="Debug">Debug</option>
                <option value="Information">Information</option>
                <option value="Warning">Warning</option>
                <option value="Error">Error</option>
                <option value="Fatal">Fatal</option>
            </select>
            <small class="form-text text-muted">Changes take effect immediately</small>
        </div>
        <div class="col-md-6">
            <label class="form-label">Log File Path</label>
            <input type="text"
                   class="form-control"
                   placeholder="logs/overlay-.log"
                   @bind="logFilePath"
                   @bind:event="onchange"
                   @bind:after="SetLogFilePath" />
            <small class="form-text text-muted">Changes take effect on next restart</small>
        </div>
    </div>

    <div class="row g-3">
        <div class="col-md-6">
            <div class="form-check">
                <input class="form-check-input"
                       type="checkbox"
                       id="enableFileLogging"
                       @bind="enableFileLogging"
                       @bind:after="SetEnableFileLogging" />
                <label class="form-check-label" for="enableFileLogging">
                    Enable File Logging
                </label>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-check">
                <input class="form-check-input"
                       type="checkbox"
                       id="enableConsoleLogging"
                       @bind="enableConsoleLogging"
                       @bind:after="SetEnableConsoleLogging" />
                <label class="form-check-label" for="enableConsoleLogging">
                    Enable Console Logging
                </label>
            </div>
        </div>
    </div>
</div>
```

**Step 5: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

**Step 6: Commit**

```bash
git add -A && git commit -m "feat: Add logging configuration UI section to Setup page"
```

---

## Phase 4: Cleanup and Version Bump

### Task 10: Clean Up GiveAwayHelpers — Remove Settings Fields

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/Helpers/GiveAwayHelpers.cs`

**Step 1: Remove settings static fields and their getter/setter methods**

Remove these fields (lines 12-19):
- `_countdownTimerEnabled`, `_countdownHours`, `_countdownMinutes`, `_countdownSeconds`
- `_prizeSectionWidthPercent`, `_prizeFontSizeRem`, `_timerFontSizeRem`, `_entriesFontSizeRem`

Remove these methods:
- `SetCountdownTime()`, `GetCountdownTime()`
- `SetCountdownTimerEnabled()`, `GetCountdownTimerEnabled()`
- `SetPrizeSectionWidth()`, `GetPrizeSectionWidth()`
- `SetPrizeFontSize()`, `GetPrizeFontSize()`
- `SetTimerFontSize()`, `GetTimerFontSize()`
- `SetEntriesFontSize()`, `GetEntriesFontSize()`
- `GetCurrentSettings()` (replaced by `ISettingsService.Current`)

**Keep these** (still used for theme management):
- `_currentTheme`, `_useCustomTheme`
- `GetCurrentTheme()`, `SetPresetTheme()`, `SetCustomTheme()`
- `UpdateCustomColor()`, `IsUsingCustomTheme()`, `GetAllPresetThemes()`
- `InitializeTheme()`
- `SetFireBotFileFolder()`, `GetFireBotFileFolder()` (delegates to `FireBotFileReader`)

**Step 2: Update `ApplySettings()` to only set what GiveAwayHelpers still owns**

```csharp
public static void ApplySettings(AppSettings settings)
{
    SetFireBotFileFolder(settings.FireBotFileFolder);
    InitializeTheme(settings.Theme.ToThemeConfig());
}
```

**Step 3: Build to verify**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build
```

If there are compilation errors from remaining references to removed methods (in `GiveAway.razor`'s `ResetTimer()` which calls `GiveAwayHelpers.GetCountdownTime()`), update them:

In `GiveAway.razor` `ResetTimer()`, replace:
```csharp
var (hours, minutes, seconds) = GiveAwayHelpers.GetCountdownTime();
```
With:
```csharp
var s = SettingsService.Current;
var hours = s.CountdownHours;
var minutes = s.CountdownMinutes;
var seconds = s.CountdownSeconds;
```

**Step 4: Commit**

```bash
git add -A && git commit -m "refactor: Remove settings fields from GiveAwayHelpers, keep theme-only logic"
```

---

### Task 11: Version Bump to 2.3.0 and Final Build

**Files:**
- Modify: `FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp/FirebotGiveawayObsOverlay.WebApp.csproj`

**Step 1: Update version properties**

Change:
```xml
<Version>2.2.1</Version>
<AssemblyVersion>2.2.1.0</AssemblyVersion>
<FileVersion>2.2.1.0</FileVersion>
<InformationalVersion>2.2.1</InformationalVersion>
```

To:
```xml
<Version>2.3.0</Version>
<AssemblyVersion>2.3.0.0</AssemblyVersion>
<FileVersion>2.3.0.0</FileVersion>
<InformationalVersion>2.3.0</InformationalVersion>
```

**Step 2: Final build verification**

```bash
cd FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp
dotnet build -c Release
```

Expected: Build succeeded with zero warnings.

**Step 3: Commit**

```bash
git add -A && git commit -m "chore: Bump version to 2.3.0 for Serilog logging, bug fixes, and settings refactor"
```

---

## Dependency Order

```
Task 1 (Serilog packages) ──┐
Task 2 (LoggingSettings)  ──┼── Task 4 (ISettingsService) ──┐
Task 3 (appsettings.json) ──┘                               │
                                                             ├── Task 6 (Program.cs) ──┐
Task 5 (FireBotFileReader) ─────────────────────────────────┘                          │
                                                                                        ├── Task 7 (GiveAway.razor)
                                                                                        ├── Task 8 (Setup.razor sliders)
                                                                                        ├── Task 9 (Setup.razor logging UI)
                                                                                        └── Task 10 (GiveAwayHelpers cleanup)
                                                                                                    │
                                                                                                    └── Task 11 (version bump)
```

Tasks 1-3 can run in parallel. Task 5 can run in parallel with Task 4. Tasks 7-10 depend on Tasks 4+6. Task 11 is last.

---

## Verification Checklist

After all tasks complete, verify:

- [ ] `dotnet build -c Release` passes with zero errors
- [ ] Application starts and shows giveaway overlay at localhost:5000/giveaway
- [ ] Logs appear in console and in `logs/` directory
- [ ] Setup page shows new Logging section
- [ ] Changing log level in Setup takes effect immediately
- [ ] Sliders drag smoothly without snapping
- [ ] Slider tick marks are visible
- [ ] Settings changes on Setup page are reflected immediately on the giveaway overlay
- [ ] Timer does not reset when Firebot files are temporarily locked
- [ ] Settings persist across application restarts
- [ ] Reset to defaults works correctly
