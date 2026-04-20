using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.States;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class StateTransitionTests
    {
        [Test]
        public void OfflineToStartingHostToHosting()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));

            h.Machine.Start<OfflineState>();

            var method = new TestConnectionMethod(h.Network, h.PayloadSerializer, h.Identity, "P1", false);
            h.Machine.StartHost(method);

            Assert.That(h.Machine.CurrentState, Is.InstanceOf<StartingHostState>());
            Assert.That(h.Network.StartHostCalls, Is.EqualTo(1));

            h.Network.RaiseServerStarted();
            Assert.That(h.Machine.CurrentState, Is.InstanceOf<HostingState>());
        }

        [Test]
        public void HostingApprovalWithValidPayloadRegistersSessionData()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));
            h.Machine.Start<OfflineState>();

            var method = new TestConnectionMethod(h.Network, h.PayloadSerializer, h.Identity, "Host", false);
            h.Machine.StartHost(method);
            h.Network.RaiseServerStarted();

            var payload = new ConnectionPayload { playerId = "pid-42", playerName = "Joiner", isDebug = false };
            var bytes = h.PayloadSerializer.Serialize(payload);

            var result = h.Network.RaiseApprovalCheck(new ApprovalRequest(clientId: 42, payload: bytes, currentConnectedCount: 1));

            Assert.That(result.Approved, Is.True);
            Assert.That(h.SessionManager.GetPlayerData(42UL), Is.Not.Null);
        }

        [Test]
        public void ConnectedToReconnectingOnTransientDisconnect()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));
            h.Machine.Start<OfflineState>();

            // 클라이언트로 연결 -> ClientConnected 진입 시뮬레이션
            var method = new TestConnectionMethod(h.Network, h.PayloadSerializer, h.Identity, "C", false);
            h.Machine.StartClient(method);
            h.Network.RaiseClientConnected(0UL);
            Assert.That(h.Machine.CurrentState, Is.InstanceOf<ClientConnectedState>());

            // 원인 불명 끊김 -> Reconnecting
            h.Network.DisconnectReason = "";
            h.Network.RaiseClientDisconnected(0UL, "");
            Assert.That(h.Machine.CurrentState, Is.InstanceOf<ClientReconnectingState>());
        }

        [Test]
        public void UserRequestedDisconnectInHostingGoesOffline()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));
            h.Machine.Start<OfflineState>();

            var method = new TestConnectionMethod(h.Network, h.PayloadSerializer, h.Identity, "H", false);
            h.Machine.StartHost(method);
            h.Network.RaiseServerStarted();

            h.Machine.RequestShutdown();

            Assert.That(h.Machine.CurrentState, Is.InstanceOf<OfflineState>());
            Assert.That(h.Network.ShutdownCalls, Is.GreaterThanOrEqualTo(1));
        }

        // 테스트 전용 ConnectionMethod — 네트워크 호출 없이 RequiresManualNetworkStart=true로 동작
        sealed class TestConnectionMethod : ConnectionMethodBase
        {
            public TestConnectionMethod(Multiplayer.Lobby.Abstractions.INetworkFacade net,
                                        Multiplayer.Lobby.Abstractions.IConnectionPayloadSerializer ser,
                                        PlayerIdentity id, string name, bool isDebug)
                : base(net, ser, id, name, isDebug) { }

            public override void SetupHostConnection()   => SetConnectionPayload(GetPlayerId(), m_PlayerName);
            public override void SetupClientConnection() => SetConnectionPayload(GetPlayerId(), m_PlayerName);
            public override System.Threading.Tasks.Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
                => System.Threading.Tasks.Task.FromResult((true, true));
        }
    }
}
