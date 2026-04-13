using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Multiplayer.Lobby
{
    /// <summary>
    /// Base class representing a connection state in the state machine.
    /// </summary>
    public abstract class ConnectionState
    {
        [Inject]
        protected LobbyConnectionManager m_ConnectionManager;

        public abstract void Enter();
        public abstract void Exit();

        protected static ConnectStatus ParseDisconnectReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return ConnectStatus.GenericDisconnect;
            }

            if (System.Enum.TryParse<ConnectStatus>(reason, out var status))
            {
                return status;
            }

            Debug.LogWarning($"[LobbySystem] Failed to parse disconnect reason: '{reason}'");
            return ConnectStatus.GenericDisconnect;
        }

        public virtual void OnClientConnected(ulong clientId) { }
        public virtual void OnClientDisconnected(ulong clientId) { }
        public virtual void OnServerStarted() { }
        public virtual void OnTransportFailure() { }
        public virtual void OnServerStopped() { }

        public virtual void StartClient(ConnectionMethodBase connectionMethod) { }
        public virtual void StartHost(ConnectionMethodBase connectionMethod) { }

        public virtual void OnUserRequestedShutdown() { }

        public virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response) { }
    }
}
