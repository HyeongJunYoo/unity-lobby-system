using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class PlayerIdentityTests
    {
        [Test]
        public void PlayerIdCombinesGuidAndProfile()
        {
            var store = new InMemoryPlayerIdentityStore { Profile = "X" };
            var id = new PlayerIdentity(store);
            var pid = id.GetPlayerId();
            Assert.That(pid, Does.EndWith("X"));
        }

        [Test]
        public void ChangingProfileResetsGuidAndRaisesEvent()
        {
            var store = new InMemoryPlayerIdentityStore { Profile = "A" };
            var id = new PlayerIdentity(store);
            var first = id.GetOrCreateGuid();

            var raised = false;
            id.OnProfileChanged += () => raised = true;
            id.Profile = "B";
            var second = id.GetOrCreateGuid();

            Assert.That(raised, Is.True);
            Assert.That(second, Is.Not.EqualTo(first));
        }
    }
}
