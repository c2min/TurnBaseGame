using System.Collections.Generic;
using System.Threading.Tasks;
using SMDevLibrary.Network.Utility;
using SM.Contracts.TurnRPG;
using UnityEngine;

public class UISkillBar : MonoBehaviour
{
    [SerializeField]
    private UISkillSlot[] _slots;
    
    [SerializeField]
    private CanvasGroup _canvasGroup;
    
    [SerializeField]
    private BattleController _battleController;
    
    [SerializeField]
    private UISkillInfoPanel _skillInfoPanel;

    [SerializeField]
    private UISkillPointDots _spDots;

    private SkillSlotRuntime _pendingSkill;
    private UnitController _currentActor;
    private int _currentSkillPoint;
    private int _maxSkillPoint;

    private void Awake()
    {
        UnitManager.Instance.OnTurnStart += OnTurnStart;
        _battleController.OnTileConfirmed += OnTileConfirmed;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        UnitManager.Instance.OnTurnStart -= OnTurnStart;
        _battleController.OnTileConfirmed -= OnTileConfirmed;
    }

    private void OnTileConfirmed()
    {
        if (_pendingSkill != null)
            ExecuteSkill(_pendingSkill);
    }

    private void OnTurnStart(ITurnActor actor)
    {
        ClearPending();

        if (actor.Team == EUnitTeam.Enemy)
        {
            _currentActor = null;
            SetVisible(false);
            return;
        }

        if (actor is not UnitController unit || unit.ActiveSkills == null || unit.ActiveSkills.Count == 0)
        {
            _currentActor = null;
            SetVisible(false);
            return;
        }

        _currentActor = unit;
        Bind(unit.ActiveSkills);
        SetVisible(true);
    }

    public void UpdateSkillPoints(int sp, int maxSp)
    {
        _currentSkillPoint = sp;
        _maxSkillPoint = maxSp;
        _spDots?.Refresh(sp, maxSp);
        RefreshAllSlots();
    }

    private void Bind(IReadOnlyList<SkillSlotRuntime> skills)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            var skill = i < skills.Count ? skills[i] : null;
            _slots[i].Bind(skill, OnSkillClicked, OnSkillLongPressStart, OnSkillLongPressEnd);
        }
        RefreshAllSlots();
    }

    private void RefreshAllSlots()
    {
        foreach (var slot in _slots)
        {
            slot.BoundSkill?.UpdateAvailableSkillPoints(_currentSkillPoint);
            slot.Refresh();
        }
    }

    private void OnSkillLongPressStart(SkillSlotRuntime skill)
    {
        _skillInfoPanel?.Show(skill.Data);
    }

    private void OnSkillLongPressEnd()
    {
        _skillInfoPanel?.Hide();
    }

    private void OnSkillClicked(SkillSlotRuntime skill)
    {
        if (!skill.IsReady) return;

        if (_pendingSkill == skill)
            ExecuteSkill(skill);
        else
            SelectSkill(skill);
    }

    private void SelectSkill(SkillSlotRuntime skill)
    {
        _pendingSkill = skill;

        for (int i = 0; i < _slots.Length; i++)
            _slots[i].SetSelected(_slots[i].BoundSkill == skill);

        var rangeData = new SkillRangeData
        {
            RangeType  = skill.Data.RangeType,
            TargetTeam = skill.Data.TargetTeam,
        };

        _battleController.PreviewSkillRange(rangeData, _currentActor, skill.Data);
    }

    private async Task ExecuteSkill(SkillSlotRuntime skill)
    {
        var targets = _battleController.GetConfirmedTargetIds();

        skill.Use();

        // ⚠️ SP는 계약 미커버(스킬포인트 와이어 없음) → 순수 클라 로컬 낙관적 표현(서버 비동기). 디자인 정합 시 재검토.
        _currentSkillPoint = System.Math.Max(0, _currentSkillPoint - skill.Data.SkillPointCost);
        RefreshAllSlots();

        ClearPending();

        // 계약 BattleSkillUseRequestPacket: 단일 TargetUnitId·int SkillId·BattleId 운반.
        // ⚠️ 멀티타겟/AoE는 계약 단일타겟 → 첫 타겟만 송신(발산, 디자인 정합 시 재검토).
        // TODO(id 규약): SkillId string→int 파싱(템플릿 id 규약 @plan/turnrpg-server 확정 필요).
        int.TryParse(skill.Data.SkillId, out var skillId);
        UnityNetworkBridge.Instance.SendPacket(new BattleSkillUseRequestPacket
        {
            BattleId     = Client.Instance.ActiveBattleId,
            CasterUnitId = _currentActor.UnitId,
            SkillId      = skillId,
            TargetUnitId = targets != null && targets.Count > 0 ? targets[0] : string.Empty,
        });

        _battleController.SignalPlayerActed();
    }

    private void ClearPending()
    {
        _pendingSkill = null;

        foreach (var slot in _slots)
            slot.SetSelected(false);

        _battleController.ClearSkillPreview();
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
