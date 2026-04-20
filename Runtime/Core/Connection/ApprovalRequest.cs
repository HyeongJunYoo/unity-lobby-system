namespace Multiplayer.Lobby.Connection
{
    public readonly struct ApprovalRequest
    {
        public ulong ClientId { get; }
        public byte[] Payload { get; }
        /// <summary>
        /// 승인 요청 시점에 이미 접속 완료된 클라이언트 수. 요청자(ClientId)는 포함되지 않는다.
        /// 따라서 MaxPlayers=8, CurrentConnectedCount=8이면 요청자는 거부되어야 한다.
        /// </summary>
        public int CurrentConnectedCount { get; }

        public ApprovalRequest(ulong clientId, byte[] payload, int currentConnectedCount)
        {
            ClientId = clientId;
            Payload = payload;
            CurrentConnectedCount = currentConnectedCount;
        }
    }
}
