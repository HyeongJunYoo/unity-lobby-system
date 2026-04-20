using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeSessionManager : ISessionManager
    {
        readonly Dictionary<ulong, string> m_Ids = new();
        readonly Dictionary<string, ISessionPlayerData> m_Data = new();
        public bool SessionStarted { get; private set; }
        public int OnServerEndedCalls { get; private set; }

        public bool IsDuplicateConnection(string playerId)
            => m_Data.ContainsKey(playerId) && m_Data[playerId].IsConnected;

        public void SetupConnectingPlayerSessionData(ulong clientId, string playerId, ISessionPlayerData data)
        {
            m_Ids[clientId] = playerId;
            m_Data[playerId] = data;
        }

        public string GetPlayerId(ulong clientId)
            => m_Ids.TryGetValue(clientId, out var v) ? v : null;

        public ISessionPlayerData GetPlayerData(ulong clientId)
        {
            var pid = GetPlayerId(clientId);
            return pid != null ? GetPlayerData(pid) : null;
        }

        public ISessionPlayerData GetPlayerData(string playerId)
            => m_Data.TryGetValue(playerId, out var d) ? d : null;

        public void SetPlayerData(ulong clientId, ISessionPlayerData data)
        {
            var pid = GetPlayerId(clientId);
            if (pid != null) m_Data[pid] = data;
        }

        public void DisconnectClient(ulong clientId)
        {
            var pid = GetPlayerId(clientId);
            if (pid != null && m_Data.TryGetValue(pid, out var d)) d.IsConnected = false;
        }

        public void OnSessionStarted() => SessionStarted = true;
        public void OnSessionEnded()   => SessionStarted = false;
        public void OnServerEnded()    { OnServerEndedCalls++; m_Ids.Clear(); m_Data.Clear(); }
    }
}
