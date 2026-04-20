using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class ClientConnectedState : OnlineState
    {
        public ClientConnectedState(IStateMachineContext context) : base(context) { }

        public override void Enter()
            => Context.LifecyclePublisher.Publish(LobbyLifecycleMessage.ClientConnected);

        public override void OnClientDisconnected(ulong _, string reason)
        {
            var actualReason = reason ?? Context.Network.GetDisconnectReason(Context.Network.LocalClientId);
            var status = ParseDisconnectReason(actualReason, Context.Logger);
            switch (status)
            {
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.HostEndedSession:
                case ConnectStatus.ServerFull:
                case ConnectStatus.IncompatibleBuildType:
                    Context.ConnectStatusPublisher.Publish(status);
                    Context.ChangeState<OfflineState>();
                    break;
                default:
                    Context.ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
                    Context.ChangeState<ClientReconnectingState>();
                    break;
            }
        }
    }
}
