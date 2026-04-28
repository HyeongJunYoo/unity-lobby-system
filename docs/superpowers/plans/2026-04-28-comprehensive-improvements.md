# Unity Lobby System v0.2.x → 0.3.0 — 종합 개선 계획 (슬림 안)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 패키지 목적("LAN 환경에서 IP로 접속하는 간단한 로비")에 충실하면서 (1) v0.2.0 리뷰에서 식별된 사용자 가시 결함(재연결 백오프 버그·테스트 인프라·샘플 사용성·문서 함정)을 정리하고, (2) **VContainer 사용 예제를 패키지에서 제거**해 "DI 컨테이너 중립"이라는 개념적 약속만 남긴다.

**Architecture:** 기존 어셈블리 구조와 공개 API는 **그대로 유지**한다. 신규 public API 0건, 신규 인터페이스 0건. 변경 범위는 (1) VContainer 샘플 제거, (2) 버그 수정, (3) 어댑터 단위 테스트가 가능하도록 어셈블리 참조 풀기, (4) 잔존 샘플(BasicManual) 사용성, (5) 내부 정리, (6) 문서화로 한정.

**Tech Stack:** Unity 6000.4 / Netcode for GameObjects 2.11.0 / Unity Transport 2.4.0 / NUnit (Unity Test Runner) / C# 9.

**버전 전략:** VContainer 샘플 제거는 사용자 가시 surface 축소이므로(`package.json`의 `samples` 배열 항목 1개 사라짐) 패치가 아닌 **0.3.0 마이너 bump**. Public API/타입은 변경 없음.

**의도적으로 제외한 항목 (직전 리뷰 결과):**
- `IStateMachineContext` ISP 분리 — 단일 구현/단일 클라이언트 컨텍스트에서 인터페이스 5분할은 YAGNI. 큰 인터페이스 ≠ ISP 위반.
- `TimeoutPolicy` 도입 — UnityTransport가 이미 `ConnectTimeoutMS`/`DisconnectTimeoutMS`를 가지므로 책임 중복(SRP 약화). LAN 로비에서 무한 대기 위험은 사용자 Shutdown 버튼으로 복구 가능.
- PlayMode 통합 테스트 인프라 — 패키지 규모 대비 유지보수 비용이 크다. 코어 회귀는 EditMode + Fake로 충분히 잡힘.
- 샘플 공통 코드 분리 — `Samples~`는 임포트 후 사용자가 가져가는 모델. 의도적 독립성을 유지해야 의존 그래프가 엉키지 않음.

---

## Phase 0: 사전 준비

### Task 0.1: 작업 브랜치 생성

**Files:** 없음 (git 작업)

- [ ] **Step 1: 작업 브랜치 생성**

```bash
git checkout -b feat/v0.3.0-improvements
git status
```

Expected: `On branch feat/v0.3.0-improvements`, clean working tree.

---

### Task 0.2: VContainer 통합 샘플 제거

**문제 설명:** 패키지는 "DI 컨테이너 중립"을 표방한다. 특정 컨테이너(VContainer)에 대한 코드 예제를 패키지에 직접 포함하면 (1) 사용자가 그 컨테이너에 묶여 있다는 인상을 주고, (2) 해당 라이브러리 버전 호환을 따라 유지보수 비용이 든다. 샘플은 `BasicManual` 하나만 남기고, README에는 "다른 DI 컨테이너에도 같은 패턴이 적용된다"는 한 문단만 남긴다.

**Files:**
- Delete: `Samples~/VContainerIntegration/` (디렉터리 + 8개 파일 전체)
- Modify: `package.json` (samples 배열에서 VContainer 항목 제거)
- Modify: `README.md` (3가지 위치 정리: 샘플 목록·"3) VContainer 연동" 섹션·샘플 표)

- [ ] **Step 1: VContainerIntegration 디렉터리 삭제**

```bash
rm -rf Samples~/VContainerIntegration
ls Samples~
```

Expected: `BasicManual`만 남음.

- [ ] **Step 2: package.json의 samples 배열에서 VContainer 항목 제거**

`package.json`의 `samples` 배열을 다음으로 교체:

```json
"samples": [
    {
      "displayName": "Basic Manual Wiring",
      "description": "DI 컨테이너 없이 LobbyBuilder로 직접 조립하는 최소 예제.",
      "path": "Samples~/BasicManual"
    }
]
```

- [ ] **Step 3: README.md의 샘플 임포트 안내 정리**

`README.md`의 "Package Manager의 **Samples** 탭에서 아래 두 샘플을 임포트할 수 있다." 문단을 다음으로 교체:

```markdown
Package Manager의 **Samples** 탭에서 아래 샘플을 임포트할 수 있다.

- **Basic Manual Wiring** — DI 컨테이너 없이 수동 조립.

> 패키지 본체는 DI 컨테이너 중립이다. VContainer / Zenject / Reflex 등의 컨테이너에서도 같은 패턴 — 컨테이너 등록 콜백 안에서 `LobbyBuilder.Build()` 한 번 호출해 `LobbyConnection`을 싱글턴으로 등록 — 으로 통합 가능. 특정 컨테이너 통합 코드는 사용자 프로젝트에 위치시키는 것을 권장한다.
```

- [ ] **Step 4: README.md의 "### 3) VContainer 연동" 섹션 제거**

빠른 시작에서 "### 3) VContainer 연동" 섹션 전체(헤더 + 코드 블록 + Zenject/Reflex 한 줄 안내)를 삭제. 빠른 시작이 1)·2) 두 항목만 남도록.

남는 빠른 시작 직후에 다음 한 단락을 추가:

```markdown
> 다른 DI 컨테이너 사용 시: 컨테이너의 등록 콜백에서 위 2)의 `LobbyBuilder` 체인을 그대로 호출해 `LobbyConnection`을 싱글턴으로 등록하면 된다. 패키지는 컨테이너에 런타임 의존하지 않는다.
```

- [ ] **Step 5: README.md의 "## 샘플" 표에서 VContainer 행 삭제**

해당 표가 다음과 같이 1행만 남도록:

```markdown
## 샘플

| 경로 | 내용 |
|---|---|
| `Samples~/BasicManual/` | `LobbyBuilder`를 수동으로 조립. UI Toolkit 기반 간단 UI(`BasicLobbyUI`) 포함. |
```

- [ ] **Step 6: 컴파일 확인**

Unity Editor에서 어셈블리 재컴파일. 본체와 BasicManual 샘플이 VContainer를 참조하지 않으므로(직전 0.2.0 변경사항) 컴파일 오류 0건이어야 함.

- [ ] **Step 7: 커밋**

```bash
git add -A Samples~/VContainerIntegration package.json README.md
git commit -m "chore(samples): remove VContainerIntegration sample

DI 컨테이너 중립을 표방하면서 특정 컨테이너 코드 예제를 패키지에 포함하는
모순을 해소. Samples~/VContainerIntegration/ 디렉터리 전체 + package.json
samples 항목 + README의 VContainer 코드 블록 제거. 일반 DI 통합 패턴은
README에 한 문단으로만 남김."
```

---

## Phase 1: 즉시 수정 (Critical Bug + 사용성)

본 단계는 **재연결 정책 문서와 실제 동작의 괴리를 제거**하고 **패키지 임포트 직후 막히는 함정을 없애는** 데 집중한다. 모두 작은 단위 수정이며 기존·신규 EditMode 테스트로 회귀를 잡는다.

---

### Task 1.1: 재연결 백오프 진행 한 단계 지연 버그 수정

**문제 설명:**
`ClientReconnectingState.ReconnectRoutine()`은 의도가 `1s → 2s → 4s → ...`이지만, 라인 102-103의 `if (m_NbAttempts == 0) yield return InitialBackoff` 블록과 라인 87-92의 백오프 진행 블록이 **첫 시도와 두 번째 시도 모두 `InitialBackoff`만 대기**하게 만든다. 두 번째 시도부터 multiplier가 적용되도록 수정한다.

**Files:**
- Test: `Tests/Editor/ClientReconnectingStateBackoffTests.cs` (Create)
- Modify: `Tests/Editor/Fakes/FakeCoroutineRunner.cs`
- Modify: `Tests/Editor/Fakes/StateHarness.cs`
- Modify: `Runtime/Core/States/ClientReconnectingState.cs:85-119`

- [ ] **Step 1: FakeCoroutineRunner에 yield 값 추출 헬퍼 추가**

`Tests/Editor/Fakes/FakeCoroutineRunner.cs` 클래스 본문 끝에 추가:

```csharp
/// <summary>현재 활성 루틴을 다음 yield까지 진행하고, yield된 값(double로 캐스팅)을 반환.</summary>
public double PumpToNextYield()
{
    if (m_Routines.Count == 0) return double.NaN;
    var routine = m_Routines[m_Routines.Count - 1];
    if (!routine.MoveNext()) return double.NaN;
    var current = routine.Current;
    return current switch
    {
        double d => d,
        float f  => f,
        int i    => i,
        _        => 0.0
    };
}

/// <summary>현재 활성 루틴을 끝까지 모두 진행한다.</summary>
public void RunRoutineToCompletion()
{
    if (m_Routines.Count == 0) return;
    var routine = m_Routines[m_Routines.Count - 1];
    while (routine.MoveNext()) { }
}
```

**전제:** `FakeCoroutineRunner`가 `m_Routines`라는 `List<IEnumerator>`를 가지고 `Start()`에서 추가하는 형태. 다른 식이면 동일 의미로 어댑트.

- [ ] **Step 2: StateHarness에 정책 파라미터 오버로드 추가**

`Tests/Editor/Fakes/StateHarness.cs`의 정적 `Build` 옆에 오버로드 추가:

```csharp
public static StateHarness Build(ReconnectPolicy policy, params Type[] stateTypes)
{
    var h = new StateHarness();
    h.ReconnectPolicy = policy;
    var states = new Dictionary<Type, ConnectionState>();
    h.Machine = new StateMachine(states, h.Network, h.Logger);
    var ctx = new StateMachineContext(h);
    foreach (var t in stateTypes)
        states[t] = (ConnectionState)Activator.CreateInstance(t, ctx);
    return h;
}
```

- [ ] **Step 3: 실패 테스트 작성**

`Tests/Editor/ClientReconnectingStateBackoffTests.cs` 생성:

```csharp
using System;
using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.States;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class ClientReconnectingStateBackoffTests
    {
        [Test]
        public void Backoff_FollowsPolicy_FirstAttemptUsesInitial_SecondUsesMultiplied()
        {
            var policy = new ReconnectPolicy
            {
                MaxAttempts       = 5,
                InitialBackoff    = TimeSpan.FromSeconds(1),
                MaxBackoff        = TimeSpan.FromSeconds(60),
                BackoffMultiplier = 2.0
            };
            var h = StateHarness.Build(policy,
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));

            h.Machine.Start<OfflineState>();
            var method = new TestConnectionMethod(
                h.Network, h.PayloadSerializer, h.Identity, "C", false);
            h.Machine.StartClient(method);
            h.Network.RaiseClientConnected(0UL);
            h.Network.DisconnectReason = "";
            h.Network.RaiseClientDisconnected(0UL, "");

            Assert.That(h.Machine.CurrentState, Is.InstanceOf<ClientReconnectingState>());

            var first = h.CoroutineRunner.PumpToNextYield();
            Assert.That(first, Is.EqualTo(1.0).Within(0.001),
                "1차 시도는 InitialBackoff(1s)만큼 대기해야 한다");

            h.CoroutineRunner.RunRoutineToCompletion();
            h.Network.RaiseClientDisconnected(0UL, "");
            var second = h.CoroutineRunner.PumpToNextYield();
            Assert.That(second, Is.EqualTo(2.0).Within(0.001),
                "2차 시도는 InitialBackoff * BackoffMultiplier(2s)만큼 대기해야 한다");
        }

        sealed class TestConnectionMethod : ConnectionMethodBase
        {
            public TestConnectionMethod(Multiplayer.Lobby.Abstractions.INetworkFacade net,
                                        Multiplayer.Lobby.Abstractions.IConnectionPayloadSerializer ser,
                                        PlayerIdentity id, string name, bool isDebug)
                : base(net, ser, id, name, isDebug) { }
            public override void SetupHostConnection()   => SetConnectionPayload(GetPlayerId(), m_PlayerName);
            public override void SetupClientConnection() => SetConnectionPayload(GetPlayerId(), m_PlayerName);
            public override System.Threading.Tasks.Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
                => System.Threading.Tasks.Task.FromResult((false, true));
        }
    }
}
```

- [ ] **Step 4: 테스트 실패 확인**

Unity Test Runner → EditMode → `Backoff_FollowsPolicy_FirstAttemptUsesInitial_SecondUsesMultiplied` 실행.
Expected: **FAIL** — `second`가 1.0이 나옴(현재 버그).

- [ ] **Step 5: 버그 수정 — `ReconnectRoutine` 라인 102-103 잉여 블록 제거 및 1차 대기 통합**

`Runtime/Core/States/ClientReconnectingState.cs`의 `ReconnectRoutine` 메서드 전체를 다음으로 교체:

```csharp
IEnumerator ReconnectRoutine()
{
    if (m_NbAttempts > 0)
    {
        var backoff = System.Math.Min(m_NextBackoffSeconds, Context.ReconnectPolicy.MaxBackoff.TotalSeconds);
        yield return backoff;   // Adapter가 WaitForSeconds로 해석
        m_NextBackoffSeconds *= Context.ReconnectPolicy.BackoffMultiplier;
    }
    else
    {
        // 1차 시도도 InitialBackoff만큼 대기 (Enter에서 m_NextBackoffSeconds = InitialBackoff)
        yield return m_NextBackoffSeconds;
        m_NextBackoffSeconds *= Context.ReconnectPolicy.BackoffMultiplier;
    }

    Context.Logger.Info("Lost connection to host, trying to reconnect...");
    Context.Network.Shutdown();
    while (Context.Network.ShutdownInProgress) yield return null;

    Context.Logger.Info($"Reconnecting attempt {m_NbAttempts + 1}/{Context.ReconnectPolicy.MaxAttempts}...");
    Context.ReconnectPublisher.Publish(
        new ReconnectMessage(m_NbAttempts, Context.ReconnectPolicy.MaxAttempts));

    m_NbAttempts++;
    var setupTask = m_ConnectionMethod.SetupClientReconnectionAsync();
    while (!setupTask.IsCompleted) yield return null;

    if (!setupTask.IsFaulted && setupTask.Result.success)
    {
        ConnectClient();
    }
    else
    {
        if (!setupTask.Result.shouldTryAgain)
            m_NbAttempts = Context.ReconnectPolicy.MaxAttempts;
        OnClientDisconnected(0UL, null);
    }
}
```

- [ ] **Step 6: 테스트 통과 확인 + 기존 회귀 없음 확인**

Unity Test Runner → EditMode 전체 실행.
Expected: 새 테스트 PASS, 기존 `StateTransitionTests` 등 모두 PASS.

- [ ] **Step 7: 커밋**

```bash
git add Runtime/Core/States/ClientReconnectingState.cs Tests/Editor/ClientReconnectingStateBackoffTests.cs Tests/Editor/Fakes/FakeCoroutineRunner.cs Tests/Editor/Fakes/StateHarness.cs
git commit -m "fix(states): ClientReconnectingState backoff progression

라인 102-103 잉여 InitialBackoff 대기 제거. 1차/2차 시도가 모두
InitialBackoff(1s)만 대기하던 버그를 수정해 정책(1s -> 2s -> 4s)대로
동작. 누적 backoff 검증 테스트 추가."
```

---

### Task 1.2: 테스트 어셈블리에 Adapters 참조 추가

**문제 설명:** `Multiplayer.Lobby.Tests.Editor.asmdef`가 Core만 참조하므로 어댑터 단위 테스트가 불가능. 회귀 감지선 확보.

**Files:**
- Modify: `Tests/Editor/Multiplayer.Lobby.Tests.Editor.asmdef`
- Create: `Tests/Editor/Adapters/JsonUtilityConnectionPayloadSerializerTests.cs`

- [ ] **Step 1: asmdef 참조 추가**

`Tests/Editor/Multiplayer.Lobby.Tests.Editor.asmdef`의 `references` 배열을 다음으로 교체:

```json
"references": [
    "Multiplayer.Lobby.Core",
    "Multiplayer.Lobby.Adapters",
    "Multiplayer.Lobby.ConnectionMethods.IP",
    "Unity.Netcode.Runtime",
    "Unity.Networking.Transport",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
]
```

- [ ] **Step 2: Unity 컴파일 확인**

Unity Editor에서 어셈블리 재컴파일 대기. Console 컴파일 오류 0건이어야 함.

- [ ] **Step 3: JsonUtility 직렬화 라운드트립 테스트 추가**

`Tests/Editor/Adapters/JsonUtilityConnectionPayloadSerializerTests.cs` 생성:

```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests.Adapters
{
    public class JsonUtilityConnectionPayloadSerializerTests
    {
        [Test]
        public void Serialize_Then_Deserialize_RoundTripsAllFields()
        {
            var sut = new JsonUtilityConnectionPayloadSerializer();
            var src = new ConnectionPayload
            {
                playerId   = "pid-42",
                playerName = "Tester",
                isDebug    = true
            };

            var bytes = sut.Serialize(src);
            var dst   = sut.Deserialize(bytes);

            Assert.That(dst, Is.Not.Null);
            Assert.That(dst.playerId,   Is.EqualTo(src.playerId));
            Assert.That(dst.playerName, Is.EqualTo(src.playerName));
            Assert.That(dst.isDebug,    Is.EqualTo(src.isDebug));
        }

        [Test]
        public void Deserialize_NullOrEmpty_ReturnsNull()
        {
            var sut = new JsonUtilityConnectionPayloadSerializer();
            Assert.That(sut.Deserialize(null), Is.Null);
            Assert.That(sut.Deserialize(System.Array.Empty<byte>()), Is.Null);
        }
    }
}
```

- [ ] **Step 4: 테스트 실행 + 커밋**

Unity Test Runner → EditMode → 새 테스트 2건 PASS.

```bash
git add Tests/Editor/Multiplayer.Lobby.Tests.Editor.asmdef Tests/Editor/Adapters/JsonUtilityConnectionPayloadSerializerTests.cs
git commit -m "test(adapters): enable Adapters references + add Json serializer tests

Tests.Editor asmdef에 Adapters/ConnectionMethods.IP/Netcode/Transport 참조 추가.
어댑터 회귀를 잡을 수 있는 첫 단위 테스트(JsonUtilitySerializer 라운드트립)
도입."
```

---

### Task 1.3: BasicLobbyUI 포트 입력 검증 추가

**문제 설명:** `BasicLobbyUI.cs:119, 124`의 `int.Parse(m_PortField.value)`가 잘못된 입력에 대해 `FormatException`을 그대로 던진다. UI에서 사전 검증.

**Files:**
- Modify: `Samples~/BasicManual/BasicLobbyUI.cs`

- [ ] **Step 1: 포트 파싱·검증 헬퍼 추가**

`BasicLobbyUI.cs`의 `OnHost`/`OnClient` 메서드 위에 추가:

```csharp
bool TryGetPort(out int port)
{
    if (!int.TryParse(m_PortField.value, out port))
    {
        SetStatus($"Invalid port: '{m_PortField.value}' (must be a number)");
        port = 0;
        return false;
    }
    if (port < 1 || port > 65535)
    {
        SetStatus($"Invalid port: {port} (must be 1-65535)");
        return false;
    }
    return true;
}
```

- [ ] **Step 2: `OnHost` / `OnClient`를 헬퍼 사용으로 교체**

```csharp
void OnHost()
{
    if (!TryGetPort(out var port)) return;
    m_Lobby.StartHostIp(m_Nm, m_Identity, m_Serializer,
        m_PlayerNameField.value, m_IpField.value, port,
        Debug.isDebugBuild);
}

void OnClient()
{
    if (!TryGetPort(out var port)) return;
    m_Lobby.StartClientIp(m_Nm, m_Identity, m_Serializer,
        m_PlayerNameField.value, m_IpField.value, port,
        Debug.isDebugBuild);
}
```

- [ ] **Step 3: 컴파일 확인 + 커밋**

```bash
git add Samples~/BasicManual/BasicLobbyUI.cs
git commit -m "fix(samples): BasicLobbyUI guards port input

int.Parse 직접 호출로 빈 입력/문자열에서 FormatException이 그대로
새어나가던 문제 수정. TryParse + 1-65535 범위 검증 후 상태 라벨에
사용자 친화적 메시지를 표시."
```

---

### Task 1.4: BasicLobbyUI 이벤트 구독 정리

**문제 설명:** `Bind()`에서 4개 이상 이벤트를 구독하지만 `OnDestroy`가 없어 `LobbyConnection`이 더 오래 살면 람다가 UI를 잡고 누수 가능.

**Files:**
- Modify: `Samples~/BasicManual/BasicLobbyUI.cs`

- [ ] **Step 1: 구독 핸들러를 필드로 보관 + OnDestroy에서 정리**

기존 `Bind()` 메서드를 다음으로 교체하고, 클래스 본문에 핸들러 필드와 `OnDestroy` 추가:

```csharp
System.Action m_OnHostStarted;
System.Action m_OnClientConnected;
System.Action m_OnDisconnected;
System.IDisposable m_StatusSub;

public void Bind(LobbyConnection lobby, NetworkManager nm, PlayerIdentity identity, IConnectionPayloadSerializer serializer)
{
    m_Lobby = lobby; m_Nm = nm; m_Identity = identity; m_Serializer = serializer;

    m_Doc = GetComponent<UIDocument>();
    BuildUI(m_Doc.rootVisualElement);

    m_HostButton.clicked += OnHost;
    m_ClientButton.clicked += OnClient;
    m_ShutdownButton.clicked += OnShutdown;

    m_OnHostStarted     = () => SetStatus("Host started");
    m_OnClientConnected = () => SetStatus("Client connected");
    m_OnDisconnected    = () => SetStatus("Disconnected");

    m_Lobby.OnHostStarted     += m_OnHostStarted;
    m_Lobby.OnClientConnected += m_OnClientConnected;
    m_Lobby.OnDisconnected    += m_OnDisconnected;

    m_StatusSub = m_Lobby.GetSubscriber<ConnectStatus>()
        .Subscribe(s => SetStatus($"Status: {s}"));
}

void OnDestroy()
{
    if (m_Lobby != null)
    {
        if (m_OnHostStarted     != null) m_Lobby.OnHostStarted     -= m_OnHostStarted;
        if (m_OnClientConnected != null) m_Lobby.OnClientConnected -= m_OnClientConnected;
        if (m_OnDisconnected    != null) m_Lobby.OnDisconnected    -= m_OnDisconnected;
    }
    m_StatusSub?.Dispose();
    m_StatusSub = null;
    if (m_HostButton     != null) m_HostButton.clicked     -= OnHost;
    if (m_ClientButton   != null) m_ClientButton.clicked   -= OnClient;
    if (m_ShutdownButton != null) m_ShutdownButton.clicked -= OnShutdown;
}
```

- [ ] **Step 2: 컴파일 + 커밋**

```bash
git add Samples~/BasicManual/BasicLobbyUI.cs
git commit -m "fix(samples): BasicLobbyUI unsubscribes events on destroy

Bind에서 등록한 LobbyConnection/Button 핸들러와 ConnectStatus 구독을
OnDestroy에서 모두 해제. UIDocument가 LobbyConnection보다 먼저 사라질
때의 잠재적 누수 차단."
```

---

### Task 1.5: BasicManual 샘플 README 추가 (Scene 셋업 가이드)

**문제 설명:** 임포트 직후 사용자가 어떤 GameObject 구성으로 시작해야 할지 모름. Scene 자동 생성 스크립트는 직전 revert(`b93616c`) 정책에 따라 도입하지 않고, 명시적 README로 셋업 절차를 문서화한다.

**Files:**
- Create: `Samples~/BasicManual/README.md`

- [ ] **Step 1: README 작성**

`Samples~/BasicManual/README.md` 생성:

```markdown
# Basic Manual Wiring 샘플

DI 컨테이너 없이 `LobbyBuilder`를 수동으로 조립하는 최소 예제.

## 셋업 절차

1. 새 씬을 만든다 (`File > New Scene > Basic Built-in`).
2. 빈 GameObject를 추가하고 이름을 `Bootstrapper`로 변경.
3. `Bootstrapper`에 다음 컴포넌트를 차례로 추가:
   - `NetworkManager` (`com.unity.netcode.gameobjects` 패키지)
   - `UnityTransport` — `NetworkManager`의 NetworkTransport 슬롯에 자동 연결됨
   - `UIDocument` — `Panel Settings`에 임의의 PanelSettings 에셋 할당
   - `BasicLobbyBootstrapper`
   - `BasicLobbyUI`
4. `BasicLobbyBootstrapper` 인스펙터에서 `NetworkManager` 슬롯에 같은 GameObject 드래그.
5. 씬을 `BasicLobbySample.unity`로 저장.
6. Play를 누르면 UI가 표시된다. `Host` 또는 `Client` 버튼으로 연결 시도.

## 멀티 인스턴스 테스트

같은 머신에서 호스트/클라이언트를 동시에 띄우려면:
- Build를 만들어 호스트로 실행 + Editor를 클라이언트로 사용
- 또는 `MPPM`(Multiplayer Play Mode) 패키지 활용

## DI 컨테이너 사용 시

본 샘플은 컨테이너 없이 동작한다. VContainer / Zenject / Reflex 등을 사용하는 경우
`BasicLobbyBootstrapper.Awake()` 안의 `LobbyBuilder` 체인을 컨테이너 등록 콜백 안으로
옮기면 된다. 패키지는 컨테이너에 런타임 의존하지 않는다.
```

- [ ] **Step 2: 커밋**

```bash
git add Samples~/BasicManual/README.md
git commit -m "docs(samples): BasicManual README with scene setup steps

임포트 직후 사용자가 어떤 GameObject/컴포넌트 조합으로 시작해야 하는지
6단계로 명시. Scene 파일 자동 생성을 회피하면서도(이전 revert 정책 준수)
첫 실행까지의 함정을 제거. DI 컨테이너 사용 가이드도 한 문단으로 포함."
```

---

### Task 1.6: README의 OnConfigure 구독 시점 명시

**문제 설명:** README "빠른 시작 1)"의 `host.OnConfigure += ...`는 `LobbyConnectionHost.Start()`에서 `Build()`가 호출되므로, 사용자가 `Start()` 이후 구독하면 호출되지 않는다.

**Files:**
- Modify: `README.md`

- [ ] **Step 1: 해당 섹션에 주의 박스와 정확한 사용 예제 추가**

`README.md`의 "1) 인스펙터 원샷 배선 (가장 간단)" 섹션을 다음으로 교체:

```markdown
### 1) 인스펙터 원샷 배선 (가장 간단)

씬에 `NetworkManager`를 두고, 그 옆에 `LobbyConnectionHost` 컴포넌트를 얹은 뒤 인스펙터에서 `NetworkManager` 레퍼런스만 연결한다. `MaxPlayers`와 `ReconnectAttempts`도 인스펙터에서 조정 가능.

> ⚠️ `OnConfigure`는 **`LobbyConnectionHost.Start()`보다 먼저 호출되는 시점**(같은 GameObject의 `Awake` 또는 다른 컴포넌트의 `OnEnable`)에서 구독해야 한다. `Start()`가 끝난 뒤 구독하면 `Build()`가 이미 끝나 호출되지 않는다.

```csharp
// 같은 GameObject에 붙은 다른 MonoBehaviour의 Awake에서:
void Awake()
{
    var host = GetComponent<LobbyConnectionHost>();
    host.OnConfigure += builder => builder.UseSessionPlayerDataFactory(
        (id, payload) => new MyPlayerData(id, payload.playerName));
}
// LobbyConnectionHost.Start()에서 Build()가 실행된 뒤 host.Connection 사용 가능
```
```

- [ ] **Step 2: 커밋**

```bash
git add README.md
git commit -m "docs(readme): clarify OnConfigure subscription timing

LobbyConnectionHost.OnConfigure는 Start() 이전에 구독해야 한다는 주의 박스
추가. 'host.Connection이 OnConfigure에서 설정한 팩토리를 무시한다'는 함정
제거."
```

---

## Phase 2: 테스트 보강 (작게)

### Task 2.1: 라이프사이클 메시지 발행 검증 1건 추가 (EditMode)

**문제 설명:** 상태 전이 단위 테스트는 있지만, 상태 진입 시 `LobbyLifecycleMessage` 발행 여부를 직접 검증하는 테스트가 없다. 한 시나리오만 추가해 회귀 감지선을 둔다(이미 발행 중이라면 통과, 누락이면 RED → 발행 추가).

**Files:**
- Create: `Tests/Editor/LobbyLifecyclePublishTests.cs`

- [ ] **Step 1: 테스트 작성**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.States;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class LobbyLifecyclePublishTests
    {
        [Test]
        public void HostStartFlow_PublishesHostStartedLifecycle()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));
            h.Machine.Start<OfflineState>();

            var lifecycle = new List<LobbyLifecycleMessage>();
            using var sub = h.LifecycleChannelPublic.Subscribe(m => lifecycle.Add(m));

            var method = new TestConnectionMethod(
                h.Network, h.PayloadSerializer, h.Identity, "Host", false);
            h.Machine.StartHost(method);
            h.Network.RaiseServerStarted();

            CollectionAssert.Contains(lifecycle, LobbyLifecycleMessage.HostStarted,
                "호스트 시작 성공 시 LobbyLifecycleMessage.HostStarted 발행");
        }

        sealed class TestConnectionMethod : ConnectionMethodBase
        {
            public TestConnectionMethod(Multiplayer.Lobby.Abstractions.INetworkFacade net,
                                        Multiplayer.Lobby.Abstractions.IConnectionPayloadSerializer ser,
                                        PlayerIdentity id, string name, bool isDebug)
                : base(net, ser, id, name, isDebug) { }
            public override void SetupHostConnection()   => SetConnectionPayload(GetPlayerId(), m_PlayerName);
            public override void SetupClientConnection() => SetConnectionPayload(GetPlayerId(), m_PlayerName);
            public override System.Threading.Tasks.Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
                => System.Threading.Tasks.Task.FromResult((true, true));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행**

PASS면 발행 경로가 이미 갖춰져 있는 것 — 그대로 회귀 가드로 둔다.
FAIL이면 `Runtime/Core/States/HostingState.cs`의 `Enter()`에 발행을 추가:

```csharp
public override void Enter()
{
    // ... 기존 코드 ...
    Context.LifecyclePublisher.Publish(LobbyLifecycleMessage.HostStarted);
}
```

- [ ] **Step 3: PASS 확인 + 커밋**

```bash
git add Tests/Editor/LobbyLifecyclePublishTests.cs
# (HostingState.cs에 발행 추가가 필요했다면 함께 add)
git commit -m "test(states): guard LobbyLifecycleMessage.HostStarted publication

호스트 진입 시 LobbyLifecycleMessage가 사라지는 회귀를 잡는 단일
시나리오 테스트 추가."
```

---

## Phase 3: 작은 정리

### Task 3.1: SessionManager 헬퍼 추출 + 일관성 테스트

**문제 설명:** `SessionManager`가 두 Dictionary(`m_ClientIDToPlayerId`, `m_ClientData`)를 분기마다 따로 만지고 있어 향후 desync 위험. 외부 API 변경 없이 내부 헬퍼로 정리.

**Files:**
- Create: `Tests/Editor/SessionManagerSyncTests.cs`
- Modify: `Runtime/Core/Session/SessionManager.cs`

- [ ] **Step 1: 일관성 테스트 작성 (현재 코드도 통과해야 하는 회귀 가드)**

```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class SessionManagerSyncTests
    {
        [Test]
        public void DisconnectBeforeSession_RemovesBothMappings()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(7UL, "pid-1", new FakeSessionPlayerData(7UL, "Alice"));

            sm.DisconnectClient(7UL);

            Assert.That(sm.GetPlayerId(7UL), Is.Null);
            Assert.That(sm.GetPlayerData("pid-1"), Is.Null);
        }

        [Test]
        public void DisconnectAfterSessionStart_KeepsDataMarkedDisconnected()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(7UL, "pid-1", new FakeSessionPlayerData(7UL, "Alice"));
            sm.OnSessionStarted();

            sm.DisconnectClient(7UL);

            var data = sm.GetPlayerData("pid-1");
            Assert.That(data, Is.Not.Null, "세션 시작 후 끊김은 데이터를 유지해야 한다");
            Assert.That(data.IsConnected, Is.False);
        }

        [Test]
        public void OnServerEnded_ClearsBothMappings()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(7UL, "pid-1", new FakeSessionPlayerData(7UL, "Alice"));
            sm.SetupConnectingPlayerSessionData(8UL, "pid-2", new FakeSessionPlayerData(8UL, "Bob"));

            sm.OnServerEnded();

            Assert.That(sm.GetPlayerId(7UL), Is.Null);
            Assert.That(sm.GetPlayerId(8UL), Is.Null);
            Assert.That(sm.GetPlayerData("pid-1"), Is.Null);
            Assert.That(sm.GetPlayerData("pid-2"), Is.Null);
        }
    }
}
```

- [ ] **Step 2: 테스트 PASS 확인 (현재 코드도 통과해야 함)**

PASS 안 되면 현재 코드가 이미 desync 상태 — 다음 스텝의 리팩토링이 더 시급한 회귀 수정이 됨.

- [ ] **Step 3: SessionManager에 Associate/Disassociate 헬퍼 추출**

`Runtime/Core/Session/SessionManager.cs` 본문 끝에 헬퍼 추가:

```csharp
void AssociateUnchecked(ulong clientId, string playerId, ISessionPlayerData data)
{
    m_ClientIDToPlayerId[clientId] = playerId;
    m_ClientData[playerId] = data;
}

void DisassociateUnchecked(ulong clientId)
{
    if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid))
    {
        m_ClientIDToPlayerId.Remove(clientId);
        if (m_ClientData.TryGetValue(pid, out var d) && d.ClientID == clientId)
            m_ClientData.Remove(pid);
    }
}
```

- [ ] **Step 4: 기존 분기를 헬퍼 사용으로 정리**

`SetupConnectingPlayerSessionData` 마지막 두 줄 →
```csharp
AssociateUnchecked(clientId, playerId, data);
```

`DisconnectClient`의 else 분기 →
```csharp
else
{
    DisassociateUnchecked(clientId);
}
```

`ClearDisconnectedPlayersData`의 toClear 루프 →
```csharp
foreach (var id in toClear) DisassociateUnchecked(id);
```

- [ ] **Step 5: 테스트 PASS 재확인 + 커밋**

```bash
git add Runtime/Core/Session/SessionManager.cs Tests/Editor/SessionManagerSyncTests.cs
git commit -m "refactor(session): extract Associate/Disassociate helpers

m_ClientIDToPlayerId와 m_ClientData를 항상 짝으로 갱신하도록 두 헬퍼로
일원화. 분기마다 두 Dict를 따로 만지던 중복 제거. 일관성 회귀 방지
테스트 3건 추가."
```

---

## Phase 4: 문서화

### Task 4.1: 메시징 동시성 정책 문서화

**문제 설명:** `MessageChannelBase`/`BufferedMessageChannel`이 단일 스레드(코루틴) 가정을 명시 안 함. 사용자가 다른 스레드에서 Publish하면 race 발생.

**Files:**
- Modify: `Runtime/Core/Messaging/IMessageChannel.cs`
- Modify: `Runtime/Core/Messaging/MessageChannelBase.cs`

- [ ] **Step 1: IMessageChannel 인터페이스 위에 동시성 계약 XML 주석**

`Runtime/Core/Messaging/IMessageChannel.cs`의 `IPublisher`/`ISubscriber` 인터페이스 위에 동일 주석 추가:

```csharp
/// <summary>
/// 동시성 계약: 모든 Publish/Subscribe/Unsubscribe 호출은 단일 스레드(일반적으로
/// Unity 메인 스레드)에서 일어난다고 가정한다. 다른 스레드에서 호출하려면 호출 측에서
/// 동기화하거나 MessageChannelBase 파생 클래스에 lock을 도입해야 한다.
/// </summary>
public interface IPublisher<in T> { /* ... */ }

/// <summary>
/// 동시성 계약: 모든 Publish/Subscribe/Unsubscribe 호출은 단일 스레드 가정.
/// 자세한 내용은 IPublisher의 주석 참고.
/// </summary>
public interface ISubscriber<out T> { /* ... */ }
```

- [ ] **Step 2: MessageChannelBase 클래스 위에도 추가**

```csharp
/// <summary>
/// PubSub 채널의 베이스. 단일 스레드 사용 가정 — 자세한 계약은 IMessageChannel 참고.
/// </summary>
public abstract class MessageChannelBase<T> : ...
```

- [ ] **Step 3: 커밋**

```bash
git add Runtime/Core/Messaging/IMessageChannel.cs Runtime/Core/Messaging/MessageChannelBase.cs
git commit -m "docs(messaging): document single-threaded concurrency contract

IMessageChannel/MessageChannelBase에 동시성 계약(단일 스레드 가정) 명시.
사용자가 다른 스레드에서 Publish 시도해 race를 만드는 함정 차단."
```

---

### Task 4.2: 공개 API XML 주석 보강 (스코프 한정)

**문제 설명:** XML 주석 커버리지 ~13%. 사용자 IDE에서 즉시 가치가 보이는 가장 핵심적인 진입점만 한 줄 주석.

**Files:**
- Modify: `Runtime/Core/Builder/LobbyBuilder.cs` (+ partial 파일들)
- Modify: `Runtime/Core/Builder/LobbyConnection.cs` (+ partial 파일들)
- Modify: `Runtime/Core/Connection/ReconnectPolicy.cs`
- Modify: `Runtime/Core/Messaging/LobbyLifecycleMessage.cs`

- [ ] **Step 1: LobbyBuilder의 Use* 메서드와 Build에 한 줄 요약**

각 메서드 위에 한 줄. 예시 (실제 메서드 시그니처에 맞춰 12개 모두):

```csharp
/// <summary>네트워크 어댑터(NGO 등)를 주입한다. 필수.</summary>
public LobbyBuilder UseNetwork(INetworkFacade network) { ... }

/// <summary>모든 의존성 검증 후 LobbyConnection을 생성한다. 필수 의존성이 빠지면 ArgumentException.</summary>
public LobbyConnection Build() { ... }
```

- [ ] **Step 2: LobbyConnection의 진입점에 주석**

```csharp
/// <summary>
/// 호스트/클라이언트 시작과 종료, PubSub/이벤트의 단일 진입점.
/// LobbyBuilder.Build()가 반환하며, 종료 시 Dispose 필요.
/// </summary>
public sealed partial class LobbyConnection : System.IDisposable { ... }
```

`StartHostIp`, `StartClientIp`, `RequestShutdown`, `GetPublisher`, `GetSubscriber` 위에도 한 줄씩.

- [ ] **Step 3: ReconnectPolicy 필드 주석**

```csharp
/// <summary>최대 재시도 횟수. 0이면 재연결을 시도하지 않는다.</summary>
public int MaxAttempts { get; init; }

/// <summary>1차 시도 대기 시간. 이후 시도마다 BackoffMultiplier만큼 곱해진다.</summary>
public TimeSpan InitialBackoff { get; init; }

/// <summary>대기 시간 상한. 누적 backoff가 이 값을 넘지 못한다.</summary>
public TimeSpan MaxBackoff { get; init; }

/// <summary>매 시도 후 대기 시간에 곱하는 배수.</summary>
public double BackoffMultiplier { get; init; }
```

- [ ] **Step 4: LobbyLifecycleMessage enum 멤버 주석**

```csharp
public enum LobbyLifecycleMessage
{
    /// <summary>호스트 시작이 성공해 HostingState로 진입한 직후.</summary>
    HostStarted,
    /// <summary>클라이언트가 호스트와 연결되어 ClientConnectedState로 진입한 직후.</summary>
    ClientConnected,
    /// <summary>Online 계열 상태에서 OfflineState로 복귀한 직후.</summary>
    Disconnected
}
```

- [ ] **Step 5: 커밋**

```bash
git add Runtime/Core/Builder Runtime/Core/Connection/ReconnectPolicy.cs Runtime/Core/Messaging/LobbyLifecycleMessage.cs
git commit -m "docs(api): XML comments on public Builder/Connection/Policy/Lifecycle

LobbyBuilder의 Use* 12종, LobbyConnection의 진입점, ReconnectPolicy 필드,
LobbyLifecycleMessage enum 멤버에 한 줄 요약 추가. IDE 툴팁에서 즉시
의도가 보이도록."
```

---

## Phase 5: 릴리즈

### Task 5.1: CHANGELOG 업데이트 + 0.3.0 마이너 릴리즈

본 계획의 변경에는 **public API 변경 0건**이지만 VContainer 샘플이 사라져 사용자 가시 surface가 줄어든다 — 0.3.0 마이너 bump.

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `package.json`
- Modify: `README.md` (버전 표만)

- [ ] **Step 1: CHANGELOG에 0.3.0 섹션 추가**

`CHANGELOG.md`의 `## [0.2.0]` 위에 삽입:

```markdown
## [0.3.0] — 2026-04-28

### Removed
- VContainer 통합 샘플(`Samples~/VContainerIntegration/`) 및 README의 "3) VContainer 연동" 섹션. 패키지는 여전히 DI 컨테이너 중립이며, 사용자가 자신의 컨테이너 등록 콜백 안에서 `LobbyBuilder.Build()`를 호출하면 된다. 패키지에 특정 컨테이너 코드를 포함하지 않아 의존 표면을 축소.

### Fixed
- `ClientReconnectingState`: 1차/2차 시도 모두 `InitialBackoff`만 대기하던 백오프 진행 버그 수정. 이제 정책대로 1s → 2s → 4s 진행.
- `BasicLobbyUI`: 포트 입력 `int.Parse` 직접 호출로 빈 입력에서 `FormatException`이 새던 문제 — `TryParse` + 1-65535 범위 검증으로 교체.
- `BasicLobbyUI`: `OnDestroy`에서 `LobbyConnection` 이벤트·`ConnectStatus` 구독·`Button.clicked` 핸들러를 모두 해제 — 잠재적 누수 차단.

### Added
- 어댑터 단위 테스트 — `JsonUtilityConnectionPayloadSerializer` 라운드트립 / null·empty 처리.
- 라이프사이클 메시지 발행 회귀 테스트 (`LobbyLifecyclePublishTests`).
- SessionManager 두 Dict 일관성 회귀 테스트 (`SessionManagerSyncTests`).
- `Samples~/BasicManual/README.md` — Scene 구성 단계별 안내.

### Changed
- `Tests/Editor/Multiplayer.Lobby.Tests.Editor.asmdef`이 `Adapters` / `ConnectionMethods.IP` / `Unity.Netcode.Runtime` / `Unity.Networking.Transport` 참조 — 어댑터 단위 테스트 가능.
- `SessionManager`: 내부 헬퍼 `AssociateUnchecked` / `DisassociateUnchecked` 추출. 외부 API 동일.
- `IMessageChannel` / `MessageChannelBase`: 단일 스레드 동시성 계약을 XML 주석으로 명시.
- `LobbyBuilder` / `LobbyConnection` / `ReconnectPolicy` / `LobbyLifecycleMessage`: 공개 진입점에 XML 주석 추가.
- `README.md`: `LobbyConnectionHost.OnConfigure` 구독 시점(`Awake`/`OnEnable`)을 명시. 샘플 목록·표에서 VContainer 항목 제거. 일반 DI 통합 안내 한 문단 추가.
```

- [ ] **Step 2: package.json 버전 bump**

`package.json`의 `"version": "0.2.0"` → `"version": "0.3.0"`.

- [ ] **Step 3: README 상단 버전 표 갱신**

`README.md`의 "현재 패키지 버전 | `0.2.0`" → `0.3.0`.

- [ ] **Step 4: 최종 커밋**

```bash
git add CHANGELOG.md package.json README.md
git commit -m "chore(release): 0.3.0

VContainer 샘플 제거(DI 중립 유지), ClientReconnectingState backoff 버그
수정, BasicManual 사용성/누수 정리, 어댑터 테스트 인프라, 문서 함정 제거
묶음. Public API 변경 없음."
```

- [ ] **Step 5: 통합 검증**

Unity Test Runner → EditMode 전체 PASS 확인. Unity Console에 컴파일 오류·경고 0건.

---

## 자체 검토 체크리스트

본 계획 작성 후 점검 결과:

1. **Spec 커버리지** — 직전 슬림화 결정에서 유지하기로 한 항목(H1, H2, H4, M1, M2, M5, L1, L3, L5)과 신규 요구(VContainer 샘플 제거)가 모두 태스크로 매핑됨. 의도적으로 제외한 항목(ISP 분리, TimeoutPolicy, PlayMode 인프라, 샘플 공통 코드)은 본 계획 헤더에 명시.
2. **플레이스홀더 스캔** — "TBD"/"implement later" 없음. 모든 코드 블록은 실제 적용 가능 형태.
3. **타입 일관성** — `FakeCoroutineRunner.PumpToNextYield` / `RunRoutineToCompletion`, `StateHarness.Build(policy, ...)` 오버로드, `AssociateUnchecked` / `DisassociateUnchecked` 시그니처가 모두 등장 위치에서 동일.
4. **공개 API 영향** — 신규 public 타입 0건, 시그니처 변경 0건. VContainer 샘플은 코드 의존 surface가 아니라 임포트 가능 항목 — 0.3.0 마이너 릴리즈가 적합.
5. **잔존 VContainer 흔적 점검** — README의 샘플 목록·"3) VContainer 연동" 섹션·샘플 표 3곳 모두 Task 0.2가 정리. `package.json`의 `samples` 배열도 갱신. VContainerIntegration 디렉터리 삭제로 코드 흔적 0.

---

## 변경 규모 요약

- 코드 삭제: ~120줄 (VContainerIntegration 디렉터리 일체)
- 코드 추가: ~120줄 (BasicLobbyUI 보호·헬퍼·주석)
- 테스트 추가: ~200줄 (4개 테스트 파일)
- 문서 추가: ~80줄 (BasicManual README + README 보강)
- 문서 삭제: ~35줄 (README의 VContainer 섹션·표 행)
- 신규 public API: **0개**
- 제거된 public API: **0개** (VContainer 샘플은 사용자 코드 의존 surface가 아님)
