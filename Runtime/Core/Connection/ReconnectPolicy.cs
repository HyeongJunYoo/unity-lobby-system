using System;

namespace Multiplayer.Lobby.Connection
{
    public readonly struct ReconnectPolicy
    {
        public int MaxAttempts { get; init; }
        public TimeSpan InitialBackoff { get; init; }
        public TimeSpan MaxBackoff { get; init; }
        public double BackoffMultiplier { get; init; }

        public static ReconnectPolicy Default => new()
        {
            MaxAttempts = 2,
            InitialBackoff = TimeSpan.FromSeconds(1),
            MaxBackoff = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0
        };
    }
}
