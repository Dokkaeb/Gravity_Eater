# Gravity Eater

> 행성을 조종해 먹이를 먹고 크기를 키우며 다른 플레이어를 잡아먹는 **실시간 멀티플레이 .io 게임**

Photon PUN2 기반 실시간 멀티플레이, Firebase 글로벌 리더보드, 오브젝트 풀링, 마스터 클라이언트 권한 분산, 맵 경계 워프까지 **1인으로 설계하고 구현**했습니다.

- 시연 영상 : https://youtu.be/EBx5C1vbSCg
- 개발기 (블로그) : https://trew202.tistory.com/105

<br>

## 1. 프로젝트 개요

| 항목 | 내용 |
|------|------|
| 장르 | 2D · 캐주얼 · 실시간 멀티플레이 (.io 서바이벌) |
| 플랫폼 | PC Web / Windows |
| 개발 형태 | 개인 프로젝트 (1인 개발) |
| 개발 기간 | 2026.01.12 ~ 2026.01.30 (약 3주) |
| 엔진 / 언어 | Unity 6 / C# |
| 핵심 기술 | Photon PUN2, Firebase Realtime Database |
| 최대 인원 | 룸당 20명 |

<br>

## 2. 게임 컨셉 및 특징

### 2.1 게임 소개
- 광활한 우주를 배경으로 소행성과 행성을 흡수하며 성장하는 블랙홀 경쟁 게임입니다.
- Slither.io의 직관적인 게임성과 블랙홀 테마를 결합했습니다.

### 2.2 핵심 재미 요소
- **성장의 희열** : 작은 행성에서 항성, 거대한 블랙홀로 진화하는 시각적 피드백
- **긴장감 넘치는 경쟁** : 한 번의 실수로 모든 것을 잃을 수 있는 하이 리스크 · 하이 리턴 구조
- **명예의 전당** : 실시간 글로벌 랭킹으로 성취감 제공

<br>

## 3. 게임 메카닉스

### 3.1 조작 및 이동
- **입력** : 마우스 위치 기반 방향 이동
- **대시** : 스태미나를 소모해 가속, 목표 점수 도달 시 해금
- **카메라** : 크기에 비례해 자동 줌아웃 (Orthographic Size 조절)
- **맵 경계 워프** : 경계에 닿으면 반대편으로 순간이동

### 3.2 성장 및 충돌 규칙
- **흡수 대상** : 우주먼지(1점) < 소행성(5점) < 큰 소행성(10점) < 행성(50%)
- **사망 판정** : 나보다 큰 블랙홀의 몸체 영역에 내 중심점이 닿으면 사망
- **전리품** : 사망 시 누적 점수의 50%가 먹이로 그 자리에 생성
- **스폰 무적** : 스폰 직후 일정 시간 무적으로 불공정한 초반 피격 방지

<br>

## 4. 시스템 구조

| 분류 | 기술 |
|------|------|
| 멀티플레이 | Photon PUN2 (IPunObservable, PunRPC, 커스텀 프로퍼티, MasterClient 권한 관리) |
| 백엔드 | Firebase Realtime Database (글로벌 리더보드, async/await) |
| 네트워크 패턴 | MasterClient 권한 분산, 3단계 RPC 흐름, 배치 RPC, 시드 기반 동기화 |
| 최적화 | 오브젝트 풀링 (PoolManager) |
| 데이터 | ScriptableObject (FoodData, ItemData, NebulaData, SoundData, PlanetSkins) |
| UI | MVP 패턴 (LeaderboardPresenter / View), UIManager 싱글톤 |
| 연출 | Shader Graph (블랙홀 머티리얼), DOTween |

<br>

## 5. 주요 구현 내용

### 5.1 실시간 동기화 — IPunObservable과 RPC 역할 분리
- `PlayerCtrl : MonoBehaviourPun, IPunObservable` 에서 `OnPhotonSerializeView()` 로 점수 · 위치를 지속 동기화
- 먹기(`RPC_ProcessEat`), 사망(`RPC_OnDead`), 아이템(`RPC_SyncItemVisual`), 스킨(`RPC_ApplySkin`) 같은 **한 번 일어나는 이벤트는 RPC**로 처리
- 점수만 보내고 **크기는 각 클라이언트가 계산**해 전송량을 줄임

> **왜 나눴나** — 계속 바뀌는 값을 RPC로 보내면 호출마다 패킷이 생겨 오버헤드가 커지고, 이벤트를 스트림으로 보내면 발생 시점을 보장하기 어렵습니다. 데이터 성격에 맞게 나눠 효율과 정확성을 함께 확보했습니다.

### 5.2 시드 기반 맵 동기화
- 마스터 클라이언트가 방을 만들 때 시드를 생성해 **커스텀 프로퍼티**에 저장
- `OnRoomPropertiesUpdate` 로 모든 클라이언트가 같은 시드를 받아 **각자 같은 맵을 생성**
- RPC가 아닌 커스텀 프로퍼티를 쓴 이유는 **중도 참여자도 언제든 시드를 확인**할 수 있어야 하기 때문

> **효과** — 1,000개가 넘는 먹이 좌표 대신 정수 시드 하나만 보내 **초기 로딩 대역폭을 90% 이상 절감**했습니다.

### 5.3 MapGenerator — 마스터 클라이언트 권한 분산
- 먹이 · 아이템 · 네뷸라의 생성과 파괴를 마스터 클라이언트가 중앙 관리 (`_activeFoodDict`, `_activeItemDict`, `_activeNebulaDict` ID 기반 딕셔너리)
- 파괴는 `RequestDestroyItem()` → `RPC_MasterDestroyItem()` → `RPC_FinalizeDestroyItem()` **3단계 흐름**으로 마스터가 검증한 뒤 전체에 전파
- `OnMasterClientSwitched()` 로 호스트가 나가도 새 마스터가 권한과 스폰 코루틴을 이어받음
- 신규 접속자에게 `SyncItemsToNewPlayer()`, `SyncNebulasToNewPlayer()`, `RPC_SyncEatenFoods()` 로 현재 상태를 일괄 복구

> **왜 3단계인가** — 여러 명이 같은 먹이를 동시에 먹으면 파괴 요청이 겹칩니다. 요청 → 마스터 검증 → 전체 전파 순서로 바꿔 **정확히 한 번만 처리**되게 했고, 10명 동시 접속 테스트에서 문제없이 동작했습니다.

### 5.4 PoolManager — 오브젝트 풀링
- 먹이 · 아이템 · 네뷸라를 `PoolManager.Instance.Get()` / `Release()` 로 재사용
- 맵 최대 오브젝트 수(`_maxItemOnMap`)를 제한해 과부하 방지

> **효과** — 반복 Instantiate / Destroy를 없애 GC와 프레임 드랍을 억제하고, 생성 · 파괴에 따른 네트워크 트래픽도 줄였습니다.

### 5.5 플레이어 컨트롤러
- 플레이어 상태를 enum 기반 상태 패턴으로 분리
- 먹이 섭취 시 크기 성장, 최소 크기(`_minScale`) 제한, 자석 범위를 크기에 비례해 계산
- 대시 : 스태미나 소모(`_dashCostPerSecond`), 소행성 이펙트 · 사운드 연동
- 맵 경계 워프 : `BoundaryWarp(string wallName)` 로 부딪힌 벽을 판별해 반대편으로 이동, `_warpOffset` 으로 이탈 버그 방지
- 스폰 무적 : `RPC_SyncInvincibleVisual()` 로 무적 상태를 모두에게 동기화

### 5.6 아이템 · 네뷸라 함정 — ScriptableObject 기반
- `ItemData`, `NebulaData` ScriptableObject로 효과 · 수치 · 스프라이트를 데이터로 분리하고 `ItemType` enum으로 타입 관리
- 아이템 **로직은 로컬 코루틴**, **비주얼만 RPC**로 동기화해 트래픽 절감
- 습득 즉시 비활성화해 피드백 지연을 없애고, 이후 마스터에 처리 요청
- 네뷸라(Red / Blue)는 스턴 · 감속 효과, `RPC_SyncNebulaBatch()` 로 **여러 개를 한 번에 동기화**

> **왜 SO + enum인가** — 코드 수정 없이 에셋만 추가해 콘텐츠를 늘릴 수 있고, 존재하지 않는 타입은 컴파일 시점에 걸러집니다. RPC로 보낼 때도 enum(정수)이 문자열보다 작습니다.

### 5.7 사망 전리품 — 배치 RPC
- 사망 시 점수의 50%를 10점 단위로 나누고, **최대 50개씩 배열로 묶어 RPC 한 번에 전송**
- AllBuffered 대신 마스터가 남아 있는 전리품만 골라 새로 온 유저에게 전송

### 5.8 Firebase 글로벌 리더보드 — MVP
- `LeaderboardPresenter + LeaderboardView` MVP 구조, `GetTopScoresAsync(10)` 으로 상위 10위 비동기 조회
- Photon(콜백 기반)과 Firebase(Task 기반)의 초기화 시점을 **TaskCompletionSource** 로 통일
- 3초 타임아웃, 최대 5회 재시도로 불안정한 연결에 대응
- 사망 시 점수가 서버에 기록된 것을 `await` 로 확인한 뒤 로비로 이동
- 상위 50위 밖이면서 24시간이 지난 기록은 자동 정리

### 5.9 로비 · 매칭 · 스킨
- `GameLauncher : MonoBehaviourPunCallbacks` 로 서버 연결 → 랜덤 룸 입장, 실패하면 최대 20인 룸 생성
- `PhotonNetwork.AutomaticallySyncScene = true` 로 씬 전환 동기화, 접속자 · 룸 수를 로비에 실시간 표시
- 스킨은 익명 계정이 많은 게임 특성상 DB 대신 **PlayerPrefs**에 저장하고, 시작 시 `RpcTarget.AllBuffered` 로 공유

### 5.10 연출 · 사운드
- 점수에 따라 행성 → 항성 → 블랙홀로 진화, **DOTween** 으로 진화 연출
- 회전하는 블랙홀 머티리얼을 **Shader Graph** 로 직접 제작
- `SoundData` ScriptableObject와 루프 전용 AudioSource로 BGM · 반복 SFX · 일회성 SFX를 분리 제어

<br>

## 6. 트러블 슈팅

| 문제 | 해결 | 결과 |
|------|------|------|
| 수백 개의 먹이를 각각 동기화하려니 네트워크 과부하 우려 | 시드를 공유해 각 클라이언트가 로컬에서 생성 | 네트워크 부담 대폭 감소 |
| 화면에 먹이가 보이는데 실제로는 없는 잔상 현상 | 고유 ID + 마스터 전담 관리, 먹으면 먼저 비활성화 후 마스터 확정, 새 유저에게 먹힌 ID 전송 | 잔상 · 점수 복사 버그 제거 |
| 아이템 · 함정이 갑자기 더 이상 안 나옴 | 마스터 이탈로 코루틴이 멈춘 것이 원인 → `OnMasterClientSwitched` 에서 재시작 | 마스터가 나가도 계속 생성 |
| 1점짜리 전리품 수백 개로 네트워크 과부하 | 10점 단위로 묶어 배치 전송 | 과부하 대부분 해소 |
| 리더보드가 불러와질 때도, 안 될 때도 있음 | TaskCompletionSource로 Photon · Firebase 초기화 시점 통일 | 연결 안정화 |
| 모든 동기화를 AllBuffered로 처리해 늦게 온 유저 부담 증가 | 로컬 처리 가능한 것은 로컬로, 이펙트는 커스텀 프로퍼티, 진행 중엔 일반 RPC | 버퍼 누적 제거 |

<br>

## 7. 개발 일정

| 주차 | 내용 |
|------|------|
| 1주차 | 게임 런처, 플레이어 이동 · 대시 · 사망, 리더보드, 로비 UI |
| 2주차 | 사운드, 맵 주요 기믹, 게임 종료 · 나가기, 블랙홀 셰이더, 스폰 무적 |
| 3주차 | 미니맵, 디버깅, 폴리싱 |

<br>

## 8. 회고
- **AllBuffered는 남발하지 말 것** — 나중에 들어온 유저의 네트워크 부담이 커집니다.
- **네트워크는 비동기로** — 지연을 기다리면 게임 진행이 늘어집니다.
- **마스터 전담과 로컬 처리의 구분** — 모두에게 같아야 하는 판정은 마스터가, 간단한 이펙트는 각자 로컬에서 처리하는 게 좋습니다.
- **늦게 들어온 유저를 위한 동기화** — 먼저 접속한 유저가 바꾼 상태를 나중에 온 유저가 못 보면 게임이 성립하지 않습니다.
