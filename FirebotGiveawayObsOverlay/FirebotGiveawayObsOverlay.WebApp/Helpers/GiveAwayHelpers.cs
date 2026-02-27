using FirebotGiveawayObsOverlay.WebApp.Models;

namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

/// <summary>
/// Static helpers for theme management and Firebot file path delegation.
/// Settings management has been moved to ISettingsService singleton.
/// </summary>
public static class GiveAwayHelpers
{
    private static ThemeConfig _currentTheme = ThemeConfig.Presets.Warframe.Clone();
    private static volatile bool _useCustomTheme = false;

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
    /// Applies settings that GiveAwayHelpers still owns: file path and theme.
    /// All other settings are managed by ISettingsService.
    /// </summary>
    public static void ApplySettings(AppSettings settings)
    {
        SetFireBotFileFolder(settings.FireBotFileFolder);
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
            _useCustomTheme = false;
        }
        else
        {
            _useCustomTheme = true;
            _currentTheme.Name = "Custom";
        }
    }
}
