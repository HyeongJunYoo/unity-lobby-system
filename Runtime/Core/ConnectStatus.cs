using System;

namespace Multiplayer.Lobby
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

    public struct ReconnectMessage
    {
        public int CurrentAttempt;
        public int MaxAttempt;

        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }

    public struct ConnectionEventMessage
    {
        public ConnectStatus ConnectStatus;
        public string PlayerName;
    }
}
