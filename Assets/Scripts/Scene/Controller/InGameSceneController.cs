using SM.Contracts.Core;
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

        RegisterPacketHandler<ResponseStageInfo>(OnStageInfo);
        RegisterPacketHandler<ResponseSkillResult>(OnSkillResult);
        RegisterPacketHandler<ResponseNextTurn>(OnNextTurn);
    }

    public override void OnSceneExit()
    {
        UnitManager.Instance.Clear();
    }

    #region Packet Handler

    private void OnStageInfo(ResponseStageInfo res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

        _stageDirector.Initialize(res.StageInfo);
        if (_uiPartyPanel != null)
            _uiPartyPanel.Initialize(UnitManager.Instance.GetAllies());
        _stageDirector.StartStage(res.FirstActorId);

        // 스테이지 진입 직후 게이지 상태 동기화 (스폰 완료 후 유닛이 등록됨)
        UnitManager.Instance.SyncTurnOrder(res.TurnInfos);
        _skillBar?.UpdateSkillPoints(res.SkillPoint, res.MaxSkillPoint);
    }

    private void OnSkillResult(ResponseSkillResult res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

        _skillBar?.UpdateSkillPoints(res.SkillPoint, res.MaxSkillPoint);

        foreach (var effect in res.Effects)
        {
            var unit = UnitManager.Instance.GetUnit(effect.TargetUnitId);
            if (unit is not ICombatant target || !target.IsAlive) continue;

            switch (effect.EffectType)
            {
                case ESkillEffectType.Damage:
                    target.TakeDamage(effect.Value);
                    break;
                case ESkillEffectType.Heal:
                    target.Heal(effect.Value);
                    break;
                case ESkillEffectType.StatusApply:
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

    private void OnNextTurn(ResponseNextTurn res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;
        UnitManager.Instance.SyncTurnOrder(res.TurnInfos);
        _stageDirector.NotifyNextActor(res.NextUnitId);
    }

    #endregion
}
