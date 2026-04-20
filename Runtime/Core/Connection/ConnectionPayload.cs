using System;

namespace Multiplayer.Lobby.Connection
{
    [Serializable]
    public class ConnectionPayload
    {
        public string playerId;
        public string playerName;
        public bool isDebug;
    }
}
