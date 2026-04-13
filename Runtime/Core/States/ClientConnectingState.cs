using System;
using UnityEngine;
using Multiplayer.Lobby;

namespace Multiplayer.Lobby.States
{
    /// <summary>
    /// State when a client is attempting to connect to a server.
    /// </summary>
    public class ClientConnectingState : OnlineState
    {
        protected ConnectionMethodBase m_ConnectionMethod;

        public ClientConnectingState Configure(ConnectionMethodBase connectionMethod)
        {
            m_ConnectionMethod = connectionMethod;
            return this;
        }

        public override void Enter()
        {
            ConnectClientAsync();
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong _)
        {
            m_ConnectStatusPublisher.Publish(ConnectStatus.Success);
            m_ConnectionManager.ChangeState(m_ConnectionManager.ClientConnected);
        }

        public override void OnClientDisconnected(ulong _)
        {
            StartingClientFailed();
        }

        void StartingClientFailed()
        {
            var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
            }
            else
            {
                var connectStatus = ParseDisconnectReason(disconnectReason);
                m_ConnectStatusPublisher.Publish(connectStatus);
            }

            m_ConnectionManager.ChangeState(m_ConnectionManager.Offline);
        }

        internal void ConnectClientAsync()
        {
            try
            {
                m_ConnectionMethod.SetupClientConnection();

                if (m_ConnectionMethod.RequiresManualNetworkStart)
                {
                    if (!m_ConnectionManager.NetworkManager.StartClient())
                    {
                        throw new Exception("NetworkManager StartClient failed");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error connecting client, see following exception");
                Debug.LogException(e);
                StartingClientFailed();
                throw;
            }
        }
    }
}
