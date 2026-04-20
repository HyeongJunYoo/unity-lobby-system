using NUnit.Framework;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class InMemoryPlayerIdentityStoreTests
    {
        [Test]
        public void SameProfileReturnsSameGuid()
        {
            var s = new InMemoryPlayerIdentityStore();
            Assert.That(s.GetOrCreateGuid("A"), Is.EqualTo(s.GetOrCreateGuid("A")));
        }

        [Test]
        public void DifferentProfilesReturnDifferentGuids()
        {
            var s = new InMemoryPlayerIdentityStore();
            Assert.That(s.GetOrCreateGuid("A"), Is.Not.EqualTo(s.GetOrCreateGuid("B")));
        }

        [Test]
        public void NullProfileTreatedAsEmpty()
        {
            var s = new InMemoryPlayerIdentityStore();
            Assert.That(s.GetOrCreateGuid(null), Is.EqualTo(s.GetOrCreateGuid("")));
        }
    }
}
