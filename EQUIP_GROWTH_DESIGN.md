# 장착·강화·캐릭터 성장 설계 (turnrpg-client)

> 세션 내부 설계 SoT(내 repo 잔류·허브 아님). 강화/장착 재설계(status TODO)의 디자인·아키텍처 근거.
> ⚠️ **디자인 의도·밸런스 값 = 사용자/디자이너 전권**(허브 §3-4). 이 문서에서 `[디자인미정=사용자]`는 세션이 확정하지 않는다 — 아키텍처(모델/계약/플로우/권위)만 세션이 잡고, 값은 사용자 채움.
> 실측 근거 = `(read)` 현행 코드(2026-06-30 조사). 상태: **설계 단계**(구현 전).

---

## 0. 확정 방향 (사용자 2026-07-02)

| 축 | 결정 |
|---|---|
| **범위** | 장착 + 장비 강화 + 캐릭터 성장 (풀) |
| **캐릭터 성장** | 레벨/경험치 + **돌파(레벨캡)**. 각성/스킬레벨 = **추후 별도**(이번 제외) |
| **장비 강화** | **확정 강화**(성공률 100%·비용/재료만·실패 없음) |
| **장착 슬롯·세트** | **현 모델 유지**(`EItemCategory`당 1슬롯 + 세트 효과) |

## 1. 원칙 (ⓐ 기술 SoT — 세션 권고·사용자 확인)

- **권위 = 서버.** 강화/장착/레벨업/돌파 **판정·재화 차감·스탯 산출 = 서버**(위변조 방지·charter). 클라 = 요청 송신 + 서버 응답 표현. 현 `EquipmentCache`가 클라 권위처럼 동작하는 것 → **서버 응답 반영 캐시**로 격하.
- **ID = int**(ADR-006 정본). 현 equip의 `string unitId`/`string ItemId`/`string InstanceId` → int 정합. (인스턴스 id는 서버 발급 long 가능 — bridge 확인.)
- **데이터 vs 와이어**: 성장/강화 **곡선·비용·상한 = 데이터(plan/서버 Config)**. 클라는 표시만. 와이어 = 요청/응답 패킷(bridge).

---

## 2. 장착 (Equip) 설계

### 2.1 현재 (실측)
- 모델: `ItemInstance{InstanceId,Data:ItemData,EnchantLevel,IsLocked,IsFavorite,CurrentSetCount,IsEquipped}` · `CharacterEquipment`(EItemCategory당 1) · `EquipmentCache`(unitId→).
- 스탯: `EquipmentStatCalculator.Calculate(baseData, equipped)`→`ComputedStats`(AllySpawner 적용).
- ⚠️ **요청 전송 0**(`SendPacket(RequestEquipItem)` 없음)·`RequestEquipItem`/`RequestUnequipItem`=STUB(계약 미존재)·응답핸들러만 존재.

### 2.2 목표 플로우 (서버 권위)
```
[장착] 인벤/캐릭선택 UI → EquipRequest{characterId(int), instanceId, slot} 송신
      → 서버 검증(소유·슬롯·조건) → EquipResponse{characterId, equippedInstanceId, unequippedInstanceId}
      → 클라 EquipmentCache 반영 + 스탯 재계산 + UI 갱신
[해제] UnequipRequest{characterId, slot} → UnequipResponse → 캐시 반영
```
- 클라 트리거 배선: `UICharacterSelectPopup.OnEquipSlotClicked`/인벤 선택 → EquipRequest 송신(현 stub 대체). (A-2 #5 여기서 실결선)
- 세트 효과: 서버가 `CurrentSetCount` 산출·반영(현 필드 유지). 세트 효과 값=데이터.

### 2.3 열린 디자인
- `[디자인미정=사용자]` **슬롯 카테고리 집합**(무기/방어구/장신구/…? 현 `EItemCategory` enum 값 확정).
- `[디자인미정=사용자]` 장착 **조건**(레벨/직업 제한 유무).

---

## 3. 장비 강화 (Enhance) 설계 — 확정 강화

### 3.1 현재 (실측)
- `ItemInstance.EnchantLevel`(int·서버 InventoryList로 받기만). 효과 = **메인스탯만 `×(1+Lv×0.1)`**(서브스탯 미반영). 강화 행위·비용·계약 전무(OnEnhance=Debug.Log).

### 3.2 목표 플로우
```
[강화] 인벤/장비 UI → EnhanceRequest{instanceId} (확정이라 1회 1레벨)
      → 서버: 비용/재료 검증·차감 → EnchantLevel+1 → EnhanceResponse{instanceId, newEnchantLevel, 차감내역}
      → 클라 InventoryCache 갱신 + 장착 중이면 스탯 재계산
```
- 확정 강화라 **성공률/실패 분기 없음** — 비용 충분하면 +1 확정.

### 3.3 확정 구조 (사용자 2026-07-02)
- **성장 대상 = 메인스탯만**(현행 `×(1+Lv×0.1)` 구조 유지·서브스탯 미성장).
- **비용 = 재화 + 강화재료**(gold + 강화 전용 재료 아이템). ⚠️ **의존**: turnrpg **재화 모델(경제 표면) 현재 미존재** → 강화 구현은 경제 표면 착지와 얽힘(§5·turnrpg-server 협의). 강화재료 = `ItemData` 카테고리(정의 필요).

### 3.4 열린 값 (사용자/디자이너 · `[디자인미정]`)
- 최대 강화 레벨 · 레벨당 증가율/곡선 · 레벨별 비용(gold/재료 수량) 곡선. = 데이터(plan/서버 Config) 저작, 클라 표시만.

---

## 4. 캐릭터 성장 설계 — 레벨/경험치 + 돌파

### 4.1 현재 (실측)
- **모델 전무**: `CharacterData.Level` 부재. 레벨/경험치/돌파 개념 0. `CharacterData`=템플릿(TemplateId·기본스탯). 런타임 캐릭 성장 상태 없음.

### 4.2 신설 모델 (제안)
- **런타임 캐릭터 성장 상태**(서버 권위·로그인 스냅샷 + 변경 응답으로 클라 캐시):
  - `characterId(int)` · `level(int)` · `exp(int)` · `breakthroughStage(int)` (돌파 단계).
  - 스탯 = 서버가 `기본스탯(TemplateId) + 레벨성장 + 돌파보너스 + 장비` 산출(권위). 클라는 표시/스냅샷 소비.
- `CharacterData`(템플릿 SO)는 **기본 스탯·레벨성장 곡선 참조**만(데이터). 성장 상태는 별도(런타임/서버).

### 4.3 목표 플로우
```
[레벨] 전투 클리어 → BattleReward에 exp 포함(또는 별도) → 서버 레벨업 판정
      → CharacterLevelChanged{characterId, newLevel, newExp} → 클라 캐시·UI 갱신
[돌파] 돌파 UI → BreakthroughRequest{characterId} → 서버: 재료 검증·차감·레벨캡↑
      → BreakthroughResponse{characterId, newStage, newLevelCap} → 클라 반영
```

### 4.4 확정 구조 (사용자 2026-07-02)
- **exp 획득원 = 전투 보상**(`BattleRewardResponsePacket`에 exp 포함). ⚠️ **배틀 C(결과·보상 배선)와 시너지** — BattleReward 배선 시 exp 동반. 계약에 exp 필드 필요(bridge·서버).

### 4.5 열린 값 (사용자/디자이너 · `[디자인미정]`)
- 레벨 상한(돌파 전 기본캡·최종캡) · exp 곡선 · 레벨업 스탯 성장(스탯별 증가) · 돌파 단계 수·단계별 레벨캡·돌파 재료 · 전투 exp 분배(생존자/전멸 등).
- ※ 각성/스킬레벨 = 이번 제외(추후 별도 설계).

---

## 5. 와이어 계약 요청 목록 (→ @bridge · 디자인 확정 후)

> ⚠️ 지금 요청 금지 — **디자인 값/구조 갈래 확정 후** bridge REQUEST(§3-4·재설계 시 락스텝 회피). 아래는 예상 표면(형상=디자인 확정 시 확정).

| 계약(예상) | 방향 | 형상(잠정) |
|---|---|---|
| `EquipRequest`/`Response` | 클→서/서→클 | `{characterId:int, instanceId, slot}` / `{characterId, equipped, unequipped}` |
| `UnequipRequest`/`Response` | | `{characterId, slot}` / `{characterId, instanceId}` |
| `EnhanceRequest`/`Response` | | `{instanceId}` / `{instanceId, newEnchantLevel, 차감}` |
| `CharacterLevelChanged`(push/resp) | 서→클 | `{characterId, newLevel, newExp}` |
| `BreakthroughRequest`/`Response` | | `{characterId}` / `{characterId, newStage, newLevelCap}` |
| BattleReward += exp? | 서→클 | (exp 획득원 ⒜ 시) |

- ※ 기존 STUB(`RequestEquipItem`/`RequestUnequipItem`/`ResponseEquipItem`/`ResponseUnequipItem`)은 계약화 시 교체. string→int 정합.
- ※ 지급물(재화/재료) 소비·검증 = 서버. 재화 모델(gold 등)은 turnrpg 경제 표면(현 미존재) 착지와 얽힘 → turnrpg-server 협의.

---

## 6. 디자인 결정 상태

**✅ 구조 갈래 확정 (사용자 2026-07-02):**
1. 강화 성장 대상 = **메인스탯만**
2. 강화 비용 = **재화 + 강화재료**
3. exp 획득원 = **전투 보상**(BattleReward += exp)

**남은 `[디자인미정=사용자/디자이너]` (밸런스 값·데이터 저작):**

**밸런스 값(데이터 저작 시·plan/서버 Config):**
- 최대 강화 레벨·강화 성장률·강화 비용 곡선
- 레벨 상한·exp 곡선·레벨업 스탯 성장
- 돌파 단계·단계별 레벨캡·돌파 재료
- 슬롯 카테고리 집합·장착 조건·세트 효과 값

## 7. 구현 순서 (디자인 확정 후)

1. **구조 갈래 3건 확정**(사용자) → 데이터/계약 형상 고정.
2. **@bridge 계약 REQUEST**(§5 표면) + **turnrpg-server 협의**(권위·재화·검증).
3. 클라 구현: 캐시(성장상태) · 요청 송신 배선(equip/enhance/breakthrough) · 응답 핸들러 · 스탯 재계산 · UI(강화/돌파 팝업 — A-2 그룹2).
4. 데이터(밸런스 값) = plan/디자이너 저작.
5. Unity 배선(팝업 프리팹) = `EDITOR_WIRING_GUIDE.md` 등재.
