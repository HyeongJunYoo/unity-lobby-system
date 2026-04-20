using System;
using Unity.Netcode;
using UnityEngine;
using Multiplayer.Lobby.Adapters.Netcode;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Adapters.Unity
{
    /// <summary>
    /// 인스펙터로 NetworkManager만 연결하면 자동으로 LobbyBuilder를 조립·빌드하는 편의 MonoBehaviour.
    /// 소비자가 확장이 필요하면 OnConfigure 이벤트에서 Builder를 추가 구성할 수 있다.
    /// </summary>
    public sealed class LobbyConnectionHost : MonoBehaviour
    {
        [SerializeField] NetworkManager m_NetworkManager;
        [SerializeField] int m_MaxPlayers = 8;
        [SerializeField] int m_ReconnectAttempts = 2;

        public LobbyConnection Connection { get; private set; }

        /// <summary>빌더 Build 직전에 호출. 소비자가 상태/채널/훅을 추가할 기회.</summary>
        public event Action<LobbyBuilder> OnConfigure;

        void Start()
        {
            if (m_NetworkManager == null)
                throw new InvalidOperationException("LobbyConnectionHost: NetworkManager가 설정되지 않았습니다.");

            var tick       = gameObject.AddComponent<MonoBehaviourTickSource>();
            var coroutines = gameObject.AddComponent<MonoBehaviourCoroutineRunner>();

            var builder = new LobbyBuilder()
                .UseNetwork(new NetcodeNetworkFacade(m_NetworkManager))
                .UseTickSource(tick)
                .UseCoroutineRunner(coroutines)
                .UseLogger(new UnityDebugLogger())
                .UsePayloadSerializer(new JsonUtilityConnectionPayloadSerializer())
                .UseIdentity(new PlayerIdentity(new PlayerPrefsPlayerIdentityStore()))
                .UseMaxPlayers(m_MaxPlayers)
                .UseReconnectPolicy(new ReconnectPolicy
                {
                    MaxAttempts = m_ReconnectAttempts,
                    InitialBackoff = System.TimeSpan.FromSeconds(1),
                    MaxBackoff = System.TimeSpan.FromSeconds(30),
                    BackoffMultiplier = 2.0
                })
                .UseDefaultMessageChannels()
                .UseDefaultStates();

            OnConfigure?.Invoke(builder);
            Connection = builder.Build();
        }

        void OnDestroy() => Connection?.Dispose();
    }
}
