using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.ConnectionMethods.IP;

namespace Multiplayer.Lobby.Sample.VContainer
{
    /// <summary>
    /// UI Toolkit 기반 VContainer 통합 샘플 UI. 같은 GameObject의 UIDocument를 사용해
    /// 프로그래밍 방식으로 UI 트리를 구성한다. UXML/USS 자산 불필요.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class VContainerLobbyUI : MonoBehaviour
    {
        UIDocument m_Doc;

        TextField m_NameField;
        TextField m_IpField;
        TextField m_PortField;
        Button m_HostBtn;
        Button m_ClientBtn;
        Button m_ShutdownBtn;
        Label m_StatusLabel;

        [Inject] LobbyConnection m_Lobby;
        [Inject] NetworkManager m_Nm;
        [Inject] PlayerIdentity m_Identity;
        [Inject] IConnectionPayloadSerializer m_Serializer;

        void Start()
        {
            m_Doc = GetComponent<UIDocument>();
            BuildUI(m_Doc.rootVisualElement);

            m_HostBtn.clicked += () =>
                m_Lobby.StartHostIp(m_Nm, m_Identity, m_Serializer,
                    m_NameField.value, m_IpField.value, int.Parse(m_PortField.value), Debug.isDebugBuild);
            m_ClientBtn.clicked += () =>
                m_Lobby.StartClientIp(m_Nm, m_Identity, m_Serializer,
                    m_NameField.value, m_IpField.value, int.Parse(m_PortField.value), Debug.isDebugBuild);
            m_ShutdownBtn.clicked += m_Lobby.RequestShutdown;

            m_Lobby.OnHostStarted     += () => SetStatus("Host started");
            m_Lobby.OnClientConnected += () => SetStatus("Client connected");
            m_Lobby.OnDisconnected    += () => SetStatus("Disconnected");
        }

        void BuildUI(VisualElement root)
        {
            root.Clear();
            root.style.flexGrow = 1;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            var panel = new VisualElement { name = "panel" };
            panel.style.width = 340;
            panel.style.paddingTop = 20;
            panel.style.paddingBottom = 20;
            panel.style.paddingLeft = 20;
            panel.style.paddingRight = 20;
            panel.style.backgroundColor = new Color(0.92f, 0.92f, 0.92f, 0.95f);
            panel.style.borderTopLeftRadius = 8;
            panel.style.borderTopRightRadius = 8;
            panel.style.borderBottomLeftRadius = 8;
            panel.style.borderBottomRightRadius = 8;
            root.Add(panel);

            var title = new Label("Multiplayer Lobby (VContainer)");
            title.style.fontSize = 18;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 12;
            panel.Add(title);

            m_NameField = new TextField("Name") { value = "" };
            m_NameField.style.marginBottom = 6;
            panel.Add(m_NameField);

            m_IpField = new TextField("IP") { value = "127.0.0.1" };
            m_IpField.style.marginBottom = 6;
            panel.Add(m_IpField);

            m_PortField = new TextField("Port") { value = "7777" };
            m_PortField.style.marginBottom = 12;
            panel.Add(m_PortField);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.SpaceBetween;
            buttonRow.style.marginBottom = 12;
            panel.Add(buttonRow);

            m_HostBtn = new Button { text = "Host" };
            m_HostBtn.style.flexGrow = 1;
            m_HostBtn.style.marginRight = 4;
            buttonRow.Add(m_HostBtn);

            m_ClientBtn = new Button { text = "Client" };
            m_ClientBtn.style.flexGrow = 1;
            m_ClientBtn.style.marginLeft = 2;
            m_ClientBtn.style.marginRight = 2;
            buttonRow.Add(m_ClientBtn);

            m_ShutdownBtn = new Button { text = "Shutdown" };
            m_ShutdownBtn.style.flexGrow = 1;
            m_ShutdownBtn.style.marginLeft = 4;
            buttonRow.Add(m_ShutdownBtn);

            m_StatusLabel = new Label("Status: Idle");
            m_StatusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(m_StatusLabel);
        }

        void SetStatus(string s)
        {
            if (m_StatusLabel != null) m_StatusLabel.text = s;
        }
    }
}
