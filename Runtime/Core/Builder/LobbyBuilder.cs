using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        INetworkFacade m_Network;
        ITickSource m_Tick;
        ICoroutineRunner m_Coroutines;
        IConnectionPayloadSerializer m_Serializer;
        PlayerIdentity m_Identity;

        // 선택 의존성 (Build 시 기본값 주입)
        ILobbyLogger m_Logger;
        ISessionManager m_Sessions;
        IConnectionApprover m_Approver;
        ReconnectPolicy? m_ReconnectPolicy;
        int m_MaxConnectedPlayers = 8;
        System.Func<ulong, ConnectionPayload, Session.ISessionPlayerData> m_CreatePlayerData;

        public LobbyBuilder UseNetwork(INetworkFacade network)          { m_Network = network; return this; }
        public LobbyBuilder UseTickSource(ITickSource tick)             { m_Tick = tick; return this; }
        public LobbyBuilder UseCoroutineRunner(ICoroutineRunner runner) { m_Coroutines = runner; return this; }
        public LobbyBuilder UsePayloadSerializer(IConnectionPayloadSerializer s) { m_Serializer = s; return this; }
        public LobbyBuilder UseIdentity(PlayerIdentity identity)        { m_Identity = identity; return this; }

        public LobbyBuilder UseLogger(ILobbyLogger logger)            { m_Logger = logger; return this; }
        public LobbyBuilder UseSessionManager(ISessionManager sm)     { m_Sessions = sm; return this; }
        public LobbyBuilder UseApprover(IConnectionApprover approver) { m_Approver = approver; return this; }
        public LobbyBuilder UseReconnectPolicy(ReconnectPolicy policy){ m_ReconnectPolicy = policy; return this; }
        public LobbyBuilder UseMaxPlayers(int max)                    { m_MaxConnectedPlayers = max; return this; }
        public LobbyBuilder UseSessionPlayerDataFactory(
            System.Func<ulong, ConnectionPayload, Session.ISessionPlayerData> factory)
        { m_CreatePlayerData = factory; return this; }

        public LobbyConnection Build()
        {
            if (m_Network     == null) throw new InvalidOperationException("LobbyBuilder.UseNetwork(...)이 호출되지 않았습니다.");
            if (m_Tick        == null) throw new InvalidOperationException("LobbyBuilder.UseTickSource(...)이 호출되지 않았습니다. TickSource");
            if (m_Coroutines  == null) throw new InvalidOperationException("LobbyBuilder.UseCoroutineRunner(...)이 호출되지 않았습니다. CoroutineRunner");
            if (m_Identity    == null) throw new InvalidOperationException("LobbyBuilder.UseIdentity(...)이 호출되지 않았습니다. Identity");
            if (m_Serializer  == null) throw new InvalidOperationException("LobbyBuilder.UsePayloadSerializer(...)이 호출되지 않았습니다. PayloadSerializer");
            if (m_StateFactories.Count == 0)
                throw new InvalidOperationException("상태가 등록되지 않았습니다. UseDefaultStates() 또는 AddState<...>를 호출하십시오.");
            if (m_Channels.Count == 0)
                throw new InvalidOperationException("메시지 채널이 등록되지 않았습니다. UseDefaultMessageChannels()를 호출하십시오.");

            var deps = new BuilderDependencies
            {
                Network = m_Network,
                CoroutineRunner = m_Coroutines,
                PayloadSerializer = m_Serializer,
                Identity = m_Identity,
                Logger = m_Logger ?? NullLogger.Instance,
                Sessions = m_Sessions ?? new Session.SessionManager(m_Logger ?? NullLogger.Instance),
                Approver = m_Approver ?? new Connection.DefaultConnectionApprover(m_MaxConnectedPlayers),
                ReconnectPolicy = m_ReconnectPolicy ?? Connection.ReconnectPolicy.Default,
                MaxConnectedPlayers = m_MaxConnectedPlayers,
                ConnectStatusPublisher     = ResolveChannel<Connection.ConnectStatus>(),
                ReconnectPublisher         = ResolveChannel<Connection.ReconnectMessage>(),
                ConnectionEventPublisher   = ResolveChannel<Connection.ConnectionEventMessage>(),
                LifecyclePublisher         = ResolveChannel<Messaging.LobbyLifecycleMessage>(),
                CreatePlayerData = m_CreatePlayerData
            };

            var states = new System.Collections.Generic.Dictionary<System.Type, StateMachine.ConnectionState>();
            var machine = new StateMachine.StateMachine(states, deps.Network, deps.Logger);
            deps.StateMachine = machine;
            var ctx = new StateMachine.StateMachineContext(deps);

            foreach (var kv in m_StateFactories)
                states[kv.Key] = kv.Value(ctx);

            var conn = new LobbyConnection(machine, deps.Sessions, deps.Network)
            {
                Channels = new System.Collections.Generic.Dictionary<System.Type, object>(m_Channels)
            };

            // 생애주기 이벤트 재발행 배선
            conn.BindLifecycle(ResolveChannel<Messaging.LobbyLifecycleMessage>());

            // 초기 상태 전이: OfflineState (기본 프리셋에 반드시 포함)
            if (!states.ContainsKey(typeof(States.OfflineState)))
                throw new InvalidOperationException("OfflineState가 등록되지 않았습니다. UseDefaultStates() 또는 초기 상태로 OfflineState를 AddState하십시오.");
            machine.Start<States.OfflineState>();

            ApplyLifecycleHooks(conn);

            return conn;
        }
    }
}
