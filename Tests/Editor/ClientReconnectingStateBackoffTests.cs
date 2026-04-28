using System;
using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.States;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class ClientReconnectingStateBackoffTests
    {
        [Test]
        public void Backoff_FollowsPolicy_FirstAttemptUsesInitial_SecondUsesMultiplied()
        {
            var policy = new ReconnectPolicy
            {
                MaxAttempts       = 5,
                InitialBackoff    = TimeSpan.FromSeconds(1),
                MaxBackoff        = TimeSpan.FromSeconds(60),
                BackoffMultiplier = 2.0
            };
            var h = StateHarness.Build(policy,
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));

            h.Machine.Start<OfflineState>();
            var method = new TestConnectionMethod(
                h.Network, h.PayloadSerializer, h.Identity, "C", false);
            h.Machine.StartClient(method);
            h.Network.RaiseClientConnected(0UL);
            h.Network.DisconnectReason = "";
            h.Network.RaiseClientDisconnected(0UL, "");

            Assert.That(h.Machine.CurrentState, Is.InstanceOf<ClientReconnectingState>());

            var first = h.CoroutineRunner.PumpToNextYield();
            Assert.That(first, Is.EqualTo(1.0).Within(0.001),
                "1차 시도는 InitialBackoff(1s)만큼 대기해야 한다");

            h.CoroutineRunner.RunRoutineToCompletion();
            h.Network.RaiseClientDisconnected(0UL, "");
            var second = h.CoroutineRunner.PumpToNextYield();
            Assert.That(second, Is.EqualTo(2.0).Within(0.001),
                "2차 시도는 InitialBackoff * BackoffMultiplier(2s)만큼 대기해야 한다");
        }

        sealed class TestConnectionMethod : ConnectionMethodBase
        {
            public TestConnectionMethod(Multiplayer.Lobby.Abstractions.INetworkFacade net,
                                        Multiplayer.Lobby.Abstractions.IConnectionPayloadSerializer ser,
                                        PlayerIdentity id, string name, bool isDebug)
                : base(net, ser, id, name, isDebug) { }
            public override void SetupHostConnection()   => SetConnectionPayload(GetPlayerId(), m_PlayerName);
            public override void SetupClientConnection() => SetConnectionPayload(GetPlayerId(), m_PlayerName);
            public override System.Threading.Tasks.Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
                => System.Threading.Tasks.Task.FromResult((false, true));
        }
    }
}
