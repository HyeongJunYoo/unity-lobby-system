using System.Collections;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class ClientReconnectingState : ClientConnectingState
    {
        object m_ReconnectHandle;
        int m_NbAttempts;
        double m_NextBackoffSeconds;

        public ClientReconnectingState(IStateMachineContext context) : base(context) { }

        public new ClientReconnectingState Configure(ConnectionMethodBase method)
        {
            m_ConnectionMethod = method;
            return this;
        }

        public override void Enter()
        {
            m_NbAttempts = 0;
            m_NextBackoffSeconds = Context.ReconnectPolicy.InitialBackoff.TotalSeconds;
            StartReconnect();
        }

        public override void Exit()
        {
            if (m_ReconnectHandle != null)
            {
                Context.CoroutineRunner.Stop(m_ReconnectHandle);
                m_ReconnectHandle = null;
            }
            Context.ReconnectPublisher.Publish(
                new ReconnectMessage(m_NbAttempts, Context.ReconnectPolicy.MaxAttempts));
        }

        public override void OnClientConnected(ulong _)
            => Context.ChangeState<ClientConnectedState>();

        public override void OnClientDisconnected(ulong _, string reason)
        {
            var actualReason = reason ?? Context.Network.GetDisconnectReason(Context.Network.LocalClientId);
            if (m_NbAttempts < Context.ReconnectPolicy.MaxAttempts)
            {
                if (string.IsNullOrEmpty(actualReason))
                {
                    StartReconnect();
                    return;
                }
                var status = ParseDisconnectReason(actualReason, Context.Logger);
                Context.ConnectStatusPublisher.Publish(status);
                switch (status)
                {
                    case ConnectStatus.UserRequestedDisconnect:
                    case ConnectStatus.HostEndedSession:
                    case ConnectStatus.ServerFull:
                    case ConnectStatus.IncompatibleBuildType:
                        Context.ChangeState<OfflineState>();
                        break;
                    default:
                        StartReconnect();
                        break;
                }
            }
            else
            {
                var status = string.IsNullOrEmpty(actualReason)
                    ? ConnectStatus.GenericDisconnect
                    : ParseDisconnectReason(actualReason, Context.Logger);
                Context.ConnectStatusPublisher.Publish(status);
                Context.ChangeState<OfflineState>();
            }
        }

        void StartReconnect()
        {
            if (m_ReconnectHandle != null)
                Context.CoroutineRunner.Stop(m_ReconnectHandle);
            m_ReconnectHandle = Context.CoroutineRunner.Start(ReconnectRoutine());
        }

        IEnumerator ReconnectRoutine()
        {
            if (m_NbAttempts > 0)
            {
                var backoff = System.Math.Min(m_NextBackoffSeconds, Context.ReconnectPolicy.MaxBackoff.TotalSeconds);
                yield return backoff;   // Adapter가 WaitForSeconds로 해석 (Task 27 참고)
                m_NextBackoffSeconds *= Context.ReconnectPolicy.BackoffMultiplier;
            }

            Context.Logger.Info("Lost connection to host, trying to reconnect...");
            Context.Network.Shutdown();
            while (Context.Network.ShutdownInProgress) yield return null;

            Context.Logger.Info($"Reconnecting attempt {m_NbAttempts + 1}/{Context.ReconnectPolicy.MaxAttempts}...");
            Context.ReconnectPublisher.Publish(
                new ReconnectMessage(m_NbAttempts, Context.ReconnectPolicy.MaxAttempts));

            if (m_NbAttempts == 0)
                yield return Context.ReconnectPolicy.InitialBackoff.TotalSeconds;

            m_NbAttempts++;
            var setupTask = m_ConnectionMethod.SetupClientReconnectionAsync();
            while (!setupTask.IsCompleted) yield return null;

            if (!setupTask.IsFaulted && setupTask.Result.success)
            {
                ConnectClient();
            }
            else
            {
                if (!setupTask.Result.shouldTryAgain)
                    m_NbAttempts = Context.ReconnectPolicy.MaxAttempts;
                OnClientDisconnected(0UL, null);
            }
        }
    }
}
