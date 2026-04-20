using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public class ClientConnectingState : OnlineState
    {
        protected ConnectionMethodBase m_ConnectionMethod;

        public ClientConnectingState(IStateMachineContext context) : base(context) { }

        public ClientConnectingState Configure(ConnectionMethodBase method)
        {
            m_ConnectionMethod = method;
            return this;
        }

        public override void Enter() => ConnectClient();

        public override void OnClientConnected(ulong _)
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.Success);
            Context.ChangeState<ClientConnectedState>();
        }

        public override void OnClientDisconnected(ulong _, string reason)
        {
            StartingClientFailed(reason);
        }

        protected void StartingClientFailed(string reason = null)
        {
            var actualReason = reason ?? Context.Network.GetDisconnectReason(Context.Network.LocalClientId);
            if (string.IsNullOrEmpty(actualReason))
                Context.ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
            else
                Context.ConnectStatusPublisher.Publish(ParseDisconnectReason(actualReason, Context.Logger));

            Context.ChangeState<OfflineState>();
        }

        protected internal void ConnectClient()
        {
            try
            {
                m_ConnectionMethod.SetupClientConnection();
                if (m_ConnectionMethod.RequiresManualNetworkStart)
                {
                    if (!Context.Network.StartClient())
                        throw new Exception("INetworkFacade.StartClient returned false");
                }
            }
            catch (Exception e)
            {
                Context.Logger.Error($"Error connecting client: {e.Message}");
                StartingClientFailed();
                throw;
            }
        }
    }
}
