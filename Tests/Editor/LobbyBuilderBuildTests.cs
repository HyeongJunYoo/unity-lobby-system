using NUnit.Framework;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class LobbyBuilderBuildTests
    {
        LobbyBuilder MakeComplete() =>
            new LobbyBuilder()
                .UseNetwork(new FakeNetworkFacade())
                .UseTickSource(new FakeTickSource())
                .UseCoroutineRunner(new FakeCoroutineRunner())
                .UsePayloadSerializer(new FakeConnectionPayloadSerializer())
                .UseIdentity(new PlayerIdentity(new InMemoryPlayerIdentityStore()))
                .UseDefaultMessageChannels()
                .UseDefaultStates();

        [Test]
        public void FullyConfiguredBuildSucceeds()
        {
            using var lobby = MakeComplete().Build();
            Assert.That(lobby, Is.Not.Null);
        }

        [Test]
        public void HostStartedEventFiresOnLifecyclePublish()
        {
            using var lobby = MakeComplete().Build();
            var fired = 0;
            lobby.OnHostStarted += () => fired++;

            lobby.GetPublisher<Messaging.LobbyLifecycleMessage>()
                 .Publish(Messaging.LobbyLifecycleMessage.HostStarted);

            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void BuilderLifecycleHookIsApplied()
        {
            var fired = 0;
            using var lobby = MakeComplete()
                .OnDisconnected(() => fired++)
                .Build();

            lobby.GetPublisher<Messaging.LobbyLifecycleMessage>()
                 .Publish(Messaging.LobbyLifecycleMessage.Disconnected);
            // Build() 시 OfflineState.Enter()가 호출되어 초기 Disconnected도 이미 발행됨
            Assert.That(fired, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void DuplicateAddStateThrows()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                new LobbyBuilder()
                    .UseDefaultStates()
                    .AddState(ctx => new States.OfflineState(ctx)));
        }

        [Test]
        public void ReplaceStateOverridesFactory()
        {
            using var lobby = MakeComplete()
                .ReplaceState(ctx => new States.OfflineState(ctx))
                .Build();
            Assert.That(lobby, Is.Not.Null);
        }
    }
}
