using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.StateMachine
{
    /// <summary>
    /// 빌더가 조립해 넘겨주는 의존성 번들.
    /// StateMachineContext가 이 번들을 래핑해 IStateMachineContext로 노출한다.
    /// </summary>
    public interface IStateMachineContextDeps
    {
        StateMachine StateMachine { get; }
        INetworkFacade Network { get; }
        ISessionManager Sessions { get; }
        IConnectionApprover Approver { get; }
        ILobbyLogger Logger { get; }
        IConnectionPayloadSerializer PayloadSerializer { get; }
        ICoroutineRunner CoroutineRunner { get; }
        PlayerIdentity Identity { get; }
        ReconnectPolicy ReconnectPolicy { get; }
        int MaxConnectedPlayers { get; }
        IPublisher<ConnectStatus> ConnectStatusPublisher { get; }
        IPublisher<ReconnectMessage> ReconnectPublisher { get; }
        IPublisher<ConnectionEventMessage> ConnectionEventPublisher { get; }
        IPublisher<LobbyLifecycleMessage> LifecyclePublisher { get; }
        Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData { get; }
    }

    public sealed class StateMachineContext : IStateMachineContext
    {
        readonly IStateMachineContextDeps m_Deps;

        public StateMachineContext(IStateMachineContextDeps deps)
        {
            m_Deps = deps ?? throw new ArgumentNullException(nameof(deps));
        }

        public INetworkFacade Network                             => m_Deps.Network;
        public ISessionManager Sessions                           => m_Deps.Sessions;
        public IConnectionApprover Approver                       => m_Deps.Approver;
        public ILobbyLogger Logger                                => m_Deps.Logger;
        public IConnectionPayloadSerializer PayloadSerializer     => m_Deps.PayloadSerializer;
        public ICoroutineRunner CoroutineRunner                   => m_Deps.CoroutineRunner;
        public PlayerIdentity Identity                            => m_Deps.Identity;
        public ReconnectPolicy ReconnectPolicy                    => m_Deps.ReconnectPolicy;
        public int MaxConnectedPlayers                            => m_Deps.MaxConnectedPlayers;
        public IPublisher<ConnectStatus> ConnectStatusPublisher   => m_Deps.ConnectStatusPublisher;
        public IPublisher<ReconnectMessage> ReconnectPublisher    => m_Deps.ReconnectPublisher;
        public IPublisher<ConnectionEventMessage> ConnectionEventPublisher => m_Deps.ConnectionEventPublisher;
        public IPublisher<LobbyLifecycleMessage> LifecyclePublisher=> m_Deps.LifecyclePublisher;
        public Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData => m_Deps.CreatePlayerData;

        public void ChangeState<TState>() where TState : ConnectionState
            => m_Deps.StateMachine.ChangeState<TState>();

        public TState GetState<TState>() where TState : ConnectionState
            => m_Deps.StateMachine.GetState<TState>();
    }
}
