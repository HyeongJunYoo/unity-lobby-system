namespace Multiplayer.Lobby.Connection
{
    public enum ConnectStatus
    {
        Undefined,
        Success,
        ServerFull,
        LoggedInAgain,
        UserRequestedDisconnect,
        GenericDisconnect,
        Reconnecting,
        IncompatibleBuildType,
        HostEndedSession,
        StartHostFailed,
        StartClientFailed
    }
}
