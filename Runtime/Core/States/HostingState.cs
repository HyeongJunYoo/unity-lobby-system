using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Multiplayer.Lobby;
using Multiplayer.Lobby.Infrastructure;
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.States
{
    /// <summary>
    /// State when the host is actively hosting a session.
    /// Handles incoming client connections and approval checks.
    /// </summary>
    public class HostingState : OnlineState
    {
        [Inject] IPublisher<ConnectionEventMessage> m_ConnectionEventPublisher;
        [Inject] SessionManager m_SessionManager;

        const int k_MaxConnectPayload = 1024;

        public override void Enter()
        {
            m_ConnectionManager.InvokeOnHostStarted();
        }

        public override void Exit()
        {
            m_SessionManager.OnServerEnded();
        }

        public override void OnClientConnected(ulong clientId)
        {
            var playerData = m_SessionManager.GetPlayerData(clientId);
            if (playerData != null)
            {
                m_ConnectionEventPublisher.Publish(new ConnectionEventMessage
                {
                    ConnectStatus = ConnectStatus.Success,
                    PlayerName = "" // Consumer can extend to include name
                });
            }
            else
            {
                Debug.LogError($"No player data associated with client {clientId}");
                var reason = ConnectStatus.GenericDisconnect.ToString();
                m_ConnectionManager.NetworkManager.DisconnectClient(clientId, reason);
            }
        }

        public override void OnClientDisconnected(ulong clientId)
        {
            if (clientId != m_ConnectionManager.NetworkManager.LocalClientId)
            {
                var playerId = m_SessionManager.GetPlayerId(clientId);
                if (playerId != null)
                {
                    var sessionData = m_SessionManager.GetPlayerData(playerId);
                    if (sessionData != null)
                    {
                        m_ConnectionEventPublisher.Publish(new ConnectionEventMessage
                        {
                            ConnectStatus = ConnectStatus.GenericDisconnect,
                            PlayerName = ""
                        });
                    }
                    m_SessionManager.DisconnectClient(clientId);
                }
            }
        }

        public override void OnUserRequestedShutdown()
        {
            var reason = ConnectStatus.HostEndedSession.ToString();
            for (var i = m_ConnectionManager.NetworkManager.ConnectedClientsIds.Count - 1; i >= 0; i--)
            {
                var id = m_ConnectionManager.NetworkManager.ConnectedClientsIds[i];
                if (id != m_ConnectionManager.NetworkManager.LocalClientId)
                {
                    m_ConnectionManager.NetworkManager.DisconnectClient(id, reason);
                }
            }
            m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
        }

        public override void OnServerStopped()
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
            m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
        }

        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;

            if (connectionData.Length > k_MaxConnectPayload)
            {
                response.Approved = false;
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);
            var gameReturnStatus = GetConnectStatus(connectionPayload);

            if (gameReturnStatus == ConnectStatus.Success)
            {
                var playerData = m_ConnectionManager.CreatePlayerData?.Invoke(clientId, connectionPayload);
                if (playerData != null)
                {
                    m_SessionManager.SetupConnectingPlayerSessionData(clientId, connectionPayload.playerId, playerData);
                }
                else
                {
                    Debug.LogWarning("CreatePlayerData factory not set. Player data will not be tracked.");
                }

                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Position = UnityEngine.Vector3.zero;
                response.Rotation = UnityEngine.Quaternion.identity;
                return;
            }

            response.Approved = false;
            response.Reason = gameReturnStatus.ToString();
        }

        ConnectStatus GetConnectStatus(ConnectionPayload connectionPayload)
        {
            if (m_ConnectionManager.NetworkManager.ConnectedClientsIds.Count >= m_ConnectionManager.MaxConnectedPlayers)
            {
                return ConnectStatus.ServerFull;
            }

            if (connectionPayload.isDebug != Debug.isDebugBuild)
            {
                return ConnectStatus.IncompatibleBuildType;
            }

            return m_SessionManager.IsDuplicateConnection(connectionPayload.playerId)
                ? ConnectStatus.LoggedInAgain
                : ConnectStatus.Success;
        }
    }
}
