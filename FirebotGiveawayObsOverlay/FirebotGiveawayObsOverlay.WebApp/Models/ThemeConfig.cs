namespace FirebotGiveawayObsOverlay.WebApp.Models;

public class ThemeConfig
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

    public ThemeConfig Clone()
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

    public static class Presets
    {
        public static ThemeConfig Warframe => new()
        {
            Name = "Warframe",
            PrimaryColor = "#00fff9",
            SecondaryColor = "#ff00c8",
            BackgroundStart = "rgba(0, 0, 0, 0.9)",
            BackgroundEnd = "rgba(15, 25, 35, 0.98)",
            BorderGlowColor = "rgba(0, 255, 255, 0.15)",
            TextColor = "#ffffff",
            TimerExpiredColor = "#ff3333",
            SeparatorColor = "rgba(0, 255, 255, 0.5)"
        };

        public static ThemeConfig Cyberpunk => new()
        {
            Name = "Cyberpunk",
            PrimaryColor = "#fcee0a",
            SecondaryColor = "#00f0ff",
            BackgroundStart = "rgba(20, 0, 30, 0.95)",
            BackgroundEnd = "rgba(40, 10, 60, 0.98)",
            BorderGlowColor = "rgba(252, 238, 10, 0.2)",
            TextColor = "#ffffff",
            TimerExpiredColor = "#ff0055",
            SeparatorColor = "rgba(252, 238, 10, 0.5)"
        };

        public static ThemeConfig Neon => new()
        {
            Name = "Neon",
            PrimaryColor = "#39ff14",
            SecondaryColor = "#ff073a",
            BackgroundStart = "rgba(0, 0, 0, 0.95)",
            BackgroundEnd = "rgba(10, 10, 20, 0.98)",
            BorderGlowColor = "rgba(57, 255, 20, 0.2)",
            TextColor = "#ffffff",
            TimerExpiredColor = "#ff073a",
            SeparatorColor = "rgba(57, 255, 20, 0.5)"
        };

        public static ThemeConfig Classic => new()
        {
            Name = "Classic",
            PrimaryColor = "#ffd700",
            SecondaryColor = "#ff8c00",
            BackgroundStart = "rgba(25, 25, 35, 0.95)",
            BackgroundEnd = "rgba(40, 40, 55, 0.98)",
            BorderGlowColor = "rgba(255, 215, 0, 0.15)",
            TextColor = "#ffffff",
            TimerExpiredColor = "#ff4444",
            SeparatorColor = "rgba(255, 215, 0, 0.5)"
        };

        public static ThemeConfig Ocean => new()
        {
            Name = "Ocean",
            PrimaryColor = "#00bfff",
            SecondaryColor = "#1e90ff",
            BackgroundStart = "rgba(0, 20, 40, 0.95)",
            BackgroundEnd = "rgba(0, 40, 80, 0.98)",
            BorderGlowColor = "rgba(0, 191, 255, 0.2)",
            TextColor = "#ffffff",
            TimerExpiredColor = "#ff6347",
            SeparatorColor = "rgba(0, 191, 255, 0.5)"
        };

        public static ThemeConfig Fire => new()
        {
            Name = "Fire",
            PrimaryColor = "#ff4500",
            SecondaryColor = "#ffa500",
            BackgroundStart = "rgba(30, 10, 0, 0.95)",
            BackgroundEnd = "rgba(50, 20, 10, 0.98)",
            BorderGlowColor = "rgba(255, 69, 0, 0.2)",
            TextColor = "#ffffff",
            TimerExpiredColor = "#ff0000",
            SeparatorColor = "rgba(255, 69, 0, 0.5)"
        };

        public static ThemeConfig Purple => new()
        {
            Name = "Purple",
            PrimaryColor = "#9d4edd",
            SecondaryColor = "#e040fb",
            BackgroundStart = "rgba(20, 0, 30, 0.95)",
            BackgroundEnd = "rgba(40, 0, 60, 0.98)",
            BorderGlowColor = "rgba(157, 78, 221, 0.2)",
            TextColor = "#ffffff",
            TimerExpiredColor = "#ff4081",
            SeparatorColor = "rgba(157, 78, 221, 0.5)"
        };

        public static ThemeConfig Custom => new()
        {
            Name = "Custom",
            PrimaryColor = "#00fff9",
            SecondaryColor = "#ff00c8",
            BackgroundStart = "rgba(0, 0, 0, 0.9)",
            BackgroundEnd = "rgba(15, 25, 35, 0.98)",
            BorderGlowColor = "rgba(0, 255, 255, 0.15)",
            TextColor = "#ffffff",
            TimerExpiredColor = "#ff3333",
            SeparatorColor = "rgba(0, 255, 255, 0.5)"
        };

        public static List<ThemeConfig> All => new()
        {
            Warframe,
            Cyberpunk,
            Neon,
            Classic,
            Ocean,
            Fire,
            Purple
        };

        public static ThemeConfig GetByName(string name)
        {
            return All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                   ?? Warframe;
        }
    }
}
