namespace FirebotGiveawayObsOverlay.WebApp.Helpers;

public static class FireBotFileReader
{

    private static string _fireBotFileFolder = "G:\\Giveaway";
    private static readonly string _prizeFile = "prize.txt";
    private static readonly string _winnerFile = "winner.txt";
    private static readonly string _entriesFile = "giveaway.txt";

    public static void SetFireBotFileFolder(string folderPath)
    {
        _fireBotFileFolder = folderPath;
    }

    public static string GetFireBotFileFolder()
    {
        return _fireBotFileFolder;
    }

    public static Task<string> GetPrizeAsync()
    {
        return GetFireBotFileAsync(_prizeFile);
    }

    public static Task<string> GetWinnerAsync()
    {
        return GetFireBotFileAsync(_winnerFile);
    }

    public static async Task<string[]> GetEntriesAsync()
    {
        string entries = await GetFireBotFileAsync(_entriesFile);
        return entries.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static async Task<string> GetFireBotFileAsync(string fileName)
    {
        string filePath = Path.Combine(_fireBotFileFolder, fileName);
        try
        {
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }        
        return string.Empty;
    }

}
