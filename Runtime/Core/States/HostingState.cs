using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class HostingState : OnlineState
    {
        public HostingState(IStateMachineContext context) : base(context) { }

        public override void Enter()
            => Context.LifecyclePublisher.Publish(LobbyLifecycleMessage.HostStarted);

        public override void Exit() => Context.Sessions.OnServerEnded();

        public override void OnClientConnected(ulong clientId)
        {
            var data = Context.Sessions.GetPlayerData(clientId);
            if (data != null)
            {
                Context.ConnectionEventPublisher.Publish(new ConnectionEventMessage
                {
                    ConnectStatus = ConnectStatus.Success,
                    PlayerName = ""
                });
            }
            else
            {
                Context.Logger.Error($"No player data associated with client {clientId}");
                Context.Network.DisconnectClient(clientId, ConnectStatus.GenericDisconnect.ToString());
            }
        }

        public override void OnClientDisconnected(ulong clientId, string reason)
        {
            if (clientId == Context.Network.LocalClientId) return;
            var pid = Context.Sessions.GetPlayerId(clientId);
            if (pid == null) return;
            var data = Context.Sessions.GetPlayerData(pid);
            if (data != null)
            {
                Context.ConnectionEventPublisher.Publish(new ConnectionEventMessage
                {
                    ConnectStatus = ConnectStatus.GenericDisconnect,
                    PlayerName = ""
                });
            }
            Context.Sessions.DisconnectClient(clientId);
        }

        public override void OnUserRequestedShutdown()
        {
            // 호스트 종료: 연결된 모든 클라이언트 끊기
            Context.Network.Shutdown();
            Context.ChangeState<OfflineState>();
        }

        public override void OnServerStopped()
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
            Context.ChangeState<OfflineState>();
        }

        public override ApprovalResult ApprovalCheck(ApprovalRequest request)
        {
            // 1) DefaultConnectionApprover로 기본 검증 (인원/페이로드)
            var baseResult = Context.Approver.Approve(request);
            if (!baseResult.Approved) return baseResult;

            // 2) 세션 레벨 검증 (중복 로그인)
            var payload = Context.PayloadSerializer.Deserialize(request.Payload);
            if (payload == null)
                return ApprovalResult.Deny(ConnectStatus.GenericDisconnect.ToString());

            if (Context.Sessions.IsDuplicateConnection(payload.playerId))
                return ApprovalResult.Deny(ConnectStatus.LoggedInAgain.ToString());

            // 3) 세션 데이터 생성·등록
            var data = Context.CreatePlayerData?.Invoke(request.ClientId, payload);
            if (data != null)
                Context.Sessions.SetupConnectingPlayerSessionData(request.ClientId, payload.playerId, data);
            else
                Context.Logger.Warning("CreatePlayerData factory not set. Player data not tracked.");

            return ApprovalResult.Allow();
        }
    }
}
