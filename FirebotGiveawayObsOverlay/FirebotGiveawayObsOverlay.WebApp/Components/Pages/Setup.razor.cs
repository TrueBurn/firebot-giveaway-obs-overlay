using FirebotGiveawayObsOverlay.WebApp.Helpers;
using FirebotGiveawayObsOverlay.WebApp.Models;
using FirebotGiveawayObsOverlay.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Serilog.Events;

namespace FirebotGiveawayObsOverlay.WebApp.Components.Pages;

public partial class Setup
{
    [Inject] private TimerService TimerService { get; set; } = default!;
    [Inject] private ThemeService ThemeService { get; set; } = default!;
    [Inject] private VersionService VersionService { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private Serilog.Core.LoggingLevelSwitch LogLevelSwitch { get; set; } = default!;

    private string versionDisplay = "";
    private string firebotFilePath = "";
    private bool countdownTimerEnabled;
    private int countdownHours;
    private int countdownMinutes;
    private int countdownSeconds;
    private string resetMessage = "";
    private bool resetSuccess = false;
    private int prizeSectionWidth;
    private double prizeFontSize;
    private double timerFontSize;
    private double entriesFontSize;

    // Theme-related fields
    private List<ThemeConfig> presetThemes = GiveAwayHelpers.GetAllPresetThemes();
    private ThemeConfig currentTheme = new();
    private string selectedThemeName = "";
    private string customPrimaryColor = "#00fff9";
    private string customSecondaryColor = "#ff00c8";
    private string customTimerExpiredColor = "#ff3333";

    // Logging settings fields
    private LogEventLevel logLevel;
    private string logFilePath = "logs/overlay-.log";
    private bool enableFileLogging = true;
    private bool enableConsoleLogging = true;

    // Reset settings fields
    private bool hasCustomSettings = false;
    private bool showDiff = false;
    private bool showResetConfirm = false;
    private List<SettingsDiff> settingsDiff = new();

    protected override void OnInitialized()
    {
        versionDisplay = VersionService.GetDisplayVersion();

        var s = SettingsService.Current;
        firebotFilePath = s.FireBotFileFolder;
        countdownTimerEnabled = s.CountdownTimerEnabled;
        countdownHours = s.CountdownHours;
        countdownMinutes = s.CountdownMinutes;
        countdownSeconds = s.CountdownSeconds;
        prizeSectionWidth = s.PrizeSectionWidthPercent;
        prizeFontSize = s.PrizeFontSizeRem;
        timerFontSize = s.TimerFontSizeRem;
        entriesFontSize = s.EntriesFontSizeRem;

        currentTheme = GiveAwayHelpers.GetCurrentTheme();
        selectedThemeName = GiveAwayHelpers.IsUsingCustomTheme() ? "Custom" : currentTheme.Name;
        customPrimaryColor = currentTheme.PrimaryColor;
        customSecondaryColor = currentTheme.SecondaryColor;
        customTimerExpiredColor = currentTheme.TimerExpiredColor;

        // Initialize logging fields
        logLevel = s.Logging.MinimumLevel;
        logFilePath = s.Logging.LogFilePath;
        enableFileLogging = s.Logging.EnableFileLogging;
        enableConsoleLogging = s.Logging.EnableConsoleLogging;

        UpdateSettingsDiff();
    }

    private void UpdateSettingsDiff()
    {
        var defaults = AppSettings.GetDefaults();
        var current = SettingsService.Current;
        settingsDiff = current.GetDifferences(defaults);
        hasCustomSettings = settingsDiff.Count > 0;
    }

    private void ToggleDiffView()
    {
        showDiff = !showDiff;
    }

    private void ShowResetConfirm()
    {
        showResetConfirm = true;
    }

    private void CancelReset()
    {
        showResetConfirm = false;
    }

    // --- Slider value callbacks (SliderSetting handles parsing + clamping) ---

    private void OnWidthChanged(double val)
    {
        prizeSectionWidth = (int)val;
        SettingsService.Update(s => s.PrizeSectionWidthPercent = prizeSectionWidth);
        UpdateSettingsDiff();
    }

    private void OnPrizeFontChanged(double val)
    {
        prizeFontSize = val;
        SettingsService.Update(s => s.PrizeFontSizeRem = prizeFontSize);
        UpdateSettingsDiff();
    }

    private void OnTimerFontChanged(double val)
    {
        timerFontSize = val;
        SettingsService.Update(s => s.TimerFontSizeRem = timerFontSize);
        UpdateSettingsDiff();
    }

    private void OnEntriesFontChanged(double val)
    {
        entriesFontSize = val;
        SettingsService.Update(s => s.EntriesFontSizeRem = entriesFontSize);
        UpdateSettingsDiff();
    }

    // --- Other setting setters ---

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
        SettingsService.Update(s => s.CountdownTimerEnabled = countdownTimerEnabled);

        if (!countdownTimerEnabled)
        {
            // Timer disabled - GiveAway.razor handles stopping via OnSettingsChanged
        }
        else
        {
            TimerService.ResetTimer();
        }

        UpdateSettingsDiff();
    }

    private void ResetGiveawayTimer()
    {
        // Only reset if timer is enabled
        if (!countdownTimerEnabled)
        {
            resetMessage = "Timer is disabled";
            resetSuccess = false;
            return;
        }

        try
        {
            TimerService.ResetTimer();
            resetMessage = "Timer reset successfully!";
            resetSuccess = true;
        }
        catch (Exception ex)
        {
            resetMessage = $"Error: {ex.Message}";
            resetSuccess = false;
        }
    }

    // --- Theme methods ---

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

    private void OnCustomColorChanged()
    {
        selectedThemeName = "Custom";
        GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.PrimaryColor), customPrimaryColor);
        GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.SecondaryColor), customSecondaryColor);
        GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.TimerExpiredColor), customTimerExpiredColor);
        currentTheme = GiveAwayHelpers.GetCurrentTheme();
        ThemeService.NotifyThemeChanged();
        SettingsService.Update(s => s.Theme = ThemeSettings.FromThemeConfig(currentTheme));
        UpdateSettingsDiff();
    }

    // --- Logging methods ---

    private void SetLogLevel()
    {
        LogLevelSwitch.MinimumLevel = logLevel;
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

    // --- Reset methods ---

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
                customPrimaryColor = defaults.Theme.PrimaryColor;
                GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.PrimaryColor), customPrimaryColor);
                currentTheme = GiveAwayHelpers.GetCurrentTheme();
                ThemeService.NotifyThemeChanged();
                SettingsService.Update(s => s.Theme = ThemeSettings.FromThemeConfig(currentTheme));
                break;

            case "Secondary Color":
                customSecondaryColor = defaults.Theme.SecondaryColor;
                GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.SecondaryColor), customSecondaryColor);
                currentTheme = GiveAwayHelpers.GetCurrentTheme();
                ThemeService.NotifyThemeChanged();
                SettingsService.Update(s => s.Theme = ThemeSettings.FromThemeConfig(currentTheme));
                break;

            case "Timer Expired Color":
                customTimerExpiredColor = defaults.Theme.TimerExpiredColor;
                GiveAwayHelpers.UpdateCustomColor(nameof(ThemeConfig.TimerExpiredColor), customTimerExpiredColor);
                currentTheme = GiveAwayHelpers.GetCurrentTheme();
                ThemeService.NotifyThemeChanged();
                SettingsService.Update(s => s.Theme = ThemeSettings.FromThemeConfig(currentTheme));
                break;

            case "Log Level":
                logLevel = defaults.Logging.MinimumLevel;
                LogLevelSwitch.MinimumLevel = logLevel;
                SettingsService.Update(s => s.Logging.MinimumLevel = logLevel);
                break;

            case "Log File Path":
                logFilePath = defaults.Logging.LogFilePath;
                SettingsService.Update(s => s.Logging.LogFilePath = logFilePath);
                break;

            case "File Logging":
                enableFileLogging = defaults.Logging.EnableFileLogging;
                SettingsService.Update(s => s.Logging.EnableFileLogging = enableFileLogging);
                break;

            case "Console Logging":
                enableConsoleLogging = defaults.Logging.EnableConsoleLogging;
                SettingsService.Update(s => s.Logging.EnableConsoleLogging = enableConsoleLogging);
                break;
        }

        UpdateSettingsDiff();
    }

    private void ConfirmReset()
    {
        SettingsService.ResetToDefaults();

        // Reload local state from the now-reset service
        var s = SettingsService.Current;
        firebotFilePath = s.FireBotFileFolder;
        countdownTimerEnabled = s.CountdownTimerEnabled;
        countdownHours = s.CountdownHours;
        countdownMinutes = s.CountdownMinutes;
        countdownSeconds = s.CountdownSeconds;
        prizeSectionWidth = s.PrizeSectionWidthPercent;
        prizeFontSize = s.PrizeFontSizeRem;
        timerFontSize = s.TimerFontSizeRem;
        entriesFontSize = s.EntriesFontSizeRem;

        currentTheme = GiveAwayHelpers.GetCurrentTheme();
        selectedThemeName = currentTheme.Name;
        customPrimaryColor = currentTheme.PrimaryColor;
        customSecondaryColor = currentTheme.SecondaryColor;
        customTimerExpiredColor = currentTheme.TimerExpiredColor;

        // Reload logging fields
        logLevel = s.Logging.MinimumLevel;
        logFilePath = s.Logging.LogFilePath;
        enableFileLogging = s.Logging.EnableFileLogging;
        enableConsoleLogging = s.Logging.EnableConsoleLogging;
        LogLevelSwitch.MinimumLevel = logLevel;

        ThemeService.NotifyThemeChanged();
        showResetConfirm = false;
        showDiff = false;
        UpdateSettingsDiff();
    }
}
