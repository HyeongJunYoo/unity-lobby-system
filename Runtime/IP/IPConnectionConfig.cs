using System;

namespace Multiplayer.Lobby.IP
{
    /// <summary>
    /// Configuration data for IP direct connections.
    /// </summary>
    [Serializable]
    public class IPConnectionConfig
    {
        public string IpAddress = "127.0.0.1";
        public ushort Port = 9998;
    }
}
