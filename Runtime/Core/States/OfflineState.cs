using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class OfflineState : ConnectionState
    {
        public OfflineState(IStateMachineContext context) : base(context) { }

        public override void Enter()
        {
            Context.Network.Shutdown();
            Context.LifecyclePublisher.Publish(LobbyLifecycleMessage.Disconnected);
        }

        public override void StartClient(Connection.ConnectionMethodBase method)
        {
            // ClientConnecting과 ClientReconnecting 모두 Configure 대상 (Task 20 참고)
            Context.GetState<ClientReconnectingState>().Configure(method);
            var cc = Context.GetState<ClientConnectingState>();
            cc.Configure(method);
            Context.ChangeState<ClientConnectingState>();
        }

        public override void StartHost(Connection.ConnectionMethodBase method)
        {
            var sh = Context.GetState<StartingHostState>();
            sh.Configure(method);
            Context.ChangeState<StartingHostState>();
        }
    }
}
