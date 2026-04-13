using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Sample
{
    /// <summary>
    /// Minimal ISessionPlayerData implementation for testing.
    /// In a real game, add avatar, HP, inventory, etc.
    /// </summary>
    public class SampleSessionPlayerData : ISessionPlayerData
    {
        public string PlayerName;
        public bool IsConnected { get; set; }
        public ulong ClientID { get; set; }

        public SampleSessionPlayerData(ulong clientId, string playerName, bool isConnected)
        {
            ClientID = clientId;
            PlayerName = playerName;
            IsConnected = isConnected;
        }

        public void Reinitialize()
        {
            PlayerName = "";
        }
    }
}
