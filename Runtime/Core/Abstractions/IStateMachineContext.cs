using System;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Abstractions
{
    public interface IStateMachineContext
    {
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

        void ChangeState<TState>() where TState : ConnectionState;
        TState GetState<TState>() where TState : ConnectionState;
    }
}
