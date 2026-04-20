using NUnit.Framework;
using Multiplayer.Lobby.States;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class OfflineStateTests
    {
        [Test]
        public void EnterShutsDownNetworkAndPublishesDisconnected()
        {
            var h = StateHarness.Build(typeof(OfflineState));
            h.Machine.Start<OfflineState>();
            Assert.That(h.Network.ShutdownCalls, Is.GreaterThanOrEqualTo(1));

            // Lifecycle Disconnected 발행 확인
            var received = 0;
            (h.LifecyclePublisher as Messaging.IMessageChannel<Messaging.LobbyLifecycleMessage>)
                .Subscribe(m => { if (m == Messaging.LobbyLifecycleMessage.Disconnected) received++; });

            // 재진입
            h.Machine.ChangeState<OfflineState>();
            Assert.That(received, Is.GreaterThanOrEqualTo(1));
        }
    }
}
