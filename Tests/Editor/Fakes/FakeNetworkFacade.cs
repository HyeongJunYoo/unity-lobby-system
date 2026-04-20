using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeNetworkFacade : INetworkFacade
    {
        public bool IsClient { get; set; }
        public bool IsServer { get; set; }
        public bool IsHost { get; set; }
        public bool IsListening { get; set; }
        public bool ShutdownInProgress { get; set; }
        public ulong LocalClientId { get; set; } = 0UL;
        public byte[] ConnectionPayload { get; set; }
        public string DisconnectReason { get; set; } = string.Empty;

        public int StartClientCalls { get; private set; }
        public int StartHostCalls { get; private set; }
        public int ShutdownCalls { get; private set; }
        public List<(ulong id, string reason)> Disconnects { get; } = new();

        public bool StartClientReturnValue { get; set; } = true;
        public bool StartHostReturnValue { get; set; } = true;

        public bool StartClient() { StartClientCalls++; return StartClientReturnValue; }
        public bool StartHost()   { StartHostCalls++;   return StartHostReturnValue; }

        public void Shutdown(bool discardMessageQueue = false)
        {
            ShutdownCalls++;
            ShutdownInProgress = false;
        }

        public void DisconnectClient(ulong clientId, string reason = null) => Disconnects.Add((clientId, reason));
        public string GetDisconnectReason(ulong clientId) => DisconnectReason;

        public event Action OnServerStarted;
        public event Action<bool> OnServerStopped;
        public event Action OnTransportFailure;
        public event Action<ulong> OnClientConnected;
        public event Action<ulong, string> OnClientDisconnected;
        public event Func<ApprovalRequest, ApprovalResult> ApprovalCheck;

        public void RaiseServerStarted()                                => OnServerStarted?.Invoke();
        public void RaiseServerStopped(bool isHost)                     => OnServerStopped?.Invoke(isHost);
        public void RaiseTransportFailure()                             => OnTransportFailure?.Invoke();
        public void RaiseClientConnected(ulong id)                      => OnClientConnected?.Invoke(id);
        public void RaiseClientDisconnected(ulong id, string reason)    => OnClientDisconnected?.Invoke(id, reason);
        public ApprovalResult RaiseApprovalCheck(ApprovalRequest req)   => ApprovalCheck?.Invoke(req) ?? ApprovalResult.Allow();
    }
}
