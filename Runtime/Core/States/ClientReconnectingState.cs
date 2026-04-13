using System.Collections;
using UnityEngine;
using VContainer;
using Multiplayer.Lobby;
using Multiplayer.Lobby.Infrastructure;

namespace Multiplayer.Lobby.States
{
    /// <summary>
    /// State when a client is attempting to reconnect after losing connection.
    /// Retries up to NbReconnectAttempts times before giving up and returning to Offline.
    /// </summary>
    public class ClientReconnectingState : ClientConnectingState
    {
        [Inject] IPublisher<ReconnectMessage> m_ReconnectMessagePublisher;

        Coroutine m_ReconnectCoroutine;
        int m_NbAttempts;

        const float k_TimeBeforeFirstAttempt = 1f;
        const float k_TimeBetweenAttempts = 5f;

        public new ClientReconnectingState Configure(ConnectionMethodBase connectionMethod)
        {
            m_ConnectionMethod = connectionMethod;
            return this;
        }

        public override void Enter()
        {
            m_NbAttempts = 0;
            StartReconnectCoroutine();
        }

        public override void Exit()
        {
            if (m_ReconnectCoroutine != null)
            {
                m_ConnectionManager.StopCoroutine(m_ReconnectCoroutine);
                m_ReconnectCoroutine = null;
            }
            m_ReconnectMessagePublisher.Publish(new ReconnectMessage(
                m_NbAttempts, m_ConnectionManager.NbReconnectAttempts));
        }

        public override void OnClientConnected(ulong _)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.ClientConnected);
        }

        public override void OnClientDisconnected(ulong _)
        {
            var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
            if (m_NbAttempts < m_ConnectionManager.NbReconnectAttempts)
            {
                if (string.IsNullOrEmpty(disconnectReason))
                {
                    StartReconnectCoroutine();
                }
                else
                {
                    var connectStatus = ParseDisconnectReason(disconnectReason);
                    m_ConnectStatusPublisher.Publish(connectStatus);
                    switch (connectStatus)
                    {
                        case ConnectStatus.UserRequestedDisconnect:
                        case ConnectStatus.HostEndedSession:
                        case ConnectStatus.ServerFull:
                        case ConnectStatus.IncompatibleBuildType:
                            m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
                            break;
                        default:
                            StartReconnectCoroutine();
                            break;
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(disconnectReason))
                {
                    m_ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
                }
                else
                {
                    var connectStatus = ParseDisconnectReason(disconnectReason);
                    m_ConnectStatusPublisher.Publish(connectStatus);
                }

                m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
            }
        }

        void StartReconnectCoroutine()
        {
            if (m_ReconnectCoroutine != null)
            {
                m_ConnectionManager.StopCoroutine(m_ReconnectCoroutine);
            }
            m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
        }

        IEnumerator ReconnectCoroutine()
        {
            if (m_NbAttempts > 0)
            {
                yield return new WaitForSeconds(k_TimeBetweenAttempts);
            }

            Debug.Log("Lost connection to host, trying to reconnect...");
            m_ConnectionManager.NetworkManager.Shutdown();
            yield return new WaitWhile(() => m_ConnectionManager.NetworkManager.ShutdownInProgress);

            Debug.Log($"Reconnecting attempt {m_NbAttempts + 1}/{m_ConnectionManager.NbReconnectAttempts}...");
            m_ReconnectMessagePublisher.Publish(new ReconnectMessage(m_NbAttempts, m_ConnectionManager.NbReconnectAttempts));

            if (m_NbAttempts == 0)
            {
                yield return new WaitForSeconds(k_TimeBeforeFirstAttempt);
            }

            m_NbAttempts++;
            var reconnectingSetupTask = m_ConnectionMethod.SetupClientReconnectionAsync();
            yield return new WaitUntil(() => reconnectingSetupTask.IsCompleted);

            if (!reconnectingSetupTask.IsFaulted && reconnectingSetupTask.Result.success)
            {
                ConnectClientAsync();
            }
            else
            {
                if (!reconnectingSetupTask.Result.shouldTryAgain)
                {
                    m_NbAttempts = m_ConnectionManager.NbReconnectAttempts;
                }
                OnClientDisconnected(0);
            }
        }
    }
}
