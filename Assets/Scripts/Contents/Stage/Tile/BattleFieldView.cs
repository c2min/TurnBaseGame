using System.Collections.Generic;
using UnityEngine;

public class BattleFieldView : MonoBehaviour
{
    [Header("타일 생성")]
    [SerializeField] private TileView _tileViewPrefab;
    [SerializeField] private Transform _allyTileRoot;   // 단일 공유 그리드 루트(레거시 필드명 유지)
    [SerializeField] private Transform _enemyTileRoot;  // (미사용 — 단일 그리드 수렴, ADR-007)
    [SerializeField] private float _tileSize = 1.2f;

    [Header("HP 게이지")]
    [SerializeField] private UnitHpGaugeView _hpGaugePrefab;
    [SerializeField] private Transform _hpGaugeContainer;

    private TileView[] _tileViews;

    private TileHighlighter _highlighter;

    // IPlaceable — 위치 기반 타일 연동
    // ICombatant — HP 게이지 연동
    private readonly Dictionary<ICombatant, UnitHpGaugeView> _hpGauges = new();
    private readonly HashSet<IPlaceable> _rangeBuffer = new();
    private static readonly HashSet<int> EmptyIndices = new();

    private TileView _selectedTile;
    private SkillRangeData _currentSkillRange;
    private ICombatant _skillCaster;
    private SkillData _currentSkillData;

    public event System.Action OnTileConfirmed;

    public void Initialize(TileHighlighter highlighter, TileGroup grid)
    {
        _highlighter = highlighter;

        _tileViews = GenerateGrid(grid);
        grid.OnTileChanged += OnTileChanged;
    }

    /// <summary>
    /// 단일 공유 그리드(가변 W×H)를 root 아래에 동적 생성. 글로벌 인덱스 = row*Width + col.
    /// ⚠️ 레이아웃: col=X(우), row=Y(상). side=Y영역 휴리스틱(저Y=아군·고Y=적, 색상용·서버 관례) — 렌더 튜닝=후속.
    /// </summary>
    private TileView[] GenerateGrid(TileGroup grid)
    {
        var views = new TileView[grid.Capacity];
        int half = grid.Height / 2;

        for (int row = 0; row < grid.Height; row++)
        {
            for (int col = 0; col < grid.Width; col++)
            {
                int index = grid.ToIndex(row, col);
                var side = row < half ? EUnitTeam.Ally : EUnitTeam.Enemy;
                var view = Instantiate(_tileViewPrefab, _allyTileRoot);
                view.name = $"Tile_{index}";
                view.transform.localPosition = new Vector3(col * _tileSize, row * _tileSize, 0f);
                view.Initialize(index, side, OnTileClicked);
                views[index] = view;
            }
        }

        return views;
    }

    private void OnTileChanged(Tile tile)
    {
        if (tile.Occupant != null)
        {
            // 유닛을 해당 타일의 월드 좌표로 이동
            if (tile.Occupant is MonoBehaviour mb)
                mb.transform.position = _tileViews[tile.Index].transform.position;

            if (tile.Occupant is ICombatant combatant)
                RegisterCombatant(combatant);
        }
        else
        {
            UnregisterByTile(tile.Index);
        }
    }

    private void RegisterCombatant(ICombatant combatant)
    {
        if (_hpGauges.ContainsKey(combatant))
            return;

        Transform worldTarget = (combatant as MonoBehaviour)?.transform;
        var gauge = Instantiate(_hpGaugePrefab, _hpGaugeContainer);
        gauge.Initialize(combatant, worldTarget);
        _hpGauges[combatant] = gauge;

        combatant.OnDamaged += OnCombatantDamaged;
        combatant.OnDied += OnCombatantDied;
    }

    private void UnregisterCombatant(ICombatant combatant)
    {
        combatant.OnDamaged -= OnCombatantDamaged;
        combatant.OnDied -= OnCombatantDied;

        if (_hpGauges.TryGetValue(combatant, out var gauge))
        {
            Destroy(gauge.gameObject);
            _hpGauges.Remove(combatant);
        }
    }

    private void UnregisterByTile(int tileIndex)
    {
        var target = FindCombatantByTile(tileIndex);
        if (target != null)
            UnregisterCombatant(target);
    }

    private ICombatant FindCombatantByTile(int tileIndex)
    {
        foreach (var combatant in _hpGauges.Keys)
        {
            if(combatant is IPlaceable placeable && placeable.TileIndex == tileIndex)
                return combatant;
        }

        return null;
    }

    private void OnCombatantDamaged(ICombatant combatant, int damage)
    {
        if (_hpGauges.TryGetValue(combatant, out var gauge))
            gauge.Refresh();
    }

    private void OnCombatantDied(ICombatant combatant)
    {
        UnregisterCombatant(combatant);
    }

    public void PreviewSkillRange(SkillRangeData skillRange, ICombatant caster, SkillData skillData)
    {
        _currentSkillRange = skillRange;
        _skillCaster       = caster;
        _currentSkillData  = skillData;
        _selectedTile      = null;
        RefreshHighlight();
    }

    public void ClearSkillPreview()
    {
        _currentSkillRange = null;
        _skillCaster       = null;
        _currentSkillData  = null;
        _selectedTile      = null;
        ClearHighlight();
    }

    private void RefreshHighlight()
    {
        if (_currentSkillRange == null)
        {
            ClearHighlight();
            return;
        }

        if (_selectedTile == null)
        {
            // 타일 미선택: 단일 그리드 전체를 피벗 후보로 하이라이트(AoE/타게팅 피벗 선택).
            // ⚠️ 구 '타겟팀 그리드 전체' 의미 → 단일 그리드선 전 셀 표시(서버 권위 타게팅, UI 의미 재정의=후속).
            HighlightAll(true);
            return;
        }

        // 타일 선택: 스킬 범위에 따른 정밀 하이라이트
        var indices = _highlighter.GetHighlightIndices(_currentSkillRange, _selectedTile.TileIndex);
        ApplyHighlight(indices);

        var unitsInRange = _highlighter.GetUnitsInRange(indices, _currentSkillRange.TargetTeam);
        UpdateHpGauges(unitsInRange);
    }

    // isDoubleClick: EventSystem의 clickCount >= 2 (동일 위치 연속 클릭)
    private void OnTileClicked(TileView tile, bool isDoubleClick)
    {
        if (_currentSkillRange == null) return;

        if (isDoubleClick || _selectedTile == tile)
        {
            OnTileConfirmed?.Invoke();
            return;
        }

        _selectedTile = tile;
        RefreshHighlight();
    }

    private void HighlightAll(bool highlight)
    {
        foreach (var view in _tileViews)
            view.SetState(highlight, false);
    }

    private void ApplyHighlight(HashSet<int> hlIndices)
    {
        foreach (var view in _tileViews)
        {
            bool isSelected = _selectedTile == view;
            bool isHL = hlIndices.Contains(view.TileIndex);
            view.SetState(isHL, isSelected);
        }
    }

    /// <summary>현재 선택된 타일 범위 안의 유닛 UnitId 목록 반환. 타일 미선택 시 빈 리스트.</summary>
    public List<string> GetConfirmedTargetUnitIds()
    {
        if (_currentSkillRange == null || _selectedTile == null)
            return new List<string>();

        var indices  = _highlighter.GetHighlightIndices(_currentSkillRange, _selectedTile.TileIndex);
        var units    = _highlighter.GetUnitsInRange(indices, _currentSkillRange.TargetTeam);
        var unitIds  = new List<string>(units.Count);
        foreach (var u in units)
            unitIds.Add(u.UnitId);
        return unitIds;
    }

    private void ClearHighlight()
    {
        foreach (var view in _tileViews) view.SetState(false, false);
        foreach (var gauge in _hpGauges.Values) gauge.ClearSimulation();
    }

    private void UpdateHpGauges(List<IPlaceable> unitsInRange)
    {
        _rangeBuffer.Clear();
        for (int i = 0; i < unitsInRange.Count; i++)
            _rangeBuffer.Add(unitsInRange[i]);

        foreach (var (combatant, gauge) in _hpGauges)
        {
            bool inRange = combatant is IPlaceable placeable && _rangeBuffer.Contains(placeable);
            if (!inRange)
            {
                gauge.ClearSimulation();
            }
            else
            {
                int projectedHp = SimulateSkillOnTarget(combatant);
                gauge.ShowSimulation(projectedHp);
            }
        }
    }

    private int SimulateSkillOnTarget(ICombatant target)
    {
        int projectedHp = target.CurrentHp;
        if (_skillCaster == null || _currentSkillData == null || _currentSkillData.Effects == null)
            return projectedHp;

        for (int i = 0; i < _currentSkillData.Effects.Length; i++)
        {
            if (_currentSkillData.Effects[i] != null)
                projectedHp = _currentSkillData.Effects[i].SimulateHp(_skillCaster, target, projectedHp);
        }
        return Mathf.Clamp(projectedHp, 0, target.MaxHp);
    }

    private void OnDestroy()
    {
        foreach (var combatant in _hpGauges.Keys)
        {
            combatant.OnDamaged -= OnCombatantDamaged;
            combatant.OnDied -= OnCombatantDied;
        }
    }
}
