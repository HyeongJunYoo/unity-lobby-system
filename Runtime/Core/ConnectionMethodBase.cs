using System.Threading.Tasks;

namespace Multiplayer.Lobby
{
    /// <summary>
    /// Base class for connection methods. Subclass to implement different connection strategies
    /// (IP direct, Relay, Photon, etc.)
    /// </summary>
    public abstract class ConnectionMethodBase
    {
        protected LobbyConnectionManager m_ConnectionManager;
        protected readonly PlayerIdentity m_PlayerIdentity;
        protected readonly string m_PlayerName;

        public ConnectionMethodBase(LobbyConnectionManager connectionManager, PlayerIdentity playerIdentity, string playerName)
        {
            m_ConnectionManager = connectionManager;
            m_PlayerIdentity = playerIdentity;
            m_PlayerName = playerName;
        }

        /// <summary>
        /// Setup the host connection prior to starting the NetworkManager.
        /// </summary>
        public abstract void SetupHostConnection();

        /// <summary>
        /// Setup the client connection prior to starting the NetworkManager.
        /// </summary>
        public abstract void SetupClientConnection();

        /// <summary>
        /// Setup the client for reconnection.
        /// Returns (success, shouldTryAgain).
        /// </summary>
        public abstract Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync();

        /// <summary>
        /// Whether this connection method requires manually calling NetworkManager.StartHost/StartClient.
        /// IP connections return true. SDK-managed connections (Relay, Photon) may return false.
        /// </summary>
        public virtual bool RequiresManualNetworkStart => true;

        protected void SetConnectionPayload(string playerId, string playerName)
        {
            var payload = UnityEngine.JsonUtility.ToJson(new ConnectionPayload
            {
                playerId = playerId,
                playerName = playerName,
                isDebug = UnityEngine.Debug.isDebugBuild
            });

            m_ConnectionManager.NetworkManager.NetworkConfig.ConnectionData =
                System.Text.Encoding.UTF8.GetBytes(payload);
        }

        protected string GetPlayerId()
        {
            return m_PlayerIdentity.GetPlayerId();
        }
    }
}
