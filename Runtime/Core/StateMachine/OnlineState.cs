using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.States;

namespace Multiplayer.Lobby.StateMachine
{
    public abstract class OnlineState : ConnectionState
    {
        protected OnlineState(IStateMachineContext context) : base(context) { }

        public override void OnUserRequestedShutdown()
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            Context.ChangeState<OfflineState>();
        }

        public override void OnTransportFailure()
        {
            Context.ChangeState<OfflineState>();
        }
    }
}
