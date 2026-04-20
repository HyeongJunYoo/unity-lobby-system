using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Abstractions
{
    public interface ISessionManager
    {
        bool IsDuplicateConnection(string playerId);
        void SetupConnectingPlayerSessionData(ulong clientId, string playerId, ISessionPlayerData data);
        string GetPlayerId(ulong clientId);
        ISessionPlayerData GetPlayerData(ulong clientId);
        ISessionPlayerData GetPlayerData(string playerId);
        void SetPlayerData(ulong clientId, ISessionPlayerData data);
        void DisconnectClient(ulong clientId);
        void OnSessionStarted();
        void OnSessionEnded();
        void OnServerEnded();
    }
}
