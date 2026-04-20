using System;
using System.Collections.Generic;

namespace Multiplayer.Lobby.Messaging
{
    public sealed class DisposableSubscription<T> : IDisposable
    {
        readonly List<Action<T>> m_Handlers;
        readonly Action<T> m_Handler;
        bool m_Disposed;

        public DisposableSubscription(List<Action<T>> handlers, Action<T> handler)
        {
            m_Handlers = handlers;
            m_Handler = handler;
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Handlers.Remove(m_Handler);
        }
    }
}
