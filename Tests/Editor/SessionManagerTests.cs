using NUnit.Framework;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class SessionManagerTests
    {
        [Test]
        public void SetupStoresPlayerDataUnderPlayerId()
        {
            var sm = new SessionManager(new FakeLogger());
            var d = new FakeSessionPlayerData(1, "A");
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1", d);
            Assert.That(sm.GetPlayerData(1UL), Is.SameAs(d));
            Assert.That(sm.GetPlayerId(1UL), Is.EqualTo("pid-1"));
        }

        [Test]
        public void DuplicateConnectionIsRejected()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1",
                new FakeSessionPlayerData(1, "A") { IsConnected = true });
            sm.SetupConnectingPlayerSessionData(2UL, "pid-1", new FakeSessionPlayerData(2, "A'"));
            Assert.That(sm.GetPlayerData(2UL), Is.Null);
        }

        [Test]
        public void DisconnectDuringSessionPreservesDataForReconnection()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.OnSessionStarted();
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1", new FakeSessionPlayerData(1, "A"));
            sm.DisconnectClient(1UL);
            var stored = sm.GetPlayerData("pid-1");
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored.IsConnected, Is.False);
        }

        [Test]
        public void ReconnectRestoresPreviousDataByPlayerId()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.OnSessionStarted();
            var original = new FakeSessionPlayerData(1, "A");
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1", original);
            sm.DisconnectClient(1UL);
            sm.SetupConnectingPlayerSessionData(2UL, "pid-1", new FakeSessionPlayerData(2, "IGNORED"));
            var after = sm.GetPlayerData(2UL);
            Assert.That(after, Is.SameAs(original));
            Assert.That(after.ClientID, Is.EqualTo(2UL));
            Assert.That(after.IsConnected, Is.True);
        }

        [Test]
        public void OnServerEndedClearsAll()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1", new FakeSessionPlayerData(1, "A"));
            sm.OnServerEnded();
            Assert.That(sm.GetPlayerId(1UL), Is.Null);
        }
    }
}
