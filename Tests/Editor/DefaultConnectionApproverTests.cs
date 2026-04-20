using NUnit.Framework;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests
{
    public class DefaultConnectionApproverTests
    {
        [Test]
        public void AllowsWhenUnderMaxPlayers()
        {
            var a = new DefaultConnectionApprover(maxPlayers: 8);
            var req = new ApprovalRequest(1, new byte[] { 1 }, currentConnectedCount: 3);
            Assert.That(a.Approve(req).Approved, Is.True);
        }

        [Test]
        public void DeniesWhenAtMaxPlayers()
        {
            var a = new DefaultConnectionApprover(maxPlayers: 8);
            var req = new ApprovalRequest(1, new byte[] { 1 }, currentConnectedCount: 8);
            var r = a.Approve(req);
            Assert.That(r.Approved, Is.False);
            Assert.That(r.Reason, Is.EqualTo(ConnectStatus.ServerFull.ToString()));
        }

        [Test]
        public void DeniesWhenPayloadEmpty()
        {
            var a = new DefaultConnectionApprover(maxPlayers: 8);
            var req = new ApprovalRequest(1, new byte[0], currentConnectedCount: 0);
            Assert.That(a.Approve(req).Approved, Is.False);
        }

        [Test]
        public void DeniesWhenPayloadExceedsMax()
        {
            var a = new DefaultConnectionApprover(maxPlayers: 8, maxPayloadBytes: 16);
            var req = new ApprovalRequest(1, new byte[17], currentConnectedCount: 0);
            Assert.That(a.Approve(req).Approved, Is.False);
        }
    }
}
