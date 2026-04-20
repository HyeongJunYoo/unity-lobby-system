using System;
using Unity.Netcode;
using UnityEngine;
using Multiplayer.Lobby.Adapters.Netcode;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Sample.BasicManual
{
    /// <summary>
    /// DI 컨테이너 없이 LobbyBuilder로 수동 조립하는 예시.
    /// 씬에 NetworkManager와 이 컴포넌트만 두면 동작.
    /// </summary>
    public sealed class BasicLobbyBootstrapper : MonoBehaviour
    {
        [SerializeField] NetworkManager m_NetworkManager;
        [SerializeField] BasicLobbyUI m_UI;
        [SerializeField] int m_MaxPlayers = 8;

        LobbyConnection m_Lobby;
        PlayerIdentity m_Identity;
        JsonUtilityConnectionPayloadSerializer m_Serializer;

        void Start()
        {
            var tick       = gameObject.AddComponent<MonoBehaviourTickSource>();
            var coroutines = gameObject.AddComponent<MonoBehaviourCoroutineRunner>();

            m_Identity   = new PlayerIdentity(new PlayerPrefsPlayerIdentityStore());
            m_Serializer = new JsonUtilityConnectionPayloadSerializer();

            m_Lobby = new LobbyBuilder()
                .UseNetwork(new NetcodeNetworkFacade(m_NetworkManager))
                .UseTickSource(tick)
                .UseCoroutineRunner(coroutines)
                .UseLogger(new UnityDebugLogger())
                .UsePayloadSerializer(m_Serializer)
                .UseIdentity(m_Identity)
                .UseMaxPlayers(m_MaxPlayers)
                .UseSessionPlayerDataFactory((clientId, payload)
                    => new SampleSessionPlayerData(clientId, payload.playerName))
                .UseReconnectPolicy(ReconnectPolicy.Default)
                .UseDefaultMessageChannels()
                .UseDefaultStates()
                .Build();

            m_UI.Bind(m_Lobby, m_NetworkManager, m_Identity, m_Serializer);
        }

        void OnDestroy() => m_Lobby?.Dispose();
    }
}
