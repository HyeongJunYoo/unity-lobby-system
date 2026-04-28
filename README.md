# Multiplayer Lobby System

Unity Netcode for GameObjects(NGO) 위에서 동작하는 **간단한 LAN 로비 시스템**입니다. Unity Gaming Services(Relay/Lobby) 없이도 IP 직접 연결로 호스트·클라이언트가 만나 게임을 시작할 수 있습니다.

- ✅ **UGS 없이** 동네 네트워크/직접 IP로 바로 연결
- ✅ **상태 머신 기반** — Offline → Hosting/Connecting → Connected → Reconnecting 흐름이 미리 짜여 있음
- ✅ **재연결 자동 처리** — 끊겼을 때 백오프 정책에 따라 알아서 재시도
- ✅ **DI 컨테이너 무관** — 수동 배선/VContainer/Zenject 등 자유롭게

---

## 요구사항

| 항목 | 버전 |
|---|---|
| Unity | 6000.4 이상 |
| `com.unity.netcode.gameobjects` | 2.11.0 |
| `com.unity.transport` | 2.4.0 |
| 패키지 | `0.3.0` |

---

## 설치

Unity Package Manager → **Add package from git URL**:

```
https://github.com/HyeongJunYoo/unity-lobby-system.git
```

또는 `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.yoojoo97.multiplayer.lobby": "0.3.0"
  }
}
```

---

## 30초 만에 써보기 (인스펙터 방식)

1. 씬에 빈 GameObject를 만들고 다음 컴포넌트를 차례로 추가:
   - `NetworkManager` (NGO 패키지 제공)
   - `UnityTransport` (자동 슬롯 연결)
   - **`LobbyConnectionHost`** (이 패키지 제공)
2. `LobbyConnectionHost` 인스펙터에서 `Network Manager` 슬롯에 같은 GameObject 드래그.
3. Play → 코드에서 호스트나 클라이언트 시작:

```csharp
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.ConnectionMethods.IP;

var host = FindFirstObjectByType<LobbyConnectionHost>();

// 호스트로 시작
host.Connection.StartHostIp(host.GetComponent<NetworkManager>(),
    new PlayerIdentity(new PlayerPrefsPlayerIdentityStore()),
    new JsonUtilityConnectionPayloadSerializer(),
    "MyName", "127.0.0.1", 7777, isDebug: false);

// 또는 클라이언트로 접속
host.Connection.StartClientIp(host.GetComponent<NetworkManager>(),
    new PlayerIdentity(new PlayerPrefsPlayerIdentityStore()),
    new JsonUtilityConnectionPayloadSerializer(),
    "MyName", "192.168.0.10", 7777, isDebug: false);

// 종료
host.Connection.RequestShutdown();
```

> 더 보기 좋은 예제는 Package Manager → **Samples** 탭에서 **Basic Manual Wiring** 샘플을 임포트하면 UI 포함 완성된 셋업이 따라옵니다.

---

## 이벤트 받기

```csharp
host.Connection.OnHostStarted     += () => Debug.Log("호스트 시작!");
host.Connection.OnClientConnected += () => Debug.Log("연결됨!");
host.Connection.OnDisconnected    += () => Debug.Log("끊김");
```

연결 결과 코드(`Success`, `ServerFull` 등)를 받고 싶다면:

```csharp
using var sub = host.Connection.GetSubscriber<ConnectStatus>()
    .Subscribe(status => Debug.Log($"상태: {status}"));
```

---

## 재연결 정책 바꾸기

기본은 시도 2회, 1초 → 2초 백오프. 인스펙터의 `Reconnect Attempts`로 시도 횟수를 조정하거나, 더 세밀하게는 코드로:

```csharp
// LobbyConnectionHost가 빌드 직전에 콜백을 던집니다 (Awake/OnEnable에서 구독)
host.OnConfigure += builder => builder.UseReconnectPolicy(new ReconnectPolicy
{
    MaxAttempts       = 5,
    InitialBackoff    = TimeSpan.FromSeconds(1),
    MaxBackoff        = TimeSpan.FromSeconds(30),
    BackoffMultiplier = 2.0
});
```

---

## 직접 배선하고 싶다면 (DI 컨테이너 사용 시)

`LobbyConnectionHost`가 너무 자동이라 싫거나, VContainer/Zenject 같은 컨테이너에 등록해 쓰고 싶으면 `LobbyBuilder`를 직접 호출하면 됩니다:

```csharp
var lobby = new LobbyBuilder()
    .UseNetwork(new NetcodeNetworkFacade(networkManager))
    .UseTickSource(go.AddComponent<MonoBehaviourTickSource>())
    .UseCoroutineRunner(go.AddComponent<MonoBehaviourCoroutineRunner>())
    .UseLogger(new UnityDebugLogger())
    .UsePayloadSerializer(new JsonUtilityConnectionPayloadSerializer())
    .UseIdentity(new PlayerIdentity(new PlayerPrefsPlayerIdentityStore()))
    .UseDefaultMessageChannels()
    .UseDefaultStates()
    .Build();
```

이 호출을 컨테이너의 등록 콜백 안에 두면 `LobbyConnection`을 싱글턴으로 활용할 수 있습니다. 패키지 본체는 컨테이너에 의존하지 않습니다.

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
       ┌───────────▼────────────┐   ┌──────────────────────┐
       │  INetworkFacade        │   │  IConnectionApprover │
       │  (NetcodeNetworkFacade)│   │  ISessionManager     │
       │  ITickSource           │   │  IConnectionPayload… │
       │  ICoroutineRunner      │   │  ILobbyLogger        │
       └────────────────────────┘   └──────────────────────┘
```

3개의 어셈블리로 분리되어 있습니다:

- **Core** (순수 C#) — 상태 머신·세션·PubSub·빌더·추상화. `UnityEngine` 의존 없음.
- **Adapters** — `NetworkManager`, `MonoBehaviour`, `PlayerPrefs`, `JsonUtility` 등 Unity/NGO 연결.
- **ConnectionMethods/IP** — IP 직접 연결 구현. 별도 asmdef이라 안 쓰면 스트립 가능.

---

## 좀 더 깊이 알고 싶다면

| 알고 싶은 것 | 어디 |
|---|---|
| 상태 머신 전이 흐름·확장 방법 | [`docs/superpowers/specs/2026-04-17-lobby-connection-architecture-design.md`](docs/superpowers/specs/2026-04-17-lobby-connection-architecture-design.md) |
| 변경 이력 | [`CHANGELOG.md`](CHANGELOG.md) |
| 실제 동작 샘플 | Package Manager → Samples 탭 → **Basic Manual Wiring** |
| 인터페이스/확장 포인트 | `Runtime/Core/Abstractions/`의 `INetworkFacade`, `ISessionManager`, `IConnectionApprover` 등을 IDE에서 직접 |

---

## 라이선스

[`LICENSE`](LICENSE) 참고.
