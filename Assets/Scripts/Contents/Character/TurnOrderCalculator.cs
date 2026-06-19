using System.Collections.Generic;

/// <summary>
/// 라운드 기반 턴 순서 계산기(서버 2D 모델 정합).
/// 라운드마다 생존 유닛을 Speed 내림차순(동률=UnitId Ordinal)으로 1회씩 행동시킨다.
/// ⚠️ HSR ATB 행동게이지 모델(점진 충전·100 임계·강인도 소진 지연) 제거 — 2026-06-20, ADR-005.
///    서버 TurnManager(라운드 Speed 내림차순)와 정합. 온라인은 서버 NextUnitId 권위(이 계산기 미사용),
///    오프라인 폴백만 이 계산기로 진행.
/// </summary>
public class TurnOrderCalculator
{
    private readonly List<ITurnActor> _units = new();
    private readonly List<ITurnActor> _roundQueue = new();

    public void Initialize(IEnumerable<ITurnActor> units)
    {
        _units.Clear();
        foreach (var unit in units)
        {
            if (unit.IsAlive)
                _units.Add(unit);
        }
        _roundQueue.Clear();
    }

    /// <summary>
    /// 다음 행동 유닛. 현재 라운드가 소진되면 생존 유닛으로 다음 라운드를 Speed 내림차순 구성.
    /// 죽었거나 행동 불가(CanAct=false) 유닛은 건너뛴다.
    /// </summary>
    public ITurnActor GetNextUnit()
    {
        while (true)
        {
            if (_roundQueue.Count == 0)
            {
                BuildRound();
                if (_roundQueue.Count == 0)
                    return null;
            }

            var next = _roundQueue[0];
            _roundQueue.RemoveAt(0);

            if (next.IsAlive && next.CanAct)
                return next;
        }
    }

    /// <summary>
    /// UI 표시용 — 다가오는 턴 순서(현 라운드 잔여 → 비면 다음 라운드 미리보기).
    /// 라운드 모델은 게이지가 없어 두 번째 값은 항상 0(시그니처 호환 유지).
    /// </summary>
    public List<(ITurnActor actor, float gauge)> GetCurrentGaugesSorted()
    {
        var preview = new List<(ITurnActor actor, float gauge)>();

        foreach (var unit in _roundQueue)
        {
            if (unit.IsAlive)
                preview.Add((unit, 0f));
        }

        if (preview.Count == 0)
        {
            var next = BuildOrder();
            foreach (var unit in next)
                preview.Add((unit, 0f));
        }

        return preview;
    }

    /// <summary>
    /// (레거시 호환·no-op) ATB 게이지 제거로 강인도 소진의 '턴 지연' 효과는 미적용.
    /// 강인도 break의 턴 효과 재정의는 후속(서버 2D 모델 기준).
    /// </summary>
    public void ResetGauge(ITurnActor unit) { /* 라운드 모델 = 게이지 없음 */ }

    private void BuildRound()
    {
        _roundQueue.Clear();
        _roundQueue.AddRange(BuildOrder());
    }

    private List<ITurnActor> BuildOrder()
    {
        var order = new List<ITurnActor>();
        foreach (var unit in _units)
        {
            if (unit.IsAlive)
                order.Add(unit);
        }

        order.Sort((a, b) =>
        {
            int bySpeed = b.Speed.CompareTo(a.Speed); // Speed 내림차순
            return bySpeed != 0 ? bySpeed : string.CompareOrdinal(a.UnitId, b.UnitId);
        });

        return order;
    }
}
