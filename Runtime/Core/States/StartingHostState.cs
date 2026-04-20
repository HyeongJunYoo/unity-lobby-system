using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class StartingHostState : OnlineState
    {
        ConnectionMethodBase m_ConnectionMethod;

        public StartingHostState(IStateMachineContext context) : base(context) { }

        public StartingHostState Configure(ConnectionMethodBase method)
        {
            m_ConnectionMethod = method;
            return this;
        }

        public override void Enter() => StartHostInternal();

        public override void OnServerStarted()
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.Success);
            Context.ChangeState<HostingState>();
        }

        public override ApprovalResult ApprovalCheck(ApprovalRequest request)
        {
            // 호스트 자기 자신 승인: 세션 데이터 초기화 포함
            if (request.ClientId == Context.Network.LocalClientId)
            {
                var payload = Context.PayloadSerializer.Deserialize(request.Payload);
                if (payload != null)
                {
                    var data = Context.CreatePlayerData?.Invoke(request.ClientId, payload);
                    if (data != null)
                        Context.Sessions.SetupConnectingPlayerSessionData(request.ClientId, payload.playerId, data);
                    else
                        Context.Logger.Warning("CreatePlayerData factory not set. Host player data not tracked.");
                }
                return ApprovalResult.Allow();
            }
            return base.ApprovalCheck(request);
        }

        public override void OnServerStopped() => StartHostFailed();

        void StartHostInternal()
        {
            try
            {
                m_ConnectionMethod.SetupHostConnection();
                if (m_ConnectionMethod.RequiresManualNetworkStart)
                {
                    if (!Context.Network.StartHost()) StartHostFailed();
                }
            }
            catch (Exception)
            {
                StartHostFailed();
                throw;
            }
        }

        void StartHostFailed()
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
            Context.ChangeState<OfflineState>();
        }
    }
}
