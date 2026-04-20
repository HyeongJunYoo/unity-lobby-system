using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Abstractions
{
    public interface IConnectionApprover
    {
        ApprovalResult Approve(ApprovalRequest request);
    }
}
