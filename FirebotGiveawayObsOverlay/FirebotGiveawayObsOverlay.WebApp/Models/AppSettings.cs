namespace FirebotGiveawayObsOverlay.WebApp.Models;

/// <summary>
/// Application settings that can be loaded from appsettings.json or usersettings.json.
/// All properties are non-nullable with sensible defaults.
/// </summary>
public class AppSettings
{
    public string FireBotFileFolder { get; set; } = @"G:\Giveaway";
    public bool CountdownTimerEnabled { get; set; } = true;
    public int CountdownHours { get; set; } = 0;
    public int CountdownMinutes { get; set; } = 60;
    public int CountdownSeconds { get; set; } = 0;
    public int PrizeSectionWidthPercent { get; set; } = 75;
    public double PrizeFontSizeRem { get; set; } = 3.5;
    public double TimerFontSizeRem { get; set; } = 3.0;
    public double EntriesFontSizeRem { get; set; } = 2.5;
    public ThemeSettings Theme { get; set; } = new();

    /// <summary>
    /// Creates a new instance with default values.
    /// </summary>
    public static AppSettings GetDefaults() => new();

    /// <summary>
    /// Gets a list of differences between this settings and another.
    /// </summary>
    public List<SettingsDiff> GetDifferences(AppSettings other)
    {
        var diffs = new List<SettingsDiff>();

        if (FireBotFileFolder != other.FireBotFileFolder)
            diffs.Add(new("Firebot File Path", other.FireBotFileFolder, FireBotFileFolder));

        if (CountdownTimerEnabled != other.CountdownTimerEnabled)
            diffs.Add(new("Timer Enabled", other.CountdownTimerEnabled.ToString(), CountdownTimerEnabled.ToString()));

        if (CountdownHours != other.CountdownHours)
            diffs.Add(new("Countdown Hours", other.CountdownHours.ToString(), CountdownHours.ToString()));

        if (CountdownMinutes != other.CountdownMinutes)
            diffs.Add(new("Countdown Minutes", other.CountdownMinutes.ToString(), CountdownMinutes.ToString()));

        if (CountdownSeconds != other.CountdownSeconds)
            diffs.Add(new("Countdown Seconds", other.CountdownSeconds.ToString(), CountdownSeconds.ToString()));

        if (PrizeSectionWidthPercent != other.PrizeSectionWidthPercent)
            diffs.Add(new("Prize Section Width", $"{other.PrizeSectionWidthPercent}%", $"{PrizeSectionWidthPercent}%"));

        if (Math.Abs(PrizeFontSizeRem - other.PrizeFontSizeRem) > 0.01)
            diffs.Add(new("Prize Font Size", $"{other.PrizeFontSizeRem:F1}rem", $"{PrizeFontSizeRem:F1}rem"));

        if (Math.Abs(TimerFontSizeRem - other.TimerFontSizeRem) > 0.01)
            diffs.Add(new("Timer Font Size", $"{other.TimerFontSizeRem:F1}rem", $"{TimerFontSizeRem:F1}rem"));

        if (Math.Abs(EntriesFontSizeRem - other.EntriesFontSizeRem) > 0.01)
            diffs.Add(new("Entries Font Size", $"{other.EntriesFontSizeRem:F1}rem", $"{EntriesFontSizeRem:F1}rem"));

        if (Theme.Name != other.Theme.Name)
            diffs.Add(new("Theme", other.Theme.Name, Theme.Name));

        if (Theme.PrimaryColor != other.Theme.PrimaryColor)
            diffs.Add(new("Primary Color", other.Theme.PrimaryColor, Theme.PrimaryColor));

        if (Theme.SecondaryColor != other.Theme.SecondaryColor)
            diffs.Add(new("Secondary Color", other.Theme.SecondaryColor, Theme.SecondaryColor));

        if (Theme.TimerExpiredColor != other.Theme.TimerExpiredColor)
            diffs.Add(new("Timer Expired Color", other.Theme.TimerExpiredColor, Theme.TimerExpiredColor));

        return diffs;
    }
}

/// <summary>
/// Represents a difference between current and default settings.
/// </summary>
public record SettingsDiff(string Name, string DefaultValue, string CurrentValue);

/// <summary>
/// Theme settings matching ThemeConfig structure for JSON serialization.
/// </summary>
public class ThemeSettings
{
    public string Name { get; set; } = "Warframe";
    public string PrimaryColor { get; set; } = "#00fff9";
    public string SecondaryColor { get; set; } = "#ff00c8";
    public string BackgroundStart { get; set; } = "rgba(0, 0, 0, 0.9)";
    public string BackgroundEnd { get; set; } = "rgba(15, 25, 35, 0.98)";
    public string BorderGlowColor { get; set; } = "rgba(0, 255, 255, 0.15)";
    public string TextColor { get; set; } = "#ffffff";
    public string TimerExpiredColor { get; set; } = "#ff3333";
    public string SeparatorColor { get; set; } = "rgba(0, 255, 255, 0.5)";

    /// <summary>
    /// Converts to ThemeConfig for use with GiveAwayHelpers.
    /// </summary>
    public ThemeConfig ToThemeConfig()
    {
        return new ThemeConfig
        {
            Name = Name,
            PrimaryColor = PrimaryColor,
            SecondaryColor = SecondaryColor,
            BackgroundStart = BackgroundStart,
            BackgroundEnd = BackgroundEnd,
            BorderGlowColor = BorderGlowColor,
            TextColor = TextColor,
            TimerExpiredColor = TimerExpiredColor,
            SeparatorColor = SeparatorColor
        };
    }

    /// <summary>
    /// Creates ThemeSettings from a ThemeConfig.
    /// </summary>
    public static ThemeSettings FromThemeConfig(ThemeConfig config)
    {
        return new ThemeSettings
        {
            Name = config.Name,
            PrimaryColor = config.PrimaryColor,
            SecondaryColor = config.SecondaryColor,
            BackgroundStart = config.BackgroundStart,
            BackgroundEnd = config.BackgroundEnd,
            BorderGlowColor = config.BorderGlowColor,
            TextColor = config.TextColor,
            TimerExpiredColor = config.TimerExpiredColor,
            SeparatorColor = config.SeparatorColor
        };
    }
}
