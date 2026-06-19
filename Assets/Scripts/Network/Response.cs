// 로컬 EItemCategory/EItemRarity 등과 SM.Contracts.Core 동명 타입 충돌 회피 → base만 타깃 별칭.
using ResponsePacket = SM.Contracts.Core.ResponsePacket;
using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// ⚠️ STUB 패킷 (turnrpg 계약 미커버 / 미마이그레이션 슬라이스)
//    로비/로그인 슬라이스(LobbyLoginResponse·CharacterListResponse·PartyValidateResponse)
//    와 계약 DTO(CharacterInfo)는 SM.Contracts.TurnRPG로 마이그레이션 완료 → 여기서 제거됨.
//    아래는 base만 SM.Contracts.Core.ResponsePacket으로 rebase한 로컬 스텁:
//      · 인벤토리/판매/장비/해제 = 계약 미존재(스코프 결정 대기)
//      · 배틀/스테이지(SkillResult/NextTurn/EnemyAction/BattleReward/StageInfo) = 배틀 슬라이스 미마이그레이션
//    ⚠️ 스텁 DTO(SkillEffectResult·ESkillEffectType 등)는 SM.Contracts.TurnRPG의 동명 타입과
//       이름 충돌하므로 이 파일은 TurnRPG 네임스페이스를 import하지 않는다(배틀 마이그레이션 시 정리).
// ─────────────────────────────────────────────────────────────────────────────

// ───────── 공유 데이터 타입 (STUB) ─────────

[Serializable]
public class ItemInstanceInfo
{
    public string InstanceId;
    public string ItemId;
    public string ItemName;
    public EItemCategory Category;
    public EItemRarity Rarity;
    public StatEntry MainStat;
    public StatEntry[] SubStats;
    public int EnchantLevel;
    public bool IsLocked;
    public bool IsFavorite;
    public int CurrentSetCount;
}

// ───────── 턴 게이지 상태 (STUB: 배틀) ─────────

/// <summary>서버에서 수신한 유닛 한 명의 행동 게이지 현재값.</summary>
[Serializable]
public class UnitTurnInfo
{
    public string UnitId;
    /// <summary>현재 행동 게이지 (0 ~ 100). 높을수록 다음 턴이 가깝습니다.</summary>
    public float ActionGauge;
}

public enum ESkillEffectType
{
    Damage,
    Heal,
    StatusApply,
}

[Serializable]
public class SkillEffectResult
{
    public string TargetUnitId;
    public ESkillEffectType EffectType;
    /// <summary>Damage/Heal: 수치 / StatusApply: 세기(데미지·속도·실드량 등)</summary>
    public int Value;
    /// <summary>EffectType == StatusApply 일 때 EStatusEffect.ToString() 값</summary>
    public string StatusType;
    /// <summary>StatusApply일 때 지속 턴 수</summary>
    public int Duration;
    /// <summary>속성 상성 배율 (1.0 = 보통, 1.5 = 유리, 0.75 = 불리)</summary>
    public float ElementMultiplier = 1f;
}

[Serializable]
public class RewardItem
{
    public string ItemId;
    public int Amount;
}

public class ResponseHeartbeat : ResponsePacket { }

// ───────── 인벤토리 (STUB: 계약 미커버) ─────────

public class ResponseInventoryList : ResponsePacket
{
    public List<ItemInstanceInfo> Items;
    public int MaxCount;
}

public class ResponseSellItem : ResponsePacket
{
    public string InstanceId;
}

// ───────── 장착 (STUB: 계약 미커버) ─────────

public class ResponseEquipItem : ResponsePacket
{
    public string UnitId;
    public string EquippedInstanceId;
    public string UnequippedInstanceId;
}

public class ResponseUnequipItem : ResponsePacket
{
    public string UnitId;
    public string InstanceId;
}

// ───────── 스테이지 (STUB: 배틀 슬라이스) ─────────

[Serializable]
public class ResponseStageInfo : ResponsePacket
{
    public StageInfo StageInfo;
    /// <summary>서버가 결정한 첫 번째 행동 유닛 ID</summary>
    public string FirstActorId;
    /// <summary>스테이지 진입 직후 모든 생존 유닛의 행동 게이지 상태</summary>
    public List<UnitTurnInfo> TurnInfos;
    public int SkillPoint;
    public int MaxSkillPoint;
}

// ───────── 배틀 (STUB: 배틀 슬라이스) ─────────

/// <summary>스킬 사용 결과 — 적용된 모든 효과와 사망한 유닛 목록</summary>
[Serializable]
public class ResponseSkillResult : ResponsePacket
{
    public string CasterUnitId;
    public string SkillId;
    public List<SkillEffectResult> Effects;
    public List<string> DeadUnitIds;
    public int SkillPoint;
    public int MaxSkillPoint;
}

/// <summary>다음 행동 유닛 통보 — 해당 유닛의 UI를 활성화</summary>
[Serializable]
public class ResponseNextTurn : ResponsePacket
{
    public string NextUnitId;
    public bool IsEnemyTurn;
    /// <summary>턴 종료 후 모든 생존 유닛의 행동 게이지 상태. 턴 순서 시뮬레이션에 사용.</summary>
    public List<UnitTurnInfo> TurnInfos;
}

/// <summary>적의 행동 결과 (서버 푸시 — 클라이언트 요청 없이 서버가 전송)</summary>
[Serializable]
public class ResponseEnemyAction : ResponsePacket
{
    public string EnemyUnitId;
    public string SkillId;
    public List<SkillEffectResult> Effects;
    public List<string> DeadUnitIds;
}

/// <summary>스테이지 전투 보상 — 클리어 여부와 획득 보상 목록</summary>
[Serializable]
public class ResponseBattleReward : ResponsePacket
{
    public bool IsCleared;
    public List<RewardItem> Rewards;
}
