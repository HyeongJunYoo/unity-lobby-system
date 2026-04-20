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
