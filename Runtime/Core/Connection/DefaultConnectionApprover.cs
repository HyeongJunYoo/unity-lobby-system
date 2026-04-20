using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Connection
{
    public sealed class DefaultConnectionApprover : IConnectionApprover
    {
        const int k_DefaultMaxPayloadBytes = 1024;

        readonly int m_MaxPlayers;
        readonly int m_MaxPayloadBytes;

        public DefaultConnectionApprover(int maxPlayers, int maxPayloadBytes = k_DefaultMaxPayloadBytes)
        {
            m_MaxPlayers = maxPlayers;
            m_MaxPayloadBytes = maxPayloadBytes;
        }

        public ApprovalResult Approve(ApprovalRequest request)
        {
            if (request.Payload == null || request.Payload.Length == 0)
                return ApprovalResult.Deny(ConnectStatus.GenericDisconnect.ToString());

            if (request.Payload.Length > m_MaxPayloadBytes)
                return ApprovalResult.Deny(ConnectStatus.GenericDisconnect.ToString());

            if (request.CurrentConnectedCount >= m_MaxPlayers)
                return ApprovalResult.Deny(ConnectStatus.ServerFull.ToString());

            return ApprovalResult.Allow();
        }
    }
}
