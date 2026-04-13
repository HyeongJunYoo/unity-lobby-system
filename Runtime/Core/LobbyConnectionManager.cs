using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Multiplayer.Lobby.IP;

namespace Multiplayer.Lobby
{
    /// <summary>
    /// State machine that manages the connection lifecycle through the NetworkManager.
    /// Listens to NetworkManager callbacks and delegates to the current ConnectionState.
    /// </summary>
    public class LobbyConnectionManager : MonoBehaviour
    {
        ConnectionState m_CurrentState;

        [Inject] NetworkManager m_NetworkManager;
        public NetworkManager NetworkManager => m_NetworkManager;

        [SerializeField] int m_NbReconnectAttempts = 2;
        public int NbReconnectAttempts => m_NbReconnectAttempts;

        [Inject] IObjectResolver m_Resolver;
        [Inject] PlayerIdentity m_PlayerIdentity;

        [SerializeField] int m_MaxConnectedPlayers = 8;
        public int MaxConnectedPlayers => m_MaxConnectedPlayers;

        // --- Events for consuming projects ---
        public event Action OnHostStarted;
        public event Action OnClientConnected;
        public event Action OnDisconnected;

        // --- Factory delegate for game-specific player data creation ---
        /// <summary>
        /// Factory to create game-specific session player data during connection approval.
        /// Set this from your consuming project.
        /// Parameters: (clientId, connectionPayload) => ISessionPlayerData
        /// </summary>
        public Func<ulong, ConnectionPayload, Session.ISessionPlayerData> CreatePlayerData { get; set; }

        // --- State instances ---
        internal readonly States.OfflineState Offline = new();
        internal readonly States.ClientConnectingState ClientConnecting = new();
        internal readonly States.ClientConnectedState ClientConnected = new();
        internal readonly States.ClientReconnectingState ClientReconnecting = new();
        internal readonly States.StartingHostState StartingHost = new();
        internal readonly States.HostingState Hosting = new();

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            var states = new List<ConnectionState>
            {
                Offline, ClientConnecting, ClientConnected,
                ClientReconnecting, StartingHost, Hosting
            };

            foreach (var state in states)
            {
                m_Resolver.Inject(state);
            }

            m_CurrentState = Offline;

            NetworkManager.OnConnectionEvent += OnConnectionEvent;
            NetworkManager.OnServerStarted += OnServerStarted;
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.OnTransportFailure += OnTransportFailure;
            NetworkManager.OnServerStopped += OnServerStopped;
        }

        void OnDestroy()
        {
            NetworkManager.OnConnectionEvent -= OnConnectionEvent;
            NetworkManager.OnServerStarted -= OnServerStarted;
            NetworkManager.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.OnTransportFailure -= OnTransportFailure;
            NetworkManager.OnServerStopped -= OnServerStopped;
        }

        internal void ChangeState(ConnectionState nextState)
        {
            Debug.Log($"{name}: Changed state from {m_CurrentState.GetType().Name} to {nextState.GetType().Name}.");

            m_CurrentState?.Exit();
            m_CurrentState = nextState;
            m_CurrentState.Enter();
        }

        // --- Event invokers (called by states) ---

        internal void InvokeOnHostStarted() => OnHostStarted?.Invoke();
        internal void InvokeOnClientConnected() => OnClientConnected?.Invoke();
        internal void InvokeOnDisconnected() => OnDisconnected?.Invoke();

        // --- NetworkManager callbacks ---

        void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            switch (connectionEventData.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    m_CurrentState.OnClientConnected(connectionEventData.ClientId);
                    break;
                case ConnectionEvent.ClientDisconnected:
                    m_CurrentState.OnClientDisconnected(connectionEventData.ClientId);
                    break;
            }
        }

        void OnServerStarted() => m_CurrentState.OnServerStarted();

        void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            m_CurrentState.ApprovalCheck(request, response);
        }

        void OnTransportFailure() => m_CurrentState.OnTransportFailure();

        void OnServerStopped(bool _) => m_CurrentState.OnServerStopped();

        // --- Public API ---

        public void StartClient(ConnectionMethodBase connectionMethod)
        {
            m_CurrentState.StartClient(connectionMethod);
        }

        public void StartHost(ConnectionMethodBase connectionMethod)
        {
            m_CurrentState.StartHost(connectionMethod);
        }

        /// <summary>
        /// IP 직접 연결로 클라이언트를 시작합니다.
        /// </summary>
        public void StartClientIp(string playerName, string ipaddress, int port)
        {
            ValidateIpConnectionParams(playerName, ipaddress, port);
            var method = new IPConnectionMethod(ipaddress, (ushort)port, this, m_PlayerIdentity, playerName);
            StartClient(method);
        }

        /// <summary>
        /// IP 직접 연결로 호스트를 시작합니다.
        /// </summary>
        public void StartHostIp(string playerName, string ipaddress, int port)
        {
            ValidateIpConnectionParams(playerName, ipaddress, port);
            var method = new IPConnectionMethod(ipaddress, (ushort)port, this, m_PlayerIdentity, playerName);
            StartHost(method);
        }

        static void ValidateIpConnectionParams(string playerName, string ipaddress, int port)
        {
            if (string.IsNullOrEmpty(playerName))
                throw new System.ArgumentException("playerName cannot be null or empty", nameof(playerName));
            if (string.IsNullOrEmpty(ipaddress))
                throw new System.ArgumentException("ipaddress cannot be null or empty", nameof(ipaddress));
            if (port < 0 || port > 65535)
                throw new System.ArgumentOutOfRangeException(nameof(port), "Port must be between 0 and 65535");
        }

        public void RequestShutdown()
        {
            m_CurrentState.OnUserRequestedShutdown();
        }
    }
}
