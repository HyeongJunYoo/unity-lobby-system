# Lobby Connection Architecture — VContainer 제거 & SOLID 구조 정리 설계

**상태**: Approved (2026-04-17)
**대상 버전**: `com.yoojoo97.multiplayer.lobby` v0.2.0
**변경 성격**: Breaking rewrite (v0.1.0 → v0.2.0)

---

## 1. 목표

1. **VContainer 의존성을 런타임 코어에서 완전히 제거.** 패키지 자체는 어떤 DI 컨테이너에도 무지(無知)해야 한다.
2. **SOLID 원칙 기반으로 패키지 구조를 재편.** 사용자 확장성을 1급 관심사로 끌어올린다.

두 목표는 동일한 리팩터링으로 함께 달성한다. 버전은 v0.2.0으로 올리며 퍼블릭 API의 Breaking change는 자유롭게 허용한다.

## 2. 제약 및 합의사항

| 항목 | 결정 |
|---|---|
| 사용자 시나리오 | **라이브러리 확장형**. 소비 프로젝트가 이 패키지를 확장해 자기 로비/매칭 시스템을 만듦. |
| DI 전략 | **컨테이너 중립 (container-agnostic)**. 순수 C# 코어 + 수동 빌더. VContainer/Zenject/Reflex 등 어떤 컨테이너와도 결합 안 됨. |
| 공식 확장 포인트 | **전부 지원**: 연결 방식(ConnectionMethod), 상태(State), 세션 데이터, 메시지 타입, 승인 로직, 트랜스포트, 로거, 재연결 정책. |
| 하위 호환성 | **완전 자유**. 네임스페이스/클래스명/시그니처 전면 개편 가능. |
| 샘플 | **수동 배선 샘플 1개 + VContainer 통합 샘플 1개**. |
| 테스트 | **EditMode 단위 테스트**. `INetworkFacade` 추상화로 Netcode 분리. PlayMode 통합 테스트는 범위 밖. |

## 3. 전체 아키텍처

### 3.1 레이어 분리 — 어셈블리 3개 + 테스트 + 샘플

```
Runtime/
  Core/                                ← Multiplayer.Lobby.Core.asmdef
    Abstractions/
      INetworkFacade.cs
      IConnectionApprover.cs
      ILobbyLogger.cs
      ITickSource.cs
      ICoroutineRunner.cs
      IConnectionPayloadSerializer.cs
      IPlayerIdentityStore.cs
      IStateMachineContext.cs
      ISessionManager.cs
    StateMachine/
      ConnectionState.cs               ← [Inject] 제거, 생성자 주입
      OnlineState.cs
      StateMachine.cs                  ← 전이 관리 전담
      StateMachineContext.cs           ← 상태가 참조하는 서비스 컨텍스트
      States/
        OfflineState.cs
        ClientConnectingState.cs
        ClientConnectedState.cs
        ClientReconnectingState.cs
        StartingHostState.cs
        HostingState.cs
    Session/
      SessionManager.cs                ← ISessionManager 구현
      ISessionPlayerData.cs
    Messaging/
      IMessageChannel.cs
      MessageChannel.cs
      BufferedMessageChannel.cs
      NetworkedMessageChannel.cs
      MessageChannelBase.cs
      DisposableSubscription.cs
      LobbyLifecycleMessage.cs
    Connection/
      ConnectionMethodBase.cs
      ConnectionPayload.cs
      ConnectStatus.cs
      PlayerIdentity.cs
      ApprovalRequest.cs
      ApprovalResult.cs
      DefaultConnectionApprover.cs
      ReconnectPolicy.cs
      ReconnectMessage.cs
      ConnectionEventMessage.cs
    Builder/
      LobbyBuilder.cs
      LobbyConnection.cs

  Adapters/                            ← Multiplayer.Lobby.Adapters.asmdef
    Netcode/
      NetcodeNetworkFacade.cs
    Unity/
      MonoBehaviourTickSource.cs
      MonoBehaviourCoroutineRunner.cs
      UnityDebugLogger.cs
      JsonUtilityConnectionPayloadSerializer.cs
      PlayerPrefsPlayerIdentityStore.cs
      LobbyConnectionHost.cs

  ConnectionMethods/
    IP/                                ← Multiplayer.Lobby.ConnectionMethods.IP.asmdef
      IPConnectionMethod.cs
      IPConnectionConfig.cs
      LobbyConnectionIpExtensions.cs

Tests/
  Editor/                              ← Multiplayer.Lobby.Tests.Editor.asmdef
    Fakes/
      FakeNetworkFacade.cs
      FakeLogger.cs
      FakeTickSource.cs
      FakeSessionPlayerData.cs
    StateMachineTests.cs
    StateTransitionTests.cs
    SessionManagerTests.cs
    PubSubTests.cs
    DefaultApproverTests.cs
    LobbyBuilderTests.cs

Samples~/
  BasicManual/                         ← Multiplayer.Lobby.Sample.BasicManual.asmdef
    BasicLobbyBootstrapper.cs
    BasicLobbyUI.cs
    SampleSessionPlayerData.cs
    BasicManual.unity
  VContainerIntegration/               ← Multiplayer.Lobby.Sample.VContainer.asmdef
    VContainerLobbyLifetimeScope.cs
    VContainerLobbyUI.cs
    VContainerIntegration.unity
```

### 3.2 의존 방향 (asmdef로 컴파일 타임 강제)

```
Core  ←  Adapters  ←  ConnectionMethods/IP
  ↑         ↑              ↑
Tests    Samples        Samples
```

- **Core는 그 무엇에도 의존하지 않는다.** `UnityEngine` / `Unity.Netcode` / `Unity.Transport` 참조 금지. Unity 없이 컴파일 및 테스트 가능해야 한다.
- Adapters/Core 역방향 참조는 허용하지 않는다.

### 3.3 SOLID 개선 요점

- **SRP**: 기존 `LobbyConnectionManager` (God Object, 174줄) → `StateMachine` (전이 관리) + `StateMachineContext` (서비스 컨텍스트) + `LobbyConnection` (퍼블릭 파사드) + `LobbyConnectionHost` (선택적 MonoBehaviour 수명 관리) + `IConnectionApprover` (승인)로 분해.
- **OCP**: 상태는 `Dictionary<Type, ConnectionState>` 레지스트리에 등록. 빌더의 `AddState/ReplaceState`로 **수정 없이 확장**. 메시지 채널도 `AddMessageChannel<T>`로 사용자 타입 추가 가능.
- **LSP**: `ConnectionState`, `OnlineState`, `ConnectionMethodBase` 가상 메서드의 기본 구현은 no-op. 서브클래스가 부분 override 가능.
- **ISP**: 상태는 `IStateMachineContext`(필요한 서비스만)에 의존. `NetworkManager` 전체가 아니라 `INetworkFacade`(축소된 표면)에 의존. 소비자는 `LobbyConnection.GetSubscriber<T>()`로 필요한 메시지만 구독.
- **DIP**: 모든 Unity/Netcode 타입은 Core의 인터페이스 뒤로 숨김. Core는 `NetworkManager`를 참조하지 않는다.

## 4. 핵심 추상화 (Core/Abstractions)

### 4.1 `INetworkFacade`

`NetworkManager` 전체 표면을 축소한 인터페이스. 상태 머신과 연결 방식이 의존하는 유일한 네트워크 창구.

```csharp
public interface INetworkFacade
{
    bool IsClient { get; }
    bool IsServer { get; }
    bool IsHost { get; }
    bool IsListening { get; }
    ulong LocalClientId { get; }

    byte[] ConnectionPayload { get; set; }

    bool StartClient();
    bool StartHost();
    void Shutdown(bool discardMessageQueue = false);
    void DisconnectClient(ulong clientId, string reason = null);
    string GetDisconnectReason(ulong clientId);

    event Action OnServerStarted;
    event Action<bool> OnServerStopped;
    event Action OnTransportFailure;
    event Action<ulong> OnClientConnected;
    event Action<ulong, string> OnClientDisconnected;
    event Func<ApprovalRequest, ApprovalResult> ApprovalCheck;
}
```

- 기본 구현: `NetcodeNetworkFacade` (Adapters).
- 테스트 구현: `FakeNetworkFacade` (Tests).
- **`ApprovalCheck` 단일 핸들러 규약**: 이 이벤트는 `StateMachine`이 **정확히 한 개의 핸들러**만 구독한다(현재 상태의 `ApprovalCheck`로 위임). 다중 구독은 지원하지 않으며, 어댑터 구현은 마지막 반환값을 사용한다. 승인 로직 교체는 `IConnectionApprover`로 수행한다.

### 4.2 `IConnectionApprover`

```csharp
public readonly struct ApprovalRequest
{
    public ulong ClientId { get; }
    public byte[] Payload { get; }
    /// <summary>
    /// 승인 요청 시점에 이미 접속 완료된 클라이언트 수. 요청자(ClientId)는 포함되지 않는다.
    /// 따라서 MaxPlayers=8, CurrentConnectedCount=8이면 요청자는 거부되어야 한다.
    /// </summary>
    public int CurrentConnectedCount { get; }
}

public readonly struct ApprovalResult
{
    public bool Approved { get; }
    public string Reason { get; }  // ConnectStatus.ToString() 등
    public static ApprovalResult Allow() => new(true, null);
    public static ApprovalResult Deny(string reason) => new(false, reason);
}

public interface IConnectionApprover
{
    ApprovalResult Approve(ApprovalRequest request);
}
```

- 기본 구현 `DefaultConnectionApprover`: 페이로드 비어있지 않은지 + `CurrentConnectedCount < MaxPlayers` 검증.
- 사용자는 자체 구현으로 교체 또는 `CompositeConnectionApprover`로 체이닝.

### 4.3 `IStateMachineContext`

상태 클래스가 참조하는 슬림 컨텍스트. `LobbyConnectionManager` 전체가 아니라 **상태가 실제로 필요한 서비스만** 노출한다.

```csharp
public interface IStateMachineContext
{
    INetworkFacade Network { get; }
    ISessionManager Sessions { get; }
    IConnectionApprover Approver { get; }
    ILobbyLogger Logger { get; }
    PlayerIdentity Identity { get; }
    ReconnectPolicy ReconnectPolicy { get; }

    IPublisher<ConnectStatus> ConnectStatusPublisher { get; }
    IPublisher<ReconnectMessage> ReconnectPublisher { get; }
    IPublisher<ConnectionEventMessage> ConnectionEventPublisher { get; }
    IPublisher<LobbyLifecycleMessage> LifecyclePublisher { get; }

    void ChangeState<TState>() where TState : ConnectionState;
    TState GetState<TState>() where TState : ConnectionState;
}
```

- 퍼블릭 생애주기 이벤트(`OnHostStarted`, `OnClientConnected`, `OnDisconnected`)는 내부적으로 `IPublisher<LobbyLifecycleMessage>` 채널로 전달된다. `LobbyConnection` 파사드가 이 채널을 구독해 C# 이벤트로 재발행한다.

### 4.4 `ILobbyLogger`

```csharp
public interface ILobbyLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
```

- 기본 구현: `UnityDebugLogger` (Adapters), `NullLogger` (Core, 테스트 기본값).

### 4.5 `ITickSource`

```csharp
public interface ITickSource
{
    event Action OnUpdate;
    event Action OnLateUpdate;
}
```

- 기본 구현: `MonoBehaviourTickSource` (Adapters), `ManualTickSource` (테스트).
- VContainer `ITickable`은 제거된다.

### 4.6 `ISessionManager`

기존 `SessionManager`의 퍼블릭 메서드를 그대로 인터페이스화. 테스트 모의/구현 교체 용이성 확보.

### 4.7 `ICoroutineRunner` — 코루틴 실행 추상화 (스펙 보강 2026-04-17)

재연결 상태가 Unity 코루틴으로 타이밍을 관리하므로, Core를 순수 C#으로 유지하기 위해 실행기를 추상화한다.

```csharp
public interface ICoroutineRunner
{
    object Start(IEnumerator routine);
    void Stop(object handle);
}
```

- 기본 구현: `MonoBehaviourCoroutineRunner` (Adapters, `MonoBehaviour.StartCoroutine` 위임).
- 테스트 구현: `FakeCoroutineRunner` — 수동으로 한 스텝씩 `Advance()` 가능.
- Core는 `System.Collections.IEnumerator`만 사용 (UnityEngine 아님).

### 4.8 `IConnectionPayloadSerializer` — 페이로드 직렬화 추상화 (스펙 보강 2026-04-17)

`ConnectionMethodBase`의 `UnityEngine.JsonUtility` 의존을 제거한다.

```csharp
public interface IConnectionPayloadSerializer
{
    byte[] Serialize(ConnectionPayload payload);
    ConnectionPayload Deserialize(byte[] bytes);
}
```

- 기본 구현: `JsonUtilityConnectionPayloadSerializer` (Adapters).
- 테스트 구현: `FakeConnectionPayloadSerializer` (인-메모리 dict).

### 4.9 `IPlayerIdentityStore` — 플레이어 식별자 저장소 추상화 (스펙 보강 2026-04-17)

`PlayerIdentity`의 `PlayerPrefs`/`Application.dataPath`/`Environment.GetCommandLineArgs` 의존을 제거한다.

```csharp
public interface IPlayerIdentityStore
{
    string GetOrCreateGuid(string profile);
    string ResolveProfile();
}
```

- 기본 구현: `PlayerPrefsPlayerIdentityStore` (Adapters) — 기존 로직 그대로 이전.
- 테스트 구현: `InMemoryPlayerIdentityStore`.

### 4.10 `ReconnectPolicy` (값 객체)

```csharp
public readonly struct ReconnectPolicy
{
    public int MaxAttempts { get; init; }
    public TimeSpan InitialBackoff { get; init; }
    public TimeSpan MaxBackoff { get; init; }
    public double BackoffMultiplier { get; init; }

    public static ReconnectPolicy Default => new()
    {
        MaxAttempts = 2,
        InitialBackoff = TimeSpan.FromSeconds(1),
        MaxBackoff = TimeSpan.FromSeconds(30),
        BackoffMultiplier = 2.0
    };
}
```

- 로직 교체가 필요해지면 v0.3에서 `IReconnectStrategy` 인터페이스로 non-breaking 승격 가능.

## 5. 상태 머신 재설계

### 5.1 `ConnectionState` (베이스)

```csharp
public abstract class ConnectionState
{
    protected IStateMachineContext Context { get; }

    protected ConnectionState(IStateMachineContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void OnClientConnected(ulong clientId) { }
    public virtual void OnClientDisconnected(ulong clientId, string reason) { }
    public virtual void OnServerStarted() { }
    public virtual void OnServerStopped() { }
    public virtual void OnTransportFailure() { }
    public virtual void StartClient(ConnectionMethodBase method) { }
    public virtual void StartHost(ConnectionMethodBase method) { }
    public virtual void OnUserRequestedShutdown() { }
    public virtual ApprovalResult ApprovalCheck(ApprovalRequest request)
        => Context.Approver.Approve(request);  // 기본은 Approver에 위임
}
```

### 5.2 `OnlineState`

```csharp
public abstract class OnlineState : ConnectionState
{
    protected OnlineState(IStateMachineContext context) : base(context) { }

    public override void OnUserRequestedShutdown()
    {
        Context.ConnectStatusPublisher.Publish(ConnectStatus.UserRequestedDisconnect);
        Context.ChangeState<OfflineState>();
    }

    public override void OnTransportFailure()
    {
        Context.ChangeState<OfflineState>();
    }
}
```

### 5.3 `StateMachine` (전이 전담)

```csharp
public sealed class StateMachine
{
    readonly IReadOnlyDictionary<Type, ConnectionState> _states;
    readonly ILobbyLogger _logger;
    readonly INetworkFacade _network;
    ConnectionState _current;

    internal StateMachine(
        IReadOnlyDictionary<Type, ConnectionState> states,
        INetworkFacade network,
        ILobbyLogger logger)
    {
        _states = states;
        _network = network;
        _logger = logger;
    }

    public void Start()
    {
        _network.OnServerStarted       += () => _current.OnServerStarted();
        _network.OnServerStopped       += _ => _current.OnServerStopped();
        _network.OnTransportFailure    += () => _current.OnTransportFailure();
        _network.OnClientConnected     += id => _current.OnClientConnected(id);
        _network.OnClientDisconnected  += (id, reason) => _current.OnClientDisconnected(id, reason);
        _network.ApprovalCheck         += req => _current.ApprovalCheck(req);
        ChangeState<OfflineState>();
    }

    public void ChangeState<TState>() where TState : ConnectionState
    {
        if (!_states.TryGetValue(typeof(TState), out var next))
            throw new InvalidOperationException($"State not registered: {typeof(TState).Name}");

        _logger.Info($"{_current?.GetType().Name ?? "(null)"} → {typeof(TState).Name}");
        _current?.Exit();
        _current = next;
        _current.Enter();
    }

    public TState GetState<TState>() where TState : ConnectionState
        => (TState)_states[typeof(TState)];

    public void StartClient(ConnectionMethodBase method) => _current.StartClient(method);
    public void StartHost(ConnectionMethodBase method)   => _current.StartHost(method);
    public void RequestShutdown()                         => _current.OnUserRequestedShutdown();
}
```

### 5.4 `StateMachineContext`

`IStateMachineContext` 구현. `StateMachine`과 분리해 SRP를 엄격화한다. `ChangeState<T>`와 `GetState<T>`는 `StateMachine` 참조를 위임 호출한다.

### 5.5 상태 전이 규칙

- 유효한 전이 검증(예: `OfflineState`에서는 `HostingState`로 직접 전이 불가 같은 규칙)은 이번 범위에 포함하지 않는다(YAGNI). 필요해지면 `ITransitionValidator`로 v0.3에서 추가.
- `OfflineState`도 `ReplaceState<OfflineState>(...)`로 교체 가능. 초기 상태 결정이 사용자 책임이 되지만 OCP 원칙을 우선한다.

## 6. 조립 API — `LobbyBuilder` / `LobbyConnection`

### 6.1 `LobbyBuilder`

```csharp
public sealed class LobbyBuilder
{
    // 필수
    public LobbyBuilder UseNetwork(INetworkFacade network);
    public LobbyBuilder UseTickSource(ITickSource tick);
    public LobbyBuilder UseIdentity(PlayerIdentity identity);

    // 선택 (기본값 존재)
    public LobbyBuilder UseLogger(ILobbyLogger logger);
    public LobbyBuilder UseSessionManager(ISessionManager sm);
    public LobbyBuilder UseApprover(IConnectionApprover approver);
    public LobbyBuilder UseReconnectPolicy(ReconnectPolicy policy);

    // 메시지 채널
    public LobbyBuilder UseDefaultMessageChannels();
    public LobbyBuilder AddMessageChannel<TMessage>();
    public LobbyBuilder AddMessageChannel<TMessage>(IMessageChannel<TMessage> channel);

    // 상태
    public LobbyBuilder UseDefaultStates();
    public LobbyBuilder AddState<TState>(Func<IStateMachineContext, TState> factory)
        where TState : ConnectionState;
    public LobbyBuilder ReplaceState<TState>(Func<IStateMachineContext, TState> factory)
        where TState : ConnectionState;

    // 생애주기 훅 (내부 Lifecycle PubSub 구독 감싼 헬퍼)
    public LobbyBuilder OnHostStarted(Action handler);
    public LobbyBuilder OnClientConnected(Action handler);
    public LobbyBuilder OnDisconnected(Action handler);

    public LobbyConnection Build();
}
```

### 6.2 `LobbyConnection` (퍼블릭 파사드)

```csharp
public sealed class LobbyConnection : IDisposable
{
    public event Action OnHostStarted;
    public event Action OnClientConnected;
    public event Action OnDisconnected;

    public ISessionManager Sessions { get; }
    public INetworkFacade Network { get; }

    public ISubscriber<TMessage> GetSubscriber<TMessage>();
    public IPublisher<TMessage>  GetPublisher<TMessage>();

    public void StartClient(ConnectionMethodBase method);
    public void StartHost(ConnectionMethodBase method);
    public void RequestShutdown();

    public void Dispose();  // 이벤트 구독 해제
}
```

- `StartClientIp/StartHostIp`는 Core에 두지 않고 `ConnectionMethods/IP` 어셈블리의 확장 메서드로 분리한다.

### 6.3 `Build()` 에러 처리

- 필수 의존성 누락 → `InvalidOperationException`에 누락된 의존성 이름을 명시한 메시지.
- 중복 `UseX` 호출 정책: 마지막 값 승리(덮어쓰기). 경고 로그 선택적.
- 중복 `AddState<T>`: `InvalidOperationException`("상태 `T`가 이미 등록되었습니다. `ReplaceState<T>`를 사용하십시오.").

## 7. 어댑터 레이어 (Adapters)

### 7.1 `NetcodeNetworkFacade`

`NetworkManager`의 `OnConnectionEvent`, `ConnectionApprovalCallback`, `OnServerStarted/Stopped`, `OnTransportFailure`를 구독해 `INetworkFacade` 이벤트로 정규화한다. 승인 콜백은 `ApprovalRequest` 값 객체로 변환해 `ApprovalCheck` 이벤트로 위임하고, `ApprovalResult`를 받아 `ConnectionApprovalResponse`에 채워 넣는다. `Dispose()`에서 모든 구독을 해제한다.

### 7.2 `MonoBehaviourTickSource`

`MonoBehaviour` 상속, `Update()`/`LateUpdate()`에서 이벤트 발화. 소비자가 원하면 자체 MonoBehaviour에 이벤트 연결만 해서 직접 구현 가능.

### 7.3 `UnityDebugLogger`

`UnityEngine.Debug.Log/LogWarning/LogError` 위임. 모든 메시지에 `[Lobby] ` 접두사.

### 7.4 `LobbyConnectionHost` (선택적 편의 컴포넌트)

MonoBehaviour 생애주기(`Start`/`OnDestroy`)에 빌더 호출과 `Dispose`를 연결. 인스펙터에서 `NetworkManager`/최대 인원/재연결 횟수 등을 설정. `OnConfigure` 이벤트로 소비자가 빌더에 확장 포인트를 추가할 기회를 준다.

## 8. 연결 방식 — `ConnectionMethods/IP`

- `IPConnectionMethod` 생성자 시그니처 변경: 기존 `LobbyConnectionManager` 의존 → `INetworkFacade` + `PlayerIdentity` + 메서드 파라미터.
- `LobbyConnectionIpExtensions`에 `StartClientIp` / `StartHostIp` 확장 메서드 정의. 파라미터 검증(playerName/ip/port)을 이 계층에서 수행.
- Core는 이 어셈블리를 참조하지 않는다. 소비자가 IP 연결을 사용하려면 패키지 매니페스트에서 이 어셈블리를 포함한다.

## 9. 샘플

### 9.1 `Samples~/BasicManual/`

- `BasicLobbyBootstrapper` (MonoBehaviour): `Start()`에서 `LobbyBuilder`로 수동 조립, `OnDestroy()`에서 `Dispose()`.
- `BasicLobbyUI`: 생성자/세터 주입(`Bind(LobbyConnection)`). `[Inject]` 없음.
- `SampleSessionPlayerData`: `ISessionPlayerData` 구현체 (기존 유지).
- 의존성: Core + Adapters + ConnectionMethods/IP. **VContainer 참조 없음.**

### 9.2 `Samples~/VContainerIntegration/`

- `VContainerLobbyLifetimeScope`: `Configure(IContainerBuilder)`에서 `LobbyBuilder`를 한 번 호출해 `LobbyConnection`을 컨테이너에 싱글턴 등록.
- `VContainerLobbyUI`: `[Inject] LobbyConnection` 한 줄.
- 핵심 메시지: 패키지는 VContainer 무지. Zenject/Reflex 사용자도 이 샘플을 복사해 포팅 가능.

## 10. 테스트 — `Tests/Editor/`

### 10.1 페이크

- `FakeNetworkFacade`: `INetworkFacade` 구현. 이벤트를 테스트에서 수동 발화할 수 있는 `Raise*` 메서드 노출.
- `FakeLogger`: 메시지를 리스트에 캡처.
- `FakeTickSource`: `Tick()` 메서드로 이벤트 발화.
- `FakeSessionPlayerData`: `ISessionPlayerData` 최소 구현.

### 10.2 커버리지 목표

- `StateMachine.ChangeState<T>()`: 등록된 타입/미등록 타입, Enter/Exit 호출 순서.
- 상태 전이: `OfflineState → StartingHostState → HostingState`, `ClientConnecting → ClientConnected → ClientReconnecting → ClientConnected`.
- `DefaultConnectionApprover`: 최대 인원 초과, 페이로드 누락.
- `SessionManager`: 재접속 시 데이터 보존, 중복 방지.
- `MessageChannel<T>`: Publish/Subscribe/Unsubscribe, `IDisposable` 구독 해제.
- `LobbyBuilder`: 필수 의존성 누락 예외, 중복 `AddState` 예외, `ReplaceState` 정상 동작.

### 10.3 범위 밖

- 실제 `NetworkManager` / 트랜스포트 통합 테스트 (PlayMode 필요, 플레이키).
- 멀티 클라이언트 시나리오.

## 11. 마이그레이션 계획

### 11.1 작업 단계

| 단계 | 작업 | 빌드 그린? |
|---|---|---|
| 1 | 신규 asmdef 생성 (Core/Adapters/IP/Tests.Editor). 기존 asmdef 임시 유지 | ✔ |
| 2 | `Abstractions/` 인터페이스 + 값 객체 작성 | ✔ |
| 3 | `NetcodeNetworkFacade`, `UnityDebugLogger`, `MonoBehaviourTickSource` 구현 | ✔ |
| 4 | PubSub에서 VContainer using 제거 | ✔ |
| 5 | `SessionManager`에서 `ISessionManager` 추출 | ✔ |
| 6 | `StateMachine` + `StateMachineContext` 구현 | ✔ |
| 7 | 6개 상태 클래스를 `[Inject]` → 생성자 주입으로 전환 | ✔ |
| 8 | `DefaultConnectionApprover` 구현 | ✔ |
| 9 | `LobbyBuilder` + `LobbyConnection` 구현 | ✔ |
| 10 | `IPConnectionMethod` 시그니처 변경 + `LobbyConnectionIpExtensions` | ✔ |
| 11 | `LobbyConnectionHost` 구현 | ✔ |
| 12 | 기존 `LobbyConnectionManager` / `UpdateRunner` 삭제, 기존 asmdef 제거 | ✔ |
| 13 | 샘플 A (BasicManual) 신규 작성 | ✔ |
| 14 | 샘플 B (VContainerIntegration) 작성 — 기존 샘플 교체 | ✔ |
| 15 | EditMode 테스트 전체 작성 및 그린 | ✔ |
| 16 | README / CHANGELOG / package.json 업데이트 | ✔ |

### 11.2 제거 대상 (Breaking Change 목록)

- `Runtime/Core/LobbyConnectionManager.cs` — 삭제. 대체: `LobbyConnection` + `LobbyConnectionHost` + `StateMachine`.
- `Runtime/Infrastructure/UpdateRunner.cs` — 삭제. 대체: `ITickSource` + `MonoBehaviourTickSource`.
- 기존 `Runtime/Multiplayer.Lobby.asmdef` — 제거(3개로 분할).
- 모든 `[Inject]` 속성 제거.
- 상태 클래스 전부 생성자 시그니처 변경.
- `ConnectionState.m_ConnectionManager` → `Context` (타입/이름 둘 다 변경).
- `IPConnectionMethod` 생성자 시그니처 변경.
- 기존 `Samples~/LobbyTest/` 폴더 삭제 → 2개 샘플로 교체.

### 11.3 버전 / 문서

- `package.json`: `"version": "0.2.0"`.
- `CHANGELOG.md` 신설. "Breaking rewrite: DI container-agnostic, SOLID restructure" 요약.
- `README.md`: VContainer 언급 제거, 수동 배선 빠른 시작, 확장 포인트 요약 테이블 추가.

### 11.4 위험 요소

- **재연결 로직 회귀**: `ClientReconnectingState`의 의존성이 `[Inject]` → 생성자 주입으로 전환되는 과정에서 버그 발생 가능. EditMode 테스트로 재연결 경로를 명시 커버.
- **Approval 경계**: 기존 `LobbyConnectionManager.ApprovalCheck` 로직이 `IConnectionApprover` + `INetworkFacade.ApprovalCheck` 두 지점으로 이동한다. 통합 시나리오 테스트로 검증.
- **VContainer 샘플 UX**: 외형(`LifetimeScope` 사용)은 유지되지만 내부는 빌더 호출. 기존 샘플 사용자에게 변경 부담 없음을 문서화.

## 12. 이후 확장 (v0.3 이상, 이번 범위 밖)

- `IReconnectStrategy` 인터페이스 승격 (값 객체 → 전략 인터페이스).
- `ITransitionValidator`로 상태 전이 규칙 검증.
- 추가 연결 방식 어셈블리: `ConnectionMethods/Relay`, `ConnectionMethods/Steam` 등.
- PlayMode 통합 테스트 스위트.
- Roslyn 분석기로 Core 어셈블리의 `UnityEngine` 참조 금지 강제.
