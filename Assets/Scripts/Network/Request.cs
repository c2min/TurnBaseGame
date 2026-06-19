// 로컬 EItemCategory 등과 SM.Contracts.Core 동명 타입 충돌 회피 → base만 타깃 별칭.
using RequestPacket = SM.Contracts.Core.RequestPacket;
using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// ⚠️ STUB 패킷 (turnrpg 계약 미커버 / 미마이그레이션 슬라이스)
//    로비/로그인 슬라이스(Login·CharacterList·PartyValidate·StageEnter)는
//    SM.Contracts.TurnRPG 계약 타입으로 마이그레이션 완료 → 여기서 제거됨.
//    아래는 base만 SM.Contracts.Core.RequestPacket으로 rebase한 로컬 스텁:
//      · 인벤토리/판매/장비/해제 = turnrpg 계약 미존재(스코프 결정 대기, status 참조)
//      · 배틀(SkillUse/TurnEnd/BattleResult) = 계약 존재하나 배틀 슬라이스 미마이그레이션
//    ⚠️ 스텁은 와이어 비유효(서버 미인식) — 해당 슬라이스 마이그레이션 시 계약 타입으로 교체.
// ─────────────────────────────────────────────────────────────────────────────

public class RequestHeartbeat : RequestPacket
{
    public static readonly RequestHeartbeat Shared = new();
}

// ───────── 인벤토리 (STUB: 계약 미커버) ─────────

public class RequestInventoryList : RequestPacket
{
    public static readonly RequestInventoryList Shared = new();
}

public class RequestSellItem : RequestPacket
{
    public string InstanceId;
}

// ───────── 장착 (STUB: 계약 미커버) ─────────

public class RequestEquipItem : RequestPacket
{
    public string UnitId;
    public string InstanceId;
    /// <summary>장착 슬롯 — 아이템 선택 시 이미 알고 있는 카테고리</summary>
    public EItemCategory Slot;
}

public class RequestUnequipItem : RequestPacket
{
    public string UnitId;
    public EItemCategory Slot;
}

// ───────── 배틀 (STUB: 미마이그레이션 — 결과보고 와이어 미배선) ─────────
// SkillUse/TurnEnd = 계약(BattleSkillUse/BattleTurnEndRequestPacket)으로 마이그레이션 완료 → 여기서 제거됨.

/// <summary>스테이지 전투 결과 보고 — 서버는 검증 후 BattleReward 반환. (STUB: 핸들러 미배선)</summary>
public class RequestBattleResult : RequestPacket
{
    public string StageId;
    public bool IsCleared;
    public int WavesCleared;
    public List<string> SurvivedAllyIds;
}
