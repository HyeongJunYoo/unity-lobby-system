using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.StateMachine
{
    public abstract class ConnectionState
    {
        protected IStateMachineContext Context { get; }

        protected ConnectionState(IStateMachineContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public virtual void Enter() { }
        public virtual void Exit() { }

        public virtual void OnClientConnected(ulong clientId) { }
        public virtual void OnClientDisconnected(ulong clientId, string reason) { }
        public virtual void OnServerStarted() { }
        public virtual void OnServerStopped() { }
        public virtual void OnTransportFailure() { }

        public virtual void StartClient(ConnectionMethodBase method) { }
        public virtual void StartHost(ConnectionMethodBase method) { }
        public virtual void OnUserRequestedShutdown() { }

        public virtual ApprovalResult ApprovalCheck(ApprovalRequest request)
            => Context.Approver.Approve(request);

        protected static ConnectStatus ParseDisconnectReason(string reason, ILobbyLogger logger = null)
        {
            if (string.IsNullOrEmpty(reason)) return ConnectStatus.GenericDisconnect;
            if (Enum.TryParse<ConnectStatus>(reason, out var s)) return s;
            logger?.Warning($"[LobbySystem] Failed to parse disconnect reason: '{reason}'");
            return ConnectStatus.GenericDisconnect;
        }
    }
}
