namespace FirebotGiveawayObsOverlay.Tests.TestHelpers;

/// <summary>
/// A simple wrapper class that implements the properties we need for testing
/// </summary>
public class TestChatMessage(string username, string message, bool isModerator, bool isBroadcaster)
{
    public string Username { get; } = username;
    public string Message { get; } = message;
    public bool IsModerator { get; } = isModerator;
    public bool IsBroadcaster { get; } = isBroadcaster;
}