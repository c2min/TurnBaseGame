// 로컬 EItemCategory/EItemRarity 등과 SM.Contracts.Core 동명 타입 충돌 회피 → base만 타깃 별칭.
using ResponsePacket = SM.Contracts.Core.ResponsePacket;
using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// ⚠️ STUB 패킷 (turnrpg 계약 미커버 / 미마이그레이션 슬라이스)
//    로비/로그인 슬라이스(LobbyLoginResponse·CharacterListResponse·PartyValidateResponse)
//    와 계약 DTO(CharacterInfo)는 SM.Contracts.TurnRPG로 마이그레이션 완료 → 여기서 제거됨.
//    아래는 base만 SM.Contracts.Core.ResponsePacket으로 rebase한 로컬 스텁:
//      · 판매 = 계약 미존재(후속). 장착/인벤 = equipment.* 계약 마이그 완료(2026-07-03) → 제거됨.
//      · 배틀 보상(BattleReward) = 계약 BattleRewardResponse 핸들러 미배선(C 배선 대상)
//    ※ 배틀 STUB(EnemyAction/SkillEffectResult/ESkillEffectType)는 계약 마이그 완료 → 제거됨(2026-07-03 D).
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class RewardItem
{
    public string ItemId;
    public int Amount;
}

public class ResponseHeartbeat : ResponsePacket { }

// ───────── 배틀 보상 (STUB: 계약 BattleRewardResponse 핸들러 미배선 — C 배선 대상) ─────────

/// <summary>스테이지 전투 보상 — 클리어 여부와 획득 보상 목록</summary>
[Serializable]
public class ResponseBattleReward : ResponsePacket
{
    public bool IsCleared;
    public List<RewardItem> Rewards;
}
