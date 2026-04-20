using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        INetworkFacade m_Network;
        ITickSource m_Tick;
        ICoroutineRunner m_Coroutines;
        IConnectionPayloadSerializer m_Serializer;
        PlayerIdentity m_Identity;

        public LobbyBuilder UseNetwork(INetworkFacade network)          { m_Network = network; return this; }
        public LobbyBuilder UseTickSource(ITickSource tick)             { m_Tick = tick; return this; }
        public LobbyBuilder UseCoroutineRunner(ICoroutineRunner runner) { m_Coroutines = runner; return this; }
        public LobbyBuilder UsePayloadSerializer(IConnectionPayloadSerializer s) { m_Serializer = s; return this; }
        public LobbyBuilder UseIdentity(PlayerIdentity identity)        { m_Identity = identity; return this; }

        public LobbyConnection Build()
        {
            if (m_Network     == null) throw new InvalidOperationException("LobbyBuilder.UseNetwork(...)이 호출되지 않았습니다.");
            if (m_Tick        == null) throw new InvalidOperationException("LobbyBuilder.UseTickSource(...)이 호출되지 않았습니다. TickSource");
            if (m_Coroutines  == null) throw new InvalidOperationException("LobbyBuilder.UseCoroutineRunner(...)이 호출되지 않았습니다. CoroutineRunner");
            if (m_Identity    == null) throw new InvalidOperationException("LobbyBuilder.UseIdentity(...)이 호출되지 않았습니다. Identity");
            if (m_Serializer  == null) throw new InvalidOperationException("LobbyBuilder.UsePayloadSerializer(...)이 호출되지 않았습니다. PayloadSerializer");

            // 실제 조립은 Task 25~26에서 확장. 지금은 Null 구현으로 최소 빌드만 가능.
            throw new InvalidOperationException(
                "LobbyBuilder.Build()는 Task 25~26 완료 후 활성화됩니다. 현재는 검증만 수행.");
        }
    }
}
