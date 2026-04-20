using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        readonly List<Action> m_OnHostStarted    = new();
        readonly List<Action> m_OnClientConnected = new();
        readonly List<Action> m_OnDisconnected   = new();

        public LobbyBuilder OnHostStarted(Action handler)    { m_OnHostStarted.Add(handler); return this; }
        public LobbyBuilder OnClientConnected(Action handler){ m_OnClientConnected.Add(handler); return this; }
        public LobbyBuilder OnDisconnected(Action handler)   { m_OnDisconnected.Add(handler); return this; }

        internal void ApplyLifecycleHooks(LobbyConnection conn)
        {
            foreach (var h in m_OnHostStarted)     conn.OnHostStarted    += h;
            foreach (var h in m_OnClientConnected) conn.OnClientConnected += h;
            foreach (var h in m_OnDisconnected)    conn.OnDisconnected   += h;
        }
    }
}
