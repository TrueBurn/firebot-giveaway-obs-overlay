namespace FirebotGiveawayObsOverlay.WebApp.Models;

using FirebotGiveawayObsOverlay.WebApp.Services;
using System;
using System.Collections.Generic;

public class TwitchSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether Twitch integration is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the Twitch channel name
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication mode (Simple or Advanced)
    /// </summary>
    public AuthMode AuthMode { get; set; } = AuthMode.Simple;

    /// <summary>
    /// Gets or sets the Twitch application client ID
    /// Only used in Advanced mode
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Twitch application client secret
    /// Only used in Advanced mode
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the redirect URI for OAuth flow
    /// Only used in Advanced mode
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access token for Twitch API
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token for Twitch API
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration date/time
    /// </summary>
    public DateTime? TokenExpiration { get; set; }

    /// <summary>
    /// Gets or sets the scopes granted to the token
    /// </summary>
    public List<string> Scopes { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the command settings
    /// </summary>
    public CommandSettings Commands { get; set; } = new CommandSettings();

    /// <summary>
    /// Gets or sets a value indicating whether followers-only mode is enabled for giveaways
    /// </summary>
    public bool RequireFollower { get; set; }

    /// <summary>
    /// Gets or sets the minimum age in days required for followers to participate
    /// </summary>
    public int FollowerMinimumAgeDays { get; set; }
}

public class CommandSettings
{
    /// <summary>
    /// Gets or sets the command to join a giveaway
    /// </summary>
    public string Join { get; set; } = "!join";

    /// <summary>
    /// Gets or sets the command to start a giveaway
    /// </summary>
    public string StartGiveaway { get; set; } = "!startgiveaway";

    /// <summary>
    /// Gets or sets the command to draw a winner
    /// </summary>
    public string DrawWinner { get; set; } = "!drawwinner";
}