using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeNetworkFacadeTests
    {
        [Test]
        public void StartClientIncrementsCallCount()
        {
            var f = new FakeNetworkFacade();
            Assert.That(f.StartClient(), Is.True);
            Assert.That(f.StartClientCalls, Is.EqualTo(1));
        }

        [Test]
        public void RaiseApprovalCheckReturnsSubscribedResult()
        {
            var f = new FakeNetworkFacade();
            f.ApprovalCheck += _ => ApprovalResult.Deny("nope");
            var r = f.RaiseApprovalCheck(new ApprovalRequest(1, new byte[0], 0));
            Assert.That(r.Approved, Is.False);
            Assert.That(r.Reason, Is.EqualTo("nope"));
        }

        [Test]
        public void DisconnectClientRecordsCall()
        {
            var f = new FakeNetworkFacade();
            f.DisconnectClient(42, "bye");
            Assert.That(f.Disconnects[0].id, Is.EqualTo(42UL));
            Assert.That(f.Disconnects[0].reason, Is.EqualTo("bye"));
        }
    }
}
