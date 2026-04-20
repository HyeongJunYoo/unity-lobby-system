using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class StateHarness : IStateMachineContextDeps
    {
        public StateMachine Machine { get; private set; }
        public FakeNetworkFacade Network { get; } = new FakeNetworkFacade();
        public FakeSessionManager SessionManager { get; } = new FakeSessionManager();
        public FakeApprover Approver { get; } = new FakeApprover();
        public FakeLogger Logger { get; } = new FakeLogger();
        public FakeConnectionPayloadSerializer PayloadSerializer { get; } = new FakeConnectionPayloadSerializer();
        public FakeCoroutineRunner CoroutineRunner { get; } = new FakeCoroutineRunner();
        public PlayerIdentity Identity { get; } = new PlayerIdentity(new InMemoryPlayerIdentityStore());
        public ReconnectPolicy ReconnectPolicy { get; set; } = ReconnectPolicy.Default;
        public int MaxConnectedPlayers { get; set; } = 8;

        public MessageChannel<ConnectStatus> ConnectStatusChannel { get; } = new();
        public MessageChannel<ReconnectMessage> ReconnectChannel { get; } = new();
        public MessageChannel<ConnectionEventMessage> ConnectionEventChannel { get; } = new();
        public MessageChannel<LobbyLifecycleMessage> LifecycleChannel { get; } = new();

        public IPublisher<ConnectStatus> ConnectStatusPublisher         => ConnectStatusChannel;
        public IPublisher<ReconnectMessage> ReconnectPublisher          => ReconnectChannel;
        public IPublisher<ConnectionEventMessage> ConnectionEventPublisher => ConnectionEventChannel;
        public IPublisher<LobbyLifecycleMessage> LifecyclePublisher     => LifecycleChannel;

        // Exposed for state tests
        public IMessageChannel<LobbyLifecycleMessage> LifecycleChannelPublic => LifecycleChannel;
        public IMessageChannel<ConnectStatus> ConnectStatusChannelPublic => ConnectStatusChannel;

        ISessionManager IStateMachineContextDeps.Sessions => SessionManager;
        IConnectionApprover IStateMachineContextDeps.Approver => Approver;
        ILobbyLogger IStateMachineContextDeps.Logger => Logger;
        IConnectionPayloadSerializer IStateMachineContextDeps.PayloadSerializer => PayloadSerializer;
        ICoroutineRunner IStateMachineContextDeps.CoroutineRunner => CoroutineRunner;
        INetworkFacade IStateMachineContextDeps.Network => Network;
        StateMachine IStateMachineContextDeps.StateMachine => Machine;

        public Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData { get; set; }
            = (id, payload) => new FakeSessionPlayerData(id, payload?.playerName ?? "");

        public static StateHarness Build(params Type[] stateTypes)
        {
            var h = new StateHarness();
            var states = new Dictionary<Type, ConnectionState>();
            h.Machine = new StateMachine(states, h.Network, h.Logger);
            var ctx = new StateMachineContext(h);
            foreach (var t in stateTypes)
                states[t] = (ConnectionState)Activator.CreateInstance(t, ctx);
            return h;
        }
    }
}
