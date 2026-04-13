using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;

namespace Multiplayer.Lobby.IP
{
    /// <summary>
    /// IP direct connection method using Unity Transport (UTP).
    /// Sets up connection data on the transport and lets the state machine call
    /// NetworkManager.StartHost/StartClient.
    /// </summary>
    public class IPConnectionMethod : ConnectionMethodBase
    {
        readonly string m_IpAddress;
        readonly ushort m_Port;

        public IPConnectionMethod(string ipAddress, ushort port,
            LobbyConnectionManager connectionManager, PlayerIdentity playerIdentity, string playerName)
            : base(connectionManager, playerIdentity, playerName)
        {
            m_IpAddress = ipAddress;
            m_Port = port;
        }

        public override void SetupClientConnection() => SetupConnection();

        public override void SetupHostConnection() => SetupConnection();

        void SetupConnection()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName);
            var transport = m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            if (transport == null)
                throw new System.InvalidOperationException("NetworkTransport is not configured on the NetworkManager.");
            var utp = (UnityTransport)transport;
            utp.SetConnectionData(m_IpAddress, m_Port);
        }

        public override Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
        {
            // IP connections can always retry without additional setup
            return Task.FromResult((true, true));
        }
    }
}
