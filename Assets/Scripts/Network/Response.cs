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

// ───────── 배틀 (STUB: 미마이그레이션 — 핸들러 미배선) ─────────
// StageInfo/SkillResult/NextTurn = 계약(BattleSnapshot/BattleSkillUseResponse/BattleNextTurnResponse)
// 으로 마이그레이션 완료 → 여기서 제거됨. 아래 EnemyAction/BattleReward는 핸들러 미배선 STUB.

/// <summary>적의 행동 결과 (서버 푸시). (STUB: 핸들러 미배선 — 계약 BattleEnemyActionPush)</summary>
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
