namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

public static class GiveAwayHelpers
{

    private static int _giveawayDuration = 69;

    public static void SetGiveawayDuration(int duration)
    {
        _giveawayDuration = duration;
    }

    public static int GetGiveawayDuration()
    {
        return _giveawayDuration;
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
