using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.ConnectionMethods.IP;

namespace Multiplayer.Lobby.Sample.VContainer
{
    public sealed class VContainerLobbyUI : MonoBehaviour
    {
        [SerializeField] InputField m_NameField;
        [SerializeField] InputField m_IpField;
        [SerializeField] InputField m_PortField;
        [SerializeField] Button m_HostBtn;
        [SerializeField] Button m_ClientBtn;
        [SerializeField] Button m_ShutdownBtn;
        [SerializeField] Text m_StatusText;

        [Inject] LobbyConnection m_Lobby;
        [Inject] NetworkManager m_Nm;
        [Inject] PlayerIdentity m_Identity;
        [Inject] IConnectionPayloadSerializer m_Serializer;

        void Start()
        {
            m_HostBtn.onClick.AddListener(() =>
                m_Lobby.StartHostIp(m_Nm, m_Identity, m_Serializer,
                    m_NameField.text, m_IpField.text, int.Parse(m_PortField.text), Debug.isDebugBuild));
            m_ClientBtn.onClick.AddListener(() =>
                m_Lobby.StartClientIp(m_Nm, m_Identity, m_Serializer,
                    m_NameField.text, m_IpField.text, int.Parse(m_PortField.text), Debug.isDebugBuild));
            m_ShutdownBtn.onClick.AddListener(m_Lobby.RequestShutdown);

            m_Lobby.OnHostStarted     += () => m_StatusText.text = "Host started";
            m_Lobby.OnClientConnected += () => m_StatusText.text = "Client connected";
            m_Lobby.OnDisconnected    += () => m_StatusText.text = "Disconnected";
        }
    }
}
