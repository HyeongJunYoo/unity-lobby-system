using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer.Lobby.Session
{
    /// <summary>
    /// Binds players to sessions using a persistent player ID. Handles reconnection by preserving
    /// player data when a known player reconnects with a new client ID.
    /// Non-generic, stores ISessionPlayerData. Register with VContainer as Singleton.
    /// </summary>
    public class SessionManager
    {
        readonly Dictionary<string, ISessionPlayerData> m_ClientData = new();
        readonly Dictionary<ulong, string> m_ClientIDToPlayerId = new();

        bool m_HasSessionStarted;

        public void DisconnectClient(ulong clientId)
        {
            if (m_HasSessionStarted)
            {
                if (m_ClientIDToPlayerId.TryGetValue(clientId, out var playerId))
                {
                    if (m_ClientData.TryGetValue(playerId, out var data) && data.ClientID == clientId)
                    {
                        data.IsConnected = false;
                        m_ClientData[playerId] = data;
                    }
                }
            }
            else
            {
                if (m_ClientIDToPlayerId.TryGetValue(clientId, out var playerId))
                {
                    m_ClientIDToPlayerId.Remove(clientId);
                    if (m_ClientData.TryGetValue(playerId, out var data) && data.ClientID == clientId)
                    {
                        m_ClientData.Remove(playerId);
                    }
                }
            }
        }

        public bool IsDuplicateConnection(string playerId)
        {
            return m_ClientData.ContainsKey(playerId) && m_ClientData[playerId].IsConnected;
        }

        public void SetupConnectingPlayerSessionData(ulong clientId, string playerId, ISessionPlayerData sessionPlayerData)
        {
            if (IsDuplicateConnection(playerId))
            {
                Debug.LogError($"Player ID {playerId} already exists. Duplicate connection rejected.");
                return;
            }

            var isReconnecting = m_ClientData.ContainsKey(playerId) && !m_ClientData[playerId].IsConnected;

            if (isReconnecting)
            {
                sessionPlayerData = m_ClientData[playerId];
                sessionPlayerData.ClientID = clientId;
                sessionPlayerData.IsConnected = true;
            }

            m_ClientIDToPlayerId[clientId] = playerId;
            m_ClientData[playerId] = sessionPlayerData;
        }

        public string GetPlayerId(ulong clientId)
        {
            if (m_ClientIDToPlayerId.TryGetValue(clientId, out var playerId))
            {
                return playerId;
            }
            Debug.Log($"No player ID found for client ID: {clientId}");
            return null;
        }

        public ISessionPlayerData GetPlayerData(ulong clientId)
        {
            var playerId = GetPlayerId(clientId);
            return playerId != null ? GetPlayerData(playerId) : null;
        }

        public ISessionPlayerData GetPlayerData(string playerId)
        {
            if (m_ClientData.TryGetValue(playerId, out var data))
            {
                return data;
            }
            Debug.Log($"No player data found for player ID: {playerId}");
            return null;
        }

        public void SetPlayerData(ulong clientId, ISessionPlayerData sessionPlayerData)
        {
            if (m_ClientIDToPlayerId.TryGetValue(clientId, out var playerId))
            {
                m_ClientData[playerId] = sessionPlayerData;
            }
            else
            {
                Debug.LogError($"No player ID found for client ID: {clientId}");
            }
        }

        public void OnSessionStarted()
        {
            m_HasSessionStarted = true;
        }

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
                var playerId = m_ClientIDToPlayerId[id];
                if (m_ClientData.TryGetValue(playerId, out var data))
                {
                    data.Reinitialize();
                    m_ClientData[playerId] = data;
                }
            }
        }

        void ClearDisconnectedPlayersData()
        {
            var idsToClear = new List<ulong>();
            foreach (var id in m_ClientIDToPlayerId.Keys)
            {
                var data = GetPlayerData(id);
                if (data != null && !data.IsConnected)
                {
                    idsToClear.Add(id);
                }
            }

            foreach (var id in idsToClear)
            {
                var playerId = m_ClientIDToPlayerId[id];
                if (m_ClientData.TryGetValue(playerId, out var data) && data.ClientID == id)
                {
                    m_ClientData.Remove(playerId);
                }
                m_ClientIDToPlayerId.Remove(id);
            }
        }
    }
}
