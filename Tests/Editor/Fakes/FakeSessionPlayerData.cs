using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeSessionPlayerData : ISessionPlayerData
    {
        public bool IsConnected { get; set; } = true;
        public ulong ClientID { get; set; }
        public string Name { get; set; } = "";
        public int ReinitializeCount { get; private set; }

        public FakeSessionPlayerData(ulong clientId, string name)
        {
            ClientID = clientId;
            Name = name;
        }

        public void Reinitialize() => ReinitializeCount++;
    }
}
