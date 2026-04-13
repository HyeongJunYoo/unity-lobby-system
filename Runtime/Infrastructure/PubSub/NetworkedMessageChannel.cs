using System;
using Unity.Collections;
using Unity.Netcode;

namespace Multiplayer.Lobby.Infrastructure
{
    /// <summary>
    /// A message channel that sends messages over the network using NGO custom named messages.
    /// Only the server can publish; all connected clients receive the message.
    /// </summary>
    public class NetworkedMessageChannel<T> : MessageChannelBase<T> where T : unmanaged, INetworkSerializeByMemcpy
    {
        readonly NetworkManager m_NetworkManager;
        readonly string m_MessageName;

        public NetworkedMessageChannel(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_MessageName = $"N_{typeof(T).Name}";
        }

        public override void Publish(T message)
        {
            ThrowIfDisposed();

            if (m_NetworkManager.IsServer)
            {
                SendMessageThroughNetwork(message);
            }

            // Also invoke locally
            InvokeHandlers(message);
        }

        public override IDisposable Subscribe(Action<T> handler)
        {
            var subscription = base.Subscribe(handler);

            if (m_Handlers.Count == 1)
            {
                RegisterNetworkHandler();
            }

            return subscription;
        }

        public override void Unsubscribe(Action<T> handler)
        {
            base.Unsubscribe(handler);

            if (m_Handlers.Count == 0)
            {
                UnregisterNetworkHandler();
            }
        }

        public override void Dispose()
        {
            if (!IsDisposed)
            {
                UnregisterNetworkHandler();
            }
            base.Dispose();
        }

        void RegisterNetworkHandler()
        {
            if (m_NetworkManager != null && m_NetworkManager.CustomMessagingManager != null)
            {
                m_NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(m_MessageName, OnReceiveMessage);
            }
        }

        void UnregisterNetworkHandler()
        {
            if (m_NetworkManager != null && m_NetworkManager.CustomMessagingManager != null)
            {
                m_NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(m_MessageName);
            }
        }

        void SendMessageThroughNetwork(T message)
        {
            if (m_NetworkManager.CustomMessagingManager == null) return;

            var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize<T>(), Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(message);
                m_NetworkManager.CustomMessagingManager.SendNamedMessageToAll(m_MessageName, writer);
            }
        }

        void OnReceiveMessage(ulong clientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out T message);
            InvokeHandlers(message);
        }
    }
}
