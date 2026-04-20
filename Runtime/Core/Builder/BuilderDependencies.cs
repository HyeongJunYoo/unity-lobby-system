using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Builder
{
    internal sealed class BuilderDependencies : IStateMachineContextDeps
    {
        public StateMachine.StateMachine StateMachine { get; set; }
        public INetworkFacade Network { get; set; }
        public ISessionManager Sessions { get; set; }
        public IConnectionApprover Approver { get; set; }
        public ILobbyLogger Logger { get; set; }
        public IConnectionPayloadSerializer PayloadSerializer { get; set; }
        public ICoroutineRunner CoroutineRunner { get; set; }
        public PlayerIdentity Identity { get; set; }
        public ReconnectPolicy ReconnectPolicy { get; set; }
        public int MaxConnectedPlayers { get; set; }
        public IPublisher<ConnectStatus> ConnectStatusPublisher { get; set; }
        public IPublisher<ReconnectMessage> ReconnectPublisher { get; set; }
        public IPublisher<ConnectionEventMessage> ConnectionEventPublisher { get; set; }
        public IPublisher<LobbyLifecycleMessage> LifecyclePublisher { get; set; }
        public Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData { get; set; }
    }
}
