# Lobby Connection Architecture 구현 계획 (VContainer 제거 + SOLID 정리)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `com.yoojoo97.multiplayer.lobby`를 v0.1.0 (VContainer 결합) → v0.2.0 (DI 컨테이너 중립 + SOLID 정리)로 전환한다.

**Architecture:** Core (순수 C#) / Adapters (Unity·Netcode) / ConnectionMethods (트랜스포트별) 3개 어셈블리. 상태 머신을 타입 키 레지스트리로 전환해 OCP 확장. MonoBehaviour는 얇은 어댑터로만 사용. `LobbyBuilder`가 조립, `LobbyConnection`이 퍼블릭 파사드.

**Tech Stack:** Unity 6000.4+, Netcode for GameObjects 2.11.0, Unity Transport 2.4.0, Unity Test Framework (NUnit), C# 9+.

**Spec:** `docs/superpowers/specs/2026-04-17-lobby-connection-architecture-design.md`

**Conventions:**
- Private fields: `m_PascalCase`. Constants: `k_PascalCase`.
- Namespace base: `Multiplayer.Lobby`. 하위: `.Abstractions`, `.StateMachine`, `.States`, `.Session`, `.Messaging`, `.Connection`, `.Builder`, `.Adapters.Netcode`, `.Adapters.Unity`, `.ConnectionMethods.IP`, `.Tests`.
- 각 태스크는 **커밋으로 종료**. 커밋 메시지는 한국어 convention 따름 (`feat:`, `test:`, `refactor:`, `chore:`, `docs:`).
- Unity Test Framework의 `NUnit.Framework`을 사용하고 `[Test]`로 마킹한다. EditMode 테스트는 `Tests/Editor/`에 위치.

---

## Phase 1: 스캐폴딩

### Task 1: 작업 브랜치 생성 및 상태 확인

**Files:**
- None (git operation only)

- [ ] **Step 1: 새 브랜치 생성**

Run: `git checkout -b refactor/vcontainer-removal-and-solid-cleanup`

- [ ] **Step 2: 워킹 트리 클린 확인**

Run: `git status`
Expected: `nothing to commit, working tree clean` (docs는 main에 이미 커밋됨).

---

### Task 2: 신규 asmdef 5개 생성 (Core / Adapters / ConnectionMethods.IP / Tests.Editor / 2개 샘플)

**Files:**
- Create: `Runtime/Core/Multiplayer.Lobby.Core.asmdef`
- Create: `Runtime/Adapters/Multiplayer.Lobby.Adapters.asmdef`
- Create: `Runtime/ConnectionMethods/IP/Multiplayer.Lobby.ConnectionMethods.IP.asmdef`
- Create: `Tests/Editor/Multiplayer.Lobby.Tests.Editor.asmdef`

기존 `Runtime/Multiplayer.Lobby.asmdef`는 Task 33에서 삭제한다. 그 전까지는 **공존**하며 기존 코드가 계속 컴파일되도록 유지한다.

- [ ] **Step 1: Core asmdef 생성**

Create `Runtime/Core/Multiplayer.Lobby.Core.asmdef`:
```json
{
    "name": "Multiplayer.Lobby.Core",
    "rootNamespace": "Multiplayer.Lobby",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

> `noEngineReferences: true`가 Core의 UnityEngine 의존 금지를 컴파일 타임에 강제한다.

- [ ] **Step 2: Adapters asmdef 생성**

Create `Runtime/Adapters/Multiplayer.Lobby.Adapters.asmdef`:
```json
{
    "name": "Multiplayer.Lobby.Adapters",
    "rootNamespace": "Multiplayer.Lobby.Adapters",
    "references": [
        "Multiplayer.Lobby.Core",
        "Unity.Netcode.Runtime"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: ConnectionMethods.IP asmdef 생성**

Create `Runtime/ConnectionMethods/IP/Multiplayer.Lobby.ConnectionMethods.IP.asmdef`:
```json
{
    "name": "Multiplayer.Lobby.ConnectionMethods.IP",
    "rootNamespace": "Multiplayer.Lobby.ConnectionMethods.IP",
    "references": [
        "Multiplayer.Lobby.Core",
        "Multiplayer.Lobby.Adapters",
        "Unity.Netcode.Runtime",
        "Unity.Networking.Transport"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Tests.Editor asmdef 생성**

Create `Tests/Editor/Multiplayer.Lobby.Tests.Editor.asmdef`:
```json
{
    "name": "Multiplayer.Lobby.Tests.Editor",
    "rootNamespace": "Multiplayer.Lobby.Tests",
    "references": [
        "Multiplayer.Lobby.Core",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5: 빌드 검증 + 커밋**

에디터에서 Assembly reloading이 성공하는지 확인 (에러 없이). 이 시점엔 코드가 전혀 없어 컴파일이 모두 성공해야 한다.

```bash
git add Runtime/Core Runtime/Adapters Runtime/ConnectionMethods Tests
git commit -m "chore: 신규 asmdef 5개 생성 — Core/Adapters/ConnectionMethods.IP/Tests.Editor"
```

---

## Phase 2: 코어 추상화 & 값 객체

### Task 3: 값 객체 4종 작성 (ApprovalRequest, ApprovalResult, ReconnectPolicy, LobbyLifecycleMessage)

**Files:**
- Create: `Runtime/Core/Connection/ApprovalRequest.cs`
- Create: `Runtime/Core/Connection/ApprovalResult.cs`
- Create: `Runtime/Core/Connection/ReconnectPolicy.cs`
- Create: `Runtime/Core/Messaging/LobbyLifecycleMessage.cs`

- [ ] **Step 1: `ApprovalRequest` 작성**

Create `Runtime/Core/Connection/ApprovalRequest.cs`:
```csharp
namespace Multiplayer.Lobby.Connection
{
    public readonly struct ApprovalRequest
    {
        public ulong ClientId { get; }
        public byte[] Payload { get; }
        public int CurrentConnectedCount { get; }

        public ApprovalRequest(ulong clientId, byte[] payload, int currentConnectedCount)
        {
            ClientId = clientId;
            Payload = payload;
            CurrentConnectedCount = currentConnectedCount;
        }
    }
}
```

- [ ] **Step 2: `ApprovalResult` 작성**

Create `Runtime/Core/Connection/ApprovalResult.cs`:
```csharp
namespace Multiplayer.Lobby.Connection
{
    public readonly struct ApprovalResult
    {
        public bool Approved { get; }
        public string Reason { get; }

        ApprovalResult(bool approved, string reason)
        {
            Approved = approved;
            Reason = reason;
        }

        public static ApprovalResult Allow() => new(true, null);
        public static ApprovalResult Deny(string reason) => new(false, reason);
    }
}
```

- [ ] **Step 3: `ReconnectPolicy` 작성**

Create `Runtime/Core/Connection/ReconnectPolicy.cs`:
```csharp
using System;

namespace Multiplayer.Lobby.Connection
{
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
}
```

- [ ] **Step 4: `LobbyLifecycleMessage` 작성**

Create `Runtime/Core/Messaging/LobbyLifecycleMessage.cs`:
```csharp
namespace Multiplayer.Lobby.Messaging
{
    public enum LobbyLifecycleMessage
    {
        HostStarted,
        ClientConnected,
        Disconnected
    }
}
```

- [ ] **Step 5: 컴파일 확인 + 커밋**

에디터에서 Assembly reload 성공 확인.

```bash
git add Runtime/Core/Connection Runtime/Core/Messaging
git commit -m "feat(core): 값 객체 추가 — ApprovalRequest/Result, ReconnectPolicy, LobbyLifecycleMessage"
```

---

### Task 4: `ILobbyLogger` + `NullLogger` + 테스트용 `FakeLogger`

**Files:**
- Create: `Runtime/Core/Abstractions/ILobbyLogger.cs`
- Create: `Runtime/Core/Abstractions/NullLogger.cs`
- Create: `Tests/Editor/Fakes/FakeLogger.cs`
- Test: `Tests/Editor/FakeLoggerTests.cs`

- [ ] **Step 1: `ILobbyLogger` 인터페이스**

Create `Runtime/Core/Abstractions/ILobbyLogger.cs`:
```csharp
namespace Multiplayer.Lobby.Abstractions
{
    public interface ILobbyLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}
```

- [ ] **Step 2: `NullLogger` 구현 (기본값)**

Create `Runtime/Core/Abstractions/NullLogger.cs`:
```csharp
namespace Multiplayer.Lobby.Abstractions
{
    public sealed class NullLogger : ILobbyLogger
    {
        public static readonly NullLogger Instance = new();
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }
}
```

- [ ] **Step 3: `FakeLogger` 테스트용 구현**

Create `Tests/Editor/Fakes/FakeLogger.cs`:
```csharp
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeLogger : ILobbyLogger
    {
        public readonly List<string> Infos    = new();
        public readonly List<string> Warnings = new();
        public readonly List<string> Errors   = new();

        public void Info(string message)    => Infos.Add(message);
        public void Warning(string message) => Warnings.Add(message);
        public void Error(string message)   => Errors.Add(message);

        public void Clear()
        {
            Infos.Clear();
            Warnings.Clear();
            Errors.Clear();
        }
    }
}
```

- [ ] **Step 4: 실패 테스트 작성**

Create `Tests/Editor/FakeLoggerTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeLoggerTests
    {
        [Test]
        public void CapturesMessagesByLevel()
        {
            var logger = new FakeLogger();

            logger.Info("hello");
            logger.Warning("careful");
            logger.Error("boom");

            Assert.That(logger.Infos,    Is.EqualTo(new[] { "hello" }));
            Assert.That(logger.Warnings, Is.EqualTo(new[] { "careful" }));
            Assert.That(logger.Errors,   Is.EqualTo(new[] { "boom" }));
        }

        [Test]
        public void ClearResetsAllLists()
        {
            var logger = new FakeLogger();
            logger.Info("x"); logger.Warning("y"); logger.Error("z");

            logger.Clear();

            Assert.That(logger.Infos,    Is.Empty);
            Assert.That(logger.Warnings, Is.Empty);
            Assert.That(logger.Errors,   Is.Empty);
        }
    }
}
```

- [ ] **Step 5: 테스트 그린 확인 + 커밋**

에디터 Test Runner에서 `FakeLoggerTests` 2개 통과 확인.

```bash
git add Runtime/Core/Abstractions Tests/Editor/Fakes Tests/Editor/FakeLoggerTests.cs
git commit -m "feat(core): ILobbyLogger/NullLogger 추가 + FakeLogger 테스트"
```

---

### Task 5: `ITickSource` + `FakeTickSource`

**Files:**
- Create: `Runtime/Core/Abstractions/ITickSource.cs`
- Create: `Tests/Editor/Fakes/FakeTickSource.cs`
- Test: `Tests/Editor/FakeTickSourceTests.cs`

- [ ] **Step 1: `ITickSource` 인터페이스**

Create `Runtime/Core/Abstractions/ITickSource.cs`:
```csharp
using System;

namespace Multiplayer.Lobby.Abstractions
{
    public interface ITickSource
    {
        event Action OnUpdate;
        event Action OnLateUpdate;
    }
}
```

- [ ] **Step 2: `FakeTickSource` 작성**

Create `Tests/Editor/Fakes/FakeTickSource.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeTickSource : ITickSource
    {
        public event Action OnUpdate;
        public event Action OnLateUpdate;
        public void TickUpdate()     => OnUpdate?.Invoke();
        public void TickLateUpdate() => OnLateUpdate?.Invoke();
    }
}
```

- [ ] **Step 3: 실패 테스트 작성 + 그린 확인**

Create `Tests/Editor/FakeTickSourceTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeTickSourceTests
    {
        [Test]
        public void TickUpdateFiresOnUpdate()
        {
            var src = new FakeTickSource();
            var count = 0;
            src.OnUpdate += () => count++;

            src.TickUpdate();
            src.TickUpdate();

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void TickLateUpdateFiresOnLateUpdate()
        {
            var src = new FakeTickSource();
            var count = 0;
            src.OnLateUpdate += () => count++;

            src.TickLateUpdate();

            Assert.That(count, Is.EqualTo(1));
        }
    }
}
```

Test Runner에서 통과 확인.

- [ ] **Step 4: 커밋**

```bash
git add Runtime/Core/Abstractions/ITickSource.cs Tests/Editor/Fakes/FakeTickSource.cs Tests/Editor/FakeTickSourceTests.cs
git commit -m "feat(core): ITickSource + FakeTickSource 추가"
```

---

### Task 6: `ICoroutineRunner` + `FakeCoroutineRunner`

**Files:**
- Create: `Runtime/Core/Abstractions/ICoroutineRunner.cs`
- Create: `Tests/Editor/Fakes/FakeCoroutineRunner.cs`
- Test: `Tests/Editor/FakeCoroutineRunnerTests.cs`

- [ ] **Step 1: `ICoroutineRunner` 인터페이스**

Create `Runtime/Core/Abstractions/ICoroutineRunner.cs`:
```csharp
using System.Collections;

namespace Multiplayer.Lobby.Abstractions
{
    public interface ICoroutineRunner
    {
        object Start(IEnumerator routine);
        void Stop(object handle);
    }
}
```

- [ ] **Step 2: `FakeCoroutineRunner` 작성**

Create `Tests/Editor/Fakes/FakeCoroutineRunner.cs`:
```csharp
using System.Collections;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeCoroutineRunner : ICoroutineRunner
    {
        readonly List<IEnumerator> m_Running = new();

        public object Start(IEnumerator routine)
        {
            m_Running.Add(routine);
            return routine;
        }

        public void Stop(object handle)
        {
            if (handle is IEnumerator e)
                m_Running.Remove(e);
        }

        /// <summary>
        /// 모든 실행 중인 루틴에 한 스텝(MoveNext) 진행. 완료된 루틴은 목록에서 제거.
        /// </summary>
        public void AdvanceAll()
        {
            for (var i = m_Running.Count - 1; i >= 0; i--)
            {
                if (!m_Running[i].MoveNext())
                    m_Running.RemoveAt(i);
            }
        }

        public int RunningCount => m_Running.Count;
    }
}
```

- [ ] **Step 3: 실패 테스트 작성 + 그린 확인**

Create `Tests/Editor/FakeCoroutineRunnerTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeCoroutineRunnerTests
    {
        [Test]
        public void StartAddsRoutineAndAdvanceProgresses()
        {
            var runner = new FakeCoroutineRunner();
            var steps = 0;

            runner.Start(ThreeSteps(() => steps++));
            Assert.That(runner.RunningCount, Is.EqualTo(1));

            runner.AdvanceAll(); // step 1
            runner.AdvanceAll(); // step 2
            runner.AdvanceAll(); // step 3
            runner.AdvanceAll(); // routine completes (MoveNext returns false)

            Assert.That(steps, Is.EqualTo(3));
            Assert.That(runner.RunningCount, Is.EqualTo(0));
        }

        [Test]
        public void StopRemovesRoutineBeforeCompletion()
        {
            var runner = new FakeCoroutineRunner();
            var handle = runner.Start(ThreeSteps(() => { }));

            runner.Stop(handle);

            Assert.That(runner.RunningCount, Is.EqualTo(0));
        }

        static IEnumerator ThreeSteps(System.Action onStep)
        {
            onStep(); yield return null;
            onStep(); yield return null;
            onStep(); yield return null;
        }
    }
}
```

- [ ] **Step 4: 커밋**

```bash
git add Runtime/Core/Abstractions/ICoroutineRunner.cs Tests/Editor/Fakes/FakeCoroutineRunner.cs Tests/Editor/FakeCoroutineRunnerTests.cs
git commit -m "feat(core): ICoroutineRunner + FakeCoroutineRunner 추가"
```

---

### Task 7: `IConnectionPayloadSerializer` + `ConnectionPayload` Core 이전

**Files:**
- Create: `Runtime/Core/Abstractions/IConnectionPayloadSerializer.cs`
- Create: `Runtime/Core/Connection/ConnectionPayload.cs`
- Create: `Tests/Editor/Fakes/FakeConnectionPayloadSerializer.cs`
- Test: `Tests/Editor/FakeConnectionPayloadSerializerTests.cs`

- [ ] **Step 1: `ConnectionPayload` Core로 복사**

Create `Runtime/Core/Connection/ConnectionPayload.cs`:
```csharp
using System;

namespace Multiplayer.Lobby.Connection
{
    [Serializable]
    public class ConnectionPayload
    {
        public string playerId;
        public string playerName;
        public bool isDebug;
    }
}
```

> 기존 `Runtime/Core/ConnectionPayload.cs` (namespace `Multiplayer.Lobby`)는 유지. Task 33에서 삭제.

- [ ] **Step 2: 인터페이스 작성**

Create `Runtime/Core/Abstractions/IConnectionPayloadSerializer.cs`:
```csharp
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Abstractions
{
    public interface IConnectionPayloadSerializer
    {
        byte[] Serialize(ConnectionPayload payload);
        ConnectionPayload Deserialize(byte[] bytes);
    }
}
```

- [ ] **Step 3: `FakeConnectionPayloadSerializer` 작성**

Create `Tests/Editor/Fakes/FakeConnectionPayloadSerializer.cs`:
```csharp
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeConnectionPayloadSerializer : IConnectionPayloadSerializer
    {
        readonly Dictionary<int, ConnectionPayload> m_Cache = new();
        int m_NextId;

        public byte[] Serialize(ConnectionPayload payload)
        {
            var id = m_NextId++;
            m_Cache[id] = payload;
            return System.BitConverter.GetBytes(id);
        }

        public ConnectionPayload Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return null;
            var id = System.BitConverter.ToInt32(bytes, 0);
            return m_Cache.TryGetValue(id, out var p) ? p : null;
        }
    }
}
```

- [ ] **Step 4: 라운드트립 테스트**

Create `Tests/Editor/FakeConnectionPayloadSerializerTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeConnectionPayloadSerializerTests
    {
        [Test]
        public void RoundTripsPayload()
        {
            var ser = new FakeConnectionPayloadSerializer();
            var payload = new ConnectionPayload { playerId = "p1", playerName = "Alice", isDebug = true };

            var bytes = ser.Serialize(payload);
            var restored = ser.Deserialize(bytes);

            Assert.That(restored.playerId, Is.EqualTo("p1"));
            Assert.That(restored.playerName, Is.EqualTo("Alice"));
            Assert.That(restored.isDebug, Is.True);
        }

        [Test]
        public void DeserializeUnknownBytesReturnsNull()
        {
            var ser = new FakeConnectionPayloadSerializer();
            var restored = ser.Deserialize(new byte[] { 99, 0, 0, 0 });
            Assert.That(restored, Is.Null);
        }
    }
}
```

Test Runner 통과 확인.

- [ ] **Step 5: 커밋**

```bash
git add Runtime/Core/Abstractions/IConnectionPayloadSerializer.cs Runtime/Core/Connection/ConnectionPayload.cs Tests/Editor/Fakes/FakeConnectionPayloadSerializer.cs Tests/Editor/FakeConnectionPayloadSerializerTests.cs
git commit -m "feat(core): IConnectionPayloadSerializer + ConnectionPayload Core 이전"
```

---

### Task 8: `IPlayerIdentityStore` + `InMemoryPlayerIdentityStore`

**Files:**
- Create: `Runtime/Core/Abstractions/IPlayerIdentityStore.cs`
- Create: `Tests/Editor/Fakes/InMemoryPlayerIdentityStore.cs`
- Test: `Tests/Editor/InMemoryPlayerIdentityStoreTests.cs`

- [ ] **Step 1: 인터페이스 작성**

Create `Runtime/Core/Abstractions/IPlayerIdentityStore.cs`:
```csharp
namespace Multiplayer.Lobby.Abstractions
{
    public interface IPlayerIdentityStore
    {
        /// <summary>Profile별 영구 GUID 반환. 없으면 생성해 저장 후 반환.</summary>
        string GetOrCreateGuid(string profile);

        /// <summary>현재 환경의 프로필 결정 (커맨드라인, 에디터 해시 등). 빈 문자열도 유효.</summary>
        string ResolveProfile();
    }
}
```

- [ ] **Step 2: `InMemoryPlayerIdentityStore` 작성**

Create `Tests/Editor/Fakes/InMemoryPlayerIdentityStore.cs`:
```csharp
using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class InMemoryPlayerIdentityStore : IPlayerIdentityStore
    {
        readonly Dictionary<string, string> m_Guids = new();
        public string Profile { get; set; } = "";

        public string GetOrCreateGuid(string profile)
        {
            var key = profile ?? "";
            if (!m_Guids.TryGetValue(key, out var g))
            {
                g = Guid.NewGuid().ToString();
                m_Guids[key] = g;
            }
            return g;
        }

        public string ResolveProfile() => Profile;
    }
}
```

- [ ] **Step 3: 테스트 작성 및 그린**

Create `Tests/Editor/InMemoryPlayerIdentityStoreTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class InMemoryPlayerIdentityStoreTests
    {
        [Test]
        public void SameProfileReturnsSameGuid()
        {
            var s = new InMemoryPlayerIdentityStore();
            Assert.That(s.GetOrCreateGuid("A"), Is.EqualTo(s.GetOrCreateGuid("A")));
        }

        [Test]
        public void DifferentProfilesReturnDifferentGuids()
        {
            var s = new InMemoryPlayerIdentityStore();
            Assert.That(s.GetOrCreateGuid("A"), Is.Not.EqualTo(s.GetOrCreateGuid("B")));
        }

        [Test]
        public void NullProfileTreatedAsEmpty()
        {
            var s = new InMemoryPlayerIdentityStore();
            Assert.That(s.GetOrCreateGuid(null), Is.EqualTo(s.GetOrCreateGuid("")));
        }
    }
}
```

- [ ] **Step 4: 커밋**

```bash
git add Runtime/Core/Abstractions/IPlayerIdentityStore.cs Tests/Editor/Fakes/InMemoryPlayerIdentityStore.cs Tests/Editor/InMemoryPlayerIdentityStoreTests.cs
git commit -m "feat(core): IPlayerIdentityStore + InMemory 테스트 더블"
```

---

### Task 9: `INetworkFacade` + `FakeNetworkFacade`

**Files:**
- Create: `Runtime/Core/Abstractions/INetworkFacade.cs`
- Create: `Tests/Editor/Fakes/FakeNetworkFacade.cs`
- Test: `Tests/Editor/FakeNetworkFacadeTests.cs`

- [ ] **Step 1: 인터페이스 작성**

Create `Runtime/Core/Abstractions/INetworkFacade.cs`:
```csharp
using System;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Abstractions
{
    public interface INetworkFacade
    {
        bool IsClient { get; }
        bool IsServer { get; }
        bool IsHost { get; }
        bool IsListening { get; }
        bool ShutdownInProgress { get; }
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

        /// <summary>
        /// 단일 핸들러 규약. StateMachine이 정확히 한 개 구독. 어댑터는 마지막 반환값 사용.
        /// </summary>
        event Func<ApprovalRequest, ApprovalResult> ApprovalCheck;
    }
}
```

- [ ] **Step 2: `FakeNetworkFacade` 작성**

Create `Tests/Editor/Fakes/FakeNetworkFacade.cs`:
```csharp
using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeNetworkFacade : INetworkFacade
    {
        public bool IsClient { get; set; }
        public bool IsServer { get; set; }
        public bool IsHost { get; set; }
        public bool IsListening { get; set; }
        public bool ShutdownInProgress { get; set; }
        public ulong LocalClientId { get; set; } = 0UL;
        public byte[] ConnectionPayload { get; set; }
        public string DisconnectReason { get; set; } = string.Empty;

        public int StartClientCalls { get; private set; }
        public int StartHostCalls { get; private set; }
        public int ShutdownCalls { get; private set; }
        public List<(ulong id, string reason)> Disconnects { get; } = new();

        public bool StartClientReturnValue { get; set; } = true;
        public bool StartHostReturnValue { get; set; } = true;

        public bool StartClient() { StartClientCalls++; return StartClientReturnValue; }
        public bool StartHost()   { StartHostCalls++;   return StartHostReturnValue; }

        public void Shutdown(bool discardMessageQueue = false)
        {
            ShutdownCalls++;
            ShutdownInProgress = false;
        }

        public void DisconnectClient(ulong clientId, string reason = null) => Disconnects.Add((clientId, reason));
        public string GetDisconnectReason(ulong clientId) => DisconnectReason;

        public event Action OnServerStarted;
        public event Action<bool> OnServerStopped;
        public event Action OnTransportFailure;
        public event Action<ulong> OnClientConnected;
        public event Action<ulong, string> OnClientDisconnected;
        public event Func<ApprovalRequest, ApprovalResult> ApprovalCheck;

        public void RaiseServerStarted()                                => OnServerStarted?.Invoke();
        public void RaiseServerStopped(bool isHost)                     => OnServerStopped?.Invoke(isHost);
        public void RaiseTransportFailure()                             => OnTransportFailure?.Invoke();
        public void RaiseClientConnected(ulong id)                      => OnClientConnected?.Invoke(id);
        public void RaiseClientDisconnected(ulong id, string reason)    => OnClientDisconnected?.Invoke(id, reason);
        public ApprovalResult RaiseApprovalCheck(ApprovalRequest req)   => ApprovalCheck?.Invoke(req) ?? ApprovalResult.Allow();
    }
}
```

- [ ] **Step 3: 최소 sanity 테스트**

Create `Tests/Editor/FakeNetworkFacadeTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class FakeNetworkFacadeTests
    {
        [Test]
        public void StartClientIncrementsCallCount()
        {
            var f = new FakeNetworkFacade();
            Assert.That(f.StartClient(), Is.True);
            Assert.That(f.StartClientCalls, Is.EqualTo(1));
        }

        [Test]
        public void RaiseApprovalCheckReturnsSubscribedResult()
        {
            var f = new FakeNetworkFacade();
            f.ApprovalCheck += _ => ApprovalResult.Deny("nope");
            var r = f.RaiseApprovalCheck(new ApprovalRequest(1, new byte[0], 0));
            Assert.That(r.Approved, Is.False);
            Assert.That(r.Reason, Is.EqualTo("nope"));
        }

        [Test]
        public void DisconnectClientRecordsCall()
        {
            var f = new FakeNetworkFacade();
            f.DisconnectClient(42, "bye");
            Assert.That(f.Disconnects[0].id, Is.EqualTo(42UL));
            Assert.That(f.Disconnects[0].reason, Is.EqualTo("bye"));
        }
    }
}
```

- [ ] **Step 4: 커밋**

```bash
git add Runtime/Core/Abstractions/INetworkFacade.cs Tests/Editor/Fakes/FakeNetworkFacade.cs Tests/Editor/FakeNetworkFacadeTests.cs
git commit -m "feat(core): INetworkFacade + FakeNetworkFacade 추가"
```

---

### Task 10: `IConnectionApprover` + `DefaultConnectionApprover` (TDD) + `ConnectStatus` Core 이전

**Files:**
- Create: `Runtime/Core/Abstractions/IConnectionApprover.cs`
- Create: `Runtime/Core/Connection/ConnectStatus.cs`
- Create: `Runtime/Core/Connection/DefaultConnectionApprover.cs`
- Test: `Tests/Editor/DefaultConnectionApproverTests.cs`

- [ ] **Step 1: `ConnectStatus` Core 복사 (승인 로직이 참조)**

Create `Runtime/Core/Connection/ConnectStatus.cs`:
```csharp
namespace Multiplayer.Lobby.Connection
{
    public enum ConnectStatus
    {
        Undefined,
        Success,
        ServerFull,
        LoggedInAgain,
        UserRequestedDisconnect,
        GenericDisconnect,
        Reconnecting,
        IncompatibleBuildType,
        HostEndedSession,
        StartHostFailed,
        StartClientFailed
    }
}
```

> 기존 `Runtime/Core/ConnectStatus.cs`는 Task 33에서 삭제.

- [ ] **Step 2: 인터페이스 작성**

Create `Runtime/Core/Abstractions/IConnectionApprover.cs`:
```csharp
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Abstractions
{
    public interface IConnectionApprover
    {
        ApprovalResult Approve(ApprovalRequest request);
    }
}
```

- [ ] **Step 3: 실패 테스트 작성**

Create `Tests/Editor/DefaultConnectionApproverTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests
{
    public class DefaultConnectionApproverTests
    {
        [Test]
        public void AllowsWhenUnderMaxPlayers()
        {
            var a = new DefaultConnectionApprover(maxPlayers: 8);
            var req = new ApprovalRequest(1, new byte[] { 1 }, currentConnectedCount: 3);
            Assert.That(a.Approve(req).Approved, Is.True);
        }

        [Test]
        public void DeniesWhenAtMaxPlayers()
        {
            var a = new DefaultConnectionApprover(maxPlayers: 8);
            var req = new ApprovalRequest(1, new byte[] { 1 }, currentConnectedCount: 8);
            var r = a.Approve(req);
            Assert.That(r.Approved, Is.False);
            Assert.That(r.Reason, Is.EqualTo(ConnectStatus.ServerFull.ToString()));
        }

        [Test]
        public void DeniesWhenPayloadEmpty()
        {
            var a = new DefaultConnectionApprover(maxPlayers: 8);
            var req = new ApprovalRequest(1, new byte[0], currentConnectedCount: 0);
            Assert.That(a.Approve(req).Approved, Is.False);
        }

        [Test]
        public void DeniesWhenPayloadExceedsMax()
        {
            var a = new DefaultConnectionApprover(maxPlayers: 8, maxPayloadBytes: 16);
            var req = new ApprovalRequest(1, new byte[17], currentConnectedCount: 0);
            Assert.That(a.Approve(req).Approved, Is.False);
        }
    }
}
```

- [ ] **Step 4: 테스트 실패 확인**

Test Runner에서 컴파일 실패 확인 (아직 `DefaultConnectionApprover` 없음).

- [ ] **Step 5: 최소 구현**

Create `Runtime/Core/Connection/DefaultConnectionApprover.cs`:
```csharp
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Connection
{
    public sealed class DefaultConnectionApprover : IConnectionApprover
    {
        const int k_DefaultMaxPayloadBytes = 1024;

        readonly int m_MaxPlayers;
        readonly int m_MaxPayloadBytes;

        public DefaultConnectionApprover(int maxPlayers, int maxPayloadBytes = k_DefaultMaxPayloadBytes)
        {
            m_MaxPlayers = maxPlayers;
            m_MaxPayloadBytes = maxPayloadBytes;
        }

        public ApprovalResult Approve(ApprovalRequest request)
        {
            if (request.Payload == null || request.Payload.Length == 0)
                return ApprovalResult.Deny(ConnectStatus.GenericDisconnect.ToString());

            if (request.Payload.Length > m_MaxPayloadBytes)
                return ApprovalResult.Deny(ConnectStatus.GenericDisconnect.ToString());

            if (request.CurrentConnectedCount >= m_MaxPlayers)
                return ApprovalResult.Deny(ConnectStatus.ServerFull.ToString());

            return ApprovalResult.Allow();
        }
    }
}
```

- [ ] **Step 6: 테스트 그린 + 커밋**

Test Runner 4개 그린 확인.

```bash
git add Runtime/Core/Abstractions/IConnectionApprover.cs Runtime/Core/Connection/ConnectStatus.cs Runtime/Core/Connection/DefaultConnectionApprover.cs Tests/Editor/DefaultConnectionApproverTests.cs
git commit -m "feat(core): IConnectionApprover + DefaultConnectionApprover (TDD)"
```

---

### Task 11: 나머지 Connection 타입 Core 이전 (`ReconnectMessage`, `ConnectionEventMessage`, `PlayerIdentity`, `ConnectionMethodBase`)

**Files:**
- Create: `Runtime/Core/Connection/ReconnectMessage.cs`
- Create: `Runtime/Core/Connection/ConnectionEventMessage.cs`
- Create: `Runtime/Core/Connection/PlayerIdentity.cs`
- Create: `Runtime/Core/Connection/ConnectionMethodBase.cs`
- Test: `Tests/Editor/PlayerIdentityTests.cs`

- [ ] **Step 1: 메시지 타입 이전**

Create `Runtime/Core/Connection/ReconnectMessage.cs`:
```csharp
namespace Multiplayer.Lobby.Connection
{
    public struct ReconnectMessage
    {
        public int CurrentAttempt;
        public int MaxAttempt;
        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }
}
```

Create `Runtime/Core/Connection/ConnectionEventMessage.cs`:
```csharp
namespace Multiplayer.Lobby.Connection
{
    public struct ConnectionEventMessage
    {
        public ConnectStatus ConnectStatus;
        public string PlayerName;
    }
}
```

- [ ] **Step 2: `PlayerIdentity` 새 버전 (IPlayerIdentityStore 주입)**

Create `Runtime/Core/Connection/PlayerIdentity.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Connection
{
    public sealed class PlayerIdentity
    {
        readonly IPlayerIdentityStore m_Store;
        string m_Profile;
        string m_Guid;

        public event Action OnProfileChanged;

        public PlayerIdentity(IPlayerIdentityStore store)
        {
            m_Store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public string Profile
        {
            get
            {
                if (m_Profile == null) m_Profile = m_Store.ResolveProfile() ?? "";
                return m_Profile;
            }
            set
            {
                m_Profile = value ?? "";
                m_Guid = null;
                OnProfileChanged?.Invoke();
            }
        }

        public string GetOrCreateGuid()
        {
            if (m_Guid != null) return m_Guid;
            m_Guid = m_Store.GetOrCreateGuid(Profile);
            return m_Guid;
        }

        public string GetPlayerId() => GetOrCreateGuid() + Profile;
    }
}
```

- [ ] **Step 3: `PlayerIdentity` 테스트**

Create `Tests/Editor/PlayerIdentityTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class PlayerIdentityTests
    {
        [Test]
        public void PlayerIdCombinesGuidAndProfile()
        {
            var store = new InMemoryPlayerIdentityStore { Profile = "X" };
            var id = new PlayerIdentity(store);
            var pid = id.GetPlayerId();
            Assert.That(pid, Does.EndWith("X"));
        }

        [Test]
        public void ChangingProfileResetsGuidAndRaisesEvent()
        {
            var store = new InMemoryPlayerIdentityStore { Profile = "A" };
            var id = new PlayerIdentity(store);
            var first = id.GetOrCreateGuid();

            var raised = false;
            id.OnProfileChanged += () => raised = true;
            id.Profile = "B";
            var second = id.GetOrCreateGuid();

            Assert.That(raised, Is.True);
            Assert.That(second, Is.Not.EqualTo(first));
        }
    }
}
```

Test Runner 2개 그린 확인.

- [ ] **Step 4: `ConnectionMethodBase` 새 버전**

Create `Runtime/Core/Connection/ConnectionMethodBase.cs`:
```csharp
using System.Threading.Tasks;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Connection
{
    public abstract class ConnectionMethodBase
    {
        protected readonly INetworkFacade m_Network;
        protected readonly IConnectionPayloadSerializer m_Serializer;
        protected readonly PlayerIdentity m_PlayerIdentity;
        protected readonly string m_PlayerName;
        protected readonly bool m_IsDebug;

        protected ConnectionMethodBase(
            INetworkFacade network,
            IConnectionPayloadSerializer serializer,
            PlayerIdentity playerIdentity,
            string playerName,
            bool isDebug)
        {
            m_Network = network;
            m_Serializer = serializer;
            m_PlayerIdentity = playerIdentity;
            m_PlayerName = playerName;
            m_IsDebug = isDebug;
        }

        public abstract void SetupHostConnection();
        public abstract void SetupClientConnection();
        public abstract Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync();

        public virtual bool RequiresManualNetworkStart => true;

        protected void SetConnectionPayload(string playerId, string playerName)
        {
            var payload = new ConnectionPayload
            {
                playerId = playerId,
                playerName = playerName,
                isDebug = m_IsDebug
            };
            m_Network.ConnectionPayload = m_Serializer.Serialize(payload);
        }

        protected string GetPlayerId() => m_PlayerIdentity.GetPlayerId();
    }
}
```

- [ ] **Step 5: 커밋**

```bash
git add Runtime/Core/Connection/ReconnectMessage.cs Runtime/Core/Connection/ConnectionEventMessage.cs Runtime/Core/Connection/PlayerIdentity.cs Runtime/Core/Connection/ConnectionMethodBase.cs Tests/Editor/PlayerIdentityTests.cs
git commit -m "feat(core): 메시지 + PlayerIdentity + ConnectionMethodBase Core 이전"
```

---

### Task 12: PubSub Core 이전 + 테스트

**Files:**
- Create: `Runtime/Core/Messaging/IMessageChannel.cs`
- Create: `Runtime/Core/Messaging/MessageChannelBase.cs`
- Create: `Runtime/Core/Messaging/MessageChannel.cs`
- Create: `Runtime/Core/Messaging/BufferedMessageChannel.cs`
- Create: `Runtime/Core/Messaging/DisposableSubscription.cs`
- Test: `Tests/Editor/MessageChannelTests.cs`

> `NetworkedMessageChannel`은 Netcode에 의존하므로 Task 28(Adapters 단계)에서 `Runtime/Adapters/Netcode/`로 이전한다. 이 태스크에선 나머지만 이전.

- [ ] **Step 1: 인터페이스 이전**

Create `Runtime/Core/Messaging/IMessageChannel.cs`:
```csharp
using System;

namespace Multiplayer.Lobby.Messaging
{
    public interface IPublisher<T> { void Publish(T message); }

    public interface ISubscriber<T>
    {
        IDisposable Subscribe(Action<T> handler);
        void Unsubscribe(Action<T> handler);
    }

    public interface IMessageChannel<T> : IPublisher<T>, ISubscriber<T>, IDisposable
    {
        bool IsDisposed { get; }
    }

    public interface IBufferedMessageChannel<T> : IMessageChannel<T>
    {
        bool HasBufferedMessage { get; }
        T BufferedMessage { get; }
    }
}
```

- [ ] **Step 2: `MessageChannelBase` 이전**

Create `Runtime/Core/Messaging/MessageChannelBase.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace Multiplayer.Lobby.Messaging
{
    public abstract class MessageChannelBase<T> : IMessageChannel<T>
    {
        protected readonly List<Action<T>> m_Handlers = new();
        public bool IsDisposed { get; private set; }

        public abstract void Publish(T message);

        public virtual IDisposable Subscribe(Action<T> handler)
        {
            ThrowIfDisposed();
            if (!m_Handlers.Contains(handler)) m_Handlers.Add(handler);
            return new DisposableSubscription<T>(m_Handlers, handler);
        }

        public virtual void Unsubscribe(Action<T> handler)
        {
            if (IsDisposed) return;
            m_Handlers.Remove(handler);
        }

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                m_Handlers.Clear();
            }
        }

        protected void InvokeHandlers(T message)
        {
            var snapshot = new List<Action<T>>(m_Handlers);
            foreach (var h in snapshot) h?.Invoke(message);
        }

        protected void ThrowIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
        }
    }
}
```

- [ ] **Step 3: `MessageChannel` / `BufferedMessageChannel` / `DisposableSubscription` 이전**

Create `Runtime/Core/Messaging/MessageChannel.cs`:
```csharp
namespace Multiplayer.Lobby.Messaging
{
    public class MessageChannel<T> : MessageChannelBase<T>
    {
        public override void Publish(T message)
        {
            ThrowIfDisposed();
            InvokeHandlers(message);
        }
    }
}
```

Create `Runtime/Core/Messaging/BufferedMessageChannel.cs` — 기존 `Runtime/Infrastructure/PubSub/BufferedMessageChannel.cs`의 내용을 복사하고 namespace를 `Multiplayer.Lobby.Messaging`으로 변경. 로직 동일.

Create `Runtime/Core/Messaging/DisposableSubscription.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace Multiplayer.Lobby.Messaging
{
    public sealed class DisposableSubscription<T> : IDisposable
    {
        readonly List<Action<T>> m_Handlers;
        readonly Action<T> m_Handler;
        bool m_Disposed;

        public DisposableSubscription(List<Action<T>> handlers, Action<T> handler)
        {
            m_Handlers = handlers;
            m_Handler = handler;
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Handlers.Remove(m_Handler);
        }
    }
}
```

- [ ] **Step 4: PubSub 테스트**

Create `Tests/Editor/MessageChannelTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Tests
{
    public class MessageChannelTests
    {
        [Test]
        public void SubscribedHandlerReceivesPublishedMessage()
        {
            var ch = new MessageChannel<int>();
            var received = 0;
            ch.Subscribe(v => received = v);
            ch.Publish(42);
            Assert.That(received, Is.EqualTo(42));
        }

        [Test]
        public void DisposingSubscriptionRemovesHandler()
        {
            var ch = new MessageChannel<int>();
            var count = 0;
            var sub = ch.Subscribe(_ => count++);
            sub.Dispose();
            ch.Publish(1);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void DisposingChannelMarksDisposed()
        {
            var ch = new MessageChannel<int>();
            ch.Dispose();
            Assert.That(ch.IsDisposed, Is.True);
        }

        [Test]
        public void DuplicateSubscribeIsIdempotent()
        {
            var ch = new MessageChannel<int>();
            var count = 0;
            System.Action<int> h = _ => count++;
            ch.Subscribe(h);
            ch.Subscribe(h);
            ch.Publish(1);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void PublishToDisposedChannelThrows()
        {
            var ch = new MessageChannel<int>();
            ch.Dispose();
            Assert.Throws<System.ObjectDisposedException>(() => ch.Publish(0));
        }
    }
}
```

Test Runner 5개 그린 확인.

- [ ] **Step 5: 커밋**

```bash
git add Runtime/Core/Messaging Tests/Editor/MessageChannelTests.cs
git commit -m "feat(core): PubSub Core 이전 (Multiplayer.Lobby.Messaging) + 테스트"
```

---

### Task 13: `ISessionManager` + `SessionManager` Core 이전 (ILobbyLogger 주입)

**Files:**
- Create: `Runtime/Core/Abstractions/ISessionManager.cs`
- Create: `Runtime/Core/Session/ISessionPlayerData.cs`
- Create: `Runtime/Core/Session/SessionManager.cs`
- Create: `Tests/Editor/Fakes/FakeSessionPlayerData.cs`
- Test: `Tests/Editor/SessionManagerTests.cs`

- [ ] **Step 1: `ISessionPlayerData` Core 이전**

Create `Runtime/Core/Session/ISessionPlayerData.cs`:
```csharp
namespace Multiplayer.Lobby.Session
{
    public interface ISessionPlayerData
    {
        bool IsConnected { get; set; }
        ulong ClientID { get; set; }
        void Reinitialize();
    }
}
```

(기존 `Runtime/Core/Session/ISessionPlayerData.cs`는 Task 33에서 삭제.)

- [ ] **Step 2: `ISessionManager` 인터페이스**

Create `Runtime/Core/Abstractions/ISessionManager.cs`:
```csharp
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Abstractions
{
    public interface ISessionManager
    {
        bool IsDuplicateConnection(string playerId);
        void SetupConnectingPlayerSessionData(ulong clientId, string playerId, ISessionPlayerData data);
        string GetPlayerId(ulong clientId);
        ISessionPlayerData GetPlayerData(ulong clientId);
        ISessionPlayerData GetPlayerData(string playerId);
        void SetPlayerData(ulong clientId, ISessionPlayerData data);
        void DisconnectClient(ulong clientId);
        void OnSessionStarted();
        void OnSessionEnded();
        void OnServerEnded();
    }
}
```

- [ ] **Step 3: `SessionManager` Core 이전 + 로거 주입**

Create `Runtime/Core/Session/SessionManager.cs` — 기존 `Runtime/Core/Session/SessionManager.cs` 로직을 옮기되:
1. 클래스 선언: `public sealed class SessionManager : ISessionManager`
2. 생성자에서 `ILobbyLogger` 받음 (null이면 `NullLogger.Instance`).
3. 모든 `Debug.Log*` 호출을 `m_Logger.Info/Error`로 대체.

```csharp
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Session
{
    public sealed class SessionManager : ISessionManager
    {
        readonly Dictionary<string, ISessionPlayerData> m_ClientData = new();
        readonly Dictionary<ulong, string> m_ClientIDToPlayerId = new();
        readonly ILobbyLogger m_Logger;
        bool m_HasSessionStarted;

        public SessionManager(ILobbyLogger logger = null)
        {
            m_Logger = logger ?? NullLogger.Instance;
        }

        public void DisconnectClient(ulong clientId)
        {
            if (m_HasSessionStarted)
            {
                if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid)
                    && m_ClientData.TryGetValue(pid, out var d) && d.ClientID == clientId)
                {
                    d.IsConnected = false;
                    m_ClientData[pid] = d;
                }
            }
            else
            {
                if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid))
                {
                    m_ClientIDToPlayerId.Remove(clientId);
                    if (m_ClientData.TryGetValue(pid, out var d) && d.ClientID == clientId)
                        m_ClientData.Remove(pid);
                }
            }
        }

        public bool IsDuplicateConnection(string playerId)
            => m_ClientData.ContainsKey(playerId) && m_ClientData[playerId].IsConnected;

        public void SetupConnectingPlayerSessionData(ulong clientId, string playerId, ISessionPlayerData data)
        {
            if (IsDuplicateConnection(playerId))
            {
                m_Logger.Error($"Player ID {playerId} already exists. Duplicate connection rejected.");
                return;
            }
            var isReconnecting = m_ClientData.ContainsKey(playerId) && !m_ClientData[playerId].IsConnected;
            if (isReconnecting)
            {
                data = m_ClientData[playerId];
                data.ClientID = clientId;
                data.IsConnected = true;
            }
            m_ClientIDToPlayerId[clientId] = playerId;
            m_ClientData[playerId] = data;
        }

        public string GetPlayerId(ulong clientId)
        {
            if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid)) return pid;
            m_Logger.Info($"No player ID found for client ID: {clientId}");
            return null;
        }

        public ISessionPlayerData GetPlayerData(ulong clientId)
        {
            var pid = GetPlayerId(clientId);
            return pid != null ? GetPlayerData(pid) : null;
        }

        public ISessionPlayerData GetPlayerData(string playerId)
        {
            if (m_ClientData.TryGetValue(playerId, out var d)) return d;
            m_Logger.Info($"No player data found for player ID: {playerId}");
            return null;
        }

        public void SetPlayerData(ulong clientId, ISessionPlayerData data)
        {
            if (m_ClientIDToPlayerId.TryGetValue(clientId, out var pid)) m_ClientData[pid] = data;
            else m_Logger.Error($"No player ID found for client ID: {clientId}");
        }

        public void OnSessionStarted() => m_HasSessionStarted = true;

        public void OnSessionEnded()
        {
            ClearDisconnectedPlayersData();
            ReinitializePlayersData();
            m_HasSessionStarted = false;
        }

        public void OnServerEnded()
        {
            m_ClientData.Clear();
            m_ClientIDToPlayerId.Clear();
            m_HasSessionStarted = false;
        }

        void ReinitializePlayersData()
        {
            foreach (var id in m_ClientIDToPlayerId.Keys)
            {
                var pid = m_ClientIDToPlayerId[id];
                if (m_ClientData.TryGetValue(pid, out var d))
                {
                    d.Reinitialize();
                    m_ClientData[pid] = d;
                }
            }
        }

        void ClearDisconnectedPlayersData()
        {
            var toClear = new List<ulong>();
            foreach (var id in m_ClientIDToPlayerId.Keys)
            {
                var d = GetPlayerData(id);
                if (d != null && !d.IsConnected) toClear.Add(id);
            }
            foreach (var id in toClear)
            {
                var pid = m_ClientIDToPlayerId[id];
                if (m_ClientData.TryGetValue(pid, out var d) && d.ClientID == id) m_ClientData.Remove(pid);
                m_ClientIDToPlayerId.Remove(id);
            }
        }
    }
}
```

- [ ] **Step 4: `FakeSessionPlayerData` + 테스트**

Create `Tests/Editor/Fakes/FakeSessionPlayerData.cs`:
```csharp
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeSessionPlayerData : ISessionPlayerData
    {
        public bool IsConnected { get; set; } = true;
        public ulong ClientID { get; set; }
        public string Name { get; set; } = "";
        public int ReinitializeCount { get; private set; }

        public FakeSessionPlayerData(ulong clientId, string name)
        {
            ClientID = clientId;
            Name = name;
        }

        public void Reinitialize() => ReinitializeCount++;
    }
}
```

Create `Tests/Editor/SessionManagerTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class SessionManagerTests
    {
        [Test]
        public void SetupStoresPlayerDataUnderPlayerId()
        {
            var sm = new SessionManager(new FakeLogger());
            var d = new FakeSessionPlayerData(1, "A");
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1", d);
            Assert.That(sm.GetPlayerData(1UL), Is.SameAs(d));
            Assert.That(sm.GetPlayerId(1UL), Is.EqualTo("pid-1"));
        }

        [Test]
        public void DuplicateConnectionIsRejected()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1",
                new FakeSessionPlayerData(1, "A") { IsConnected = true });
            sm.SetupConnectingPlayerSessionData(2UL, "pid-1", new FakeSessionPlayerData(2, "A'"));
            Assert.That(sm.GetPlayerData(2UL), Is.Null);
        }

        [Test]
        public void DisconnectDuringSessionPreservesDataForReconnection()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.OnSessionStarted();
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1", new FakeSessionPlayerData(1, "A"));
            sm.DisconnectClient(1UL);
            var stored = sm.GetPlayerData("pid-1");
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored.IsConnected, Is.False);
        }

        [Test]
        public void ReconnectRestoresPreviousDataByPlayerId()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.OnSessionStarted();
            var original = new FakeSessionPlayerData(1, "A");
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1", original);
            sm.DisconnectClient(1UL);
            sm.SetupConnectingPlayerSessionData(2UL, "pid-1", new FakeSessionPlayerData(2, "IGNORED"));
            var after = sm.GetPlayerData(2UL);
            Assert.That(after, Is.SameAs(original));
            Assert.That(after.ClientID, Is.EqualTo(2UL));
            Assert.That(after.IsConnected, Is.True);
        }

        [Test]
        public void OnServerEndedClearsAll()
        {
            var sm = new SessionManager(new FakeLogger());
            sm.SetupConnectingPlayerSessionData(1UL, "pid-1", new FakeSessionPlayerData(1, "A"));
            sm.OnServerEnded();
            Assert.That(sm.GetPlayerId(1UL), Is.Null);
        }
    }
}
```

Test Runner 5개 그린 확인.

- [ ] **Step 5: 커밋**

```bash
git add Runtime/Core/Abstractions/ISessionManager.cs Runtime/Core/Session Tests/Editor/Fakes/FakeSessionPlayerData.cs Tests/Editor/SessionManagerTests.cs
git commit -m "feat(core): ISessionManager 추출 + SessionManager Core 이전"
```

---

### Task 14: `IStateMachineContext` 인터페이스 (forward-decl ConnectionState 포함)

**Files:**
- Create: `Runtime/Core/StateMachine/ConnectionState.cs` (forward declaration / 기본 클래스)
- Create: `Runtime/Core/Abstractions/IStateMachineContext.cs`

순환 의존을 피하기 위해 `ConnectionState`를 먼저 **빈 추상 클래스**로 생성하고 `IStateMachineContext`가 이를 참조한다. `ConnectionState`의 전체 구현은 Task 15에서 완성.

- [ ] **Step 1: `ConnectionState` forward declaration**

Create `Runtime/Core/StateMachine/ConnectionState.cs`:
```csharp
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.StateMachine
{
    /// <summary>
    /// 상태 머신의 기본 상태. Task 15에서 가상 메서드들이 추가된다.
    /// </summary>
    public abstract class ConnectionState
    {
        protected IStateMachineContext Context { get; }

        protected ConnectionState(IStateMachineContext context)
        {
            Context = context ?? throw new System.ArgumentNullException(nameof(context));
        }
    }
}
```

- [ ] **Step 2: `IStateMachineContext` 작성**

Create `Runtime/Core/Abstractions/IStateMachineContext.cs`:
```csharp
using System;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Abstractions
{
    public interface IStateMachineContext
    {
        INetworkFacade Network { get; }
        ISessionManager Sessions { get; }
        IConnectionApprover Approver { get; }
        ILobbyLogger Logger { get; }
        IConnectionPayloadSerializer PayloadSerializer { get; }
        ICoroutineRunner CoroutineRunner { get; }
        PlayerIdentity Identity { get; }
        ReconnectPolicy ReconnectPolicy { get; }
        int MaxConnectedPlayers { get; }

        IPublisher<ConnectStatus> ConnectStatusPublisher { get; }
        IPublisher<ReconnectMessage> ReconnectPublisher { get; }
        IPublisher<ConnectionEventMessage> ConnectionEventPublisher { get; }
        IPublisher<LobbyLifecycleMessage> LifecyclePublisher { get; }

        Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData { get; }

        void ChangeState<TState>() where TState : ConnectionState;
        TState GetState<TState>() where TState : ConnectionState;
    }
}
```

- [ ] **Step 3: 컴파일 확인 + 커밋**

에디터 컴파일 성공 확인 (ConnectionState가 아직 가상 메서드가 없어 구체 상태에서 override는 못 하지만, 이 시점엔 구체 상태도 없음).

```bash
git add Runtime/Core/StateMachine/ConnectionState.cs Runtime/Core/Abstractions/IStateMachineContext.cs
git commit -m "feat(core): IStateMachineContext + ConnectionState forward-decl"
```

---

## Phase 3: 상태 머신 본체

### Task 15: `ConnectionState` 완성 + `OnlineState`

**Files:**
- Modify: `Runtime/Core/StateMachine/ConnectionState.cs` (가상 메서드 추가)
- Create: `Runtime/Core/StateMachine/OnlineState.cs`

- [ ] **Step 1: `ConnectionState`에 가상 메서드 추가**

Overwrite `Runtime/Core/StateMachine/ConnectionState.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.StateMachine
{
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
            => Context.Approver.Approve(request);

        protected static ConnectStatus ParseDisconnectReason(string reason, ILobbyLogger logger = null)
        {
            if (string.IsNullOrEmpty(reason)) return ConnectStatus.GenericDisconnect;
            if (Enum.TryParse<ConnectStatus>(reason, out var s)) return s;
            logger?.Warning($"[LobbySystem] Failed to parse disconnect reason: '{reason}'");
            return ConnectStatus.GenericDisconnect;
        }
    }
}
```

- [ ] **Step 2: `OnlineState` 작성**

Create `Runtime/Core/StateMachine/OnlineState.cs`:
```csharp
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.States;

namespace Multiplayer.Lobby.StateMachine
{
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
}
```

> `OfflineState`는 Task 17에서 생성되며 namespace `Multiplayer.Lobby.States`에 배치. 이 시점엔 컴파일 에러 — Task 17까지 묶어 처리.

- [ ] **Step 3: Task 17까지 묶어 커밋 (여기서는 커밋 없음, Task 17 완료 후 한 번에)**

---

### Task 16: `StateMachine` + `StateMachineContext` 구현 + 테스트

**Files:**
- Create: `Runtime/Core/StateMachine/StateMachine.cs`
- Create: `Runtime/Core/StateMachine/StateMachineContext.cs`
- Test: `Tests/Editor/StateMachineTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Tests/Editor/StateMachineTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.StateMachine;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class StateMachineTests
    {
        sealed class StubState : ConnectionState
        {
            public int Enters, Exits;
            public StubState(IStateMachineContext ctx) : base(ctx) { }
            public override void Enter() => Enters++;
            public override void Exit()  => Exits++;
        }
        sealed class OtherState : ConnectionState
        {
            public int Enters;
            public OtherState(IStateMachineContext ctx) : base(ctx) { }
            public override void Enter() => Enters++;
        }

        [Test]
        public void StartTransitionsToInitialState()
        {
            var (sm, states) = BuildMachine();
            sm.Start<StubState>();
            Assert.That(((StubState)states[typeof(StubState)]).Enters, Is.EqualTo(1));
        }

        [Test]
        public void ChangeStateCallsExitThenEnter()
        {
            var (sm, states) = BuildMachine();
            sm.Start<StubState>();
            sm.ChangeState<OtherState>();
            Assert.That(((StubState)states[typeof(StubState)]).Exits, Is.EqualTo(1));
            Assert.That(((OtherState)states[typeof(OtherState)]).Enters, Is.EqualTo(1));
        }

        [Test]
        public void ChangeStateUnregisteredThrows()
        {
            var (sm, _) = BuildMachine();
            sm.Start<StubState>();
            Assert.Throws<System.InvalidOperationException>(
                () => sm.ChangeState<ThirdState>());
        }

        sealed class ThirdState : ConnectionState
        {
            public ThirdState(IStateMachineContext ctx) : base(ctx) { }
        }

        static (StateMachine, Dictionary<System.Type, ConnectionState>) BuildMachine()
        {
            var ctxHolder = new ContextHolder();
            var states = new Dictionary<System.Type, ConnectionState>();
            var sm = new StateMachine(states, new FakeNetworkFacade(), new FakeLogger());
            ctxHolder.StateMachine = sm;
            var ctx = new StateMachineContext(ctxHolder); // Task 16에서 정의됨
            states[typeof(StubState)]  = new StubState(ctx);
            states[typeof(OtherState)] = new OtherState(ctx);
            return (sm, states);
        }

        // 테스트 전용 컨텍스트 조립 헬퍼
        sealed class ContextHolder : IStateMachineContextDeps
        {
            public StateMachine StateMachine { get; set; }
            public INetworkFacade Network { get; } = new FakeNetworkFacade();
            public ISessionManager Sessions { get; } = new FakeSessionManager();
            public IConnectionApprover Approver { get; } = new FakeApprover();
            public ILobbyLogger Logger { get; } = new FakeLogger();
            public IConnectionPayloadSerializer PayloadSerializer { get; } = new FakeConnectionPayloadSerializer();
            public ICoroutineRunner CoroutineRunner { get; } = new FakeCoroutineRunner();
            public Connection.PlayerIdentity Identity { get; } = new Connection.PlayerIdentity(new InMemoryPlayerIdentityStore());
            public Connection.ReconnectPolicy ReconnectPolicy { get; } = Connection.ReconnectPolicy.Default;
            public int MaxConnectedPlayers => 8;
            public Messaging.IPublisher<Connection.ConnectStatus> ConnectStatusPublisher { get; } = new Messaging.MessageChannel<Connection.ConnectStatus>();
            public Messaging.IPublisher<Connection.ReconnectMessage> ReconnectPublisher { get; } = new Messaging.MessageChannel<Connection.ReconnectMessage>();
            public Messaging.IPublisher<Connection.ConnectionEventMessage> ConnectionEventPublisher { get; } = new Messaging.MessageChannel<Connection.ConnectionEventMessage>();
            public Messaging.IPublisher<Messaging.LobbyLifecycleMessage> LifecyclePublisher { get; } = new Messaging.MessageChannel<Messaging.LobbyLifecycleMessage>();
            public System.Func<ulong, Connection.ConnectionPayload, Session.ISessionPlayerData> CreatePlayerData { get; } = (_, _) => null;
        }
    }
}
```

> `IStateMachineContextDeps`, `StateMachineContext`, `FakeSessionManager`, `FakeApprover`는 다음 스텝과 Task 16 후반에 정의된다. 위 테스트는 Task 16 전체가 끝난 뒤 그린되어야 한다. 태스크 전체를 묶어 한 번에 커밋.

- [ ] **Step 2: `IStateMachineContextDeps` 내부 DTO + `StateMachineContext` 구현**

Core의 의존성 번들을 받아 `IStateMachineContext`를 구현하는 위임 클래스.

Create `Runtime/Core/StateMachine/StateMachineContext.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.StateMachine
{
    /// <summary>
    /// 빌더가 조립해 넘겨주는 의존성 번들.
    /// StateMachineContext가 이 번들을 래핑해 IStateMachineContext로 노출한다.
    /// </summary>
    public interface IStateMachineContextDeps
    {
        StateMachine StateMachine { get; }
        INetworkFacade Network { get; }
        ISessionManager Sessions { get; }
        IConnectionApprover Approver { get; }
        ILobbyLogger Logger { get; }
        IConnectionPayloadSerializer PayloadSerializer { get; }
        ICoroutineRunner CoroutineRunner { get; }
        PlayerIdentity Identity { get; }
        ReconnectPolicy ReconnectPolicy { get; }
        int MaxConnectedPlayers { get; }
        IPublisher<ConnectStatus> ConnectStatusPublisher { get; }
        IPublisher<ReconnectMessage> ReconnectPublisher { get; }
        IPublisher<ConnectionEventMessage> ConnectionEventPublisher { get; }
        IPublisher<LobbyLifecycleMessage> LifecyclePublisher { get; }
        Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData { get; }
    }

    public sealed class StateMachineContext : IStateMachineContext
    {
        readonly IStateMachineContextDeps m_Deps;

        public StateMachineContext(IStateMachineContextDeps deps)
        {
            m_Deps = deps ?? throw new ArgumentNullException(nameof(deps));
        }

        public INetworkFacade Network                             => m_Deps.Network;
        public ISessionManager Sessions                           => m_Deps.Sessions;
        public IConnectionApprover Approver                       => m_Deps.Approver;
        public ILobbyLogger Logger                                => m_Deps.Logger;
        public IConnectionPayloadSerializer PayloadSerializer     => m_Deps.PayloadSerializer;
        public ICoroutineRunner CoroutineRunner                   => m_Deps.CoroutineRunner;
        public PlayerIdentity Identity                            => m_Deps.Identity;
        public ReconnectPolicy ReconnectPolicy                    => m_Deps.ReconnectPolicy;
        public int MaxConnectedPlayers                            => m_Deps.MaxConnectedPlayers;
        public IPublisher<ConnectStatus> ConnectStatusPublisher   => m_Deps.ConnectStatusPublisher;
        public IPublisher<ReconnectMessage> ReconnectPublisher    => m_Deps.ReconnectPublisher;
        public IPublisher<ConnectionEventMessage> ConnectionEventPublisher => m_Deps.ConnectionEventPublisher;
        public IPublisher<LobbyLifecycleMessage> LifecyclePublisher=> m_Deps.LifecyclePublisher;
        public Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData => m_Deps.CreatePlayerData;

        public void ChangeState<TState>() where TState : ConnectionState
            => m_Deps.StateMachine.ChangeState<TState>();

        public TState GetState<TState>() where TState : ConnectionState
            => m_Deps.StateMachine.GetState<TState>();
    }
}
```

- [ ] **Step 3: `StateMachine` 구현**

Create `Runtime/Core/StateMachine/StateMachine.cs`:
```csharp
using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.StateMachine
{
    public sealed class StateMachine : IDisposable
    {
        readonly IReadOnlyDictionary<Type, ConnectionState> m_States;
        readonly INetworkFacade m_Network;
        readonly ILobbyLogger m_Logger;
        ConnectionState m_Current;
        bool m_Started;

        // 이벤트 핸들러 참조 보관 (Dispose 시 해제용)
        Action m_OnServerStartedHandler;
        Action<bool> m_OnServerStoppedHandler;
        Action m_OnTransportFailureHandler;
        Action<ulong> m_OnClientConnectedHandler;
        Action<ulong, string> m_OnClientDisconnectedHandler;
        Func<ApprovalRequest, ApprovalResult> m_OnApprovalHandler;

        public StateMachine(
            IReadOnlyDictionary<Type, ConnectionState> states,
            INetworkFacade network,
            ILobbyLogger logger)
        {
            m_States = states ?? throw new ArgumentNullException(nameof(states));
            m_Network = network ?? throw new ArgumentNullException(nameof(network));
            m_Logger = logger ?? NullLogger.Instance;
        }

        public void Start<TInitial>() where TInitial : ConnectionState
        {
            if (m_Started) throw new InvalidOperationException("StateMachine already started.");
            m_Started = true;

            m_OnServerStartedHandler      = () => m_Current.OnServerStarted();
            m_OnServerStoppedHandler      = _ => m_Current.OnServerStopped();
            m_OnTransportFailureHandler   = () => m_Current.OnTransportFailure();
            m_OnClientConnectedHandler    = id => m_Current.OnClientConnected(id);
            m_OnClientDisconnectedHandler = (id, reason) => m_Current.OnClientDisconnected(id, reason);
            m_OnApprovalHandler           = req => m_Current.ApprovalCheck(req);

            m_Network.OnServerStarted      += m_OnServerStartedHandler;
            m_Network.OnServerStopped      += m_OnServerStoppedHandler;
            m_Network.OnTransportFailure   += m_OnTransportFailureHandler;
            m_Network.OnClientConnected    += m_OnClientConnectedHandler;
            m_Network.OnClientDisconnected += m_OnClientDisconnectedHandler;
            m_Network.ApprovalCheck        += m_OnApprovalHandler;

            ChangeState<TInitial>();
        }

        public void ChangeState<TState>() where TState : ConnectionState
        {
            if (!m_States.TryGetValue(typeof(TState), out var next))
                throw new InvalidOperationException($"State not registered: {typeof(TState).Name}");

            m_Logger.Info($"{m_Current?.GetType().Name ?? "(null)"} → {typeof(TState).Name}");
            m_Current?.Exit();
            m_Current = next;
            m_Current.Enter();
        }

        public TState GetState<TState>() where TState : ConnectionState
            => (TState)m_States[typeof(TState)];

        public void StartClient(ConnectionMethodBase method) => m_Current.StartClient(method);
        public void StartHost(ConnectionMethodBase method)   => m_Current.StartHost(method);
        public void RequestShutdown()                         => m_Current.OnUserRequestedShutdown();

        public ConnectionState CurrentState => m_Current;

        public void Dispose()
        {
            if (!m_Started) return;
            m_Network.OnServerStarted      -= m_OnServerStartedHandler;
            m_Network.OnServerStopped      -= m_OnServerStoppedHandler;
            m_Network.OnTransportFailure   -= m_OnTransportFailureHandler;
            m_Network.OnClientConnected    -= m_OnClientConnectedHandler;
            m_Network.OnClientDisconnected -= m_OnClientDisconnectedHandler;
            m_Network.ApprovalCheck        -= m_OnApprovalHandler;
            m_Started = false;
        }
    }
}
```

- [ ] **Step 4: 테스트 헬퍼용 `FakeSessionManager` + `FakeApprover`**

Create `Tests/Editor/Fakes/FakeSessionManager.cs`:
```csharp
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeSessionManager : ISessionManager
    {
        readonly Dictionary<ulong, string> m_Ids = new();
        readonly Dictionary<string, ISessionPlayerData> m_Data = new();
        public bool SessionStarted { get; private set; }
        public int OnServerEndedCalls { get; private set; }

        public bool IsDuplicateConnection(string playerId)
            => m_Data.ContainsKey(playerId) && m_Data[playerId].IsConnected;

        public void SetupConnectingPlayerSessionData(ulong clientId, string playerId, ISessionPlayerData data)
        {
            m_Ids[clientId] = playerId;
            m_Data[playerId] = data;
        }

        public string GetPlayerId(ulong clientId)
            => m_Ids.TryGetValue(clientId, out var v) ? v : null;

        public ISessionPlayerData GetPlayerData(ulong clientId)
        {
            var pid = GetPlayerId(clientId);
            return pid != null ? GetPlayerData(pid) : null;
        }

        public ISessionPlayerData GetPlayerData(string playerId)
            => m_Data.TryGetValue(playerId, out var d) ? d : null;

        public void SetPlayerData(ulong clientId, ISessionPlayerData data)
        {
            var pid = GetPlayerId(clientId);
            if (pid != null) m_Data[pid] = data;
        }

        public void DisconnectClient(ulong clientId)
        {
            var pid = GetPlayerId(clientId);
            if (pid != null && m_Data.TryGetValue(pid, out var d)) d.IsConnected = false;
        }

        public void OnSessionStarted() => SessionStarted = true;
        public void OnSessionEnded()   => SessionStarted = false;
        public void OnServerEnded()    { OnServerEndedCalls++; m_Ids.Clear(); m_Data.Clear(); }
    }
}
```

Create `Tests/Editor/Fakes/FakeApprover.cs`:
```csharp
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class FakeApprover : IConnectionApprover
    {
        public ApprovalResult NextResult { get; set; } = ApprovalResult.Allow();
        public int Calls { get; private set; }

        public ApprovalResult Approve(ApprovalRequest request)
        {
            Calls++;
            return NextResult;
        }
    }
}
```

- [ ] **Step 5: 테스트 그린 + 커밋**

Test Runner에서 `StateMachineTests` 3개 + 앞선 팩토리 헬퍼 임포트 성공 확인.

```bash
git add Runtime/Core/StateMachine/StateMachineContext.cs Runtime/Core/StateMachine/StateMachine.cs Tests/Editor/Fakes/FakeSessionManager.cs Tests/Editor/Fakes/FakeApprover.cs Tests/Editor/StateMachineTests.cs
git commit -m "feat(core): StateMachine + StateMachineContext + 전이 단위 테스트"
```

---

### Task 17: `OfflineState` 신규 구현

**Files:**
- Create: `Runtime/Core/States/OfflineState.cs`
- Test: `Tests/Editor/OfflineStateTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Tests/Editor/OfflineStateTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.States;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class OfflineStateTests
    {
        [Test]
        public void EnterShutsDownNetworkAndPublishesDisconnected()
        {
            var h = StateHarness.Build(typeof(OfflineState));
            h.Machine.Start<OfflineState>();
            Assert.That(h.Network.ShutdownCalls, Is.GreaterThanOrEqualTo(1));

            // Lifecycle Disconnected 발행 확인
            var received = 0;
            (h.LifecyclePublisher as Messaging.IMessageChannel<Messaging.LobbyLifecycleMessage>)
                .Subscribe(m => { if (m == Messaging.LobbyLifecycleMessage.Disconnected) received++; });

            // 재진입
            h.Machine.ChangeState<OfflineState>();
            Assert.That(received, Is.GreaterThanOrEqualTo(1));
        }
    }
}
```

> `StateHarness`는 테스트용 통합 조립 헬퍼로, 아래 Step에서 정의. 여러 상태 테스트에 공유된다.

- [ ] **Step 2: `StateHarness` 테스트 헬퍼**

Create `Tests/Editor/Fakes/StateHarness.cs`:
```csharp
using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Tests.Fakes
{
    public sealed class StateHarness : IStateMachineContextDeps
    {
        public StateMachine Machine { get; private set; }
        public FakeNetworkFacade Network { get; } = new FakeNetworkFacade();
        public FakeSessionManager SessionManager { get; } = new FakeSessionManager();
        public FakeApprover Approver { get; } = new FakeApprover();
        public FakeLogger Logger { get; } = new FakeLogger();
        public FakeConnectionPayloadSerializer PayloadSerializer { get; } = new FakeConnectionPayloadSerializer();
        public FakeCoroutineRunner CoroutineRunner { get; } = new FakeCoroutineRunner();
        public PlayerIdentity Identity { get; } = new PlayerIdentity(new InMemoryPlayerIdentityStore());
        public ReconnectPolicy ReconnectPolicy { get; set; } = ReconnectPolicy.Default;
        public int MaxConnectedPlayers { get; set; } = 8;

        public MessageChannel<ConnectStatus> ConnectStatusChannel { get; } = new();
        public MessageChannel<ReconnectMessage> ReconnectChannel { get; } = new();
        public MessageChannel<ConnectionEventMessage> ConnectionEventChannel { get; } = new();
        public MessageChannel<LobbyLifecycleMessage> LifecycleChannel { get; } = new();

        public IPublisher<ConnectStatus> ConnectStatusPublisher         => ConnectStatusChannel;
        public IPublisher<ReconnectMessage> ReconnectPublisher          => ReconnectChannel;
        public IPublisher<ConnectionEventMessage> ConnectionEventPublisher => ConnectionEventChannel;
        public IPublisher<LobbyLifecycleMessage> LifecyclePublisher     => LifecycleChannel;

        // Exposed for state tests
        public IMessageChannel<LobbyLifecycleMessage> LifecycleChannelPublic => LifecycleChannel;
        public IMessageChannel<ConnectStatus> ConnectStatusChannelPublic => ConnectStatusChannel;

        ISessionManager IStateMachineContextDeps.Sessions => SessionManager;
        IConnectionApprover IStateMachineContextDeps.Approver => Approver;
        ILobbyLogger IStateMachineContextDeps.Logger => Logger;
        IConnectionPayloadSerializer IStateMachineContextDeps.PayloadSerializer => PayloadSerializer;
        ICoroutineRunner IStateMachineContextDeps.CoroutineRunner => CoroutineRunner;
        INetworkFacade IStateMachineContextDeps.Network => Network;
        StateMachine IStateMachineContextDeps.StateMachine => Machine;

        public Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData { get; set; }
            = (id, payload) => new FakeSessionPlayerData(id, payload?.playerName ?? "");

        public static StateHarness Build(params Type[] stateTypes)
        {
            var h = new StateHarness();
            var states = new Dictionary<Type, ConnectionState>();
            h.Machine = new StateMachine(states, h.Network, h.Logger);
            var ctx = new StateMachineContext(h);
            foreach (var t in stateTypes)
                states[t] = (ConnectionState)Activator.CreateInstance(t, ctx);
            return h;
        }
    }
}
```

- [ ] **Step 3: `OfflineState` 구현**

Create `Runtime/Core/States/OfflineState.cs`:
```csharp
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class OfflineState : ConnectionState
    {
        public OfflineState(IStateMachineContext context) : base(context) { }

        public override void Enter()
        {
            Context.Network.Shutdown();
            Context.LifecyclePublisher.Publish(LobbyLifecycleMessage.Disconnected);
        }

        public override void StartClient(Connection.ConnectionMethodBase method)
        {
            // ClientConnecting과 ClientReconnecting 모두 Configure 대상 (Task 20 참고)
            Context.GetState<ClientReconnectingState>().Configure(method);
            var cc = Context.GetState<ClientConnectingState>();
            cc.Configure(method);
            Context.ChangeState<ClientConnectingState>();
        }

        public override void StartHost(Connection.ConnectionMethodBase method)
        {
            var sh = Context.GetState<StartingHostState>();
            sh.Configure(method);
            Context.ChangeState<StartingHostState>();
        }
    }
}
```

> `ClientConnectingState`, `ClientReconnectingState`, `StartingHostState`는 각각 Task 18~22에서 생성. 이 시점엔 컴파일 에러 — 묶음 커밋.

- [ ] **Step 4: 커밋은 Task 22 이후 일괄로 진행**

Task 17~22는 상호 의존하므로 모두 구현한 뒤 한 번에 커밋한다. 이 태스크 단독 커밋은 생략.

---

### Task 18: `StartingHostState` 신규 구현

**Files:**
- Create: `Runtime/Core/States/StartingHostState.cs`

- [ ] **Step 1: 구현**

Create `Runtime/Core/States/StartingHostState.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class StartingHostState : OnlineState
    {
        ConnectionMethodBase m_ConnectionMethod;

        public StartingHostState(IStateMachineContext context) : base(context) { }

        public StartingHostState Configure(ConnectionMethodBase method)
        {
            m_ConnectionMethod = method;
            return this;
        }

        public override void Enter() => StartHostInternal();

        public override void OnServerStarted()
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.Success);
            Context.ChangeState<HostingState>();
        }

        public override ApprovalResult ApprovalCheck(ApprovalRequest request)
        {
            // 호스트 자기 자신 승인: 세션 데이터 초기화 포함
            if (request.ClientId == Context.Network.LocalClientId)
            {
                var payload = Context.PayloadSerializer.Deserialize(request.Payload);
                if (payload != null)
                {
                    var data = Context.CreatePlayerData?.Invoke(request.ClientId, payload);
                    if (data != null)
                        Context.Sessions.SetupConnectingPlayerSessionData(request.ClientId, payload.playerId, data);
                    else
                        Context.Logger.Warning("CreatePlayerData factory not set. Host player data not tracked.");
                }
                return ApprovalResult.Allow();
            }
            return base.ApprovalCheck(request);
        }

        public override void OnServerStopped() => StartHostFailed();

        void StartHostInternal()
        {
            try
            {
                m_ConnectionMethod.SetupHostConnection();
                if (m_ConnectionMethod.RequiresManualNetworkStart)
                {
                    if (!Context.Network.StartHost()) StartHostFailed();
                }
            }
            catch (Exception)
            {
                StartHostFailed();
                throw;
            }
        }

        void StartHostFailed()
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
            Context.ChangeState<OfflineState>();
        }
    }
}
```

- [ ] **Step 2: 다음 태스크로 진행 (커밋 없음)**

---

### Task 19: `HostingState` 신규 구현

**Files:**
- Create: `Runtime/Core/States/HostingState.cs`

- [ ] **Step 1: 구현**

Create `Runtime/Core/States/HostingState.cs`:
```csharp
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class HostingState : OnlineState
    {
        public HostingState(IStateMachineContext context) : base(context) { }

        public override void Enter()
            => Context.LifecyclePublisher.Publish(LobbyLifecycleMessage.HostStarted);

        public override void Exit() => Context.Sessions.OnServerEnded();

        public override void OnClientConnected(ulong clientId)
        {
            var data = Context.Sessions.GetPlayerData(clientId);
            if (data != null)
            {
                Context.ConnectionEventPublisher.Publish(new ConnectionEventMessage
                {
                    ConnectStatus = ConnectStatus.Success,
                    PlayerName = ""
                });
            }
            else
            {
                Context.Logger.Error($"No player data associated with client {clientId}");
                Context.Network.DisconnectClient(clientId, ConnectStatus.GenericDisconnect.ToString());
            }
        }

        public override void OnClientDisconnected(ulong clientId, string reason)
        {
            if (clientId == Context.Network.LocalClientId) return;
            var pid = Context.Sessions.GetPlayerId(clientId);
            if (pid == null) return;
            var data = Context.Sessions.GetPlayerData(pid);
            if (data != null)
            {
                Context.ConnectionEventPublisher.Publish(new ConnectionEventMessage
                {
                    ConnectStatus = ConnectStatus.GenericDisconnect,
                    PlayerName = ""
                });
            }
            Context.Sessions.DisconnectClient(clientId);
        }

        public override void OnUserRequestedShutdown()
        {
            // 호스트 종료: 연결된 모든 클라이언트 끊기
            Context.Network.Shutdown();
            Context.ChangeState<OfflineState>();
        }

        public override void OnServerStopped()
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.GenericDisconnect);
            Context.ChangeState<OfflineState>();
        }

        public override ApprovalResult ApprovalCheck(ApprovalRequest request)
        {
            // 1) DefaultConnectionApprover로 기본 검증 (인원/페이로드)
            var baseResult = Context.Approver.Approve(request);
            if (!baseResult.Approved) return baseResult;

            // 2) 세션 레벨 검증 (중복 로그인)
            var payload = Context.PayloadSerializer.Deserialize(request.Payload);
            if (payload == null)
                return ApprovalResult.Deny(ConnectStatus.GenericDisconnect.ToString());

            if (Context.Sessions.IsDuplicateConnection(payload.playerId))
                return ApprovalResult.Deny(ConnectStatus.LoggedInAgain.ToString());

            // 3) 세션 데이터 생성·등록
            var data = Context.CreatePlayerData?.Invoke(request.ClientId, payload);
            if (data != null)
                Context.Sessions.SetupConnectingPlayerSessionData(request.ClientId, payload.playerId, data);
            else
                Context.Logger.Warning("CreatePlayerData factory not set. Player data not tracked.");

            return ApprovalResult.Allow();
        }
    }
}
```

- [ ] **Step 2: 다음 태스크로 (커밋 없음)**

---

### Task 20: `ClientConnectingState` 신규 구현

**Files:**
- Create: `Runtime/Core/States/ClientConnectingState.cs`

- [ ] **Step 1: 구현**

Create `Runtime/Core/States/ClientConnectingState.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public class ClientConnectingState : OnlineState
    {
        protected ConnectionMethodBase m_ConnectionMethod;

        public ClientConnectingState(IStateMachineContext context) : base(context) { }

        public ClientConnectingState Configure(ConnectionMethodBase method)
        {
            m_ConnectionMethod = method;
            return this;
        }

        public override void Enter() => ConnectClient();

        public override void OnClientConnected(ulong _)
        {
            Context.ConnectStatusPublisher.Publish(ConnectStatus.Success);
            Context.ChangeState<ClientConnectedState>();
        }

        public override void OnClientDisconnected(ulong _, string reason)
        {
            StartingClientFailed(reason);
        }

        protected void StartingClientFailed(string reason = null)
        {
            var actualReason = reason ?? Context.Network.GetDisconnectReason(Context.Network.LocalClientId);
            if (string.IsNullOrEmpty(actualReason))
                Context.ConnectStatusPublisher.Publish(ConnectStatus.StartClientFailed);
            else
                Context.ConnectStatusPublisher.Publish(ParseDisconnectReason(actualReason, Context.Logger));

            Context.ChangeState<OfflineState>();
        }

        protected internal void ConnectClient()
        {
            try
            {
                m_ConnectionMethod.SetupClientConnection();
                if (m_ConnectionMethod.RequiresManualNetworkStart)
                {
                    if (!Context.Network.StartClient())
                        throw new Exception("INetworkFacade.StartClient returned false");
                }
            }
            catch (Exception e)
            {
                Context.Logger.Error($"Error connecting client: {e.Message}");
                StartingClientFailed();
                throw;
            }
        }
    }
}
```

- [ ] **Step 2: 다음 태스크로 (커밋 없음)**

---

### Task 21: `ClientConnectedState` 신규 구현

**Files:**
- Create: `Runtime/Core/States/ClientConnectedState.cs`

- [ ] **Step 1: 구현**

Create `Runtime/Core/States/ClientConnectedState.cs`:
```csharp
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class ClientConnectedState : OnlineState
    {
        public ClientConnectedState(IStateMachineContext context) : base(context) { }

        public override void Enter()
            => Context.LifecyclePublisher.Publish(LobbyLifecycleMessage.ClientConnected);

        public override void OnClientDisconnected(ulong _, string reason)
        {
            var actualReason = reason ?? Context.Network.GetDisconnectReason(Context.Network.LocalClientId);
            var status = ParseDisconnectReason(actualReason, Context.Logger);
            switch (status)
            {
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.HostEndedSession:
                case ConnectStatus.ServerFull:
                case ConnectStatus.IncompatibleBuildType:
                    Context.ConnectStatusPublisher.Publish(status);
                    Context.ChangeState<OfflineState>();
                    break;
                default:
                    Context.ConnectStatusPublisher.Publish(ConnectStatus.Reconnecting);
                    Context.ChangeState<ClientReconnectingState>();
                    break;
            }
        }
    }
}
```

- [ ] **Step 2: 다음 태스크로 (커밋 없음)**

---

### Task 22: `ClientReconnectingState` 신규 구현 (ICoroutineRunner 사용)

**Files:**
- Create: `Runtime/Core/States/ClientReconnectingState.cs`

- [ ] **Step 1: 구현**

Create `Runtime/Core/States/ClientReconnectingState.cs`:
```csharp
using System.Collections;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.States
{
    public sealed class ClientReconnectingState : ClientConnectingState
    {
        object m_ReconnectHandle;
        int m_NbAttempts;
        double m_NextBackoffSeconds;

        public ClientReconnectingState(IStateMachineContext context) : base(context) { }

        public new ClientReconnectingState Configure(ConnectionMethodBase method)
        {
            m_ConnectionMethod = method;
            return this;
        }

        public override void Enter()
        {
            m_NbAttempts = 0;
            m_NextBackoffSeconds = Context.ReconnectPolicy.InitialBackoff.TotalSeconds;
            StartReconnect();
        }

        public override void Exit()
        {
            if (m_ReconnectHandle != null)
            {
                Context.CoroutineRunner.Stop(m_ReconnectHandle);
                m_ReconnectHandle = null;
            }
            Context.ReconnectPublisher.Publish(
                new ReconnectMessage(m_NbAttempts, Context.ReconnectPolicy.MaxAttempts));
        }

        public override void OnClientConnected(ulong _)
            => Context.ChangeState<ClientConnectedState>();

        public override void OnClientDisconnected(ulong _, string reason)
        {
            var actualReason = reason ?? Context.Network.GetDisconnectReason(Context.Network.LocalClientId);
            if (m_NbAttempts < Context.ReconnectPolicy.MaxAttempts)
            {
                if (string.IsNullOrEmpty(actualReason))
                {
                    StartReconnect();
                    return;
                }
                var status = ParseDisconnectReason(actualReason, Context.Logger);
                Context.ConnectStatusPublisher.Publish(status);
                switch (status)
                {
                    case ConnectStatus.UserRequestedDisconnect:
                    case ConnectStatus.HostEndedSession:
                    case ConnectStatus.ServerFull:
                    case ConnectStatus.IncompatibleBuildType:
                        Context.ChangeState<OfflineState>();
                        break;
                    default:
                        StartReconnect();
                        break;
                }
            }
            else
            {
                var status = string.IsNullOrEmpty(actualReason)
                    ? ConnectStatus.GenericDisconnect
                    : ParseDisconnectReason(actualReason, Context.Logger);
                Context.ConnectStatusPublisher.Publish(status);
                Context.ChangeState<OfflineState>();
            }
        }

        void StartReconnect()
        {
            if (m_ReconnectHandle != null)
                Context.CoroutineRunner.Stop(m_ReconnectHandle);
            m_ReconnectHandle = Context.CoroutineRunner.Start(ReconnectRoutine());
        }

        IEnumerator ReconnectRoutine()
        {
            if (m_NbAttempts > 0)
            {
                var backoff = System.Math.Min(m_NextBackoffSeconds, Context.ReconnectPolicy.MaxBackoff.TotalSeconds);
                yield return backoff;   // Adapter가 WaitForSeconds로 해석 (Task 27 참고)
                m_NextBackoffSeconds *= Context.ReconnectPolicy.BackoffMultiplier;
            }

            Context.Logger.Info("Lost connection to host, trying to reconnect...");
            Context.Network.Shutdown();
            while (Context.Network.ShutdownInProgress) yield return null;

            Context.Logger.Info($"Reconnecting attempt {m_NbAttempts + 1}/{Context.ReconnectPolicy.MaxAttempts}...");
            Context.ReconnectPublisher.Publish(
                new ReconnectMessage(m_NbAttempts, Context.ReconnectPolicy.MaxAttempts));

            if (m_NbAttempts == 0)
                yield return Context.ReconnectPolicy.InitialBackoff.TotalSeconds;

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
    }
}
```

> 주의: `yield return <double>` 값을 MonoBehaviour 런너가 `WaitForSeconds`로 해석해야 한다. `MonoBehaviourCoroutineRunner`(Task 27)는 `yield return double/float` 반환 시 `WaitForSeconds`로 래핑하는 어댑터 형태로 구현한다. `FakeCoroutineRunner`는 `AdvanceAll()`로 한 스텝씩 진행(시간 무시).

- [ ] **Step 2: 묶음 커밋 (Task 17~22 모두 완성된 상태)**

이 시점에 모든 6개 상태가 준비되었고 `OfflineState`가 참조하는 다른 상태들도 모두 존재한다.

```bash
git add Runtime/Core/StateMachine/ConnectionState.cs Runtime/Core/StateMachine/OnlineState.cs Runtime/Core/States
git commit -m "feat(core): 상태 머신 본체 + 6개 상태 신규 구현 (ctor 주입, OCP 레지스트리)"
```

---

### Task 23: 상태 전이 통합 테스트

**Files:**
- Test: `Tests/Editor/StateTransitionTests.cs`

빌더 없이 `StateHarness`로 조립하여 주요 전이 경로를 검증.

- [ ] **Step 1: 전이 테스트 작성**

Create `Tests/Editor/StateTransitionTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.States;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class StateTransitionTests
    {
        [Test]
        public void OfflineToStartingHostToHosting()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));

            h.Machine.Start<OfflineState>();

            var method = new TestConnectionMethod(h.Network, h.PayloadSerializer, h.Identity, "P1", false);
            h.Machine.StartHost(method);

            Assert.That(h.Machine.CurrentState, Is.InstanceOf<StartingHostState>());
            Assert.That(h.Network.StartHostCalls, Is.EqualTo(1));

            h.Network.RaiseServerStarted();
            Assert.That(h.Machine.CurrentState, Is.InstanceOf<HostingState>());
        }

        [Test]
        public void HostingApprovalWithValidPayloadRegistersSessionData()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));
            h.Machine.Start<OfflineState>();

            var method = new TestConnectionMethod(h.Network, h.PayloadSerializer, h.Identity, "Host", false);
            h.Machine.StartHost(method);
            h.Network.RaiseServerStarted();

            var payload = new ConnectionPayload { playerId = "pid-42", playerName = "Joiner", isDebug = false };
            var bytes = h.PayloadSerializer.Serialize(payload);

            var result = h.Network.RaiseApprovalCheck(new ApprovalRequest(clientId: 42, payload: bytes, currentConnectedCount: 1));

            Assert.That(result.Approved, Is.True);
            Assert.That(h.SessionManager.GetPlayerData(42UL), Is.Not.Null);
        }

        [Test]
        public void ConnectedToReconnectingOnTransientDisconnect()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));
            h.Machine.Start<OfflineState>();

            // 클라이언트로 연결 -> ClientConnected 진입 시뮬레이션
            var method = new TestConnectionMethod(h.Network, h.PayloadSerializer, h.Identity, "C", false);
            h.Machine.StartClient(method);
            h.Network.RaiseClientConnected(0UL);
            Assert.That(h.Machine.CurrentState, Is.InstanceOf<ClientConnectedState>());

            // 원인 불명 끊김 -> Reconnecting
            h.Network.DisconnectReason = "";
            h.Network.RaiseClientDisconnected(0UL, "");
            Assert.That(h.Machine.CurrentState, Is.InstanceOf<ClientReconnectingState>());
        }

        [Test]
        public void UserRequestedDisconnectInHostingGoesOffline()
        {
            var h = StateHarness.Build(
                typeof(OfflineState), typeof(StartingHostState), typeof(HostingState),
                typeof(ClientConnectingState), typeof(ClientConnectedState), typeof(ClientReconnectingState));
            h.Machine.Start<OfflineState>();

            var method = new TestConnectionMethod(h.Network, h.PayloadSerializer, h.Identity, "H", false);
            h.Machine.StartHost(method);
            h.Network.RaiseServerStarted();

            h.Machine.RequestShutdown();

            Assert.That(h.Machine.CurrentState, Is.InstanceOf<OfflineState>());
            Assert.That(h.Network.ShutdownCalls, Is.GreaterThanOrEqualTo(1));
        }

        // 테스트 전용 ConnectionMethod — 네트워크 호출 없이 RequiresManualNetworkStart=true로 동작
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

Test Runner 4개 그린 확인.

- [ ] **Step 2: 커밋**

```bash
git add Tests/Editor/StateTransitionTests.cs
git commit -m "test(core): 상태 전이 통합 테스트 (Offline↔Host↔Hosting, 재연결 경로)"
```

---

## Phase 4: Builder + Facade

### Task 24: `LobbyBuilder` 기본 (필수 의존성 + 검증)

**Files:**
- Create: `Runtime/Core/Builder/LobbyBuilder.cs`
- Create: `Runtime/Core/Builder/LobbyConnection.cs` (forward declaration — 속성만)
- Test: `Tests/Editor/LobbyBuilderTests.cs`

- [ ] **Step 1: `LobbyConnection` forward declaration (최소 파사드)**

Create `Runtime/Core/Builder/LobbyConnection.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyConnection : IDisposable
    {
        readonly StateMachine.StateMachine m_Machine;
        readonly ISessionManager m_Sessions;
        readonly INetworkFacade m_Network;
        bool m_Disposed;

        internal LobbyConnection(StateMachine.StateMachine machine, ISessionManager sessions, INetworkFacade network)
        {
            m_Machine = machine;
            m_Sessions = sessions;
            m_Network = network;
        }

        public ISessionManager Sessions => m_Sessions;
        public INetworkFacade Network   => m_Network;

        public void StartClient(ConnectionMethodBase method) => m_Machine.StartClient(method);
        public void StartHost(ConnectionMethodBase method)   => m_Machine.StartHost(method);
        public void RequestShutdown()                         => m_Machine.RequestShutdown();

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Machine.Dispose();
        }
    }
}
```

> partial — Task 25에서 메시지 구독/이벤트 API를 추가 파일에 덧붙인다.

- [ ] **Step 2: 실패 테스트 작성**

Create `Tests/Editor/LobbyBuilderTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class LobbyBuilderTests
    {
        [Test]
        public void BuildWithoutNetworkThrows()
        {
            var b = new LobbyBuilder();
            Assert.Throws<System.InvalidOperationException>(() => b.Build());
        }

        [Test]
        public void BuildWithoutTickSourceThrows()
        {
            var b = new LobbyBuilder().UseNetwork(new FakeNetworkFacade());
            var ex = Assert.Throws<System.InvalidOperationException>(() => b.Build());
            Assert.That(ex.Message, Does.Contain("TickSource"));
        }

        [Test]
        public void BuildWithoutCoroutineRunnerThrows()
        {
            var b = new LobbyBuilder()
                .UseNetwork(new FakeNetworkFacade())
                .UseTickSource(new FakeTickSource());
            var ex = Assert.Throws<System.InvalidOperationException>(() => b.Build());
            Assert.That(ex.Message, Does.Contain("CoroutineRunner"));
        }

        [Test]
        public void BuildWithoutIdentityThrows()
        {
            var b = new LobbyBuilder()
                .UseNetwork(new FakeNetworkFacade())
                .UseTickSource(new FakeTickSource())
                .UseCoroutineRunner(new FakeCoroutineRunner());
            var ex = Assert.Throws<System.InvalidOperationException>(() => b.Build());
            Assert.That(ex.Message, Does.Contain("Identity"));
        }

        [Test]
        public void BuildWithoutPayloadSerializerThrows()
        {
            var b = new LobbyBuilder()
                .UseNetwork(new FakeNetworkFacade())
                .UseTickSource(new FakeTickSource())
                .UseCoroutineRunner(new FakeCoroutineRunner())
                .UseIdentity(new Connection.PlayerIdentity(new InMemoryPlayerIdentityStore()));
            var ex = Assert.Throws<System.InvalidOperationException>(() => b.Build());
            Assert.That(ex.Message, Does.Contain("PayloadSerializer"));
        }
    }
}
```

- [ ] **Step 3: `LobbyBuilder` 최소 구현 (필수 의존성 + 검증)**

Create `Runtime/Core/Builder/LobbyBuilder.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        INetworkFacade m_Network;
        ITickSource m_Tick;
        ICoroutineRunner m_Coroutines;
        IConnectionPayloadSerializer m_Serializer;
        PlayerIdentity m_Identity;

        public LobbyBuilder UseNetwork(INetworkFacade network)          { m_Network = network; return this; }
        public LobbyBuilder UseTickSource(ITickSource tick)             { m_Tick = tick; return this; }
        public LobbyBuilder UseCoroutineRunner(ICoroutineRunner runner) { m_Coroutines = runner; return this; }
        public LobbyBuilder UsePayloadSerializer(IConnectionPayloadSerializer s) { m_Serializer = s; return this; }
        public LobbyBuilder UseIdentity(PlayerIdentity identity)        { m_Identity = identity; return this; }

        public LobbyConnection Build()
        {
            if (m_Network     == null) throw new InvalidOperationException("LobbyBuilder.UseNetwork(...)이 호출되지 않았습니다.");
            if (m_Tick        == null) throw new InvalidOperationException("LobbyBuilder.UseTickSource(...)이 호출되지 않았습니다.");
            if (m_Coroutines  == null) throw new InvalidOperationException("LobbyBuilder.UseCoroutineRunner(...)이 호출되지 않았습니다.");
            if (m_Identity    == null) throw new InvalidOperationException("LobbyBuilder.UseIdentity(...)이 호출되지 않았습니다.");
            if (m_Serializer  == null) throw new InvalidOperationException("LobbyBuilder.UsePayloadSerializer(...)이 호출되지 않았습니다.");

            // 실제 조립은 Task 25~26에서 확장. 지금은 Null 구현으로 최소 빌드만 가능.
            throw new InvalidOperationException(
                "LobbyBuilder.Build()는 Task 25~26 완료 후 활성화됩니다. 현재는 검증만 수행.");
        }
    }
}
```

> Step 3의 `Build()` 마지막 throw는 의도적으로 placeholder 역할. Task 25에서 대체된다. 위 테스트는 `Build()`가 검증 에러 **전에** 도달해 필수 의존성 검증이 정확히 동작하는지만 확인 — 모두 검증 통과 후 마지막 `InvalidOperationException`에서 테스트가 실패한다. 이를 피하기 위해 **Task 24의 테스트는 검증 단계까지만** 다루고, "모든 의존성 충족된 Build 성공" 테스트는 Task 26에서 추가한다.

- [ ] **Step 4: 테스트 그린 확인 + 커밋**

Test Runner에서 5개 검증 테스트 그린 확인.

```bash
git add Runtime/Core/Builder Tests/Editor/LobbyBuilderTests.cs
git commit -m "feat(core): LobbyBuilder 필수 의존성 + 검증 + LobbyConnection forward-decl"
```

---

### Task 25: `LobbyBuilder` 선택 의존성 + 메시지 채널 + 상태 등록

**Files:**
- Modify: `Runtime/Core/Builder/LobbyBuilder.cs` (partial 확장)
- Create: `Runtime/Core/Builder/LobbyBuilder.States.cs`
- Create: `Runtime/Core/Builder/LobbyBuilder.Channels.cs`
- Create: `Runtime/Core/Builder/LobbyConnection.Messaging.cs`

- [ ] **Step 1: 선택 의존성 세터**

Append to `Runtime/Core/Builder/LobbyBuilder.cs` — `m_Serializer` 선언 뒤에 다음 필드/메서드 추가:
```csharp
// 선택 의존성 (Build 시 기본값 주입)
ILobbyLogger m_Logger;
ISessionManager m_Sessions;
IConnectionApprover m_Approver;
ReconnectPolicy? m_ReconnectPolicy;
int m_MaxConnectedPlayers = 8;
System.Func<ulong, ConnectionPayload, Session.ISessionPlayerData> m_CreatePlayerData;

public LobbyBuilder UseLogger(ILobbyLogger logger)            { m_Logger = logger; return this; }
public LobbyBuilder UseSessionManager(ISessionManager sm)     { m_Sessions = sm; return this; }
public LobbyBuilder UseApprover(IConnectionApprover approver) { m_Approver = approver; return this; }
public LobbyBuilder UseReconnectPolicy(ReconnectPolicy policy){ m_ReconnectPolicy = policy; return this; }
public LobbyBuilder UseMaxPlayers(int max)                    { m_MaxConnectedPlayers = max; return this; }
public LobbyBuilder UseSessionPlayerDataFactory(
    System.Func<ulong, ConnectionPayload, Session.ISessionPlayerData> factory)
{ m_CreatePlayerData = factory; return this; }
```

- [ ] **Step 2: 메시지 채널 등록 API**

Create `Runtime/Core/Builder/LobbyBuilder.Channels.cs`:
```csharp
using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        readonly Dictionary<Type, object> m_Channels = new();

        public LobbyBuilder UseDefaultMessageChannels()
        {
            AddChannel(new MessageChannel<ConnectStatus>());
            AddChannel(new MessageChannel<ReconnectMessage>());
            AddChannel(new MessageChannel<ConnectionEventMessage>());
            AddChannel(new MessageChannel<LobbyLifecycleMessage>());
            return this;
        }

        public LobbyBuilder AddMessageChannel<TMessage>()
        {
            AddChannel(new MessageChannel<TMessage>());
            return this;
        }

        public LobbyBuilder AddMessageChannel<TMessage>(IMessageChannel<TMessage> channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            m_Channels[typeof(TMessage)] = channel;
            return this;
        }

        void AddChannel<TMessage>(IMessageChannel<TMessage> channel)
            => m_Channels[typeof(TMessage)] = channel;

        internal IMessageChannel<TMessage> ResolveChannel<TMessage>()
        {
            if (m_Channels.TryGetValue(typeof(TMessage), out var ch))
                return (IMessageChannel<TMessage>)ch;
            throw new InvalidOperationException(
                $"Message channel not registered for {typeof(TMessage).Name}. Call UseDefaultMessageChannels() or AddMessageChannel<{typeof(TMessage).Name}>().");
        }
    }
}
```

- [ ] **Step 3: 상태 등록 API**

Create `Runtime/Core/Builder/LobbyBuilder.States.cs`:
```csharp
using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.StateMachine;
using Multiplayer.Lobby.States;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        readonly Dictionary<Type, Func<IStateMachineContext, ConnectionState>> m_StateFactories = new();

        public LobbyBuilder UseDefaultStates()
        {
            AddState(ctx => new OfflineState(ctx));
            AddState(ctx => new StartingHostState(ctx));
            AddState(ctx => new HostingState(ctx));
            AddState(ctx => new ClientConnectingState(ctx));
            AddState(ctx => new ClientConnectedState(ctx));
            AddState(ctx => new ClientReconnectingState(ctx));
            return this;
        }

        public LobbyBuilder AddState<TState>(Func<IStateMachineContext, TState> factory)
            where TState : ConnectionState
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (m_StateFactories.ContainsKey(typeof(TState)))
                throw new InvalidOperationException(
                    $"상태 {typeof(TState).Name}이 이미 등록되었습니다. ReplaceState<{typeof(TState).Name}>를 사용하십시오.");
            m_StateFactories[typeof(TState)] = ctx => factory(ctx);
            return this;
        }

        public LobbyBuilder ReplaceState<TState>(Func<IStateMachineContext, TState> factory)
            where TState : ConnectionState
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            m_StateFactories[typeof(TState)] = ctx => factory(ctx);
            return this;
        }
    }
}
```

- [ ] **Step 4: `LobbyConnection` PubSub 확장**

Create `Runtime/Core/Builder/LobbyConnection.Messaging.cs`:
```csharp
using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyConnection
    {
        internal Dictionary<Type, object> Channels { get; set; }

        public IPublisher<TMessage> GetPublisher<TMessage>()
            => ResolveChannel<TMessage>();

        public ISubscriber<TMessage> GetSubscriber<TMessage>()
            => ResolveChannel<TMessage>();

        IMessageChannel<TMessage> ResolveChannel<TMessage>()
        {
            if (Channels != null && Channels.TryGetValue(typeof(TMessage), out var ch))
                return (IMessageChannel<TMessage>)ch;
            throw new InvalidOperationException(
                $"Message channel not registered for {typeof(TMessage).Name}.");
        }
    }
}
```

- [ ] **Step 5: 커밋**

```bash
git add Runtime/Core/Builder
git commit -m "feat(core): LobbyBuilder 선택 의존성 + 메시지 채널 + 상태 등록 API"
```

---

### Task 26: `LobbyBuilder.Build()` 완성 + 생애주기 훅 + 통합 테스트

**Files:**
- Modify: `Runtime/Core/Builder/LobbyBuilder.cs` (`Build()` 실구현)
- Create: `Runtime/Core/Builder/LobbyBuilder.Lifecycle.cs`
- Create: `Runtime/Core/Builder/BuilderDependencies.cs` (IStateMachineContextDeps 구현체)
- Create: `Runtime/Core/Builder/LobbyConnection.Events.cs`
- Test: `Tests/Editor/LobbyBuilderBuildTests.cs`

- [ ] **Step 1: `BuilderDependencies` — IStateMachineContextDeps 구현**

Create `Runtime/Core/Builder/BuilderDependencies.cs`:
```csharp
using System;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Messaging;
using Multiplayer.Lobby.Session;
using Multiplayer.Lobby.StateMachine;

namespace Multiplayer.Lobby.Builder
{
    internal sealed class BuilderDependencies : IStateMachineContextDeps
    {
        public StateMachine.StateMachine StateMachine { get; set; }
        public INetworkFacade Network { get; set; }
        public ISessionManager Sessions { get; set; }
        public IConnectionApprover Approver { get; set; }
        public ILobbyLogger Logger { get; set; }
        public IConnectionPayloadSerializer PayloadSerializer { get; set; }
        public ICoroutineRunner CoroutineRunner { get; set; }
        public PlayerIdentity Identity { get; set; }
        public ReconnectPolicy ReconnectPolicy { get; set; }
        public int MaxConnectedPlayers { get; set; }
        public IPublisher<ConnectStatus> ConnectStatusPublisher { get; set; }
        public IPublisher<ReconnectMessage> ReconnectPublisher { get; set; }
        public IPublisher<ConnectionEventMessage> ConnectionEventPublisher { get; set; }
        public IPublisher<LobbyLifecycleMessage> LifecyclePublisher { get; set; }
        public Func<ulong, ConnectionPayload, ISessionPlayerData> CreatePlayerData { get; set; }
    }
}
```

- [ ] **Step 2: `LobbyBuilder.Build()` 실구현**

Replace the placeholder `Build()` in `Runtime/Core/Builder/LobbyBuilder.cs` with:
```csharp
public LobbyConnection Build()
{
    if (m_Network     == null) throw new InvalidOperationException("LobbyBuilder.UseNetwork(...)이 호출되지 않았습니다.");
    if (m_Tick        == null) throw new InvalidOperationException("LobbyBuilder.UseTickSource(...)이 호출되지 않았습니다.");
    if (m_Coroutines  == null) throw new InvalidOperationException("LobbyBuilder.UseCoroutineRunner(...)이 호출되지 않았습니다.");
    if (m_Identity    == null) throw new InvalidOperationException("LobbyBuilder.UseIdentity(...)이 호출되지 않았습니다.");
    if (m_Serializer  == null) throw new InvalidOperationException("LobbyBuilder.UsePayloadSerializer(...)이 호출되지 않았습니다.");
    if (m_StateFactories.Count == 0)
        throw new InvalidOperationException("상태가 등록되지 않았습니다. UseDefaultStates() 또는 AddState<...>를 호출하십시오.");
    if (m_Channels.Count == 0)
        throw new InvalidOperationException("메시지 채널이 등록되지 않았습니다. UseDefaultMessageChannels()를 호출하십시오.");

    var deps = new BuilderDependencies
    {
        Network = m_Network,
        CoroutineRunner = m_Coroutines,
        PayloadSerializer = m_Serializer,
        Identity = m_Identity,
        Logger = m_Logger ?? NullLogger.Instance,
        Sessions = m_Sessions ?? new Session.SessionManager(m_Logger ?? NullLogger.Instance),
        Approver = m_Approver ?? new DefaultConnectionApprover(m_MaxConnectedPlayers),
        ReconnectPolicy = m_ReconnectPolicy ?? Connection.ReconnectPolicy.Default,
        MaxConnectedPlayers = m_MaxConnectedPlayers,
        ConnectStatusPublisher     = ResolveChannel<ConnectStatus>(),
        ReconnectPublisher         = ResolveChannel<Connection.ReconnectMessage>(),
        ConnectionEventPublisher   = ResolveChannel<Connection.ConnectionEventMessage>(),
        LifecyclePublisher         = ResolveChannel<Messaging.LobbyLifecycleMessage>(),
        CreatePlayerData = m_CreatePlayerData
    };

    var states = new System.Collections.Generic.Dictionary<System.Type, StateMachine.ConnectionState>();
    var machine = new StateMachine.StateMachine(states, deps.Network, deps.Logger);
    deps.StateMachine = machine;
    var ctx = new StateMachine.StateMachineContext(deps);

    foreach (var kv in m_StateFactories)
        states[kv.Key] = kv.Value(ctx);

    var conn = new LobbyConnection(machine, deps.Sessions, deps.Network)
    {
        Channels = new System.Collections.Generic.Dictionary<System.Type, object>(m_Channels)
    };

    // 생애주기 이벤트 재발행 배선 (Task 26 Step 4 참고)
    conn.BindLifecycle(ResolveChannel<Messaging.LobbyLifecycleMessage>());

    // 초기 상태 전이: OfflineState (기본 프리셋에 반드시 포함)
    if (!states.ContainsKey(typeof(States.OfflineState)))
        throw new InvalidOperationException("OfflineState가 등록되지 않았습니다. UseDefaultStates() 또는 초기 상태로 OfflineState를 AddState하십시오.");
    machine.Start<States.OfflineState>();

    return conn;
}
```

- [ ] **Step 3: 생애주기 훅 편의 API**

Create `Runtime/Core/Builder/LobbyBuilder.Lifecycle.cs`:
```csharp
using System;
using System.Collections.Generic;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyBuilder
    {
        readonly List<Action> m_OnHostStarted    = new();
        readonly List<Action> m_OnClientConnected = new();
        readonly List<Action> m_OnDisconnected   = new();

        public LobbyBuilder OnHostStarted(Action handler)    { m_OnHostStarted.Add(handler); return this; }
        public LobbyBuilder OnClientConnected(Action handler){ m_OnClientConnected.Add(handler); return this; }
        public LobbyBuilder OnDisconnected(Action handler)   { m_OnDisconnected.Add(handler); return this; }

        internal void ApplyLifecycleHooks(LobbyConnection conn)
        {
            foreach (var h in m_OnHostStarted)     conn.OnHostStarted    += h;
            foreach (var h in m_OnClientConnected) conn.OnClientConnected += h;
            foreach (var h in m_OnDisconnected)    conn.OnDisconnected   += h;
        }
    }
}
```

Append to `Build()` 끝부분 (return 전):
```csharp
ApplyLifecycleHooks(conn);
```

- [ ] **Step 4: `LobbyConnection` C# 이벤트 + PubSub 구독 재발행**

Create `Runtime/Core/Builder/LobbyConnection.Events.cs`:
```csharp
using System;
using Multiplayer.Lobby.Messaging;

namespace Multiplayer.Lobby.Builder
{
    public sealed partial class LobbyConnection
    {
        public event Action OnHostStarted;
        public event Action OnClientConnected;
        public event Action OnDisconnected;

        IDisposable m_LifecycleSubscription;

        internal void BindLifecycle(IMessageChannel<LobbyLifecycleMessage> channel)
        {
            m_LifecycleSubscription = channel.Subscribe(OnLifecycleMessage);
        }

        void OnLifecycleMessage(LobbyLifecycleMessage msg)
        {
            switch (msg)
            {
                case LobbyLifecycleMessage.HostStarted:    OnHostStarted?.Invoke(); break;
                case LobbyLifecycleMessage.ClientConnected: OnClientConnected?.Invoke(); break;
                case LobbyLifecycleMessage.Disconnected:   OnDisconnected?.Invoke(); break;
            }
        }
    }
}
```

Modify `Dispose()` in `LobbyConnection.cs`:
```csharp
public void Dispose()
{
    if (m_Disposed) return;
    m_Disposed = true;
    m_LifecycleSubscription?.Dispose();
    m_Machine.Dispose();
}
```

- [ ] **Step 5: 통합 테스트 — 성공 빌드 + 이벤트 발행**

Create `Tests/Editor/LobbyBuilderBuildTests.cs`:
```csharp
using NUnit.Framework;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Tests.Fakes;

namespace Multiplayer.Lobby.Tests
{
    public class LobbyBuilderBuildTests
    {
        LobbyBuilder MakeComplete() =>
            new LobbyBuilder()
                .UseNetwork(new FakeNetworkFacade())
                .UseTickSource(new FakeTickSource())
                .UseCoroutineRunner(new FakeCoroutineRunner())
                .UsePayloadSerializer(new FakeConnectionPayloadSerializer())
                .UseIdentity(new PlayerIdentity(new InMemoryPlayerIdentityStore()))
                .UseDefaultMessageChannels()
                .UseDefaultStates();

        [Test]
        public void FullyConfiguredBuildSucceeds()
        {
            using var lobby = MakeComplete().Build();
            Assert.That(lobby, Is.Not.Null);
        }

        [Test]
        public void HostStartedEventFiresOnLifecyclePublish()
        {
            using var lobby = MakeComplete().Build();
            var fired = 0;
            lobby.OnHostStarted += () => fired++;

            lobby.GetPublisher<Messaging.LobbyLifecycleMessage>()
                 .Publish(Messaging.LobbyLifecycleMessage.HostStarted);

            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void BuilderLifecycleHookIsApplied()
        {
            var fired = 0;
            using var lobby = MakeComplete()
                .OnDisconnected(() => fired++)
                .Build();

            lobby.GetPublisher<Messaging.LobbyLifecycleMessage>()
                 .Publish(Messaging.LobbyLifecycleMessage.Disconnected);
            // Build() 시 OfflineState.Enter()가 호출되어 초기 Disconnected도 이미 발행됨
            Assert.That(fired, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void DuplicateAddStateThrows()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                new LobbyBuilder()
                    .UseDefaultStates()
                    .AddState(ctx => new States.OfflineState(ctx)));
        }

        [Test]
        public void ReplaceStateOverridesFactory()
        {
            using var lobby = MakeComplete()
                .ReplaceState(ctx => new States.OfflineState(ctx))
                .Build();
            Assert.That(lobby, Is.Not.Null);
        }
    }
}
```

Test Runner 5개 그린 확인.

- [ ] **Step 6: 커밋**

```bash
git add Runtime/Core/Builder Tests/Editor/LobbyBuilderBuildTests.cs
git commit -m "feat(core): LobbyBuilder.Build() 완성 + 생애주기 훅 + 통합 테스트"
```

---

## Phase 5: 어댑터 레이어 (Unity/Netcode)

### Task 27: `UnityDebugLogger` + `MonoBehaviourTickSource` + `MonoBehaviourCoroutineRunner`

**Files:**
- Create: `Runtime/Adapters/Unity/UnityDebugLogger.cs`
- Create: `Runtime/Adapters/Unity/MonoBehaviourTickSource.cs`
- Create: `Runtime/Adapters/Unity/MonoBehaviourCoroutineRunner.cs`

- [ ] **Step 1: `UnityDebugLogger` 작성**

Create `Runtime/Adapters/Unity/UnityDebugLogger.cs`:
```csharp
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class UnityDebugLogger : ILobbyLogger
    {
        const string k_Prefix = "[Lobby] ";
        public void Info(string message)    => UnityEngine.Debug.Log(k_Prefix + message);
        public void Warning(string message) => UnityEngine.Debug.LogWarning(k_Prefix + message);
        public void Error(string message)   => UnityEngine.Debug.LogError(k_Prefix + message);
    }
}
```

- [ ] **Step 2: `MonoBehaviourTickSource` 작성**

Create `Runtime/Adapters/Unity/MonoBehaviourTickSource.cs`:
```csharp
using System;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class MonoBehaviourTickSource : MonoBehaviour, ITickSource
    {
        public event Action OnUpdate;
        public event Action OnLateUpdate;
        void Update()     => OnUpdate?.Invoke();
        void LateUpdate() => OnLateUpdate?.Invoke();
    }
}
```

- [ ] **Step 3: `MonoBehaviourCoroutineRunner` 작성 (yield return double 지원)**

Create `Runtime/Adapters/Unity/MonoBehaviourCoroutineRunner.cs`:
```csharp
using System.Collections;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class MonoBehaviourCoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        public object Start(IEnumerator routine)
            => StartCoroutine(Wrap(routine));

        public void Stop(object handle)
        {
            if (handle is Coroutine c) StopCoroutine(c);
        }

        // Core가 yield return double/float 하면 WaitForSeconds로 해석.
        // 기타는 Unity 기본 처리에 위임.
        static IEnumerator Wrap(IEnumerator inner)
        {
            while (inner.MoveNext())
            {
                switch (inner.Current)
                {
                    case double d: yield return new WaitForSeconds((float)d); break;
                    case float f:  yield return new WaitForSeconds(f); break;
                    default:       yield return inner.Current; break;
                }
            }
        }
    }
}
```

- [ ] **Step 4: 커밋**

```bash
git add Runtime/Adapters/Unity
git commit -m "feat(adapters): UnityDebugLogger + MonoBehaviourTickSource + CoroutineRunner"
```

---

### Task 28: `JsonUtilityConnectionPayloadSerializer` + `PlayerPrefsPlayerIdentityStore` + `NetworkedMessageChannel` 이전

**Files:**
- Create: `Runtime/Adapters/Unity/JsonUtilityConnectionPayloadSerializer.cs`
- Create: `Runtime/Adapters/Unity/PlayerPrefsPlayerIdentityStore.cs`
- Create: `Runtime/Adapters/Netcode/NetworkedMessageChannel.cs` (기존 PubSub에서 이전)

- [ ] **Step 1: `JsonUtilityConnectionPayloadSerializer`**

Create `Runtime/Adapters/Unity/JsonUtilityConnectionPayloadSerializer.cs`:
```csharp
using System.Text;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class JsonUtilityConnectionPayloadSerializer : IConnectionPayloadSerializer
    {
        public byte[] Serialize(ConnectionPayload payload)
        {
            var json = JsonUtility.ToJson(payload);
            return Encoding.UTF8.GetBytes(json);
        }

        public ConnectionPayload Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            var json = Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<ConnectionPayload>(json);
        }
    }
}
```

- [ ] **Step 2: `PlayerPrefsPlayerIdentityStore`**

Create `Runtime/Adapters/Unity/PlayerPrefsPlayerIdentityStore.cs`:
```csharp
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Multiplayer.Lobby.Abstractions;

namespace Multiplayer.Lobby.Adapters.Unity
{
    public sealed class PlayerPrefsPlayerIdentityStore : IPlayerIdentityStore
    {
        const string k_GuidKey = "lobby_player_guid";
        const string k_ProfileCommandLineArg = "-AuthProfile";

        public string GetOrCreateGuid(string profile)
        {
            var key = string.IsNullOrEmpty(profile) ? k_GuidKey : $"{k_GuidKey}_{profile}";
            var guid = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(key, guid);
                PlayerPrefs.Save();
            }
            return guid;
        }

        public string ResolveProfile()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
                if (args[i] == k_ProfileCommandLineArg && i + 1 < args.Length) return args[i + 1];

#if UNITY_EDITOR
            var hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashed, 16);
            return new Guid(hashed).ToString("N").Substring(0, 30);
#else
            return "";
#endif
        }
    }
}
```

- [ ] **Step 3: `NetworkedMessageChannel` Adapters로 이전**

기존 `Runtime/Infrastructure/PubSub/NetworkedMessageChannel.cs` 내용을 읽어 namespace를 `Multiplayer.Lobby.Adapters.Netcode`로 변경하고 `Runtime/Adapters/Netcode/NetworkedMessageChannel.cs`에 저장. 내부에서 `MessageChannel<T>` 참조는 `Multiplayer.Lobby.Messaging.MessageChannel<T>`로, `IPublisher<T>`는 `Multiplayer.Lobby.Messaging.IPublisher<T>`로 변경.

> 기존 파일은 Task 33에서 삭제.

- [ ] **Step 4: 커밋**

```bash
git add Runtime/Adapters
git commit -m "feat(adapters): JsonUtility 직렬화 + PlayerPrefs 저장소 + NetworkedMessageChannel 이전"
```

---

### Task 29: `NetcodeNetworkFacade` 구현

**Files:**
- Create: `Runtime/Adapters/Netcode/NetcodeNetworkFacade.cs`

- [ ] **Step 1: 구현**

Create `Runtime/Adapters/Netcode/NetcodeNetworkFacade.cs`:
```csharp
using System;
using Unity.Netcode;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.Adapters.Netcode
{
    public sealed class NetcodeNetworkFacade : INetworkFacade, IDisposable
    {
        readonly NetworkManager m_Nm;
        Func<ApprovalRequest, ApprovalResult> m_ApprovalHandler;

        public NetcodeNetworkFacade(NetworkManager networkManager)
        {
            m_Nm = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            m_Nm.OnServerStarted        += InvokeServerStarted;
            m_Nm.OnServerStopped        += InvokeServerStopped;
            m_Nm.OnTransportFailure     += InvokeTransportFailure;
            m_Nm.OnConnectionEvent      += OnConnectionEvent;
            m_Nm.ConnectionApprovalCallback += OnApprovalCallback;
        }

        public bool IsClient             => m_Nm.IsClient;
        public bool IsServer             => m_Nm.IsServer;
        public bool IsHost               => m_Nm.IsHost;
        public bool IsListening          => m_Nm.IsListening;
        public bool ShutdownInProgress   => m_Nm.ShutdownInProgress;
        public ulong LocalClientId       => m_Nm.LocalClientId;

        public byte[] ConnectionPayload
        {
            get => m_Nm.NetworkConfig.ConnectionData;
            set => m_Nm.NetworkConfig.ConnectionData = value;
        }

        public bool StartClient()                                         => m_Nm.StartClient();
        public bool StartHost()                                           => m_Nm.StartHost();
        public void Shutdown(bool discardMessageQueue = false)            => m_Nm.Shutdown(discardMessageQueue);
        public void DisconnectClient(ulong clientId, string reason = null)
        {
            if (string.IsNullOrEmpty(reason)) m_Nm.DisconnectClient(clientId);
            else m_Nm.DisconnectClient(clientId, reason);
        }
        public string GetDisconnectReason(ulong clientId)                 => m_Nm.DisconnectReason;

        public event Action OnServerStarted;
        public event Action<bool> OnServerStopped;
        public event Action OnTransportFailure;
        public event Action<ulong> OnClientConnected;
        public event Action<ulong, string> OnClientDisconnected;

        public event Func<ApprovalRequest, ApprovalResult> ApprovalCheck
        {
            add { m_ApprovalHandler = value; }      // 단일 핸들러 규약 — 덮어쓰기
            remove
            {
                if (m_ApprovalHandler == value) m_ApprovalHandler = null;
            }
        }

        void OnConnectionEvent(NetworkManager nm, ConnectionEventData data)
        {
            switch (data.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    OnClientConnected?.Invoke(data.ClientId);
                    break;
                case ConnectionEvent.ClientDisconnected:
                    OnClientDisconnected?.Invoke(data.ClientId, m_Nm.DisconnectReason);
                    break;
            }
        }

        void OnApprovalCallback(NetworkManager.ConnectionApprovalRequest req,
                                NetworkManager.ConnectionApprovalResponse res)
        {
            var request = new ApprovalRequest(req.ClientNetworkId, req.Payload, m_Nm.ConnectedClientsIds.Count);
            var result = m_ApprovalHandler?.Invoke(request) ?? ApprovalResult.Allow();
            res.Approved = result.Approved;
            res.Reason   = result.Reason;
            res.CreatePlayerObject = result.Approved;
            res.Position = UnityEngine.Vector3.zero;
            res.Rotation = UnityEngine.Quaternion.identity;
        }

        void InvokeServerStarted()            => OnServerStarted?.Invoke();
        void InvokeServerStopped(bool isHost) => OnServerStopped?.Invoke(isHost);
        void InvokeTransportFailure()         => OnTransportFailure?.Invoke();

        public void Dispose()
        {
            m_Nm.OnServerStarted        -= InvokeServerStarted;
            m_Nm.OnServerStopped        -= InvokeServerStopped;
            m_Nm.OnTransportFailure     -= InvokeTransportFailure;
            m_Nm.OnConnectionEvent      -= OnConnectionEvent;
            m_Nm.ConnectionApprovalCallback -= OnApprovalCallback;
        }
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Runtime/Adapters/Netcode/NetcodeNetworkFacade.cs
git commit -m "feat(adapters): NetcodeNetworkFacade (NetworkManager → INetworkFacade 어댑터)"
```

---

### Task 30: `LobbyConnectionHost` MonoBehaviour 편의 컴포넌트

**Files:**
- Create: `Runtime/Adapters/Unity/LobbyConnectionHost.cs`

- [ ] **Step 1: 구현**

Create `Runtime/Adapters/Unity/LobbyConnectionHost.cs`:
```csharp
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
```

- [ ] **Step 2: 커밋**

```bash
git add Runtime/Adapters/Unity/LobbyConnectionHost.cs
git commit -m "feat(adapters): LobbyConnectionHost 편의 MonoBehaviour"
```

---

## Phase 6: ConnectionMethods/IP

### Task 31: `IPConnectionMethod` + `IPConnectionConfig` 이전 및 시그니처 변경

**Files:**
- Create: `Runtime/ConnectionMethods/IP/IPConnectionMethod.cs`
- Create: `Runtime/ConnectionMethods/IP/IPConnectionConfig.cs`

- [ ] **Step 1: `IPConnectionConfig` 이전**

Create `Runtime/ConnectionMethods/IP/IPConnectionConfig.cs`:
```csharp
using System;

namespace Multiplayer.Lobby.ConnectionMethods.IP
{
    [Serializable]
    public class IPConnectionConfig
    {
        public string IpAddress = "127.0.0.1";
        public ushort Port = 9998;
    }
}
```

- [ ] **Step 2: `IPConnectionMethod` 이전 + 시그니처 변경**

Create `Runtime/ConnectionMethods/IP/IPConnectionMethod.cs`:
```csharp
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.ConnectionMethods.IP
{
    public sealed class IPConnectionMethod : ConnectionMethodBase
    {
        readonly NetworkManager m_NetworkManager;  // UTP 설정을 위해 어댑터 경유가 아닌 직접 접근 (ConnectionMethods/IP는 Netcode 참조 허용)
        readonly string m_IpAddress;
        readonly ushort m_Port;

        public IPConnectionMethod(
            INetworkFacade network,
            IConnectionPayloadSerializer serializer,
            NetworkManager networkManager,
            PlayerIdentity playerIdentity,
            string playerName,
            string ipAddress,
            ushort port,
            bool isDebug)
            : base(network, serializer, playerIdentity, playerName, isDebug)
        {
            m_NetworkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            m_IpAddress = ipAddress;
            m_Port = port;
        }

        public override void SetupClientConnection() => SetupConnection();
        public override void SetupHostConnection()   => SetupConnection();

        void SetupConnection()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName);
            var transport = m_NetworkManager.NetworkConfig.NetworkTransport;
            if (transport == null)
                throw new InvalidOperationException("NetworkTransport is not configured on the NetworkManager.");
            ((UnityTransport)transport).SetConnectionData(m_IpAddress, m_Port);
        }

        public override Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
            => Task.FromResult((true, true));
    }
}
```

- [ ] **Step 3: 커밋**

```bash
git add Runtime/ConnectionMethods/IP/IPConnectionConfig.cs Runtime/ConnectionMethods/IP/IPConnectionMethod.cs
git commit -m "feat(ip): IPConnectionMethod 이전 + INetworkFacade/IPayloadSerializer 주입"
```

---

### Task 32: `LobbyConnectionIpExtensions`

**Files:**
- Create: `Runtime/ConnectionMethods/IP/LobbyConnectionIpExtensions.cs`

- [ ] **Step 1: 구현**

Create `Runtime/ConnectionMethods/IP/LobbyConnectionIpExtensions.cs`:
```csharp
using System;
using Unity.Netcode;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;

namespace Multiplayer.Lobby.ConnectionMethods.IP
{
    public static class LobbyConnectionIpExtensions
    {
        /// <summary>
        /// IP 직접 연결로 클라이언트를 시작한다. NetworkManager가 필요하므로 파라미터로 받는다
        /// (LobbyConnection은 INetworkFacade만 알고 NetworkManager는 모름).
        /// </summary>
        public static void StartClientIp(this LobbyConnection lobby,
            NetworkManager networkManager,
            PlayerIdentity identity,
            IConnectionPayloadSerializer serializer,
            string playerName, string ipAddress, int port,
            bool isDebug)
        {
            ValidateIpParams(playerName, ipAddress, port);
            var method = new IPConnectionMethod(
                lobby.Network, serializer, networkManager, identity,
                playerName, ipAddress, (ushort)port, isDebug);
            lobby.StartClient(method);
        }

        public static void StartHostIp(this LobbyConnection lobby,
            NetworkManager networkManager,
            PlayerIdentity identity,
            IConnectionPayloadSerializer serializer,
            string playerName, string ipAddress, int port,
            bool isDebug)
        {
            ValidateIpParams(playerName, ipAddress, port);
            var method = new IPConnectionMethod(
                lobby.Network, serializer, networkManager, identity,
                playerName, ipAddress, (ushort)port, isDebug);
            lobby.StartHost(method);
        }

        static void ValidateIpParams(string name, string ip, int port)
        {
            if (string.IsNullOrEmpty(name))  throw new ArgumentException("playerName cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(ip))    throw new ArgumentException("ipAddress cannot be null or empty", nameof(ip));
            if (port < 0 || port > 65535)    throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 0 and 65535");
        }
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Runtime/ConnectionMethods/IP/LobbyConnectionIpExtensions.cs
git commit -m "feat(ip): StartClientIp/StartHostIp 확장 메서드"
```

---

## Phase 7: 레거시 제거

### Task 33: 기존 `LobbyConnectionManager` / `UpdateRunner` / 기존 asmdef / 기존 샘플 삭제

**Files (모두 삭제):**
- Delete: `Runtime/Core/LobbyConnectionManager.cs`
- Delete: `Runtime/Core/ConnectionState.cs`
- Delete: `Runtime/Core/OnlineState.cs`
- Delete: `Runtime/Core/ConnectionMethodBase.cs` (기존, Core/Connection/으로 대체됨)
- Delete: `Runtime/Core/ConnectionPayload.cs`
- Delete: `Runtime/Core/ConnectStatus.cs`
- Delete: `Runtime/Core/PlayerIdentity.cs`
- Delete: `Runtime/Core/Session/SessionManager.cs` (기존, 새 SessionManager로 대체)
- Delete: `Runtime/Core/Session/ISessionPlayerData.cs` (기존, 새 것으로 대체)
- Delete: `Runtime/Core/States/` (전체 — OfflineState.cs 등 6개)
- Delete: `Runtime/Infrastructure/` (전체 — UpdateRunner + PubSub 6개)
- Delete: `Runtime/IP/` (전체 — 기존 IPConnectionMethod/IPConnectionConfig)
- Delete: `Runtime/Multiplayer.Lobby.asmdef`
- Delete: `Samples~/LobbyTest/` (전체)

- [ ] **Step 1: 기존 Core 파일 삭제**

```bash
git rm Runtime/Core/LobbyConnectionManager.cs \
       Runtime/Core/ConnectionState.cs \
       Runtime/Core/OnlineState.cs \
       Runtime/Core/ConnectionMethodBase.cs \
       Runtime/Core/ConnectionPayload.cs \
       Runtime/Core/ConnectStatus.cs \
       Runtime/Core/PlayerIdentity.cs
git rm Runtime/Core/Session/SessionManager.cs \
       Runtime/Core/Session/ISessionPlayerData.cs
git rm -r Runtime/Core/States
```

> 단, Runtime/Core/Session/SessionManager.cs / ISessionPlayerData.cs는 Task 13에서 같은 경로에 **새** 버전을 만들었다. Task 13이 이미 끝난 상태라면 이 `git rm`는 불필요. 실제로는 `git rm` 대신 단순히 `git add Runtime/Core/Session`으로 덮어쓰기 돼야 한다. 이 Step은 **기존 Core 루트의 클래스들**과 `States/` 디렉토리를 제거하는 것이 목적.

실제 수행:
```bash
git rm Runtime/Core/LobbyConnectionManager.cs Runtime/Core/ConnectionState.cs Runtime/Core/OnlineState.cs Runtime/Core/ConnectionMethodBase.cs Runtime/Core/ConnectionPayload.cs Runtime/Core/ConnectStatus.cs Runtime/Core/PlayerIdentity.cs
git rm -r Runtime/Core/States
```

- [ ] **Step 2: Infrastructure 및 IP 전체 삭제**

```bash
git rm -r Runtime/Infrastructure
git rm -r Runtime/IP
```

- [ ] **Step 3: 기존 asmdef 삭제**

```bash
git rm Runtime/Multiplayer.Lobby.asmdef
```

- [ ] **Step 4: 기존 샘플 삭제**

```bash
git rm -r Samples~/LobbyTest
```

- [ ] **Step 5: 빌드 검증**

Unity 에디터에서 Assembly reload 진행. 모든 컴파일 성공해야 한다. 에러가 나면 어느 참조가 남았는지 확인 후 수정. 그 후 EditMode 테스트 전체 그린 확인.

- [ ] **Step 6: 커밋**

```bash
git commit -m "chore: 레거시 제거 — LobbyConnectionManager/UpdateRunner/기존 asmdef/기존 샘플"
```

---

## Phase 8: 샘플

### Task 34: `Samples~/BasicManual/` 작성

**Files:**
- Create: `Samples~/BasicManual/Multiplayer.Lobby.Sample.BasicManual.asmdef`
- Create: `Samples~/BasicManual/BasicLobbyBootstrapper.cs`
- Create: `Samples~/BasicManual/BasicLobbyUI.cs`
- Create: `Samples~/BasicManual/SampleSessionPlayerData.cs`
- Create: `Samples~/BasicManual/BasicManual.unity` (수동 작성 — Unity Editor에서 새 씬 만들고 저장. 세부는 Step 5)

- [ ] **Step 1: asmdef 작성**

Create `Samples~/BasicManual/Multiplayer.Lobby.Sample.BasicManual.asmdef`:
```json
{
    "name": "Multiplayer.Lobby.Sample.BasicManual",
    "rootNamespace": "Multiplayer.Lobby.Sample.BasicManual",
    "references": [
        "Multiplayer.Lobby.Core",
        "Multiplayer.Lobby.Adapters",
        "Multiplayer.Lobby.ConnectionMethods.IP",
        "Unity.Netcode.Runtime"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: `SampleSessionPlayerData` 작성**

Create `Samples~/BasicManual/SampleSessionPlayerData.cs`:
```csharp
using Multiplayer.Lobby.Session;

namespace Multiplayer.Lobby.Sample.BasicManual
{
    public sealed class SampleSessionPlayerData : ISessionPlayerData
    {
        public bool IsConnected { get; set; } = true;
        public ulong ClientID { get; set; }
        public string PlayerName { get; set; }

        public SampleSessionPlayerData(ulong clientId, string name)
        {
            ClientID = clientId;
            PlayerName = name;
        }

        public void Reinitialize() { /* 게임 진입 시 리셋할 상태 있으면 여기에 */ }
    }
}
```

- [ ] **Step 3: `BasicLobbyBootstrapper` 작성**

Create `Samples~/BasicManual/BasicLobbyBootstrapper.cs`:
```csharp
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
```

- [ ] **Step 4: `BasicLobbyUI` 작성**

Create `Samples~/BasicManual/BasicLobbyUI.cs`:
```csharp
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.ConnectionMethods.IP;

namespace Multiplayer.Lobby.Sample.BasicManual
{
    public sealed class BasicLobbyUI : MonoBehaviour
    {
        [SerializeField] InputField m_PlayerNameField;
        [SerializeField] InputField m_IpField;
        [SerializeField] InputField m_PortField;
        [SerializeField] Button m_HostButton;
        [SerializeField] Button m_ClientButton;
        [SerializeField] Button m_ShutdownButton;
        [SerializeField] Text m_StatusText;

        LobbyConnection m_Lobby;
        NetworkManager m_Nm;
        PlayerIdentity m_Identity;
        IConnectionPayloadSerializer m_Serializer;

        public void Bind(LobbyConnection lobby, NetworkManager nm, PlayerIdentity identity, IConnectionPayloadSerializer serializer)
        {
            m_Lobby = lobby; m_Nm = nm; m_Identity = identity; m_Serializer = serializer;

            m_HostButton.onClick.AddListener(OnHost);
            m_ClientButton.onClick.AddListener(OnClient);
            m_ShutdownButton.onClick.AddListener(OnShutdown);

            m_Lobby.OnHostStarted     += () => SetStatus("Host started");
            m_Lobby.OnClientConnected += () => SetStatus("Client connected");
            m_Lobby.OnDisconnected    += () => SetStatus("Disconnected");

            m_Lobby.GetSubscriber<Connection.ConnectStatus>()
                   .Subscribe(s => SetStatus($"Status: {s}"));
        }

        void OnHost()
            => m_Lobby.StartHostIp(m_Nm, m_Identity, m_Serializer,
                m_PlayerNameField.text, m_IpField.text, int.Parse(m_PortField.text),
                Debug.isDebugBuild);

        void OnClient()
            => m_Lobby.StartClientIp(m_Nm, m_Identity, m_Serializer,
                m_PlayerNameField.text, m_IpField.text, int.Parse(m_PortField.text),
                Debug.isDebugBuild);

        void OnShutdown() => m_Lobby.RequestShutdown();

        void SetStatus(string s) { if (m_StatusText) m_StatusText.text = s; }
    }
}
```

- [ ] **Step 5: 씬 파일 생성**

Unity 에디터에서:
1. 새 씬 생성, `Samples~/BasicManual/BasicManual.unity`로 저장.
2. GameObject 3개 추가:
   - `NetworkManager` (Unity Netcode 기본 `NetworkManager` 컴포넌트 + UnityTransport 붙임).
   - `Bootstrapper`: `BasicLobbyBootstrapper` 컴포넌트 부착, NetworkManager/BasicLobbyUI 필드 연결.
   - `Canvas`: `BasicLobbyUI` 컴포넌트, 인풋 필드 3개(이름/IP/포트), 버튼 3개(Host/Client/Shutdown), Text(상태) 배치.
3. 씬 저장 후 Play 모드에서 수동 검증: Host 버튼 → Host started 표시 확인.

- [ ] **Step 6: 커밋**

```bash
git add Samples~/BasicManual
git commit -m "feat(samples): BasicManual 샘플 — DI 컨테이너 없음, 수동 배선"
```

---

### Task 35: `Samples~/VContainerIntegration/` 작성

**Files:**
- Create: `Samples~/VContainerIntegration/Multiplayer.Lobby.Sample.VContainer.asmdef`
- Create: `Samples~/VContainerIntegration/VContainerLobbyLifetimeScope.cs`
- Create: `Samples~/VContainerIntegration/VContainerLobbyUI.cs`
- Create: `Samples~/VContainerIntegration/VContainerIntegration.unity`

- [ ] **Step 1: asmdef 작성**

Create `Samples~/VContainerIntegration/Multiplayer.Lobby.Sample.VContainer.asmdef`:
```json
{
    "name": "Multiplayer.Lobby.Sample.VContainer",
    "rootNamespace": "Multiplayer.Lobby.Sample.VContainer",
    "references": [
        "Multiplayer.Lobby.Core",
        "Multiplayer.Lobby.Adapters",
        "Multiplayer.Lobby.ConnectionMethods.IP",
        "Unity.Netcode.Runtime",
        "VContainer"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: `VContainerLobbyLifetimeScope` 작성**

Create `Samples~/VContainerIntegration/VContainerLobbyLifetimeScope.cs`:
```csharp
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Adapters.Netcode;
using Multiplayer.Lobby.Adapters.Unity;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.Sample.BasicManual;

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
```

- [ ] **Step 3: `VContainerLobbyUI` 작성**

Create `Samples~/VContainerIntegration/VContainerLobbyUI.cs`:
```csharp
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Multiplayer.Lobby.Abstractions;
using Multiplayer.Lobby.Builder;
using Multiplayer.Lobby.Connection;
using Multiplayer.Lobby.ConnectionMethods.IP;

namespace Multiplayer.Lobby.Sample.VContainer
{
    public sealed class VContainerLobbyUI : MonoBehaviour
    {
        [SerializeField] InputField m_NameField;
        [SerializeField] InputField m_IpField;
        [SerializeField] InputField m_PortField;
        [SerializeField] Button m_HostBtn;
        [SerializeField] Button m_ClientBtn;
        [SerializeField] Button m_ShutdownBtn;
        [SerializeField] Text m_StatusText;

        [Inject] LobbyConnection m_Lobby;
        [Inject] NetworkManager m_Nm;
        [Inject] PlayerIdentity m_Identity;
        [Inject] IConnectionPayloadSerializer m_Serializer;

        void Start()
        {
            m_HostBtn.onClick.AddListener(() =>
                m_Lobby.StartHostIp(m_Nm, m_Identity, m_Serializer,
                    m_NameField.text, m_IpField.text, int.Parse(m_PortField.text), Debug.isDebugBuild));
            m_ClientBtn.onClick.AddListener(() =>
                m_Lobby.StartClientIp(m_Nm, m_Identity, m_Serializer,
                    m_NameField.text, m_IpField.text, int.Parse(m_PortField.text), Debug.isDebugBuild));
            m_ShutdownBtn.onClick.AddListener(m_Lobby.RequestShutdown);

            m_Lobby.OnHostStarted     += () => m_StatusText.text = "Host started";
            m_Lobby.OnClientConnected += () => m_StatusText.text = "Client connected";
            m_Lobby.OnDisconnected    += () => m_StatusText.text = "Disconnected";
        }
    }
}
```

- [ ] **Step 4: 씬 생성 + 수동 검증 + 커밋**

BasicManual 샘플과 동일하게 씬 구성. `VContainerLobbyLifetimeScope`를 루트 GameObject에 부착.

```bash
git add Samples~/VContainerIntegration
git commit -m "feat(samples): VContainerIntegration 샘플 — LifetimeScope에서 빌더 1회 호출"
```

---

## Phase 9: 릴리스 문서

### Task 36: `package.json` 버전 업

**Files:**
- Modify: `package.json`

- [ ] **Step 1: 버전 변경**

Edit `package.json`: `"version": "0.1.0"` → `"version": "0.2.0"`.

- [ ] **Step 2: 커밋**

```bash
git add package.json
git commit -m "chore: 버전 0.2.0으로 업"
```

---

### Task 37: `CHANGELOG.md` 신설

**Files:**
- Create: `CHANGELOG.md`

- [ ] **Step 1: 작성**

Create `CHANGELOG.md`:
```markdown
# Changelog

이 프로젝트의 변경 이력. [Keep a Changelog](https://keepachangelog.com/) 형식을 따른다.

## [0.2.0] — 2026-04-17

### Breaking
- VContainer 런타임 의존성 **완전 제거**. 패키지는 이제 DI 컨테이너 중립(container-agnostic).
- 네임스페이스 재편: `Multiplayer.Lobby.Abstractions`, `.StateMachine`, `.States`, `.Session`, `.Messaging`, `.Connection`, `.Builder`, `.Adapters.*`, `.ConnectionMethods.IP`.
- `LobbyConnectionManager` 삭제 → `LobbyBuilder` + `LobbyConnection` + `StateMachine` + `LobbyConnectionHost` 로 분해.
- 상태 머신: 하드코딩된 상태 필드 → 타입 키 레지스트리. 사용자가 상태를 추가/교체 가능 (`AddState<T>`, `ReplaceState<T>`).
- `ConnectionState` / `OnlineState`: `[Inject]` 기반 → 생성자 주입 (`IStateMachineContext`).
- `ConnectionMethodBase`: `LobbyConnectionManager` 의존 제거 → `INetworkFacade` + `IConnectionPayloadSerializer` 주입.
- `IPConnectionMethod` 생성자 시그니처 변경.
- `UpdateRunner` 삭제 → `ITickSource` + `MonoBehaviourTickSource`.
- 기존 `Samples~/LobbyTest/` 삭제 → `Samples~/BasicManual/` + `Samples~/VContainerIntegration/` 2종.

### Added
- 어셈블리 3분할: `Multiplayer.Lobby.Core` (순수 C#), `Multiplayer.Lobby.Adapters` (Unity/Netcode), `Multiplayer.Lobby.ConnectionMethods.IP` (IP 직접 연결).
- 추상화: `INetworkFacade`, `IConnectionApprover`, `ILobbyLogger`, `ITickSource`, `ICoroutineRunner`, `IConnectionPayloadSerializer`, `IPlayerIdentityStore`, `ISessionManager`, `IStateMachineContext`.
- `DefaultConnectionApprover` — 페이로드/인원 검증 기본 구현 (교체 가능).
- `ReconnectPolicy` 값 객체 — 재연결 정책 설정.
- `LobbyLifecycleMessage` PubSub 채널 — 생애주기 이벤트 통합 전달 경로.
- `LobbyConnection.GetPublisher<T>()` / `GetSubscriber<T>()` — 사용자 정의 메시지 타입 추가 가능.
- `AddMessageChannel<T>()` — 사용자 커스텀 채널 등록.
- `Tests/Editor/` — EditMode 단위 테스트 스위트. 상태 전이, 빌더, 세션, PubSub, 승인 로직 커버.

### Changed
- `SessionManager`: `Debug.Log*` → `ILobbyLogger` 주입.
- `PlayerIdentity`: `PlayerPrefs`/`Application.dataPath` 직접 의존 → `IPlayerIdentityStore` 주입.
- `LobbyConnectionHost` (선택적 MonoBehaviour): 인스펙터 1회 배선으로 자동 조립.
```

- [ ] **Step 2: 커밋**

```bash
git add CHANGELOG.md
git commit -m "docs: CHANGELOG 신설 (v0.2.0)"
```

---

### Task 38: `README.md` 업데이트

**Files:**
- Modify: `README.md`

- [ ] **Step 1: README 재작성**

Overwrite `README.md`:
```markdown
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
```

- [ ] **Step 2: 커밋**

```bash
git add README.md
git commit -m "docs: README — DI 중립 + 확장 포인트 + 빠른 시작 예시로 재작성"
```

---

## Phase 10: 최종 검증 + PR

### Task 39: 전체 테스트 그린 확인 + PR 준비

**Files:** None (verification)

- [ ] **Step 1: Unity EditMode 테스트 전체 실행**

Unity 에디터 → Test Runner → EditMode 탭 → Run All. 모든 테스트 그린 확인.

기대 테스트 수: 약 30+ 개.
- `FakeLoggerTests` (2)
- `FakeTickSourceTests` (2)
- `FakeCoroutineRunnerTests` (2)
- `FakeConnectionPayloadSerializerTests` (2)
- `InMemoryPlayerIdentityStoreTests` (3)
- `FakeNetworkFacadeTests` (3)
- `DefaultConnectionApproverTests` (4)
- `PlayerIdentityTests` (2)
- `MessageChannelTests` (5)
- `SessionManagerTests` (5)
- `StateMachineTests` (3)
- `OfflineStateTests` (1)
- `StateTransitionTests` (4)
- `LobbyBuilderTests` (5)
- `LobbyBuilderBuildTests` (5)

- [ ] **Step 2: 샘플 씬 2개 Play 모드 수동 검증**

- `Samples~/BasicManual/BasicManual.unity`: Host 버튼 클릭 → "Host started" 표시 / Shutdown 클릭 → "Disconnected" 표시.
- `Samples~/VContainerIntegration/VContainerIntegration.unity`: 동일 시나리오 확인.

- [ ] **Step 3: VContainer 런타임 참조 흔적 검색 (정리 완결 확인)**

Unity 에디터 외부에서 전수 조사:
- `Runtime/Core/` 아래에 `VContainer` 문자열이 없는지 확인.
- `Runtime/Adapters/` 아래에도 없는지 확인.
- 남은 VContainer 참조는 `Samples~/VContainerIntegration/`과 `CHANGELOG.md` 설명부만 있어야 함.

정리 결과를 stdout으로 출력하고 이상이 있으면 제거.

- [ ] **Step 4: PR 생성 (명령 예시)**

```bash
git push -u origin refactor/vcontainer-removal-and-solid-cleanup
gh pr create --title "refactor(0.2.0): VContainer 제거 + SOLID 구조 정리" --body "$(cat <<'EOF'
## Summary
- 어셈블리 3분할 (Core/Adapters/ConnectionMethods.IP) — Core는 UnityEngine 참조 없음.
- 상태 머신을 타입 키 레지스트리로 전환, OCP 만족 (상태 추가/교체 가능).
- `LobbyBuilder`가 조립, `LobbyConnection`이 퍼블릭 파사드.
- EditMode 단위 테스트 스위트 추가.
- 샘플 2종(BasicManual/VContainerIntegration)로 교체.

## Test plan
- [ ] Unity 에디터 EditMode Test Runner 전체 그린
- [ ] BasicManual 씬 Play 모드 Host/Client/Shutdown 동작 확인
- [ ] VContainerIntegration 씬 Play 모드 Host/Client/Shutdown 동작 확인
- [ ] `Runtime/Core/`와 `Runtime/Adapters/`에 VContainer 참조 0건 확인
EOF
)"
```

- [ ] **Step 5: (선택) 작업 브랜치를 main에 머지**

PR 리뷰 후 머지. 이 단계는 소유자 결정.

---

## Self-Review

### Spec coverage 체크

| Spec 항목 | 구현 태스크 |
|---|---|
| §3.1 레이어 분리 (Core/Adapters/IP/Tests/Samples) | Task 2, 27~32, 34, 35 |
| §3.2 의존 방향 | asmdef로 강제 (Task 2) |
| §3.3 SOLID 개선 (SRP/OCP/LSP/ISP/DIP) | Task 15~23 (SM), Task 24~26 (Builder), Task 17~22 (States) |
| §4.1 `INetworkFacade` | Task 9, 29 |
| §4.2 `IConnectionApprover` + Default | Task 10 |
| §4.3 `IStateMachineContext` | Task 14, 16 |
| §4.4 `ILobbyLogger` | Task 4, 27 |
| §4.5 `ITickSource` | Task 5, 27 |
| §4.6 `ISessionManager` | Task 13 |
| §4.7 `ICoroutineRunner` (스펙 보강) | Task 6, 27 |
| §4.8 `IConnectionPayloadSerializer` (스펙 보강) | Task 7, 28 |
| §4.9 `IPlayerIdentityStore` (스펙 보강) | Task 8, 28 |
| §4.10 `ReconnectPolicy` | Task 3 |
| §5 상태 머신 재설계 | Task 15~22 |
| §6 `LobbyBuilder`/`LobbyConnection` | Task 24~26 |
| §7 어댑터 | Task 27~30 |
| §8 IP 분리 + 확장 메서드 | Task 31~32 |
| §9 샘플 2종 | Task 34, 35 |
| §10 테스트 커버리지 | Task 4~13, 16, 17, 23, 24, 26 |
| §11 마이그레이션 (삭제) | Task 33 |
| §11.3 버전/문서 | Task 36~38 |

모든 스펙 항목이 태스크로 매핑됨. 갭 없음.

### Placeholder 스캔

Task 24 Step 3의 `Build()` 메서드에 **의도적 placeholder 예외**가 있으며 Task 26 Step 2에서 실구현으로 교체된다고 주석으로 명시. 그 외에는 "TODO", "TBD", 추상 안내문 없음. 모든 코드 스텝에 완전한 코드가 제공됨.

### 타입 일관성 체크

- `ChangeState<T>()`: Task 16, 19, 22, 25, 26 전반에서 동일 시그니처 사용 ✔
- `ApprovalCheck`: `event Func<ApprovalRequest, ApprovalResult>` 전반 일치 ✔
- `ConnectionMethodBase` 생성자: `(INetworkFacade, IConnectionPayloadSerializer, PlayerIdentity, string, bool)` Task 11/31 일치 ✔
- `LobbyConnection`: `Sessions`, `Network`, `StartClient/Host`, `RequestShutdown`, `GetPublisher/GetSubscriber<T>`, `OnHostStarted/ClientConnected/Disconnected`, `Dispose` — Task 24/25/26 일관 ✔
- `IStateMachineContext` 멤버: Task 14 정의 ↔ Task 16/17~22 사용 일치 ✔

---

## 실행 방식 선택

플랜 작성 완료 — `docs/superpowers/plans/2026-04-17-lobby-connection-architecture.md`.

**두 실행 옵션:**

**1. Subagent-Driven (추천)** — 태스크별로 새 서브에이전트 디스패치, 태스크 사이에 리뷰. 컨텍스트 오염 최소.

**2. Inline Execution** — 현재 세션에서 `executing-plans` 스킬로 배치 실행 + 체크포인트.

어느 방식을 선호하시나요?

