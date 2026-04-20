using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class UnityDebugLogger : ILobbyLogger
    {
        const string k_Prefix = "[Lobby] ";
        public void Info(string message)    => UnityEngine.Debug.Log(k_Prefix + message);
        public void Warning(string message) => UnityEngine.Debug.LogWarning(k_Prefix + message);
        public void Error(string message)   => UnityEngine.Debug.LogError(k_Prefix + message);
    }
}
