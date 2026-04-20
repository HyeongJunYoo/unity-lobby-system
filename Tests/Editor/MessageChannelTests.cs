using NUnit.Framework;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Tests
{
    public class MessageChannelTests
    {
        [Test]
        public void SubscribedHandlerReceivesPublishedMessage()
        {
            var ch = new MessageChannel<int>();
            var received = 0;
            ch.Subscribe(v => received = v);
            ch.Publish(42);
            Assert.That(received, Is.EqualTo(42));
        }

        [Test]
        public void DisposingSubscriptionRemovesHandler()
        {
            var ch = new MessageChannel<int>();
            var count = 0;
            var sub = ch.Subscribe(_ => count++);
            sub.Dispose();
            ch.Publish(1);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void DisposingChannelMarksDisposed()
        {
            var ch = new MessageChannel<int>();
            ch.Dispose();
            Assert.That(ch.IsDisposed, Is.True);
        }

        [Test]
        public void DuplicateSubscribeIsIdempotent()
        {
            var ch = new MessageChannel<int>();
            var count = 0;
            System.Action<int> h = _ => count++;
            ch.Subscribe(h);
            ch.Subscribe(h);
            ch.Publish(1);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void PublishToDisposedChannelThrows()
        {
            var ch = new MessageChannel<int>();
            ch.Dispose();
            Assert.Throws<System.ObjectDisposedException>(() => ch.Publish(0));
        }
    }
}
