using System;
using Unity.Netcode;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Adapters.Netcode
{
    public sealed class NetcodeNetworkFacade : INetworkFacade, IDisposable
    {
        readonly NetworkManager m_Nm;
        Func<ApprovalRequest, ApprovalResult> m_ApprovalHandler;

        public NetcodeNetworkFacade(NetworkManager networkManager)
        {
            m_Nm = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            m_Nm.OnServerStarted        += InvokeServerStarted;
            m_Nm.OnServerStopped        += InvokeServerStopped;
            m_Nm.OnTransportFailure     += InvokeTransportFailure;
            m_Nm.OnConnectionEvent      += OnConnectionEvent;
            m_Nm.ConnectionApprovalCallback += OnApprovalCallback;
        }

        public bool IsClient             => m_Nm.IsClient;
        public bool IsServer             => m_Nm.IsServer;
        public bool IsHost               => m_Nm.IsHost;
        public bool IsListening          => m_Nm.IsListening;
        public bool ShutdownInProgress   => m_Nm.ShutdownInProgress;
        public ulong LocalClientId       => m_Nm.LocalClientId;

        public byte[] ConnectionPayload
        {
            get => m_Nm.NetworkConfig.ConnectionData;
            set => m_Nm.NetworkConfig.ConnectionData = value;
        }

        public bool StartClient()                                         => m_Nm.StartClient();
        public bool StartHost()                                           => m_Nm.StartHost();
        public void Shutdown(bool discardMessageQueue = false)            => m_Nm.Shutdown(discardMessageQueue);
        public void DisconnectClient(ulong clientId, string reason = null)
        {
            if (string.IsNullOrEmpty(reason)) m_Nm.DisconnectClient(clientId);
            else m_Nm.DisconnectClient(clientId, reason);
        }
        public string GetDisconnectReason(ulong clientId)                 => m_Nm.DisconnectReason;

        public event Action OnServerStarted;
        public event Action<bool> OnServerStopped;
        public event Action OnTransportFailure;
        public event Action<ulong> OnClientConnected;
        public event Action<ulong, string> OnClientDisconnected;

        public event Func<ApprovalRequest, ApprovalResult> ApprovalCheck
        {
            add { m_ApprovalHandler = value; }      // 단일 핸들러 규약 — 덮어쓰기
            remove
            {
                if (m_ApprovalHandler == value) m_ApprovalHandler = null;
            }
        }

        void OnConnectionEvent(NetworkManager nm, ConnectionEventData data)
        {
            switch (data.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    OnClientConnected?.Invoke(data.ClientId);
                    break;
                case ConnectionEvent.ClientDisconnected:
                    OnClientDisconnected?.Invoke(data.ClientId, m_Nm.DisconnectReason);
                    break;
            }
        }

        void OnApprovalCallback(NetworkManager.ConnectionApprovalRequest req,
                                NetworkManager.ConnectionApprovalResponse res)
        {
            var request = new ApprovalRequest(req.ClientNetworkId, req.Payload, m_Nm.ConnectedClientsIds.Count);
            var result = m_ApprovalHandler?.Invoke(request) ?? ApprovalResult.Allow();
            res.Approved = result.Approved;
            res.Reason   = result.Reason;
            res.CreatePlayerObject = result.Approved;
            res.Position = UnityEngine.Vector3.zero;
            res.Rotation = UnityEngine.Quaternion.identity;
        }

        void InvokeServerStarted()            => OnServerStarted?.Invoke();
        void InvokeServerStopped(bool isHost) => OnServerStopped?.Invoke(isHost);
        void InvokeTransportFailure()         => OnTransportFailure?.Invoke();

        public void Dispose()
        {
            m_Nm.OnServerStarted        -= InvokeServerStarted;
            m_Nm.OnServerStopped        -= InvokeServerStopped;
            m_Nm.OnTransportFailure     -= InvokeTransportFailure;
            m_Nm.OnConnectionEvent      -= OnConnectionEvent;
            m_Nm.ConnectionApprovalCallback -= OnApprovalCallback;
        }
    }
}
