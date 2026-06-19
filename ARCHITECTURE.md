# TurnBase 클라 설계 정본 (ARCHITECTURE)

> turnrpg-client 세션의 **전용 설계 SoT**. charter `CLAUDE.md`(정체성/규약)와 분리 — 구조·근거·결정기록(ADR)을 여기 누적.
> 형제 선례: `TurnBasedServer/ARCHITECTURE.md`(turnrpg-server) · arpg 클라.
> ⚠️ 실측 기반 — 코드와 어긋나면 코드가 정본. 갱신 책임 = turnrpg-client.

---

## §0 범위·경계

- **소유:** `E:\Work\pp\TurnBase\Assets`(Unity 2022.3·C#9) — TurnBaseRPG(2D 타일맵·턴베이스) 게임 클라.
- **소비만:** `SMDevLibrary.dll`(클라 엔진, engine-sdk) · `SM.Contracts.Core`+`SM.Contracts.TurnRPG`(와이어, bridge). 변경=허브 REQUEST. 바이너리 소비(`Assets/Plugins/`).
- **장르 엔진 부재:** `TurnBasedLibrary`(turnrpg 장르 클라 엔진) 미존재 → 분할 전엔 게임 고유 로직도 이 세션 겸함. 분할 시 `turnrpg-systems`로 이관.

## §1 모듈맵 (실측 2026-06-20 · `Assets/Scripts` 111 .cs)

```
Assets/Scripts/
  Common/         ── Client(MonoSingleton, 로그인 데이터→캐시)·SceneManager·GameDataConfig·Resource(Addressables 캐시)·Spine(애니/스킨 컨트롤러)·UserInfo
  Network/        ── Request.cs / Response.cs ── ⚠️ 로컬 패킷 정의(구 base 참조, 마이그레이션 대상 §4)
  Scene/          ── SceneManager·SceneTransition + Controller(Init/Lobby/InGame/SceneController)
  Contents/
    Audio/        ── SFX(Manager/Player/Clip)
    Character/    ── 데이터(CharacterData/Database)·장비(Equipment/ComputedStats/StatCalculator)·상태이상(StatusEffect*)·파티 UI·플레이어/적 컨트롤러·게이지/턴오더 계산
    Inventory/    ── 데이터(ItemData/Database/Instance)·UI(Popup/ScrollView/Slot/DetailPanel)
    Skill/        ── ISkill/SkillData·효과(Damage/Heal/StatusApply)·UI(Bar/Slot/InfoPanel/PointDots)
    Stage/        ── BattleController·StageDirector·Wave·Spawner(Ally/Monster)·Commands(스킬연출 커맨드)·Tile(BattleField/Tile/Highlighter/View)·VFX
    UI/Battle/    ── 전투 파티 패널/멤버 카드
```

- **전송 경계(구축 예정):** 엔진 SDK 전송레이어(`NetworkClient`·`PacketDispatcher`·`ClientPacketRegistry`·`UnityNetworkBridge`)가 송수신/디스패치 인프라 제공. **게임 패킷 핸들러 등록·클라 상태 반영 = 클라 책임**(미배선=§4 seam).
- **상태 캐시:** `Client`(MonoSingleton)가 로그인 응답→`UserInfo`/파티/캐릭터 캐시. 뷰는 캐시·패킷 수신으로 갱신.

## §2 와이어 소비 모델 (계약 = SM.Contracts.*)

- **패킷 식별:** 계약 패킷에 `[PacketType("turnrpg.*")]`(B12 안정 id). 디스패치=SDK `PacketDispatcher`. 와이어 토큰 SoT=bridge.
- **계약 표면(실측 2026-06-20):**
  - `SM.Contracts.Core`: `IPacket`·`PacketTypeAttribute`·`ENetworkStatusCode` + 크로스게임 base 패킷(Login·PlayerMove·Friend·Shop·World·Party preset 등). ⚠️ `LoginRequestPacket` 존재 → turnrpg는 `LobbyLoginRequestPacket`(bridge rename, 동명 충돌 회피).
  - `SM.Contracts.TurnRPG`: 패킷 10쌍 — `LobbyLogin`·`CharacterList`·`PartyValidate`·`StageEnter`·`BattleSkillUse`·`BattleTurnEnd`·`BattleNextTurn`(resp)·`BattleEnemyActionPush`·`BattleResult`(req)·`BattleReward`(resp). DTO=`CharacterInfo`·`SkillEffectDto`·`TileIndex`·`BattleSnapshotPacket`. enum=`StatusType`·`SkillEffectType`.
- **PROVISIONAL:** TurnRPG 와이어 id=frozen, **필드 형상=미확정**(reshape 여지). 클라 필드 참조 시작=free-reshape 창 닫힘 → 자명 1:1만 선행, 이슈 시 bridge REQUEST.

## §3 경계 매트릭스 (엔진/계약 대비 — 요약, 근거는 §6 ADR)

- **엔진 재사용(SDK):** FSM·Input(InputBuffer)·Gameplay(Gauge·CooldownTracker)·Collections·CameraControl·Behaviour(BT)·Network 전송·PathFinder/AStar(2D 그리드 친화)·UI/Layout·VFX·Sound·Generics·Utils.
- **계약 소비(bridge):** 패킷/DTO/공유 enum 전부. 클라 로컬 재정의 금지.
- **클라 자작:** 씬/UI/배틀뷰/연출(Command)·상태 캐시·핸들러 등록·타일맵 뷰(`TileIndex`→그리드 표시).
- **변경 요청:** 엔진=engine-sdk / 와이어=bridge / 게임로직 정합=turnrpg-server·plan.

## §4 미구현 seam (마이그레이션·콘텐츠 후)

- **⚠️ 와이어 마이그레이션(컴파일 브레이커, 최우선):** `Network/Request.cs`·`Response.cs`의 로컬 패킷(`RequestLogin:RequestPacket` 등, base=소거된 `SMDevLibrary.Network.Shared.*`) → `SM.Contracts.TurnRPG` 계약 타입 소비로 전환 + SDK 전송 배선(`NetworkClient`/`PacketDispatcher`/`ClientPacketRegistry` 등록). 로컬 DTO(`CharacterInfo`/`SkillEffectResult`/`UserInfo` 등)와 계약 DTO 정합·치환.
- **핸들러 등록·디스패치 배선:** SDK 전송 위 게임 패킷 핸들러 등록부 신설.
- **콘텐츠 데이터(@plan/turnrpg-server 정합):** 캐릭터 로스터·스킬·스테이지·보상 표시 데이터.
- **서버 권위 정합:** 낙관적 클라 표현 ↔ 서버 응답 정정 정책.

## §5 변경이력 (append-only · *무엇*/기계적 변경 추적)

- 2026-06-20 — 세션 부트(turnrpg-client 첫 가동). foundation/구코드 실측 + charter(`CLAUDE.md`)·설계 SoT(`ARCHITECTURE.md`)·status 신설. 코드 변경 0(마이그레이션 미착수).

---

## §6 결정 기록(ADR) · *왜*/비자명 결정 (append + supersede)

> 규율(adr-discipline): ①결정 시점 포착 ②소급=`[재구성·미검증]` ③타 세션 소유=포인터만 ④기존 행 수정 대신 새 행+`supersedes`. 자명·기계적 변경은 §5로.
> ADR-001~003 = 부트 시점 비자명 결정.

| # | 일자 | 결정 | 기각안 / 트레이드오프 | 태그·근거 |
|---|---|---|---|---|
| ADR-001 | 2026-06-20 | **foundation = 바이너리 DLL 소비(`Assets/Plugins/`).** `SMDevLibrary.dll`(engine-sdk)·`SM.Contracts.Core`+`SM.Contracts.TurnRPG`(bridge)를 소스 아닌 .dll로 드롭. `CombatLibrary.dll`(arpg 전투엔진)=장르불일치 제거(사용자 정리). | 기각=소스/asmdef 참조. 트레이드오프=빌드 단순·소유 경계 명확(클라는 엔진/계약 내부 무지) ↔ 디버깅 시 소스 부재. 엔진/계약 변경=재배포(허브 REQUEST). | `(grep Plugins: SMDevLibrary/SM.Contracts.* dll·meta · CombatLibrary D)` |
| ADR-002 | 2026-06-20 | **와이어 = 로컬 핸드롤 패킷 폐기 → `SM.Contracts.TurnRPG` 계약 소비.** 구코드 `Network/Request.cs`·`Response.cs`의 로컬 패킷(base=소거된 `SMDevLibrary.Network.Shared.*`)을 계약 타입으로 대체, 전송=SDK 레이어. | 기각=⒜로컬 base 재정의 유지(드리프트·계약 분열) ⒝구 SMDevLibrary 핀. 트레이드오프=서버와 단일 와이어 SoT·B12 안정 id ↔ 111 .cs 마이그레이션 비용·PROVISIONAL 필드 형상 위험. **착수=free-reshape 창 닫힘**(필드 참조 후 형상변경=조정 재배포). | `[방향·미착수]` `(grep Network.Shared 소거·TurnRPG 패킷15)` |
| ADR-003 | 2026-06-20 | **DLL VCS 정책 = .dll+.dll.meta 직접 트래킹.** 이 repo 기존 관행(DOTween·Newtonsoft·SMDevLibrary 전부 .dll+.meta 커밋) 따름 — arpg의 `*.dll` gitignore 패턴 **미채용**. | 기각=arpg 동형(.dll gitignore + .meta만 커밋). 사유=repo가 이미 DLL 직접 트래킹(일관성)·Unity .meta GUID 안정. 트레이드오프=repo 용량↑ ↔ 배포/클론 단순. | `(grep git ls-files: *.dll tracked · .gitignore dll 0건)` |

**규율③ 포인터(타 세션 소유 결정 — *왜*는 해당 SoT, 여기선 참조만):**
- 와이어/계약(TurnRPG 스코프·패킷 id·필드 형상·`LobbyLogin` rename) = **bridge** SoT. `(=bridge 확인)`
- 클라 엔진 모듈/전송레이어 시그니처 = **engine-sdk** SoT(SMDevLibrary). `(=engine-sdk 확인)`
- 게임 플로우·전투 규칙·콘텐츠 데이터 = **turnrpg-server**·plan. `(=turnrpg-server 확인)`

**미해소 결정 seam(차기 ADR 후보):** 마이그레이션 순서/단위(로비→배틀 점진 vs 일괄)·로컬 DTO↔계약 DTO 치환 정책·낙관적 표현 vs 서버 정정·SDK 전송 부트스트랩 구조·`TileIndex`↔클라 타일뷰 매핑.
