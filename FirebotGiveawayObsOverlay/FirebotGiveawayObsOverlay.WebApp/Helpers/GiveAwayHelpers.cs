using Serilog;

namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

public static class GiveAwayHelpers
{
    private static int _countdownMinutes = 60;
    private static int _countdownSeconds = 0;
    private static int _prizeSectionWidthPercent = 75;
    private static double _prizeFontSizeRem = 3.5;
    private static double _timerFontSizeRem = 3.0;
    private static double _entriesFontSizeRem = 2.5;

    public static void SetCountdownTime(int minutes, int seconds)
    {
        _countdownMinutes = minutes;
        _countdownSeconds = seconds;
    }

    public static (int minutes, int seconds) GetCountdownTime()
    {
        return (_countdownMinutes, _countdownSeconds);
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
        Log.Information("GiveAwayHelpers.SetFireBotFileFolder called with path: {Path}", folderPath);
        FireBotFileReader.SetFireBotFileFolder(folderPath);
    }

    public static string GetFireBotFileFolder()
    {
        return FireBotFileReader.GetFireBotFileFolder();
    }
}
