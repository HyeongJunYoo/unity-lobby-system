using System;
using Unity.Netcode;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.ConnectionMethods.IP
{
    public static class LobbyConnectionIpExtensions
    {
        /// <summary>
        /// IP 직접 연결로 클라이언트를 시작한다. NetworkManager가 필요하므로 파라미터로 받는다
        /// (LobbyConnection은 INetworkFacade만 알고 NetworkManager는 모름).
        /// </summary>
        public static void StartClientIp(this LobbyConnection lobby,
            NetworkManager networkManager,
            PlayerIdentity identity,
            IConnectionPayloadSerializer serializer,
            string playerName, string ipAddress, int port,
            bool isDebug)
        {
            ValidateIpParams(playerName, ipAddress, port);
            var method = new IPConnectionMethod(
                lobby.Network, serializer, networkManager, identity,
                playerName, ipAddress, (ushort)port, isDebug);
            lobby.StartClient(method);
        }

        public static void StartHostIp(this LobbyConnection lobby,
            NetworkManager networkManager,
            PlayerIdentity identity,
            IConnectionPayloadSerializer serializer,
            string playerName, string ipAddress, int port,
            bool isDebug)
        {
            ValidateIpParams(playerName, ipAddress, port);
            var method = new IPConnectionMethod(
                lobby.Network, serializer, networkManager, identity,
                playerName, ipAddress, (ushort)port, isDebug);
            lobby.StartHost(method);
        }

        static void ValidateIpParams(string name, string ip, int port)
        {
            if (string.IsNullOrEmpty(name))  throw new ArgumentException("playerName cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(ip))    throw new ArgumentException("ipAddress cannot be null or empty", nameof(ip));
            if (port < 0 || port > 65535)    throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 0 and 65535");
        }
    }
}
