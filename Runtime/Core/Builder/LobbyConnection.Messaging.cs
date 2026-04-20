using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyConnection
    {
        internal Dictionary<Type, object> Channels { get; set; }

        public IPublisher<TMessage> GetPublisher<TMessage>()
            => ResolveChannel<TMessage>();

        public ISubscriber<TMessage> GetSubscriber<TMessage>()
            => ResolveChannel<TMessage>();

        IMessageChannel<TMessage> ResolveChannel<TMessage>()
        {
            if (Channels != null && Channels.TryGetValue(typeof(TMessage), out var ch))
                return (IMessageChannel<TMessage>)ch;
            throw new InvalidOperationException(
                $"Message channel not registered for {typeof(TMessage).Name}.");
        }
    }
}
