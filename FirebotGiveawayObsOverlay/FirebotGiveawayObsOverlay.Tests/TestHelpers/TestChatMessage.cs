namespace FirebotGiveawayObsOverlay.Tests.TestHelpers
{
    /// <summary>
    /// A simple wrapper class that implements the properties we need for testing
    /// </summary>
    public class TestChatMessage
    {
        public string Username { get; }
        public string Message { get; }
        public bool IsModerator { get; }
        public bool IsBroadcaster { get; }

        public TestChatMessage(string username, string message, bool isModerator, bool isBroadcaster)
        {
            Username = username;
            Message = message;
            IsModerator = isModerator;
            IsBroadcaster = isBroadcaster;
        }
    }
}