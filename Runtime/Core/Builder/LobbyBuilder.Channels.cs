using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        readonly Dictionary<Type, object> m_Channels = new();

        public LobbyBuilder UseDefaultMessageChannels()
        {
            AddChannel(new MessageChannel<ConnectStatus>());
            AddChannel(new MessageChannel<ReconnectMessage>());
            AddChannel(new MessageChannel<ConnectionEventMessage>());
            AddChannel(new MessageChannel<LobbyLifecycleMessage>());
            return this;
        }

        public LobbyBuilder AddMessageChannel<TMessage>()
        {
            AddChannel(new MessageChannel<TMessage>());
            return this;
        }

        public LobbyBuilder AddMessageChannel<TMessage>(IMessageChannel<TMessage> channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            m_Channels[typeof(TMessage)] = channel;
            return this;
        }

        void AddChannel<TMessage>(IMessageChannel<TMessage> channel)
            => m_Channels[typeof(TMessage)] = channel;

        internal IMessageChannel<TMessage> ResolveChannel<TMessage>()
        {
            if (m_Channels.TryGetValue(typeof(TMessage), out var ch))
                return (IMessageChannel<TMessage>)ch;
            throw new InvalidOperationException(
                $"Message channel not registered for {typeof(TMessage).Name}. Call UseDefaultMessageChannels() or AddMessageChannel<{typeof(TMessage).Name}>().");
        }
    }
}
