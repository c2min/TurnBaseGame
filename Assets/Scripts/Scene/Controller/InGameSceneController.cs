using SM.Contracts.Core;
using SM.Contracts.TurnRPG;
// 로컬 global ESkillEffectType(STUB)와 alias 동명 충돌(CS0576) 회피 → 구분 이름으로 계약 enum 참조.
using ContractEffect = SM.Contracts.TurnRPG.ESkillEffectType;
using UnityEngine;

public class InGameSceneController : SceneController
{
    [SerializeField]
    private StageDirector _stageDirector;

    [SerializeField]
    private UIPartyPanel _uiPartyPanel;

    [SerializeField]
    private UISkillBar _skillBar;

    protected override void Awake()
    {
        base.Awake();

        RegisterPacketHandler<StageEnterResponsePacket>(OnStageEnter);
        RegisterPacketHandler<BattleSnapshotPacket>(OnBattleSnapshot);
        RegisterPacketHandler<BattleSkillUseResponsePacket>(OnSkillResult);
        RegisterPacketHandler<BattleNextTurnResponsePacket>(OnNextTurn);
    }

    public override void OnSceneExit()
    {
        UnitManager.Instance.Clear();
    }

    #region Packet Handler

    /// <summary>StageEnter ack — 서버가 생성한 전투의 BattleId 캡처(이후 SkillUse/TurnEnd 송신에 사용).</summary>
    private void OnStageEnter(StageEnterResponsePacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;
        Client.Instance.ActiveBattleId = res.BattleId;
    }

    /// <summary>
    /// 전투 스냅샷(서버 권위 초기 상태). BattleId 캡처.
    /// ⚠️ TODO(배틀-init 그리드모델 리워크): 계약 스냅샷=Grid(W/H)+Units(BattleUnitDto)+CurrentUnitId 형상으로
    ///    구 클라 init(StageDirector.Initialize(StageInfo 웨이브)·스폰)과 모델 발산. 또한 BattleUnitDto는
    ///    UnitId만 운반(템플릿/비주얼 id 없음)→비주얼 해소 불가. 그리드 init은 후속(디자인/계약 정합 필요).
    /// </summary>
    private void OnBattleSnapshot(BattleSnapshotPacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;
        Client.Instance.ActiveBattleId = res.BattleId;
        // TODO: res.Grid/res.Units/res.CurrentUnitId 기반 전투 초기화(StageDirector·스폰·StartStage). 후속 슬라이스.
    }

    private void OnSkillResult(BattleSkillUseResponsePacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

        // ⚠️ 계약 미커버: SkillPoint(SP)는 와이어 없음 → 서버 보정 불가(클라 로컬 낙관값 유지).
        foreach (var effect in res.Effects)
        {
            var unit = UnitManager.Instance.GetUnit(effect.TargetUnitId);
            if (unit is not ICombatant target || !target.IsAlive) continue;

            switch (effect.EffectType)
            {
                case ContractEffect.Damage:
                    target.TakeDamage(effect.Value);
                    break;
                case ContractEffect.Heal:
                    target.Heal(effect.Value);
                    break;
                case ContractEffect.StatusApply:
                    if (target is IStatusReceiver receiver)
                    {
                        var statusEffect = StatusEffectFactory.Create(effect.StatusType, effect.Duration, effect.Value);
                        if (statusEffect != null)
                            receiver.ApplyStatus(statusEffect);
                    }
                    break;
            }
        }
    }

    private void OnNextTurn(BattleNextTurnResponsePacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;
        // ⚠️ 계약은 라운드 기반(NextUnitId/RoundNumber) — 구 ATB 게이지(UnitTurnInfo) 미운반 →
        //    UnitManager.SyncTurnOrder(게이지 동기화) 생략. 턴 루프는 NextUnitId로 진행(BattleController).
        _stageDirector.NotifyNextActor(res.NextUnitId);
    }

    #endregion
}
