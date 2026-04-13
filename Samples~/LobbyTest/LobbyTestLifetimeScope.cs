using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Multiplayer.Lobby.Infrastructure;
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Sample
{
    /// <summary>
    /// VContainer LifetimeScope that wires up all lobby system dependencies.
    /// Attach this to a GameObject in your test scene.
    /// </summary>
    public class LobbyTestLifetimeScope : LifetimeScope
    {
        [SerializeField] LobbyConnectionManager connectionManager;
        [SerializeField] NetworkManager networkManager;
        [SerializeField] LobbyTestUI lobbyTestUI;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(connectionManager);
            builder.RegisterComponent(networkManager);
            builder.RegisterComponent(lobbyTestUI);

            // PubSub channels
            builder.Register<MessageChannel<ConnectStatus>>(Lifetime.Singleton)
                .AsImplementedInterfaces();
            builder.Register<MessageChannel<ReconnectMessage>>(Lifetime.Singleton)
                .AsImplementedInterfaces();
            builder.Register<MessageChannel<ConnectionEventMessage>>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            // Session
            builder.Register<SessionManager>(Lifetime.Singleton);

            // Player identity
            builder.Register<PlayerIdentity>(Lifetime.Singleton);

            // Update runner
            builder.Register<UpdateRunner>(Lifetime.Singleton)
                .AsImplementedInterfaces();
        }
    }
}
