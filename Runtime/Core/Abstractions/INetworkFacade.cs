using System;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Abstractions
{
    public interface INetworkFacade
    {
        bool IsClient { get; }
        bool IsServer { get; }
        bool IsHost { get; }
        bool IsListening { get; }
        bool ShutdownInProgress { get; }
        ulong LocalClientId { get; }

        byte[] ConnectionPayload { get; set; }

        bool StartClient();
        bool StartHost();
        void Shutdown(bool discardMessageQueue = false);
        void DisconnectClient(ulong clientId, string reason = null);
        string GetDisconnectReason(ulong clientId);

        event Action OnServerStarted;
        event Action<bool> OnServerStopped;
        event Action OnTransportFailure;
        event Action<ulong> OnClientConnected;
        event Action<ulong, string> OnClientDisconnected;

        /// <summary>
        /// 단일 핸들러 규약. StateMachine이 정확히 한 개 구독. 어댑터는 마지막 반환값 사용.
        /// </summary>
        event Func<ApprovalRequest, ApprovalResult> ApprovalCheck;
    }
}
