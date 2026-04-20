using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyConnection : IDisposable
    {
        readonly StateMachine.StateMachine m_Machine;
        readonly ISessionManager m_Sessions;
        readonly INetworkFacade m_Network;
        bool m_Disposed;

        internal LobbyConnection(StateMachine.StateMachine machine, ISessionManager sessions, INetworkFacade network)
        {
            m_Machine = machine;
            m_Sessions = sessions;
            m_Network = network;
        }

        public ISessionManager Sessions => m_Sessions;
        public INetworkFacade Network   => m_Network;

        public void StartClient(ConnectionMethodBase method) => m_Machine.StartClient(method);
        public void StartHost(ConnectionMethodBase method)   => m_Machine.StartHost(method);
        public void RequestShutdown()                         => m_Machine.RequestShutdown();

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Machine.Dispose();
        }
    }
}
