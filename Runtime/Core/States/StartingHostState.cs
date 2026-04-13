using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Multiplayer.Lobby;
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.States
{
    /// <summary>
    /// State when the host is starting up. If successful, transitions to Hosting.
    /// </summary>
    public class StartingHostState : OnlineState
    {
        [Inject] SessionManager m_SessionManager;

        ConnectionMethodBase m_ConnectionMethod;

        public StartingHostState Configure(ConnectionMethodBase connectionMethod)
        {
            m_ConnectionMethod = connectionMethod;
            return this;
        }

        public override void Enter()
        {
            StartHost();
        }

        public override void Exit() { }

        public override void OnServerStarted()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.Success);
            m_ConnectionManager.ChangeState(m_ConnectionManager.Hosting);
        }

        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;

            // Host approving itself during StartHost
            if (clientId == m_ConnectionManager.NetworkManager.LocalClientId)
            {
                var payload = System.Text.Encoding.UTF8.GetString(connectionData);
                var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

                var playerData = m_ConnectionManager.CreatePlayerData?.Invoke(clientId, connectionPayload);
                if (playerData != null)
                {
                    m_SessionManager.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId, playerData);
                }
                else
                {
                    Debug.LogWarning("CreatePlayerData factory not set. Host player data will not be tracked.");
                }

                response.Approved = true;
                response.CreatePlayerObject = true;
            }
        }

        public override void OnServerStopped()
        {
            StartHostFailed();
        }

        void StartHost()
        {
            try
            {
                m_ConnectionMethod.SetupHostConnection();

                if (m_ConnectionMethod.RequiresManualNetworkStart)
                {
                    if (!m_ConnectionManager.NetworkManager.StartHost())
                    {
                        StartHostFailed();
                    }
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
            m_ConnectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
            m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
        }
    }
}
