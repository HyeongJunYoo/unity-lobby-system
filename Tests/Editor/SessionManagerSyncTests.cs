using NUnit.Framework;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class SessionManagerSyncTests
    {
        [Test]
        public void DisconnectBeforeSession_RemovesBothMappings()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(7UL, "pid-1", new FakeSessionPlayerData(7UL, "Alice"));

            sm.DisconnectClient(7UL);

            Assert.That(sm.GetPlayerId(7UL), Is.Null);
            Assert.That(sm.GetPlayerData("pid-1"), Is.Null);
        }

        [Test]
        public void DisconnectAfterSessionStart_KeepsDataMarkedDisconnected()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(7UL, "pid-1", new FakeSessionPlayerData(7UL, "Alice"));
            sm.OnSessionStarted();

            sm.DisconnectClient(7UL);

            var data = sm.GetPlayerData("pid-1");
            Assert.That(data, Is.Not.Null, "세션 시작 후 끊김은 데이터를 유지해야 한다");
            Assert.That(data.IsConnected, Is.False);
        }

        [Test]
        public void OnServerEnded_ClearsBothMappings()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(7UL, "pid-1", new FakeSessionPlayerData(7UL, "Alice"));
            sm.SetupConnectingPlayerSessionData(8UL, "pid-2", new FakeSessionPlayerData(8UL, "Bob"));

            sm.OnServerEnded();

            Assert.That(sm.GetPlayerId(7UL), Is.Null);
            Assert.That(sm.GetPlayerId(8UL), Is.Null);
            Assert.That(sm.GetPlayerData("pid-1"), Is.Null);
            Assert.That(sm.GetPlayerData("pid-2"), Is.Null);
        }
    }
}
