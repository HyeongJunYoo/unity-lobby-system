using UnityEngine;
using VContainer;
using Multiplayer.Lobby.Infrastructure;
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Sample
{
    /// <summary>
    /// Simple OnGUI test UI for the lobby system.
    /// Lets you Host or Join via IP with one click.
    /// </summary>
    public class LobbyTestUI : MonoBehaviour
    {
        [Inject] LobbyConnectionManager m_ConnectionManager;
        [Inject] ISubscriber<ConnectStatus> m_ConnectStatusSubscriber;
        [Inject] SessionManager m_SessionManager;

        string m_IP = "127.0.0.1";
        string m_Port = "9998";
        string m_PlayerName = "Player";
        string m_StatusMessage = "Disconnected";
        Vector2 m_PlayerListScroll;

        void Start()
        {
            // Wire up the player data factory
            m_ConnectionManager.CreatePlayerData = (clientId, payload) =>
                new SampleSessionPlayerData(clientId, payload.playerName, true);

            // Subscribe to connection events
            m_ConnectionManager.OnHostStarted += OnHostStartedHandler;
            m_ConnectionManager.OnClientConnected += OnClientConnectedHandler;
            m_ConnectionManager.OnDisconnected += OnDisconnectedHandler;

            m_ConnectStatusSubscriber.Subscribe(OnConnectStatus);
        }

        void OnDestroy()
        {
            if (m_ConnectionManager != null)
            {
                m_ConnectionManager.OnHostStarted -= OnHostStartedHandler;
                m_ConnectionManager.OnClientConnected -= OnClientConnectedHandler;
                m_ConnectionManager.OnDisconnected -= OnDisconnectedHandler;
            }

            m_ConnectStatusSubscriber?.Unsubscribe(OnConnectStatus);
        }

        void OnHostStartedHandler() => m_StatusMessage = "Hosting!";
        void OnClientConnectedHandler() => m_StatusMessage = "Connected!";
        void OnDisconnectedHandler() => m_StatusMessage = "Disconnected";

        void OnConnectStatus(ConnectStatus status)
        {
            m_StatusMessage = status.ToString();
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label($"Status: {m_StatusMessage}", GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.Label("Player Name:");
            m_PlayerName = GUILayout.TextField(m_PlayerName);

            GUILayout.Label("IP:");
            m_IP = GUILayout.TextField(m_IP);

            GUILayout.Label("Port:");
            m_Port = GUILayout.TextField(m_Port);

            GUILayout.Space(10);

            if (GUILayout.Button("Host", GUILayout.Height(40)))
            {
                int.TryParse(m_Port, out var port);
                m_StatusMessage = "Starting Host...";
                m_ConnectionManager.StartHostIp(m_PlayerName, m_IP, port);
            }

            if (GUILayout.Button("Join", GUILayout.Height(40)))
            {
                int.TryParse(m_Port, out var port);
                m_StatusMessage = "Connecting...";
                m_ConnectionManager.StartClientIp(m_PlayerName, m_IP, port);
            }

            if (GUILayout.Button("Disconnect", GUILayout.Height(40)))
            {
                m_ConnectionManager.RequestShutdown();
            }

            GUILayout.EndArea();

            // Connected players list
            GUILayout.BeginArea(new Rect(320, 10, 250, 400));
            GUILayout.Label("Connected Players", GUI.skin.box);

            var nm = m_ConnectionManager.NetworkManager;
            if (nm != null && nm.IsListening)
            {
                m_PlayerListScroll = GUILayout.BeginScrollView(m_PlayerListScroll, GUILayout.ExpandHeight(true));
                foreach (var clientId in nm.ConnectedClientsIds)
                {
                    // SessionManager data is only available on the host
                    var playerName = "Unknown";
                    if (nm.IsHost || nm.IsServer)
                    {
                        var playerData = m_SessionManager.GetPlayerData(clientId);
                        if (playerData is SampleSessionPlayerData sample)
                        {
                            playerName = sample.PlayerName;
                        }
                    }
                    var hostTag = clientId == nm.LocalClientId && nm.IsHost ? " (Host)" : "";
                    GUILayout.Label($"  [{clientId}] {playerName}{hostTag}");
                }
                GUILayout.EndScrollView();

                GUILayout.Label($"Total: {nm.ConnectedClientsIds.Count} / {m_ConnectionManager.MaxConnectedPlayers}");
            }
            else
            {
                GUILayout.Label("  Not connected");
            }

            GUILayout.EndArea();
        }
    }
}
