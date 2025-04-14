namespace FirebotGiveawayObsOverlay.WebApp.Models;

public class TwitchSettings
{
    public bool Enabled { get; set; } = false;
    public string Channel { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public CommandSettings Commands { get; set; } = new CommandSettings();
    public bool RequireFollower { get; set; }
    public int FollowerMinimumAgeDays { get; set; }
}

public class CommandSettings
{
    public string Join { get; set; } = "!join";
    public string StartGiveaway { get; set; } = "!startgiveaway";
    public string DrawWinner { get; set; } = "!drawwinner";
}