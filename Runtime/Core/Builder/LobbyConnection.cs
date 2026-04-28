using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Builder
{
    /// <summary>
    /// 호스트/클라이언트 시작과 종료, PubSub/이벤트의 단일 진입점.
    /// LobbyBuilder.Build()가 반환하며, 종료 시 Dispose 필요.
    /// </summary>
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

        /// <summary>지정한 ConnectionMethod로 클라이언트 시작 흐름에 진입한다.</summary>
        public void StartClient(ConnectionMethodBase method) => m_Machine.StartClient(method);
        /// <summary>지정한 ConnectionMethod로 호스트 시작 흐름에 진입한다.</summary>
        public void StartHost(ConnectionMethodBase method)   => m_Machine.StartHost(method);
        /// <summary>사용자 의도적 종료. 현재 상태에 따라 클라이언트 끊기 또는 호스트 종료가 일어난다.</summary>
        public void RequestShutdown()                         => m_Machine.RequestShutdown();

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_LifecycleSubscription?.Dispose();
            m_Machine.Dispose();
        }
    }
}
