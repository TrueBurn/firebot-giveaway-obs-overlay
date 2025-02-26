﻿namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

public static class GiveAwayHelpers
{
    private static int _countdownMinutes = 60;
    private static int _countdownSeconds = 0;

    public static void SetCountdownTime(int minutes, int seconds)
    {
        _countdownMinutes = minutes;
        _countdownSeconds = seconds;
    }

    public static (int minutes, int seconds) GetCountdownTime()
    {
        return (_countdownMinutes, _countdownSeconds);
    }

    public static void SetFireBotFileFolder(string folderPath)
    {
        FireBotFileReader.SetFireBotFileFolder(folderPath);
    }

    public static string GetFireBotFileFolder()
    {
        return FireBotFileReader.GetFireBotFileFolder();
    }
}
