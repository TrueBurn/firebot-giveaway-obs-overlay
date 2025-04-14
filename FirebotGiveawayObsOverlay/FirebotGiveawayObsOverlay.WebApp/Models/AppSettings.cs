using System;

namespace FirebotGiveawayObsOverlay.WebApp.Models;

public class AppSettings
{
    public string FireBotFileFolder { get; set; } = "G:\\Giveaway";
    public CountdownSettings Countdown { get; set; } = new();
    public LayoutSettings Layout { get; set; } = new();
    public FontSettings Fonts { get; set; } = new();
}

public class CountdownSettings
{
    public int Minutes { get; set; } = 60;
    public int Seconds { get; set; } = 0;
}

public class LayoutSettings
{
    public int PrizeSectionWidthPercent { get; set; } = 75;
}

public class FontSettings
{
    public double PrizeFontSizeRem { get; set; } = 3.5;
    public double TimerFontSizeRem { get; set; } = 3.0;
    public double EntriesFontSizeRem { get; set; } = 2.5;
}