using NUnit.Framework;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests.Adapters
{
    public class JsonUtilityConnectionPayloadSerializerTests
    {
        [Test]
        public void Serialize_Then_Deserialize_RoundTripsAllFields()
        {
            var sut = new JsonUtilityConnectionPayloadSerializer();
            var src = new ConnectionPayload
            {
                playerId   = "pid-42",
                playerName = "Tester",
                isDebug    = true
            };

            var bytes = sut.Serialize(src);
            var dst   = sut.Deserialize(bytes);

            Assert.That(dst, Is.Not.Null);
            Assert.That(dst.playerId,   Is.EqualTo(src.playerId));
            Assert.That(dst.playerName, Is.EqualTo(src.playerName));
            Assert.That(dst.isDebug,    Is.EqualTo(src.isDebug));
        }

        [Test]
        public void Deserialize_NullOrEmpty_ReturnsNull()
        {
            var sut = new JsonUtilityConnectionPayloadSerializer();
            Assert.That(sut.Deserialize(null), Is.Null);
            Assert.That(sut.Deserialize(System.Array.Empty<byte>()), Is.Null);
        }
    }
}
