using NUnit.Framework;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class LobbyBuilderTests
    {
        [Test]
        public void BuildWithoutNetworkThrows()
        {
            var b = new LobbyBuilder();
            Assert.Throws<System.InvalidOperationException>(() => b.Build());
        }

        [Test]
        public void BuildWithoutTickSourceThrows()
        {
            var b = new LobbyBuilder().UseNetwork(new FakeNetworkFacade());
            var ex = Assert.Throws<System.InvalidOperationException>(() => b.Build());
            Assert.That(ex.Message, Does.Contain("TickSource"));
        }

        [Test]
        public void BuildWithoutCoroutineRunnerThrows()
        {
            var b = new LobbyBuilder()
                .UseNetwork(new FakeNetworkFacade())
                .UseTickSource(new FakeTickSource());
            var ex = Assert.Throws<System.InvalidOperationException>(() => b.Build());
            Assert.That(ex.Message, Does.Contain("CoroutineRunner"));
        }

        [Test]
        public void BuildWithoutIdentityThrows()
        {
            var b = new LobbyBuilder()
                .UseNetwork(new FakeNetworkFacade())
                .UseTickSource(new FakeTickSource())
                .UseCoroutineRunner(new FakeCoroutineRunner());
            var ex = Assert.Throws<System.InvalidOperationException>(() => b.Build());
            Assert.That(ex.Message, Does.Contain("Identity"));
        }

        [Test]
        public void BuildWithoutPayloadSerializerThrows()
        {
            var b = new LobbyBuilder()
                .UseNetwork(new FakeNetworkFacade())
                .UseTickSource(new FakeTickSource())
                .UseCoroutineRunner(new FakeCoroutineRunner())
                .UseIdentity(new Connection.PlayerIdentity(new InMemoryPlayerIdentityStore()));
            var ex = Assert.Throws<System.InvalidOperationException>(() => b.Build());
            Assert.That(ex.Message, Does.Contain("PayloadSerializer"));
        }
    }
}
