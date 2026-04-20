using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Sample.BasicManual
{
    public sealed class SampleSessionPlayerData : ISessionPlayerData
    {
        public bool IsConnected { get; set; } = true;
        public ulong ClientID { get; set; }
        public string PlayerName { get; set; }

        public SampleSessionPlayerData(ulong clientId, string name)
        {
            ClientID = clientId;
            PlayerName = name;
        }

        public void Reinitialize() { /* 게임 진입 시 리셋할 상태 있으면 여기에 */ }
    }
}
