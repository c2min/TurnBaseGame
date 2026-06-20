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
    /// 전투 스냅샷(서버 권위 초기 상태) — BattleId 캡처 + 그리드/유닛 초기화 + 턴 루프 시작.
    /// 유닛 배치·스탯=서버 권위, 비주얼=TemplateId 해소(아군=CharacterDatabase / 적 TemplateId=0=플레이스홀더).
    /// 단일 공유 그리드·글로벌 TileIndex(ADR-007, 서버 규약 확정).
    /// </summary>
    private void OnBattleSnapshot(BattleSnapshotPacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;
        Client.Instance.ActiveBattleId = res.BattleId;

        _stageDirector.InitializeFromSnapshot(res);
        if (_uiPartyPanel != null)
            _uiPartyPanel.Initialize(UnitManager.Instance.GetAllies());
        _stageDirector.StartBattle(res.CurrentUnitId);
    }

    private void OnSkillResult(BattleSkillUseResponsePacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

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
        // 라운드 기반(NextUnitId/RoundNumber) — 턴 루프는 서버 NextUnitId로 진행(BattleController).
        _stageDirector.NotifyNextActor(res.NextUnitId);
    }

    #endregion
}
