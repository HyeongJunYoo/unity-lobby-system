using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.ConnectionMethods.IP;

namespace Multiplayer.Lobby.Sample.BasicManual
{
    public sealed class BasicLobbyUI : MonoBehaviour
    {
        [SerializeField] InputField m_PlayerNameField;
        [SerializeField] InputField m_IpField;
        [SerializeField] InputField m_PortField;
        [SerializeField] Button m_HostButton;
        [SerializeField] Button m_ClientButton;
        [SerializeField] Button m_ShutdownButton;
        [SerializeField] Text m_StatusText;

        LobbyConnection m_Lobby;
        NetworkManager m_Nm;
        PlayerIdentity m_Identity;
        IConnectionPayloadSerializer m_Serializer;

        public void Bind(LobbyConnection lobby, NetworkManager nm, PlayerIdentity identity, IConnectionPayloadSerializer serializer)
        {
            m_Lobby = lobby; m_Nm = nm; m_Identity = identity; m_Serializer = serializer;

            m_HostButton.onClick.AddListener(OnHost);
            m_ClientButton.onClick.AddListener(OnClient);
            m_ShutdownButton.onClick.AddListener(OnShutdown);

            m_Lobby.OnHostStarted     += () => SetStatus("Host started");
            m_Lobby.OnClientConnected += () => SetStatus("Client connected");
            m_Lobby.OnDisconnected    += () => SetStatus("Disconnected");

            m_Lobby.GetSubscriber<ConnectStatus>()
                   .Subscribe(s => SetStatus($"Status: {s}"));
        }

        void OnHost()
            => m_Lobby.StartHostIp(m_Nm, m_Identity, m_Serializer,
                m_PlayerNameField.text, m_IpField.text, int.Parse(m_PortField.text),
                Debug.isDebugBuild);

        void OnClient()
            => m_Lobby.StartClientIp(m_Nm, m_Identity, m_Serializer,
                m_PlayerNameField.text, m_IpField.text, int.Parse(m_PortField.text),
                Debug.isDebugBuild);

        void OnShutdown() => m_Lobby.RequestShutdown();

        void SetStatus(string s) { if (m_StatusText) m_StatusText.text = s; }
    }
}
