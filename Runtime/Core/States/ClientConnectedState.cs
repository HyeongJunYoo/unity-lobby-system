using Multiplayer.Lobby;

namespace Multiplayer.Lobby.States
{
    /// <summary>
    /// State when a client is connected to a server.
    /// On disconnect, transitions to Reconnecting or Offline depending on the reason.
    /// </summary>
    public class ClientConnectedState : OnlineState
    {
        public override void Enter()
        {
            m_ConnectionManager.InvokeOnClientConnected();
        }

        public override void Exit() { }

        public override void OnClientDisconnected(ulong _)
        {
            var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
            var connectStatus = ParseDisconnectReason(disconnectReason);

            switch (connectStatus)
            {
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.HostEndedSession:
                case ConnectStatus.ServerFull:
                case ConnectStatus.IncompatibleBuildType:
                    m_ConnectStatusPublisher.Publish(connectStatus);
                    m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
                    break;
                default:
                    m_ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
                    m_ConnectionManager.ChangeState(m_ConnectionManager.ClientReconnecting);
                    break;
            }
        }
    }
}
