using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeConnectionPayloadSerializerTests
    {
        [Test]
        public void RoundTripsPayload()
        {
            var ser = new FakeConnectionPayloadSerializer();
            var payload = new ConnectionPayload { playerId = "p1", playerName = "Alice", isDebug = true };

            var bytes = ser.Serialize(payload);
            var restored = ser.Deserialize(bytes);

            Assert.That(restored.playerId, Is.EqualTo("p1"));
            Assert.That(restored.playerName, Is.EqualTo("Alice"));
            Assert.That(restored.isDebug, Is.True);
        }

        [Test]
        public void DeserializeUnknownBytesReturnsNull()
        {
            var ser = new FakeConnectionPayloadSerializer();
            var restored = ser.Deserialize(new byte[] { 99, 0, 0, 0 });
            Assert.That(restored, Is.Null);
        }
    }
}
