using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

public static class GiveAwayHelpers
{
    private static bool _countdownTimerEnabled = true;
    private static int _countdownHours = 0;
    private static int _countdownMinutes = 60;
    private static int _countdownSeconds = 0;
    private static int _prizeSectionWidthPercent = 75;
    private static double _prizeFontSizeRem = 3.5;
    private static double _timerFontSizeRem = 3.0;
    private static double _entriesFontSizeRem = 2.5;
    private static ThemeConfig _currentTheme = ThemeConfig.Presets.Warframe.Clone();
    private static bool _useCustomTheme = false;

    public static void SetCountdownTime(int hours, int minutes, int seconds)
    {
        _countdownHours = hours;
        _countdownMinutes = minutes;
        _countdownSeconds = seconds;
    }

    public static (int hours, int minutes, int seconds) GetCountdownTime()
    {
        return (_countdownHours, _countdownMinutes, _countdownSeconds);
    }

    public static void SetCountdownTimerEnabled(bool enabled)
    {
        _countdownTimerEnabled = enabled;
    }

    public static bool GetCountdownTimerEnabled()
    {
        return _countdownTimerEnabled;
    }

    public static void SetPrizeSectionWidth(int widthPercent)
    {
        _prizeSectionWidthPercent = Math.Clamp(widthPercent, 50, 90);
    }

    public static int GetPrizeSectionWidth()
    {
        return _prizeSectionWidthPercent;
    }

    public static void SetPrizeFontSize(double sizeRem)
    {
        _prizeFontSizeRem = Math.Clamp(sizeRem, 1.0, 6.0);
    }

    public static double GetPrizeFontSize()
    {
        return _prizeFontSizeRem;
    }

    public static void SetTimerFontSize(double sizeRem)
    {
        _timerFontSizeRem = Math.Clamp(sizeRem, 1.0, 6.0);
    }

    public static double GetTimerFontSize()
    {
        return _timerFontSizeRem;
    }

    public static void SetEntriesFontSize(double sizeRem)
    {
        _entriesFontSizeRem = Math.Clamp(sizeRem, 1.0, 6.0);
    }

    public static double GetEntriesFontSize()
    {
        return _entriesFontSizeRem;
    }

    public static void SetFireBotFileFolder(string folderPath)
    {
        FireBotFileReader.SetFireBotFileFolder(folderPath);
    }

    public static string GetFireBotFileFolder()
    {
        return FireBotFileReader.GetFireBotFileFolder();
    }

    public static ThemeConfig GetCurrentTheme()
    {
        return _currentTheme;
    }

    public static void SetPresetTheme(string themeName)
    {
        _currentTheme = ThemeConfig.Presets.GetByName(themeName).Clone();
        _useCustomTheme = false;
    }

    public static void SetCustomTheme(ThemeConfig theme)
    {
        _currentTheme = theme.Clone();
        _currentTheme.Name = "Custom";
        _useCustomTheme = true;
    }

    public static void UpdateCustomColor(string property, string value)
    {
        _useCustomTheme = true;
        _currentTheme.Name = "Custom";

        switch (property)
        {
            case nameof(ThemeConfig.PrimaryColor):
                _currentTheme.PrimaryColor = value;
                break;
            case nameof(ThemeConfig.SecondaryColor):
                _currentTheme.SecondaryColor = value;
                break;
            case nameof(ThemeConfig.BackgroundStart):
                _currentTheme.BackgroundStart = value;
                break;
            case nameof(ThemeConfig.BackgroundEnd):
                _currentTheme.BackgroundEnd = value;
                break;
            case nameof(ThemeConfig.BorderGlowColor):
                _currentTheme.BorderGlowColor = value;
                break;
            case nameof(ThemeConfig.TextColor):
                _currentTheme.TextColor = value;
                break;
            case nameof(ThemeConfig.TimerExpiredColor):
                _currentTheme.TimerExpiredColor = value;
                break;
            case nameof(ThemeConfig.SeparatorColor):
                _currentTheme.SeparatorColor = value;
                break;
        }
    }

    public static bool IsUsingCustomTheme()
    {
        return _useCustomTheme;
    }

    public static List<ThemeConfig> GetAllPresetThemes()
    {
        return ThemeConfig.Presets.All;
    }

    /// <summary>
    /// Gets all current settings as an AppSettings object for persistence.
    /// </summary>
    public static AppSettings GetCurrentSettings()
    {
        return new AppSettings
        {
            FireBotFileFolder = GetFireBotFileFolder(),
            CountdownTimerEnabled = _countdownTimerEnabled,
            CountdownHours = _countdownHours,
            CountdownMinutes = _countdownMinutes,
            CountdownSeconds = _countdownSeconds,
            PrizeSectionWidthPercent = _prizeSectionWidthPercent,
            PrizeFontSizeRem = _prizeFontSizeRem,
            TimerFontSizeRem = _timerFontSizeRem,
            EntriesFontSizeRem = _entriesFontSizeRem,
            Theme = ThemeSettings.FromThemeConfig(_currentTheme)
        };
    }

    /// <summary>
    /// Applies all settings from an AppSettings object.
    /// </summary>
    public static void ApplySettings(AppSettings settings)
    {
        SetFireBotFileFolder(settings.FireBotFileFolder);
        SetCountdownTimerEnabled(settings.CountdownTimerEnabled);
        SetCountdownTime(settings.CountdownHours, settings.CountdownMinutes, settings.CountdownSeconds);
        SetPrizeSectionWidth(settings.PrizeSectionWidthPercent);
        SetPrizeFontSize(settings.PrizeFontSizeRem);
        SetTimerFontSize(settings.TimerFontSizeRem);
        SetEntriesFontSize(settings.EntriesFontSizeRem);
        InitializeTheme(settings.Theme.ToThemeConfig());
    }

    public static void InitializeTheme(ThemeConfig theme)
    {
        _currentTheme = theme.Clone();
        // Check if this matches a preset theme by name
        var preset = ThemeConfig.Presets.All.FirstOrDefault(p =>
            p.Name.Equals(theme.Name, StringComparison.OrdinalIgnoreCase));

        if (preset != null &&
            preset.PrimaryColor == theme.PrimaryColor &&
            preset.SecondaryColor == theme.SecondaryColor &&
            preset.TimerExpiredColor == theme.TimerExpiredColor)
        {
            // Matches a preset, not custom
            _useCustomTheme = false;
        }
        else
        {
            // Custom theme or modified preset
            _useCustomTheme = true;
            _currentTheme.Name = "Custom";
        }
    }
}
