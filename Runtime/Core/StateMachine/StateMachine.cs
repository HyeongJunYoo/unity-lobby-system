using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.StateMachine
{
    public sealed class StateMachine : IDisposable
    {
        readonly IReadOnlyDictionary<Type, ConnectionState> m_States;
        readonly INetworkFacade m_Network;
        readonly ILobbyLogger m_Logger;
        ConnectionState m_Current;
        bool m_Started;

        // 이벤트 핸들러 참조 보관 (Dispose 시 해제용)
        Action m_OnServerStartedHandler;
        Action<bool> m_OnServerStoppedHandler;
        Action m_OnTransportFailureHandler;
        Action<ulong> m_OnClientConnectedHandler;
        Action<ulong, string> m_OnClientDisconnectedHandler;
        Func<ApprovalRequest, ApprovalResult> m_OnApprovalHandler;

        public StateMachine(
            IReadOnlyDictionary<Type, ConnectionState> states,
            INetworkFacade network,
            ILobbyLogger logger)
        {
            m_States = states ?? throw new ArgumentNullException(nameof(states));
            m_Network = network ?? throw new ArgumentNullException(nameof(network));
            m_Logger = logger ?? NullLogger.Instance;
        }

        public void Start<TInitial>() where TInitial : ConnectionState
        {
            if (m_Started) throw new InvalidOperationException("StateMachine already started.");
            m_Started = true;

            m_OnServerStartedHandler      = () => m_Current.OnServerStarted();
            m_OnServerStoppedHandler      = _ => m_Current.OnServerStopped();
            m_OnTransportFailureHandler   = () => m_Current.OnTransportFailure();
            m_OnClientConnectedHandler    = id => m_Current.OnClientConnected(id);
            m_OnClientDisconnectedHandler = (id, reason) => m_Current.OnClientDisconnected(id, reason);
            m_OnApprovalHandler           = req => m_Current.ApprovalCheck(req);

            m_Network.OnServerStarted      += m_OnServerStartedHandler;
            m_Network.OnServerStopped      += m_OnServerStoppedHandler;
            m_Network.OnTransportFailure   += m_OnTransportFailureHandler;
            m_Network.OnClientConnected    += m_OnClientConnectedHandler;
            m_Network.OnClientDisconnected += m_OnClientDisconnectedHandler;
            m_Network.ApprovalCheck        += m_OnApprovalHandler;

            ChangeState<TInitial>();
        }

        public void ChangeState<TState>() where TState : ConnectionState
        {
            if (!m_States.TryGetValue(typeof(TState), out var next))
                throw new InvalidOperationException($"State not registered: {typeof(TState).Name}");

            m_Logger.Info($"{m_Current?.GetType().Name ?? "(null)"} → {typeof(TState).Name}");
            m_Current?.Exit();
            m_Current = next;
            m_Current.Enter();
        }

        public TState GetState<TState>() where TState : ConnectionState
            => (TState)m_States[typeof(TState)];

        public void StartClient(ConnectionMethodBase method) => m_Current.StartClient(method);
        public void StartHost(ConnectionMethodBase method)   => m_Current.StartHost(method);
        public void RequestShutdown()                         => m_Current.OnUserRequestedShutdown();

        public ConnectionState CurrentState => m_Current;

        public void Dispose()
        {
            if (!m_Started) return;
            m_Network.OnServerStarted      -= m_OnServerStartedHandler;
            m_Network.OnServerStopped      -= m_OnServerStoppedHandler;
            m_Network.OnTransportFailure   -= m_OnTransportFailureHandler;
            m_Network.OnClientConnected    -= m_OnClientConnectedHandler;
            m_Network.OnClientDisconnected -= m_OnClientDisconnectedHandler;
            m_Network.ApprovalCheck        -= m_OnApprovalHandler;
            m_Started = false;
        }
    }
}
