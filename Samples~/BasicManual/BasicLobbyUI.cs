using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.ConnectionMethods.IP;

namespace Multiplayer.Lobby.Sample.BasicManual
{
    /// <summary>
    /// UI Toolkit 기반 로비 UI. 같은 GameObject에 UIDocument 컴포넌트(PanelSettings 할당 필수)만 있으면
    /// UI 트리를 프로그래밍 방식으로 구성한다. UXML/USS 자산은 불필요.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class BasicLobbyUI : MonoBehaviour
    {
        UIDocument m_Doc;

        TextField m_PlayerNameField;
        TextField m_IpField;
        TextField m_PortField;
        Button m_HostButton;
        Button m_ClientButton;
        Button m_ShutdownButton;
        Label m_StatusLabel;

        LobbyConnection m_Lobby;
        NetworkManager m_Nm;
        PlayerIdentity m_Identity;
        IConnectionPayloadSerializer m_Serializer;

        public void Bind(LobbyConnection lobby, NetworkManager nm, PlayerIdentity identity, IConnectionPayloadSerializer serializer)
        {
            m_Lobby = lobby; m_Nm = nm; m_Identity = identity; m_Serializer = serializer;

            m_Doc = GetComponent<UIDocument>();
            BuildUI(m_Doc.rootVisualElement);

            m_HostButton.clicked += OnHost;
            m_ClientButton.clicked += OnClient;
            m_ShutdownButton.clicked += OnShutdown;

            m_Lobby.OnHostStarted     += () => SetStatus("Host started");
            m_Lobby.OnClientConnected += () => SetStatus("Client connected");
            m_Lobby.OnDisconnected    += () => SetStatus("Disconnected");

            m_Lobby.GetSubscriber<ConnectStatus>()
                   .Subscribe(s => SetStatus($"Status: {s}"));
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

            var title = new Label("Multiplayer Lobby (Basic Manual)");
            title.style.fontSize = 18;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 12;
            panel.Add(title);

            m_PlayerNameField = new TextField("Name") { value = "" };
            m_PlayerNameField.style.marginBottom = 6;
            panel.Add(m_PlayerNameField);

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

            m_HostButton = new Button { text = "Host" };
            m_HostButton.style.flexGrow = 1;
            m_HostButton.style.marginRight = 4;
            buttonRow.Add(m_HostButton);

            m_ClientButton = new Button { text = "Client" };
            m_ClientButton.style.flexGrow = 1;
            m_ClientButton.style.marginLeft = 2;
            m_ClientButton.style.marginRight = 2;
            buttonRow.Add(m_ClientButton);

            m_ShutdownButton = new Button { text = "Shutdown" };
            m_ShutdownButton.style.flexGrow = 1;
            m_ShutdownButton.style.marginLeft = 4;
            buttonRow.Add(m_ShutdownButton);

            m_StatusLabel = new Label("Status: Idle");
            m_StatusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(m_StatusLabel);
        }

        void OnHost()
            => m_Lobby.StartHostIp(m_Nm, m_Identity, m_Serializer,
                m_PlayerNameField.value, m_IpField.value, int.Parse(m_PortField.value),
                Debug.isDebugBuild);

        void OnClient()
            => m_Lobby.StartClientIp(m_Nm, m_Identity, m_Serializer,
                m_PlayerNameField.value, m_IpField.value, int.Parse(m_PortField.value),
                Debug.isDebugBuild);

        void OnShutdown() => m_Lobby.RequestShutdown();

        void SetStatus(string s)
        {
            if (m_StatusLabel != null) m_StatusLabel.text = s;
        }
    }
}
