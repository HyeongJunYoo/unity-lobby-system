using System;

namespace Multiplayer.Lobby.Connection
{
    /// <summary>
    /// 연결 끊김 후 자동 재연결 시도 정책.
    /// </summary>
    public readonly struct ReconnectPolicy
    {
        /// <summary>최대 재시도 횟수. 0이면 재연결을 시도하지 않는다.</summary>
        public int MaxAttempts { get; init; }
        /// <summary>1차 시도 대기 시간. 이후 시도마다 BackoffMultiplier만큼 곱해진다.</summary>
        public TimeSpan InitialBackoff { get; init; }
        /// <summary>대기 시간 상한. 누적 backoff가 이 값을 넘지 못한다.</summary>
        public TimeSpan MaxBackoff { get; init; }
        /// <summary>매 시도 후 대기 시간에 곱하는 배수. 보통 2.0(지수 backoff).</summary>
        public double BackoffMultiplier { get; init; }

        /// <summary>기본값: 시도 2회, 1s → 2s 백오프, 최대 30s.</summary>
        public static ReconnectPolicy Default => new()
        {
            MaxAttempts = 2,
            InitialBackoff = TimeSpan.FromSeconds(1),
            MaxBackoff = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0
        };
    }
}
