using System;

namespace Multiplayer.Lobby.ConnectionMethods.IP
{
    [Serializable]
    public class IPConnectionConfig
    {
        public string IpAddress = "127.0.0.1";
        public ushort Port = 9998;
    }
}
