// 로컬 EItemCategory 등과 SM.Contracts.Core 동명 타입 충돌 회피 → base만 타깃 별칭.
using RequestPacket = SM.Contracts.Core.RequestPacket;
using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// ⚠️ STUB 패킷 (turnrpg 계약 미커버 / 미마이그레이션 슬라이스)
//    로비/로그인 슬라이스(Login·CharacterList·PartyValidate·StageEnter)는
//    SM.Contracts.TurnRPG 계약 타입으로 마이그레이션 완료 → 여기서 제거됨.
//    아래는 base만 SM.Contracts.Core.RequestPacket으로 rebase한 로컬 스텁:
//      · 판매 = turnrpg 계약 미존재(후속). 장착/인벤 = equipment.* 계약 마이그 완료(2026-07-03) → 제거됨.
//      · 배틀 결과보고(BattleResult) = 계약 존재하나 미배선(C 대상)
//    ⚠️ 스텁은 와이어 비유효(서버 미인식) — 해당 슬라이스 마이그레이션 시 계약 타입으로 교체.
// ─────────────────────────────────────────────────────────────────────────────

public class RequestHeartbeat : RequestPacket
{
    public static readonly RequestHeartbeat Shared = new();
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
