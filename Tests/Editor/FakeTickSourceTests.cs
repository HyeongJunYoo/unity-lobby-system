using NUnit.Framework;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeTickSourceTests
    {
        [Test]
        public void TickUpdateFiresOnUpdate()
        {
            var src = new FakeTickSource();
            var count = 0;
            src.OnUpdate += () => count++;

            src.TickUpdate();
            src.TickUpdate();

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void TickLateUpdateFiresOnLateUpdate()
        {
            var src = new FakeTickSource();
            var count = 0;
            src.OnLateUpdate += () => count++;

            src.TickLateUpdate();

            Assert.That(count, Is.EqualTo(1));
        }
    }
}
