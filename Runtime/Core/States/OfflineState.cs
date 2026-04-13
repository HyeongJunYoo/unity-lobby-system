namespace Multiplayer.Lobby.States
{
    /// <summary>
    /// Connection state when the NetworkManager is shut down.
    /// Transitions to ClientConnecting or StartingHost on connection requests.
    /// </summary>
    public class OfflineState : ConnectionState
    {
        public override void Enter()
        {
            m_ConnectionManager.NetworkManager.Shutdown();
            m_ConnectionManager.InvokeOnDisconnected();
        }

        public override void Exit() { }

        public override void StartClient(ConnectionMethodBase connectionMethod)
        {
            m_ConnectionManager.ClientReconnecting.Configure(connectionMethod);
            m_ConnectionManager.ChangeState(m_ConnectionManager.ClientConnecting.Configure(connectionMethod));
        }

        public override void StartHost(ConnectionMethodBase connectionMethod)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.StartingHost.Configure(connectionMethod));
        }
    }
}
