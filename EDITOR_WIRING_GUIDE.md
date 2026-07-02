# 에디터 배선 가이드 — turnrpg-client (사용자 액션)

> 소유: **turnrpg-client 단일 세션**(분할 전 — arpg의 architect/ui/systems 3분할과 달리 단일 소유). 코드는 완결됐으나 **Unity 에디터에서 씬/프리팹/`.asset` 수동 배선**이 필요한 항목 목록.
> 배선 전까지 해당 기능은 무동작(컴파일·`dotnet build`는 정상이나 프리팹 미로드/핸들러 미부착/데이터 미입력).
> 형식 참고 = arpg `EDITOR_WIRING_GUIDE.md`(rollup 모델). 본 파일은 단일 소유라 rollup/HANDOFF 제출 절차는 생략.

---

## 📐 작성 규약

- **소유/SoT**: 이 파일 = turnrpg-client 단일 소유. 배선 필요 컴포넌트를 추가/변경하면 이 파일을 갱신한다.
- **실측 규약**: 필드명·타입·부착지점·경로는 **소스/grep 기반 실측**(추정 금지 — 틀린 배선 가이드는 무배선보다 위험).
- **체크박스**: 각 항목 `[ ]`(미배선) / `[x]`(사용자 배선완료 보고). **체크는 사용자**가 한다.
- **섹션 템플릿**:
  ```markdown
  ## <이모지> <기능명> (<날짜> · <코드상태>)
  ### <N>. <컴포넌트명> — <타입>
  - [ ] 부착·생성 대상: <구체>
  - [ ] SerializeField 배선: (표 또는 "없음")
  - [ ] 의존·순서: <선행>
  - 검증 포인트: <확인법 / 미배선 증상>
  ```

---

## 🎬 씬 플로우 매니저 — SceneFlowManager 수렴 (2026-06-30 · 코드 완결)

> 구 `SceneManager`(Client 자식 plain MonoBehaviour) → engine-sdk `SceneFlowManager<T>` 베이스 서브클래스 `TurnRpgSceneFlow`(MonoSingleton)로 수렴. ⚠️ 구조 변경: **Client 자식 → 독립 MonoSingleton**.

### 1. `TurnRpgSceneFlow` — MonoSingleton (씬 오브젝트)
- [ ] **부착·생성 대상**: 부트(Init) 씬에 `TurnRpgSceneFlow` 컴포넌트를 가진 GameObject **신규 배치**. 구 `Client` 자식 `SceneManager` 오브젝트를 대체. `Assets/Scripts/Scene/TurnRpgSceneFlow.cs`.
- [ ] **SerializeField 배선**:
  | 필드 | 타입 | 할당 대상 |
  |---|---|---|
  | `_transition` | `SceneTransition` | 페이드용 SceneTransition 컴포넌트(§2) |
- [ ] **의존·순서**: MonoSingleton 인스턴스 인식 — 씬에 배치된 인스턴스를 베이스가 사용(없으면 auto-create 시 `_transition` null → 페이드 생략·graceful). **DontDestroyOnLoad 거동 확인**(씬 전환 가로질러 단일 인스턴스 유지).
- 검증 포인트: 씬 전환 시 페이드 동작·전투 진입 시 `OnBeforeLoadAsync`(리소스 리셋+요청 송신)·`OnControllerRegistered`(패킷 flush 후 FadeIn). 미배치=`TurnRpgSceneFlow.Instance` auto-create로 동작하나 `_transition` 없으면 페이드 무·연출 빠짐.

### 2. `SceneTransition` — MonoBehaviour (페이드)
- [ ] **부착·생성 대상**: 페이드 Canvas(전체화면 Image). `Assets/Scripts/Scene/SceneTransition.cs`. §1 `_transition`이 이걸 가리킴.
  | 필드 | 타입 | 할당 대상 |
  |---|---|---|
  | `_fadeImage` | `Image` | 전체화면 페이드 이미지(초기 alpha 0) |
  | `_duration` | `float` | 기본 0.5 |
  | `_easeType` | `Ease`(DOTween) | 기본 InOutQuad |
- 검증 포인트: 미배선=FadeOut/In NRE 또는 무효. 페이드 Canvas는 씬 전환 중 생존 필요(DontDestroyOnLoad 계층).

---

## 🗺️ 스테이지 선택 UI (#7, 2026-06-30 · 코드 완결)

> 구 `StageId=1` 고정 → 선택 UI. plan `stage_catalog.json`(dual-export) 소비. name=LocalizedString(turnrpg 최초 i18n 소비).

### 1. `UIStageSelectPopup` — BasePopup (프리팹, PopupPath "Stage")
- [ ] **부착·생성 대상**: 신규 프리팹 `[PopupPath("Stage")]` — UIManager가 PopupPath로 로드(기존 팝업 프리팹 폴더 규약 따라 배치). `Assets/Scripts/Contents/Stage/UI/UIStageSelectPopup.cs`.
- [ ] **SerializeField 배선**:
  | 필드 | 타입 | 할당 대상 |
  |---|---|---|
  | `_listContainer` | `Transform` | 스테이지 슬롯 부모(레이아웃 그룹) |
  | `_slotPrefab` | `UIStageSlot` | 슬롯 프리팹(§2) |
  | `_closeButton` | `Button` | 닫기 |
- [ ] **의존·순서**: §2 슬롯 프리팹 선행. 오픈 주체=`LobbySceneController.OnClickStageButton`→`Show<UIStageSelectPopup>(p=>p.Open(OnStageSelected))`(코드 완결·배선 불요).
- 검증 포인트: 미생성/PopupPath 불일치=스테이지 버튼 눌러도 팝업 안 뜸. 선택→`StageEnterRequest{StageId}` 송신(코드).

### 2. `UIStageSlot` — MonoBehaviour (목록 항목 프리팹)
- [ ] **부착·생성 대상**: 신규 슬롯 프리팹(§1 `_slotPrefab`). `Assets/Scripts/Contents/Stage/UI/UIStageSlot.cs`.
  | 필드 | 타입 | 할당 대상 |
  |---|---|---|
  | `_nameText` | `TextMeshProUGUI` | 스테이지명(LocalizedString ko 해소) |
  | `_selectButton` | `Button` | 선택 클릭 |
  | `_lockOverlay` | `GameObject` | (선택) 잠금 오버레이 — 현재 전부 해금(진행도 시스템 미착지) |
- 검증 포인트: `_nameText`/`_selectButton` 미배선=항목 표시·클릭 불가.

### 3. `stage_catalog.json` — 데이터 (plan export·사용자 배치)
- [ ] **배치**: plan TurnRpg DataTool가 export한 `stage_catalog.json`을 **`Assets/Resources/Data/stage_catalog.json`**에 배치(클라 `Resources.Load<TextAsset>("Data/stage_catalog")`). 키=int stageId.
- [ ] **의존·순서**: plan export 실값(스테이지명/썸네일/해금/정렬) 디자인 입력 후. 형상=`StageCatalogEntry`(name LocalizedString·thumbnailPath·unlockStageId·sortOrder·rewardPreview) 1:1.
- 검증 포인트: 파일 부재=로더 graceful(경고 로그+빈 목록·크래시 0). 배치 시 즉시 목록 표시.

---

## 🆔 콘텐츠 id int 전환 — .asset 재입력 (2026-06-24 · 코드 완결·ADR-006)

> `CharacterData.CharacterId(string)→TemplateId(int)`·`SkillData.SkillId(string→int)` 타입 전환. ⚠️ **SerializeField 타입 변경으로 기존 `.asset`의 string 값 소실** → int 재입력 필요.

### 1. `CharacterData` / `SkillData` `.asset` 값 재입력
- [ ] **CharacterData .asset** (`Assets/Resources/Data/Character/CharacterData*.asset`): `TemplateId`(int) 값 입력. plan 데모: **캐릭 101~103 / 적 201~203**.
- [ ] **SkillData .asset** (`Assets/Resources/Data/Skill/SkillData*.asset`): `SkillId`(int) 값 입력.
- [ ] **`CharacterDatabase.asset`** (`Assets/Resources/Data/Character/CharacterDatabase.asset`): int 키 매핑 확인(TemplateId→CharacterData). `GameDataConfig._characters`에 연결돼 `Client.GameData.Characters`로 조회.
- 검증 포인트: 미입력=int 기본값(0) → CharacterDatabase 조회 미스(비주얼/이름 미해소). 적 TemplateId=0은 graceful(플레이스홀더).

---

## 🧹 HSR 잔재 정리 — Missing Script + orphan SerializeField (2026-06-20 · 코드 완결)

> ATB·SP·궁극기 게이지 등 HSR 잔재 컴포넌트 삭제(2D 수렴). 삭제된 **컴포넌트**를 참조하던 씬/프리팹에 Missing Script 발생 가능.

### 1. Missing Script 정리 (삭제 컴포넌트)
- [ ] **삭제된 컴포넌트**: `UISkillPointDots`(SP)·`UltimateGauge`(궁극기 에너지 게이지). 이들을 부착했던 프리팹/씬(스킬바·파티멤버카드 등)에 **Missing Script** 가능 → Unity "Remove Missing Scripts"로 제거(YAML 직접편집 금지).
- 검증 포인트: 프리팹/씬 열어 Missing Script 경고 확인 후 제거+저장.

### 2. orphan SerializeField (코스메틱·선택)
- [ ] 삭제된 **필드**(`_spDots`/`_gaugeBar` 등)의 직렬화 잔재는 Unity가 무시(무해)·필수 아님. 인스펙터 정리 차원에서만.
- 검증 포인트: 기능 영향 없음(필드 제거는 Missing Script 아님).

---

## 🔊 VFX/SFX 추출분 채택 — clip `.asset` 재생성 (2026-06-30 · 코드 완결)

> 자체 VFX/SFX 6파일 → engine-sdk `SMDevLibrary.VFX`/`SMDevLibrary.Sound` 추출분 소비로 수렴(중복 제거). 자체 `VFXClip`/`SFXClip` 스크립트 삭제로 기존 clip `.asset`의 `m_Script` 끊김.

### 1. `SFXClip.asset` / `VFXClip.asset` 재생성 (엔진 타입)
- [ ] **`Assets/Resources/Data/SFXClip.asset`** — 엔진 타입 `SMDevLibrary/Audio/SFXClip`로 재생성 or 삭제. ⚠️ 현재 **빈 플레이스홀더**(Clip 미할당·기본값)라 **실데이터 0**(손실 없음).
- [ ] **`Assets/Resources/Data/VFXClip.asset`** — 엔진 타입 `SMDevLibrary/VFX/VFXClip`로 재생성 or 삭제(Prefab 미할당·빈 플레이스홀더).
- [ ] **`SkillEffect.HitVFX`/`HitSFX`** 필드(엔진 타입으로 변경됨)에 실 clip 재연결(실 VFX/SFX 콘텐츠 저작 시).
- 검증 포인트: 빈 .asset은 미참조라 영향 0. 실 콘텐츠는 엔진 타입 clip 저작 후 SkillEffect에 연결. DLL 타입 m_Script는 손편집 불가 → 에디터로 재생성.

---

## 🔧 강화·성장 UI 셸 (2026-07-02 · 셸 저작·실동작=계약 후)

> `EQUIP_GROWTH_DESIGN.md` 기반 선행 셸. ⚠️ 강화/돌파 **실동작**(요청 송신·비용/재료 표시)은 계약 배포(turnrpg-server→bridge) + O7(경제/영속) 착지 후. 현재 = 표시 셸 + 버튼(TODO 로그).

### 1. `UIEnhancePopup` — BasePopup (프리팹, PopupPath "Enhance")
- [ ] **부착·생성**: 신규 프리팹 `[PopupPath("Enhance")]`. `Contents/Inventory/UI/UIEnhancePopup.cs`. 오픈=`UIInventoryPopup.OnEnhanceClicked`→`Show<UIEnhancePopup>(p=>p.Open(item))`(코드).
  | 필드 | 타입 | 할당 |
  |---|---|---|
  | `_itemNameText` | `TextMeshProUGUI` | 아이템명 |
  | `_enchantText` | `TextMeshProUGUI` | "+N → +N+1" |
  | `_enhanceButton` | `Button` | 확정 강화 |
  | `_closeButton` | `Button` | 닫기 |
- 검증 포인트: 미생성/PopupPath 불일치=강화 버튼 눌러도 안 뜸.

### 2. `UIBreakthroughPopup` — BasePopup (프리팹, PopupPath "Breakthrough")
- [ ] **부착·생성**: 신규 프리팹 `[PopupPath("Breakthrough")]`. `Contents/Character/Party/UI/UIBreakthroughPopup.cs`. 오픈=`UICharacterSelectPopup.OnLevelUpClicked`→`Show<UIBreakthroughPopup>(p=>p.Open(ally))`(코드).
  | 필드 | 타입 | 할당 |
  |---|---|---|
  | `_nameText` | `TextMeshProUGUI` | 캐릭터명 |
  | `_levelText` | `TextMeshProUGUI` | "Lv.N" |
  | `_stageText` | `TextMeshProUGUI` | "돌파 N→N+1"(O7 후 실값·현 "—") |
  | `_breakthroughButton` | `Button` | 돌파 |
  | `_closeButton` | `Button` | 닫기 |
- 검증 포인트: 셸 표시는 name/level만(돌파 단계·재료=O7/계약 후).

---

## ⚔️ 배틀 서버권위 — 런타임 검증 전용 (배선 무관·참고)

> 아래는 신규 프리팹/SerializeField 배선이 **아니라** 코드 자동(핸들러 등록 등)이므로 배선 액션 없음. **Unity 런타임 검증만** 필요.

- [ ] **적 행동 서버권위(A)**: 적 푸시 수신→이동(`MovedToTileIndex`)/데미지(`Effects`)/턴 진행(`NextUnitId`/`IsEnemyTurn`)·연속 푸시 순서·연출 타이밍. (`InGameSceneController` 핸들러 자동 등록·배선 불요)
- [ ] **Periodic(독 DoT/재생 HoT)**: NextTurn `Effects` 렌더(피해숫자/회복/상태아이콘)·독 치사 연출.
- [ ] **단일 공유 그리드/배틀-init**: 타일 레이아웃·하이라이트·유닛 배치(서버 BattleSnapshot)·레인 타게팅 의미(ADR-007 후속).
- [ ] **이동 시각 재배치**: `UnitManager.MoveUnit`→`BattleFieldView.OnTileChanged`로 유닛 transform 갱신 확인.

---

## 🔗 외부 의존 (타 세션/디자인 — 참고)

- **plan**: `stage_catalog.json` export 실값(스테이지명/썸네일/해금/정렬) · CharacterData/SkillData 데모값(캐릭 101~103·적 201~203).
- **turnrpg-server**: 배틀 런타임(적 푸시·Periodic·BattleSnapshot) 가동 시 실 e2e.
- **engine-sdk**: `SMDevLibrary.dll`(SceneFlow/Resource/VFX/Sound/Localization 베이스) — Plugins 배포분 소비.
