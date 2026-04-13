using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Multiplayer.Lobby.Infrastructure
{
    /// <summary>
    /// Provides a MonoBehaviour-free way to run update callbacks via VContainer's ITickable.
    /// </summary>
    public class UpdateRunner : ITickable, IDisposable
    {
        readonly Queue<Action> m_PendingHandlers = new();
        readonly List<Action<float>> m_Subscribers = new();
        bool m_IsDisposed;

        public void Subscribe(Action<float> handler)
        {
            if (!m_IsDisposed && !m_Subscribers.Contains(handler))
            {
                m_PendingHandlers.Enqueue(() => m_Subscribers.Add(handler));
            }
        }

        public void Unsubscribe(Action<float> handler)
        {
            if (!m_IsDisposed)
            {
                m_PendingHandlers.Enqueue(() => m_Subscribers.Remove(handler));
            }
        }

        public void Tick()
        {
            if (m_IsDisposed) return;

            while (m_PendingHandlers.Count > 0)
            {
                m_PendingHandlers.Dequeue()?.Invoke();
            }

            var dt = Time.deltaTime;
            foreach (var subscriber in m_Subscribers)
            {
                subscriber?.Invoke(dt);
            }
        }

        public void Dispose()
        {
            m_IsDisposed = true;
            m_Subscribers.Clear();
            m_PendingHandlers.Clear();
        }
    }
}
