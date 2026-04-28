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
