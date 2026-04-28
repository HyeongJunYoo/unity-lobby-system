# Changelog

이 프로젝트의 변경 이력. [Keep a Changelog](https://keepachangelog.com/) 형식을 따른다.

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
- 백오프 누적 검증 테스트 (`ClientReconnectingStateBackoffTests`).
- `Samples~/BasicManual/README.md` — Scene 구성 단계별 안내.
- `FakeCoroutineRunner.PumpToNextYield` / `RunRoutineToCompletion` 테스트 헬퍼.
- `StateHarness.Build(ReconnectPolicy, ...)` 오버로드.

### Changed
- `Tests/Editor/Multiplayer.Lobby.Tests.Editor.asmdef`이 `Adapters` / `ConnectionMethods.IP` / `Unity.Netcode.Runtime` / `Unity.Networking.Transport` 참조 — 어댑터 단위 테스트 가능.
- `SessionManager`: 내부 헬퍼 `AssociateUnchecked` / `DisassociateUnchecked` 추출. 외부 API 동일.
- `IMessageChannel` / `MessageChannelBase`: 단일 스레드 동시성 계약을 XML 주석으로 명시.
- `LobbyBuilder` / `LobbyConnection` / `ReconnectPolicy` / `LobbyLifecycleMessage`: 공개 진입점에 XML 주석 추가.
- `README.md`: `LobbyConnectionHost.OnConfigure` 구독 시점(`Awake`/`OnEnable`)을 명시. 샘플 목록·표에서 VContainer 항목 제거. 일반 DI 통합 안내 한 문단 추가.

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
