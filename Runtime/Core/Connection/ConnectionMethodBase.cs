using System.Threading.Tasks;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Connection
{
    public abstract class ConnectionMethodBase
    {
        protected readonly INetworkFacade m_Network;
        protected readonly IConnectionPayloadSerializer m_Serializer;
        protected readonly PlayerIdentity m_PlayerIdentity;
        protected readonly string m_PlayerName;
        protected readonly bool m_IsDebug;

        protected ConnectionMethodBase(
            INetworkFacade network,
            IConnectionPayloadSerializer serializer,
            PlayerIdentity playerIdentity,
            string playerName,
            bool isDebug)
        {
            m_Network = network;
            m_Serializer = serializer;
            m_PlayerIdentity = playerIdentity;
            m_PlayerName = playerName;
            m_IsDebug = isDebug;
        }

        public abstract void SetupHostConnection();
        public abstract void SetupClientConnection();
        public abstract Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync();

        public virtual bool RequiresManualNetworkStart => true;

        protected void SetConnectionPayload(string playerId, string playerName)
        {
            var payload = new ConnectionPayload
            {
                playerId = playerId,
                playerName = playerName,
                isDebug = m_IsDebug
            };
            m_Network.ConnectionPayload = m_Serializer.Serialize(payload);
        }

        protected string GetPlayerId() => m_PlayerIdentity.GetPlayerId();
    }
}
