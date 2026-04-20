namespace Multiplayer.Lobby.Abstractions
{
    public sealed class NullLogger : ILobbyLogger
    {
        public static readonly NullLogger Instance = new();
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }
}
