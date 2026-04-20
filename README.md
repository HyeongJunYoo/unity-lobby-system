# Multiplayer Lobby System

상태 머신 기반 멀티플레이어 로비/연결 관리자 — Unity Netcode for GameObjects(NGO) 위에서 동작한다.

- **UGS-free** — Unity Gaming Services(Relay/Lobby) 없이도 IP 직접 연결로 완결 동작.
- **DI 컨테이너 중립(container-agnostic)** — 수동 배선, VContainer, Zenject, Reflex 등과 동일 패턴으로 붙는다.
- **Core는 순수 C#** — `UnityEngine` 의존 없이 EditMode 단위 테스트로 전량 커버 가능.
- **확장 포인트 전부 인터페이스** — 트랜스포트·로거·세션·승인·재연결 정책·메시지 채널·상태를 전부 교체·추가 가능.

---

## 요구사항

| 항목 | 버전 |
|---|---|
| Unity | 6000.4+ |
| `com.unity.netcode.gameobjects` | 2.11.0 |
| `com.unity.transport` | 2.4.0 |
| 현재 패키지 버전 | `0.2.0` |

VContainer 샘플을 사용할 경우에만 VContainer 패키지가 필요하다. 본체는 VContainer를 **런타임 의존하지 않는다**.

---

## 설치

Unity Package Manager → `Add package from git URL`:

```
https://github.com/<org-or-user>/unity-lobby-system.git
```

또는 `Packages/manifest.json` 직접 편집:

```json
{
  "dependencies": {
    "com.yoojoo97.multiplayer.lobby": "0.2.0"
  }
}
```

Package Manager의 **Samples** 탭에서 아래 두 샘플을 임포트할 수 있다.

- **Basic Manual Wiring** — DI 컨테이너 없이 수동 조립.
- **VContainer Integration** — VContainer `LifetimeScope` 예시.

---

## 어셈블리 레이아웃

패키지는 3개의 asmdef로 분리되어 있다.

| 어셈블리 | 경로 | 의존 |
|---|---|---|
| `Multiplayer.Lobby.Core` | `Runtime/Core/` | 순수 C# (UnityEngine 없음) |
| `Multiplayer.Lobby.Adapters` | `Runtime/Adapters/` | Netcode + Unity |
| `Multiplayer.Lobby.ConnectionMethods.IP` | `Runtime/ConnectionMethods/IP/` | Adapters + Core |
| `Multiplayer.Lobby.Tests.Editor` | `Tests/Editor/` | Core (EditMode 테스트) |

주요 네임스페이스:

- `Multiplayer.Lobby.Abstractions` — 인터페이스 (`INetworkFacade`, `ILobbyLogger`, `ITickSource`, `ICoroutineRunner`, `IConnectionPayloadSerializer`, `IConnectionApprover`, `IPlayerIdentityStore`, `ISessionManager`, `IStateMachineContext`).
- `Multiplayer.Lobby.StateMachine` / `.States` — 상태 머신과 기본 6개 상태.
- `Multiplayer.Lobby.Session` — 세션 플레이어 데이터 관리.
- `Multiplayer.Lobby.Messaging` — 타입 키 PubSub 채널.
- `Multiplayer.Lobby.Connection` — 페이로드, 재연결 정책, 상태 enum.
- `Multiplayer.Lobby.Builder` — `LobbyBuilder`, `LobbyConnection`.
- `Multiplayer.Lobby.Adapters.Netcode` / `.Adapters.Unity` — NGO·Unity 어댑터.
- `Multiplayer.Lobby.ConnectionMethods.IP` — IP 직결 구현.

---

## 빠른 시작

### 1) 인스펙터 원샷 배선 (가장 간단)

씬에 `NetworkManager`를 두고, 그 옆에 `LobbyConnectionHost` 컴포넌트를 얹은 뒤 인스펙터에서 `NetworkManager` 레퍼런스만 연결한다. `MaxPlayers`와 `ReconnectAttempts`도 인스펙터에서 조정 가능.

```csharp
var host = FindObjectOfType<LobbyConnectionHost>();
host.OnConfigure += builder => builder.UseSessionPlayerDataFactory(
    (id, payload) => new MyPlayerData(id, payload.playerName));
// Start()에서 Build()가 실행된 뒤 host.Connection 사용 가능
```

### 2) 수동 배선 (DI 컨테이너 없음)

```csharp
using Multiplayer.Lobby.Adapters.Netcode;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.ConnectionMethods.IP;

var tick       = gameObject.AddComponent<MonoBehaviourTickSource>();
var coroutines = gameObject.AddComponent<MonoBehaviourCoroutineRunner>();
var identity   = new PlayerIdentity(new PlayerPrefsPlayerIdentityStore());
var serializer = new JsonUtilityConnectionPayloadSerializer();

var lobby = new LobbyBuilder()
    .UseNetwork(new NetcodeNetworkFacade(networkManager))
    .UseTickSource(tick)
    .UseCoroutineRunner(coroutines)
    .UseLogger(new UnityDebugLogger())
    .UsePayloadSerializer(serializer)
    .UseIdentity(identity)
    .UseMaxPlayers(8)
    .UseSessionPlayerDataFactory((id, p) => new MyPlayerData(id, p.playerName))
    .UseReconnectPolicy(ReconnectPolicy.Default)
    .UseDefaultMessageChannels()
    .UseDefaultStates()
    .Build();

// 호스트 시작
lobby.StartHostIp(networkManager, identity, serializer,
    "Me", "127.0.0.1", 7777, Debug.isDebugBuild);

// 또는 클라이언트 접속
lobby.StartClientIp(networkManager, identity, serializer,
    "Me", "192.168.0.10", 7777, Debug.isDebugBuild);

// 종료
lobby.RequestShutdown();
lobby.Dispose();
```

### 3) VContainer 연동

```csharp
public sealed class LobbyScope : LifetimeScope
{
    [SerializeField] NetworkManager m_NetworkManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IConnectionPayloadSerializer,
            JsonUtilityConnectionPayloadSerializer>(Lifetime.Singleton);
        builder.Register<IPlayerIdentityStore,
            PlayerPrefsPlayerIdentityStore>(Lifetime.Singleton);
        builder.Register<PlayerIdentity>(Lifetime.Singleton);

        builder.Register<LobbyConnection>(resolver =>
        {
            var go = m_NetworkManager.gameObject;
            return new LobbyBuilder()
                .UseNetwork(new NetcodeNetworkFacade(m_NetworkManager))
                .UseTickSource(go.AddComponent<MonoBehaviourTickSource>())
                .UseCoroutineRunner(go.AddComponent<MonoBehaviourCoroutineRunner>())
                .UseLogger(new UnityDebugLogger())
                .UsePayloadSerializer(resolver.Resolve<IConnectionPayloadSerializer>())
                .UseIdentity(resolver.Resolve<PlayerIdentity>())
                .UseDefaultMessageChannels()
                .UseDefaultStates()
                .Build();
        }, Lifetime.Singleton);
    }
}
```

Zenject/Reflex 사용자도 같은 패턴으로 포팅 가능 — `LobbyBuilder`를 한 번 호출해 `LobbyConnection` 싱글턴으로 등록하면 된다.

---

## 상태 머신

기본 상태 (`UseDefaultStates()`로 전부 등록):

```
OfflineState
   ├─ StartHost ─► StartingHostState ─► HostingState
   └─ StartClient ─► ClientConnectingState ─► ClientConnectedState
                                                 └─ (disconnect) ─► ClientReconnectingState
```

- 상태 추가: `builder.AddState<MyState>(ctx => new MyState(ctx))`
- 상태 교체: `builder.ReplaceState<ClientConnectingState>(ctx => new MyCustomConnecting(ctx))`
- `ConnectionState` / `OnlineState`를 상속해 새 전이를 정의한다.
- 초기 상태는 항상 `OfflineState` (필수).

---

## 메시지 채널 (PubSub)

`LobbyBuilder.UseDefaultMessageChannels()`로 아래 4개가 기본 등록된다.

| 메시지 타입 | 용도 |
|---|---|
| `ConnectStatus` (enum) | 연결 결과 코드(Success/ServerFull/IncompatibleBuildType 등) |
| `ReconnectMessage` | 재연결 시도 이벤트 |
| `ConnectionEventMessage` | 클라이언트 입·퇴장 이벤트 |
| `LobbyLifecycleMessage` (enum) | HostStarted / ClientConnected / Disconnected 통합 경로 |

소비:

```csharp
using var sub = lobby.GetSubscriber<ConnectStatus>().Subscribe(status =>
{
    if (status == ConnectStatus.ServerFull) ShowServerFullDialog();
});
```

커스텀 타입 등록:

```csharp
builder.AddMessageChannel<ChatMessage>();
...
lobby.GetPublisher<ChatMessage>().Publish(new ChatMessage(...));
```

편의용 C# 이벤트도 함께 제공된다 (`LobbyLifecycleMessage`를 랩핑):

```csharp
lobby.OnHostStarted    += () => { ... };
lobby.OnClientConnected += () => { ... };
lobby.OnDisconnected   += () => { ... };
```

---

## 재연결 정책

```csharp
builder.UseReconnectPolicy(new ReconnectPolicy
{
    MaxAttempts       = 3,
    InitialBackoff    = TimeSpan.FromSeconds(1),
    MaxBackoff        = TimeSpan.FromSeconds(30),
    BackoffMultiplier = 2.0
});
```

기본값은 `ReconnectPolicy.Default` (시도 2회, 1s → 2s → 4s … 최대 30s).

---

## 확장 포인트

| 확장 대상 | 인터페이스 / 진입점 | 방법 |
|---|---|---|
| 연결 방식 (Relay/Steam 등) | `ConnectionMethodBase` | 서브클래스 + 별도 asmdef 권장 |
| 상태 추가 | `IStateMachineContext` | `builder.AddState<T>(ctx => new T(ctx))` |
| 상태 교체 | 동일 | `builder.ReplaceState<T>(ctx => new T(ctx))` |
| 세션 데이터 | `ISessionPlayerData` | `builder.UseSessionPlayerDataFactory(...)` |
| 세션 매니저 | `ISessionManager` | `builder.UseSessionManager(...)` |
| 승인 로직 | `IConnectionApprover` | `builder.UseApprover(...)` (기본 `DefaultConnectionApprover`) |
| 로거 | `ILobbyLogger` | `builder.UseLogger(...)` (기본 `NullLogger`) |
| 트랜스포트 | `INetworkFacade` | 직접 구현 후 `builder.UseNetwork(...)` |
| 페이로드 직렬화 | `IConnectionPayloadSerializer` | `builder.UsePayloadSerializer(...)` |
| 플레이어 ID 저장소 | `IPlayerIdentityStore` | `new PlayerIdentity(myStore)` |
| 틱 소스 | `ITickSource` | `builder.UseTickSource(...)` |
| 코루틴 러너 | `ICoroutineRunner` | `builder.UseCoroutineRunner(...)` |
| 커스텀 메시지 | `IMessageChannel<T>` | `builder.AddMessageChannel<T>()` |
| 재연결 정책 | `ReconnectPolicy` | `builder.UseReconnectPolicy(...)` |
| 최대 인원 | int | `builder.UseMaxPlayers(...)` (기본 8) |
| 생애주기 훅 | `Action` | `builder.OnHostStarted/OnClientConnected/OnDisconnected(handler)` |

---

## 아키텍처

```
┌─────────────────────────────────────────────┐
│  LobbyConnection  (public API)              │
│    • StartHost / StartClient / Shutdown     │
│    • OnHostStarted / OnClientConnected …    │
│    • GetPublisher / GetSubscriber           │
└──────────────────┬──────────────────────────┘
                   │
           ┌───────▼────────┐        ┌────────────────┐
           │  StateMachine  │◄───────┤   States (6)   │
           └───────┬────────┘        └────────────────┘
                   │
       ┌───────────▼────────────┐   ┌──────────────────┐
       │  INetworkFacade        │   │  IConnectionApprover │
       │  (NetcodeNetworkFacade)│   │  ISessionManager     │
       │  ITickSource           │   │  IConnectionPayload… │
       │  ICoroutineRunner      │   │  ILobbyLogger        │
       └────────────────────────┘   └──────────────────┘
```

- **Core** (순수 C#) — 상태 머신·세션·PubSub·빌더·추상화. `UnityEngine` 직접 의존 없음.
- **Adapters** (Unity/Netcode) — `NetworkManager`, `MonoBehaviour`, `PlayerPrefs`, `JsonUtility` 어댑터.
- **ConnectionMethods/IP** — IP 직접 연결 구현. 별도 asmdef이라 미사용 시 스트립 가능.

상세 설계 문서: [`docs/superpowers/specs/2026-04-17-lobby-connection-architecture-design.md`](docs/superpowers/specs/2026-04-17-lobby-connection-architecture-design.md).

---

## 테스트

EditMode 단위 테스트가 `Tests/Editor/`에 포함되어 있다. Unity Test Runner에서 실행:

- 상태 전이 (`StateMachineTests`, `StateTransitionTests`, `OfflineStateTests`)
- 빌더 유효성 (`LobbyBuilderTests`, `LobbyBuilderBuildTests`)
- 세션 (`SessionManagerTests`)
- 승인 (`DefaultConnectionApproverTests`)
- PubSub (`MessageChannelTests`)
- 플레이어 ID (`PlayerIdentityTests`, `InMemoryPlayerIdentityStoreTests`)
- 각 Fake 어댑터 자체 검증 (`Tests/Editor/Fakes/`)

Core가 순수 C#이기 때문에 `INetworkFacade`, `ITickSource` 등을 Fake로 갈아끼우면 엔진 실행 없이 로직을 전량 검증할 수 있다.

---

## 샘플

| 경로 | 내용 |
|---|---|
| `Samples~/BasicManual/` | `LobbyBuilder`를 수동으로 조립. UI Toolkit 기반 간단 UI(`BasicLobbyUI`) 포함. |
| `Samples~/VContainerIntegration/` | `LifetimeScope`에서 `LobbyBuilder`를 호출해 `LobbyConnection`을 싱글턴 등록. VContainer 패키지 필요. |

---

## 변경 이력

주요 변경은 [`CHANGELOG.md`](CHANGELOG.md) 참고.

- **0.2.0** — VContainer 런타임 의존성 제거. 어셈블리 3분할. 상태 머신 타입 키 레지스트리화. `LobbyBuilder`·`LobbyConnection`·`LobbyConnectionHost` 구조로 재편. EditMode 테스트 스위트 도입.

---

## 라이선스

[`LICENSE`](LICENSE) 참고.
