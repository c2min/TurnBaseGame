using System.Collections.Generic;

/// <summary>
/// EEffectTarget 기준으로 실제 대상 목록을 반환합니다.
///
/// 팀 기반 타겟팅:  시전자의 Team 기준으로 적/아군 전체에서 선택합니다.
/// 위치 기반 타겟팅: 시전자의 row(라인)와 같은 라인을 col 순으로 정렬해 선택합니다.
///   앞(front) = col 0, 뒤(back) = col 4
/// </summary>
public static class SkillTargetResolver
{
    public static List<ICombatant> Resolve(EEffectTarget effectTarget, ICombatant caster)
    {
        return effectTarget switch
        {
            // ── 팀 기반 ─────────────────────────────────────
            EEffectTarget.Self               => new List<ICombatant> { caster },
            EEffectTarget.SingleEnemy        => PickLowestHp(GetOpponents(caster)),
            EEffectTarget.AllEnemies         => GetOpponents(caster),
            EEffectTarget.SingleAlly         => PickLowestHp(GetAllies(caster)),
            EEffectTarget.AllAllies          => GetAllies(caster),

            // ── 위치 기반 ────────────────────────────────────
            EEffectTarget.FrontOfSameLane    => PickFront(GetSameLaneOpponents(caster)),
            EEffectTarget.BackOfSameLane     => PickBack(GetSameLaneOpponents(caster)),
            EEffectTarget.OneStepBehindFront => PickOneStepBehindFront(GetSameLaneOpponents(caster)),
            EEffectTarget.AllSameLane        => GetSameLaneOpponents(caster),

            _ => new List<ICombatant>(),
        };
    }

    // ── 팀 기반 헬퍼 ──────────────────────────────────────────────────────────

    private static List<ICombatant> GetOpponents(ICombatant caster)
        => ToCombatants(caster.Team == EUnitTeam.Enemy
            ? UnitManager.Instance.GetAllies()
            : UnitManager.Instance.GetEnemies());

    private static List<ICombatant> GetAllies(ICombatant caster)
        => ToCombatants(caster.Team == EUnitTeam.Enemy
            ? UnitManager.Instance.GetEnemies()
            : UnitManager.Instance.GetAllies());

    private static List<ICombatant> PickLowestHp(List<ICombatant> candidates)
    {
        if (candidates.Count == 0) return candidates;

        var lowest = candidates[0];
        for (int i = 1; i < candidates.Count; i++)
            if (candidates[i].CurrentHp < lowest.CurrentHp) lowest = candidates[i];

        return new List<ICombatant> { lowest };
    }

    // ── 위치 기반 헬퍼 ────────────────────────────────────────────────────────

    /// <summary>
    /// 시전자와 같은 row의 상대 팀 유닛을 col 오름차순으로 반환.
    /// ⚠️ 레거시(BrownDust2 레인) 의미 — 단일 공유 그리드(FFT)에선 같은 row의 적팀 점유자(col순)로 근사.
    ///    온라인 타게팅은 서버 권위(앵커 송신·Effects 수신)라 이 경로는 오프라인/프리뷰용. 의미 재정의=후속(ADR-007).
    /// </summary>
    private static List<ICombatant> GetSameLaneOpponents(ICombatant caster)
    {
        int casterRow = GetRow(caster);
        if (casterRow < 0) return new List<ICombatant>();

        var opponentTeam = caster.Team == EUnitTeam.Enemy ? EUnitTeam.Ally : EUnitTeam.Enemy;
        var grid = UnitManager.Instance.BattleField?.Grid;
        if (grid == null) return new List<ICombatant>();

        // 같은 row를 col 오름차순으로 순회, 적팀 점유자만
        var result = new List<ICombatant>();
        for (int col = 0; col < grid.Width; col++)
        {
            int index    = grid.ToIndex(casterRow, col);
            var occupant = grid.GetUnit(index);
            if (occupant is ICombatant c && c.IsAlive && c.Team == opponentTeam)
                result.Add(c);
        }

        return result; // 이미 col 오름차순
    }

    /// <summary>리스트의 첫 번째(가장 앞) 유닛을 반환합니다.</summary>
    private static List<ICombatant> PickFront(List<ICombatant> laneSorted)
    {
        if (laneSorted.Count == 0) return laneSorted;
        return new List<ICombatant> { laneSorted[0] };
    }

    /// <summary>리스트의 마지막(가장 뒤) 유닛을 반환합니다.</summary>
    private static List<ICombatant> PickBack(List<ICombatant> laneSorted)
    {
        if (laneSorted.Count == 0) return laneSorted;
        return new List<ICombatant> { laneSorted[laneSorted.Count - 1] };
    }

    /// <summary>
    /// 앞에서 두 번째 유닛을 반환합니다.
    /// 두 번째가 없으면 최전방으로 폴백합니다.
    /// </summary>
    private static List<ICombatant> PickOneStepBehindFront(List<ICombatant> laneSorted)
    {
        if (laneSorted.Count == 0) return laneSorted;
        var target = laneSorted.Count >= 2 ? laneSorted[1] : laneSorted[0];
        return new List<ICombatant> { target };
    }

    // ── 공통 유틸 ─────────────────────────────────────────────────────────────

    /// <summary>시전자의 TileIndex에서 row를 추출합니다. IPlaceable가 아니면 -1.</summary>
    private static int GetRow(ICombatant caster)
    {
        if (caster is not IPlaceable placeable || placeable.TileIndex < 0) return -1;
        var grid = UnitManager.Instance.BattleField?.Grid;
        if (grid == null) return -1;
        return grid.ToRowCol(placeable.TileIndex).row;
    }

    private static List<ICombatant> ToCombatants(List<IUnit> units)
    {
        var result = new List<ICombatant>(units.Count);
        foreach (var u in units)
            if (u is ICombatant c && c.IsAlive) result.Add(c);
        return result;
    }
}
