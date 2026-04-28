namespace Multiplayer.Lobby.Messaging
{
    /// <summary>
    /// 로비 생애주기 핵심 단계. LobbyConnection의 OnHostStarted/OnClientConnected/OnDisconnected 이벤트로도 노출된다.
    /// </summary>
    public enum LobbyLifecycleMessage
    {
        /// <summary>호스트 시작이 성공해 HostingState로 진입한 직후.</summary>
        HostStarted,
        /// <summary>클라이언트가 호스트와 연결되어 ClientConnectedState로 진입한 직후.</summary>
        ClientConnected,
        /// <summary>Online 계열 상태에서 OfflineState로 복귀한 직후.</summary>
        Disconnected
    }
}
