using System.Collections.Generic;
using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.States;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class LobbyLifecyclePublishTests
    {
        [Test]
        public void HostStartFlow_PublishesHostStartedLifecycle()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));
            h.Machine.Start<OfflineState>();

            var lifecycle = new List<LobbyLifecycleMessage>();
            using var sub = h.LifecycleChannelPublic.Subscribe(m => lifecycle.Add(m));

            var method = new TestConnectionMethod(
                h.Network, h.PayloadSerializer, h.Identity, "Host", false);
            h.Machine.StartHost(method);
            h.Network.RaiseServerStarted();

            CollectionAssert.Contains(lifecycle, LobbyLifecycleMessage.HostStarted,
                "호스트 시작 성공 시 LobbyLifecycleMessage.HostStarted 발행");
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
                => System.Threading.Tasks.Task.FromResult((true, true));
        }
    }
}
