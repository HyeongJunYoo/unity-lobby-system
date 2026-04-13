using System;
using System.Collections.Generic;

namespace Multiplayer.Lobby.Infrastructure
{
    public class DisposableSubscription<T> : IDisposable
    {
        Action<T> m_Handler;
        bool m_IsDisposed;
        List<Action<T>> m_Handlers;

        public DisposableSubscription(List<Action<T>> handlers, Action<T> handler)
        {
            m_Handlers = handlers;
            m_Handler = handler;
        }

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                if (m_Handlers.Contains(m_Handler))
                {
                    m_Handlers.Remove(m_Handler);
                }
                m_Handler = null;
                m_Handlers = null;
            }
        }
    }
}
