using System;
using System.Collections.Generic;

namespace Multiplayer.Lobby.Messaging
{
    public abstract class MessageChannelBase<T> : IMessageChannel<T>
    {
        protected readonly List<Action<T>> m_Handlers = new();
        public bool IsDisposed { get; private set; }

        public abstract void Publish(T message);

        public virtual IDisposable Subscribe(Action<T> handler)
        {
            ThrowIfDisposed();
            if (!m_Handlers.Contains(handler)) m_Handlers.Add(handler);
            return new DisposableSubscription<T>(m_Handlers, handler);
        }

        public virtual void Unsubscribe(Action<T> handler)
        {
            if (IsDisposed) return;
            m_Handlers.Remove(handler);
        }

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                m_Handlers.Clear();
            }
        }

        protected void InvokeHandlers(T message)
        {
            var snapshot = new List<Action<T>>(m_Handlers);
            foreach (var h in snapshot) h?.Invoke(message);
        }

        protected void ThrowIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
        }
    }
}
