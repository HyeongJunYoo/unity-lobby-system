using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyConnection
    {
        internal Dictionary<Type, object> Channels { get; set; }

        /// <summary>등록된 메시지 채널의 Publisher를 반환한다. 미등록 타입이면 InvalidOperationException.</summary>
        public IPublisher<TMessage> GetPublisher<TMessage>()
            => ResolveChannel<TMessage>();

        /// <summary>등록된 메시지 채널의 Subscriber를 반환한다. 미등록 타입이면 InvalidOperationException.</summary>
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
