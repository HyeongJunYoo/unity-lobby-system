namespace Multiplayer.Lobby.Connection
{
    public struct ReconnectMessage
    {
        public int CurrentAttempt;
        public int MaxAttempt;
        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }
}
