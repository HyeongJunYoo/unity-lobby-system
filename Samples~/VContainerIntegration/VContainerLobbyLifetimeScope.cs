using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Adapters.Netcode;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Sample.VContainer
{
    /// <summary>
    /// VContainer 예시. 패키지 자체는 VContainer를 모르며,
    /// 이 LifetimeScope가 LobbyBuilder를 한 번 호출해 LobbyConnection을 컨테이너에 싱글턴 등록.
    /// Zenject/Reflex 사용자도 동일 패턴으로 포팅 가능.
    /// </summary>
    public sealed class VContainerLobbyLifetimeScope : LifetimeScope
    {
        [SerializeField] NetworkManager m_NetworkManager;
        [SerializeField] VContainerLobbyUI m_UI;
        [SerializeField] int m_MaxPlayers = 8;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(m_UI);
            builder.RegisterComponent(m_NetworkManager);

            builder.Register<IConnectionPayloadSerializer, JsonUtilityConnectionPayloadSerializer>(Lifetime.Singleton);
            builder.Register<IPlayerIdentityStore, PlayerPrefsPlayerIdentityStore>(Lifetime.Singleton);
            builder.Register<PlayerIdentity>(Lifetime.Singleton);

            builder.Register<LobbyConnection>(resolver =>
            {
                var tick       = m_NetworkManager.gameObject.AddComponent<MonoBehaviourTickSource>();
                var coroutines = m_NetworkManager.gameObject.AddComponent<MonoBehaviourCoroutineRunner>();

                return new LobbyBuilder()
                    .UseNetwork(new NetcodeNetworkFacade(m_NetworkManager))
                    .UseTickSource(tick)
                    .UseCoroutineRunner(coroutines)
                    .UseLogger(new UnityDebugLogger())
                    .UsePayloadSerializer(resolver.Resolve<IConnectionPayloadSerializer>())
                    .UseIdentity(resolver.Resolve<PlayerIdentity>())
                    .UseMaxPlayers(m_MaxPlayers)
                    .UseSessionPlayerDataFactory((id, p) => new SampleSessionPlayerData(id, p.playerName))
                    .UseReconnectPolicy(ReconnectPolicy.Default)
                    .UseDefaultMessageChannels()
                    .UseDefaultStates()
                    .Build();
            }, Lifetime.Singleton);
        }
    }
}
