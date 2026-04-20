using System.Collections.Generic;
using NUnit.Framework;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.StateMachine;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class StateMachineTests
    {
        sealed class StubState : ConnectionState
        {
            public int Enters, Exits;
            public StubState(IStateMachineContext ctx) : base(ctx) { }
            public override void Enter() => Enters++;
            public override void Exit()  => Exits++;
        }
        sealed class OtherState : ConnectionState
        {
            public int Enters;
            public OtherState(IStateMachineContext ctx) : base(ctx) { }
            public override void Enter() => Enters++;
        }

        [Test]
        public void StartTransitionsToInitialState()
        {
            var (sm, states) = BuildMachine();
            sm.Start<StubState>();
            Assert.That(((StubState)states[typeof(StubState)]).Enters, Is.EqualTo(1));
        }

        [Test]
        public void ChangeStateCallsExitThenEnter()
        {
            var (sm, states) = BuildMachine();
            sm.Start<StubState>();
            sm.ChangeState<OtherState>();
            Assert.That(((StubState)states[typeof(StubState)]).Exits, Is.EqualTo(1));
            Assert.That(((OtherState)states[typeof(OtherState)]).Enters, Is.EqualTo(1));
        }

        [Test]
        public void ChangeStateUnregisteredThrows()
        {
            var (sm, _) = BuildMachine();
            sm.Start<StubState>();
            Assert.Throws<System.InvalidOperationException>(
                () => sm.ChangeState<ThirdState>());
        }

        sealed class ThirdState : ConnectionState
        {
            public ThirdState(IStateMachineContext ctx) : base(ctx) { }
        }

        static (StateMachine, Dictionary<System.Type, ConnectionState>) BuildMachine()
        {
            var ctxHolder = new ContextHolder();
            var states = new Dictionary<System.Type, ConnectionState>();
            var sm = new StateMachine(states, new FakeNetworkFacade(), new FakeLogger());
            ctxHolder.StateMachine = sm;
            var ctx = new StateMachineContext(ctxHolder); // Task 16에서 정의됨
            states[typeof(StubState)]  = new StubState(ctx);
            states[typeof(OtherState)] = new OtherState(ctx);
            return (sm, states);
        }

        // 테스트 전용 컨텍스트 조립 헬퍼
        sealed class ContextHolder : IStateMachineContextDeps
        {
            public StateMachine StateMachine { get; set; }
            public INetworkFacade Network { get; } = new FakeNetworkFacade();
            public ISessionManager Sessions { get; } = new FakeSessionManager();
            public IConnectionApprover Approver { get; } = new FakeApprover();
            public ILobbyLogger Logger { get; } = new FakeLogger();
            public IConnectionPayloadSerializer PayloadSerializer { get; } = new FakeConnectionPayloadSerializer();
            public ICoroutineRunner CoroutineRunner { get; } = new FakeCoroutineRunner();
            public Connection.PlayerIdentity Identity { get; } = new Connection.PlayerIdentity(new InMemoryPlayerIdentityStore());
            public Connection.ReconnectPolicy ReconnectPolicy { get; } = Connection.ReconnectPolicy.Default;
            public int MaxConnectedPlayers => 8;
            public Messaging.IPublisher<Connection.ConnectStatus> ConnectStatusPublisher { get; } = new Messaging.MessageChannel<Connection.ConnectStatus>();
            public Messaging.IPublisher<Connection.ReconnectMessage> ReconnectPublisher { get; } = new Messaging.MessageChannel<Connection.ReconnectMessage>();
            public Messaging.IPublisher<Connection.ConnectionEventMessage> ConnectionEventPublisher { get; } = new Messaging.MessageChannel<Connection.ConnectionEventMessage>();
            public Messaging.IPublisher<Messaging.LobbyLifecycleMessage> LifecyclePublisher { get; } = new Messaging.MessageChannel<Messaging.LobbyLifecycleMessage>();
            public System.Func<ulong, Connection.ConnectionPayload, Session.ISessionPlayerData> CreatePlayerData { get; } = (_, _) => null;
        }
    }
}
