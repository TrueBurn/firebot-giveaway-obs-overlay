namespace FirebotGiveawayObsOverlay.Tests.TestHelpers;

/// <summary>
/// A custom implementation of OnMessageReceivedArgs for testing
/// </summary>
public class TestMessageReceivedArgs : EventArgs
{
    public TestChatMessage ChatMessage { get; }

    public TestMessageReceivedArgs(TestChatMessage chatMessage) => ChatMessage = chatMessage;
}