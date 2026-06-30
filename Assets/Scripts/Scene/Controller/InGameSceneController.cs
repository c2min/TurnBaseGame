using Cysharp.Threading.Tasks;
using SM.Contracts.Core;
using SM.Contracts.TurnRPG;
// 로컬 global ESkillEffectType(STUB)와 alias 동명 충돌(CS0576) 회피 → 구분 이름으로 계약 enum 참조.
using ContractEffect = SM.Contracts.TurnRPG.ESkillEffectType;
using System.Collections.Generic;
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
        RegisterPacketHandler<BattleEnemyActionPushPacket>(OnEnemyAction);
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

    /// <summary>아군 스킬 사용 결과(서버 권위 Effects) 적용.</summary>
    private void OnSkillResult(BattleSkillUseResponsePacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;
        ApplyEffects(res.Effects);
    }

    /// <summary>
    /// 적 행동(서버 권위 푸시) — 적 턴은 완전 서버 주도(클라 입력 0·연속 푸시). 적 행동/타겟/데미지/이동=서버.
    /// 순서: ①이동 반영(그리드 desync 방지) ②연출 ③Effects 적용 ④적 페이즈 종료 시 다음 actor 진행.
    /// </summary>
    private void OnEnemyAction(BattleEnemyActionPushPacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

        var caster = UnitManager.Instance.GetUnit(res.CasterUnitId);

        // ① 이동 먼저(MovedToTileIndex<0=이동 안 함). 시각 재배치는 BattleFieldView가 OnTileChanged로 처리.
        if (res.MovedToTileIndex >= 0 && caster is IPlaceable mover)
            UnitManager.Instance.MoveUnit(mover, res.MovedToTileIndex);

        // ② 적 행동 애니메이션(연출·fire-and-forget — 푸시는 연속 송신이라 핸들러는 동기 적용)
        if (caster is UnitController casterUnit)
            casterUnit.PlayActionAnimationAsync().Forget();

        // ③ Effects 적용(아군 응답과 동형·서버 권위)
        ApplyEffects(res.Effects);

        // ④ 진행: 적 페이즈 종료(IsEnemyTurn=false)면 NextUnitId(아군)로 턴 루프 재개.
        //    적 페이즈 도중(true)은 다음 적 푸시가 이어 운반 → 루프 통지 안 함.
        if (!res.IsEnemyTurn)
            _stageDirector.NotifyNextActor(res.NextUnitId);
    }

    private void OnNextTurn(BattleNextTurnResponsePacket res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

        // 턴 종료 유닛의 Periodic(독 DoT/재생 HoT) 효과 — 스킬과 동형 SkillEffectDto·기존 렌더러 재사용.
        // 독 치사 시 해당 UnitId가 Effects 데미지로 사망(TakeDamage→OnDied→그리드 제거, 스킬 사망과 동일).
        ApplyEffects(res.Effects);

        // 적 페이즈 시작(IsEnemyTurn=true)은 진행을 적 푸시가 운반 → 루프 대기 유지(여기서 진행 안 함).
        if (res.IsEnemyTurn) return;

        // 라운드 기반(NextUnitId/RoundNumber) — 아군 턴 진행은 서버 NextUnitId로(BattleController).
        _stageDirector.NotifyNextActor(res.NextUnitId);
    }

    // INFO :: 스킬 효과 적용(아군 BattleSkillUseResponse·적 BattleEnemyActionPush 공용). 타겟/수치=서버 권위.
    private void ApplyEffects(List<SkillEffectDto> effects)
    {
        if (effects == null) return;

        foreach (var effect in effects)
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

    #endregion
}
