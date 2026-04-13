namespace Multiplayer.Lobby.Session
{
    /// <summary>
    /// Interface for game-specific player session data.
    /// Consuming projects implement this to store whatever player data their game needs.
    /// </summary>
    public interface ISessionPlayerData
    {
        bool IsConnected { get; set; }
        ulong ClientID { get; set; }
        void Reinitialize();
    }
}
