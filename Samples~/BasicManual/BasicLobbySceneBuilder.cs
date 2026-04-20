#if UNITY_EDITOR
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Multiplayer.Lobby.Sample.BasicManual
{
    /// <summary>
    /// BasicManual 샘플 씬을 자동 생성하는 Editor 유틸리티.
    /// 메뉴: Tools → Multiplayer Lobby → Create Basic Manual Sample Scene
    ///
    /// 생성되는 씬 구성:
    /// - NetworkManager (NetworkManager + UnityTransport, 127.0.0.1:7777)
    /// - Canvas (BasicLobbyUI + InputField 3 + Button 3 + Text 1)
    /// - EventSystem (UI 입력용)
    /// - LobbyBootstrapper (BasicLobbyBootstrapper, NetworkManager/UI 연결됨)
    ///
    /// 사용법: 샘플을 Package Manager에서 Import한 뒤 메뉴를 클릭하면
    /// 새 씬이 생성되며 저장 경로를 묻는다. 저장 후 Play 모드로 바로 테스트 가능.
    /// </summary>
    public static class BasicLobbySceneBuilder
    {
        const string k_MenuPath = "Tools/Multiplayer Lobby/Create Basic Manual Sample Scene";

        [MenuItem(k_MenuPath)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1. NetworkManager + UnityTransport
            var nmGO = new GameObject("NetworkManager");
            var nm = nmGO.AddComponent<NetworkManager>();
            var utp = nmGO.AddComponent<UnityTransport>();
            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = utp,
                ConnectionApproval = true
            };
            utp.ConnectionData.Address = "127.0.0.1";
            utp.ConnectionData.Port = 7777;
            utp.ConnectionData.ServerListenAddress = "0.0.0.0";

            // 2. Canvas + Scaler + Raycaster
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // 3. EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();

            // 4. UI elements (세로로 배치)
            var nameField     = CreateInputField(canvasGO.transform, "PlayerNameField", "Your Name",   new Vector2(0,  180));
            var ipField       = CreateInputField(canvasGO.transform, "IpField",         "127.0.0.1",   new Vector2(0,  130));
            var portField     = CreateInputField(canvasGO.transform, "PortField",       "7777",        new Vector2(0,   80));
            var hostBtn       = CreateButton(canvasGO.transform,     "HostButton",      "Host",        new Vector2(-110, 20));
            var clientBtn     = CreateButton(canvasGO.transform,     "ClientButton",    "Client",      new Vector2(0,   20));
            var shutdownBtn   = CreateButton(canvasGO.transform,     "ShutdownButton",  "Shutdown",    new Vector2(110, 20));
            var statusText    = CreateText(canvasGO.transform,       "StatusText",      "Status: Idle", new Vector2(0,  -40));

            // 5. BasicLobbyUI (Canvas에 부착, 필드 자동 바인딩)
            var ui = canvasGO.AddComponent<BasicLobbyUI>();
            var soUI = new SerializedObject(ui);
            AssignField(soUI, "m_PlayerNameField", nameField);
            AssignField(soUI, "m_IpField",         ipField);
            AssignField(soUI, "m_PortField",       portField);
            AssignField(soUI, "m_HostButton",      hostBtn);
            AssignField(soUI, "m_ClientButton",    clientBtn);
            AssignField(soUI, "m_ShutdownButton",  shutdownBtn);
            AssignField(soUI, "m_StatusText",      statusText);
            soUI.ApplyModifiedProperties();

            // 6. Bootstrapper
            var bootGO = new GameObject("LobbyBootstrapper");
            var boot = bootGO.AddComponent<BasicLobbyBootstrapper>();
            var soBoot = new SerializedObject(boot);
            AssignField(soBoot, "m_NetworkManager", nm);
            AssignField(soBoot, "m_UI", ui);
            var maxPlayersProp = soBoot.FindProperty("m_MaxPlayers");
            if (maxPlayersProp != null) maxPlayersProp.intValue = 8;
            soBoot.ApplyModifiedProperties();

            // 7. 씬 저장 경로 선택
            var path = EditorUtility.SaveFilePanel(
                "Save Basic Manual Lobby Sample Scene",
                Application.dataPath,
                "BasicManualLobbyScene",
                "unity");

            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("[Lobby] 씬 저장이 취소되었습니다. 씬은 메모리에만 존재합니다.");
                return;
            }

            if (!path.StartsWith(Application.dataPath))
            {
                Debug.LogError("[Lobby] 씬은 Assets 폴더 하위에 저장해야 합니다.");
                return;
            }

            var assetPath = "Assets" + path.Substring(Application.dataPath.Length);
            EditorSceneManager.SaveScene(scene, assetPath);
            Debug.Log($"[Lobby] 샘플 씬 저장 완료: {assetPath}");

            // Hierarchy에서 Bootstrapper 선택해서 주목시킴
            Selection.activeGameObject = bootGO;
        }

        static void AssignField(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[Lobby] SerializedProperty '{fieldName}' not found.");
                return;
            }
            prop.objectReferenceValue = value;
        }

        static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(260, 36);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            var field = go.AddComponent<InputField>();

            // Placeholder
            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(go.transform, false);
            StretchFill((RectTransform)placeholderGO.transform, 8, 4);
            var placeholderText = placeholderGO.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.font = DefaultFont();
            placeholderText.fontSize = 16;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.supportRichText = false;

            // Text
            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            StretchFill((RectTransform)textGO.transform, 8, 4);
            var textComp = textGO.AddComponent<Text>();
            textComp.color = Color.black;
            textComp.font = DefaultFont();
            textComp.fontSize = 16;
            textComp.alignment = TextAnchor.MiddleLeft;
            textComp.supportRichText = false;

            field.textComponent = textComp;
            field.placeholder = placeholderText;

            return field;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(100, 36);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.82f, 0.82f, 0.82f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            StretchFill((RectTransform)textGO.transform, 0, 0);
            var text = textGO.AddComponent<Text>();
            text.text = label;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = DefaultFont();
            text.fontSize = 16;

            return btn;
        }

        static Text CreateText(Transform parent, string name, string content, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(360, 30);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = DefaultFont();
            text.fontSize = 16;

            return text;
        }

        static void StretchFill(RectTransform rt, float padX, float padY)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        static Font DefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
#endif
