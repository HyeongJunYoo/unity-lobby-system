using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Session
{
    public sealed class SessionManager : ISessionManager
    {
        readonly Dictionary<string, ISessionPlayerData> m_ClientData = new();
        readonly Dictionary<ulong, string> m_ClientIDToPlayerId = new();
        readonly ILobbyLogger m_Logger;
        bool m_HasSessionStarted;

        public SessionManager(ILobbyLogger logger = null)
        {
            m_Logger = logger ?? NullLogger.Instance;
        }

        public void DisconnectClient(ulong clientId)
        {
            if (m_HasSessionStarted)
            {
                if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid)
                    && m_ClientData.TryGetValue(pid, out var d) && d.ClientID == clientId)
                {
                    d.IsConnected = false;
                    m_ClientData[pid] = d;
                }
            }
            else
            {
                if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid))
                {
                    m_ClientIDToPlayerId.Remove(clientId);
                    if (m_ClientData.TryGetValue(pid, out var d) && d.ClientID == clientId)
                        m_ClientData.Remove(pid);
                }
            }
        }

        public bool IsDuplicateConnection(string playerId)
            => m_ClientData.ContainsKey(playerId) && m_ClientData[playerId].IsConnected;

        public void SetupConnectingPlayerSessionData(ulong clientId, string playerId, ISessionPlayerData data)
        {
            if (IsDuplicateConnection(playerId))
            {
                m_Logger.Error($"Player ID {playerId} already exists. Duplicate connection rejected.");
                return;
            }
            var isReconnecting = m_ClientData.ContainsKey(playerId) && !m_ClientData[playerId].IsConnected;
            if (isReconnecting)
            {
                data = m_ClientData[playerId];
                data.ClientID = clientId;
                data.IsConnected = true;
            }
            m_ClientIDToPlayerId[clientId] = playerId;
            m_ClientData[playerId] = data;
        }

        public string GetPlayerId(ulong clientId)
        {
            if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid)) return pid;
            m_Logger.Info($"No player ID found for client ID: {clientId}");
            return null;
        }

        public ISessionPlayerData GetPlayerData(ulong clientId)
        {
            var pid = GetPlayerId(clientId);
            return pid != null ? GetPlayerData(pid) : null;
        }

        public ISessionPlayerData GetPlayerData(string playerId)
        {
            if (m_ClientData.TryGetValue(playerId, out var d)) return d;
            m_Logger.Info($"No player data found for player ID: {playerId}");
            return null;
        }

        public void SetPlayerData(ulong clientId, ISessionPlayerData data)
        {
            if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid)) m_ClientData[pid] = data;
            else m_Logger.Error($"No player ID found for client ID: {clientId}");
        }

        public void OnSessionStarted() => m_HasSessionStarted = true;

        public void OnSessionEnded()
        {
            ClearDisconnectedPlayersData();
            ReinitializePlayersData();
            m_HasSessionStarted = false;
        }

        public void OnServerEnded()
        {
            m_ClientData.Clear();
            m_ClientIDToPlayerId.Clear();
            m_HasSessionStarted = false;
        }

        void ReinitializePlayersData()
        {
            foreach (var id in m_ClientIDToPlayerId.Keys)
            {
                var pid = m_ClientIDToPlayerId[id];
                if (m_ClientData.TryGetValue(pid, out var d))
                {
                    d.Reinitialize();
                    m_ClientData[pid] = d;
                }
            }
        }

        void ClearDisconnectedPlayersData()
        {
            var toClear = new List<ulong>();
            foreach (var id in m_ClientIDToPlayerId.Keys)
            {
                var d = GetPlayerData(id);
                if (d != null && !d.IsConnected) toClear.Add(id);
            }
            foreach (var id in toClear)
            {
                var pid = m_ClientIDToPlayerId[id];
                if (m_ClientData.TryGetValue(pid, out var d) && d.ClientID == id) m_ClientData.Remove(pid);
                m_ClientIDToPlayerId.Remove(id);
            }
        }
    }
}
