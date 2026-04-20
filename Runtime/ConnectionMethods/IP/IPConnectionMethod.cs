using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.ConnectionMethods.IP
{
    public sealed class IPConnectionMethod : ConnectionMethodBase
    {
        readonly NetworkManager m_NetworkManager;  // UTP 설정을 위해 어댑터 경유가 아닌 직접 접근 (ConnectionMethods/IP는 Netcode 참조 허용)
        readonly string m_IpAddress;
        readonly ushort m_Port;

        public IPConnectionMethod(
            INetworkFacade network,
            IConnectionPayloadSerializer serializer,
            NetworkManager networkManager,
            PlayerIdentity playerIdentity,
            string playerName,
            string ipAddress,
            ushort port,
            bool isDebug)
            : base(network, serializer, playerIdentity, playerName, isDebug)
        {
            m_NetworkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            m_IpAddress = ipAddress;
            m_Port = port;
        }

        public override void SetupClientConnection() => SetupConnection();
        public override void SetupHostConnection()   => SetupConnection();

        void SetupConnection()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName);
            var transport = m_NetworkManager.NetworkConfig.NetworkTransport;
            if (transport == null)
                throw new InvalidOperationException("NetworkTransport is not configured on the NetworkManager.");
            ((UnityTransport)transport).SetConnectionData(m_IpAddress, m_Port);
        }

        public override Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
            => Task.FromResult((true, true));
    }
}
