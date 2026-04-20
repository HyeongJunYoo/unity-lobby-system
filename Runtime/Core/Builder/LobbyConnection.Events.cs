using System;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyConnection
    {
        public event Action OnHostStarted;
        public event Action OnClientConnected;
        public event Action OnDisconnected;

        IDisposable m_LifecycleSubscription;

        internal void BindLifecycle(IMessageChannel<LobbyLifecycleMessage> channel)
        {
            m_LifecycleSubscription = channel.Subscribe(OnLifecycleMessage);
        }

        void OnLifecycleMessage(LobbyLifecycleMessage msg)
        {
            switch (msg)
            {
                case LobbyLifecycleMessage.HostStarted:    OnHostStarted?.Invoke(); break;
                case LobbyLifecycleMessage.ClientConnected: OnClientConnected?.Invoke(); break;
                case LobbyLifecycleMessage.Disconnected:   OnDisconnected?.Invoke(); break;
            }
        }
    }
}
