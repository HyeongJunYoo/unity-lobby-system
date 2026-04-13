using Multiplayer.Lobby.Infrastructure;
using VContainer;

namespace Multiplayer.Lobby
{
    /// <summary>
    /// Base class for states where the player is online (connected or hosting).
    /// Provides common shutdown and transport failure behavior.
    /// </summary>
    public abstract class OnlineState : ConnectionState
    {
        [Inject]
        protected IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        public override void OnUserRequestedShutdown()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
            m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
        }

        public override void OnTransportFailure()
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
        }
    }
}
