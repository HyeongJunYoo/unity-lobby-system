using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeApprover : IConnectionApprover
    {
        public ApprovalResult NextResult { get; set; } = ApprovalResult.Allow();
        public int Calls { get; private set; }

        public ApprovalResult Approve(ApprovalRequest request)
        {
            Calls++;
            return NextResult;
        }
    }
}
