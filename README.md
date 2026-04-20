# Multiplayer Lobby System

상태 머신 기반 멀티플레이어 로비 연결 관리자. Unity Netcode for GameObjects 위에서 동작하며
**UGS(Unity Gaming Services)에 의존하지 않는다.** **DI 컨테이너 중립** — 수동 배선으로도,
VContainer/Zenject/Reflex 등 어떤 DI 컨테이너와도 사용 가능.

## 설치

Unity Package Manager → Add package from git URL:
```
https://github.com/<repo>/unity-lobby-system.git
```
또는 `manifest.json`:
```json
"com.yoojoo97.multiplayer.lobby": "0.2.0"
```

## 빠른 시작 (수동 배선)

```csharp
using Multiplayer.Lobby.Adapters.Netcode;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;

var tick       = gameObject.AddComponent<MonoBehaviourTickSource>();
var coroutines = gameObject.AddComponent<MonoBehaviourCoroutineRunner>();

var lobby = new LobbyBuilder()
    .UseNetwork(new NetcodeNetworkFacade(networkManager))
    .UseTickSource(tick)
    .UseCoroutineRunner(coroutines)
    .UseLogger(new UnityDebugLogger())
    .UsePayloadSerializer(new JsonUtilityConnectionPayloadSerializer())
    .UseIdentity(new PlayerIdentity(new PlayerPrefsPlayerIdentityStore()))
    .UseMaxPlayers(8)
    .UseSessionPlayerDataFactory((id, p) => new MyPlayerData(id, p.playerName))
    .UseReconnectPolicy(ReconnectPolicy.Default)
    .UseDefaultMessageChannels()
    .UseDefaultStates()
    .Build();

lobby.StartHostIp(networkManager, identity, serializer, "Me", "127.0.0.1", 7777, Debug.isDebugBuild);
```

## 확장 포인트

| 확장 대상 | 방법 |
|---|---|
| 연결 방식 (Relay/Steam 등) | `ConnectionMethodBase` 서브클래스 + 별도 asmdef 권장 |
| 상태 추가/교체 | `builder.AddState<T>(ctx => new T(ctx))` / `ReplaceState<T>(...)` |
| 세션 데이터 | `ISessionPlayerData` 구현 후 `UseSessionPlayerDataFactory(...)` |
| 승인 로직 | `IConnectionApprover` 구현 후 `UseApprover(...)` |
| 커스텀 메시지 | `builder.AddMessageChannel<MyMessage>()`, `lobby.GetSubscriber<MyMessage>()` |
| 로거 | `ILobbyLogger` 구현 후 `UseLogger(...)` |
| 트랜스포트 (고급) | `INetworkFacade` 직접 구현 |
| 재연결 정책 | `ReconnectPolicy` 값 조정 후 `UseReconnectPolicy(...)` |

## 샘플

- `Samples~/BasicManual/` — DI 컨테이너 없이 수동 배선.
- `Samples~/VContainerIntegration/` — VContainer 통합 예시.

## 아키텍처

- **Core** (순수 C#, UnityEngine 의존 없음) — 상태 머신, PubSub, 세션, 빌더, 추상화.
- **Adapters** (Unity/Netcode) — `NetworkManager`/`MonoBehaviour`/`PlayerPrefs`/`JsonUtility` 어댑터.
- **ConnectionMethods/IP** — IP 직접 연결 구현.

상세: `docs/superpowers/specs/2026-04-17-lobby-connection-architecture-design.md`.

## 요구사항

- Unity 6000.4+
- `com.unity.netcode.gameobjects` 2.11.0
- `com.unity.transport` 2.4.0
