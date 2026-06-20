using System.Collections.Generic;

/// <summary>
/// 단일 공유 그리드 상의 스킬 범위 하이라이트 인덱스 계산. 가변 Width/Height 기준.
/// 팀 필터(GetUnitsInRange)는 공유 그리드 점유 유닛을 Team으로 거른다.
/// </summary>
public class TileHighlighter
{
    private readonly TileGroup _grid;

    // 재사용 버퍼 — GetHighlightIndices 반환값은 다음 호출 전까지만 유효
    private readonly HashSet<int> _indexBuffer = new();

    private static readonly (int dr, int dc)[] CrossOffsets =
        { (0, 0), (-1, 0), (1, 0), (0, -1), (0, 1) };

    public TileHighlighter(TileGroup grid)
    {
        _grid = grid;
    }

    public HashSet<int> GetHighlightIndices(SkillRangeData rangeData, int pivotIndex)
    {
        _indexBuffer.Clear();

        switch (rangeData.RangeType)
        {
            case ESkillRangeType.Single:
                _indexBuffer.Add(pivotIndex);
                break;
            case ESkillRangeType.Row:
                FillRow(pivotIndex);
                break;
            case ESkillRangeType.Column:
                FillColumn(pivotIndex);
                break;
            case ESkillRangeType.All:
                FillAll();
                break;
            case ESkillRangeType.Square3x3:
                FillSquare3x3(pivotIndex);
                break;
            case ESkillRangeType.Cross:
                FillCross(pivotIndex);
                break;
        }

        return _indexBuffer;
    }

    /// <summary>범위 내 점유 유닛 중 지정 팀만 반환(단일 공유 그리드 → 점유자 Team 필터).</summary>
    public List<IPlaceable> GetUnitsInRange(HashSet<int> indices, EUnitTeam team)
    {
        var result = new List<IPlaceable>();
        foreach (var unit in _grid.GetUnitsInRange(indices))
            if (unit.Team == team) result.Add(unit);
        return result;
    }

    private void FillRow(int pivot)
    {
        var (r, _) = _grid.ToRowCol(pivot);
        for (int c = 0; c < _grid.Width; c++)
            _indexBuffer.Add(_grid.ToIndex(r, c));
    }

    private void FillColumn(int pivot)
    {
        var (_, c) = _grid.ToRowCol(pivot);
        for (int r = 0; r < _grid.Height; r++)
            _indexBuffer.Add(_grid.ToIndex(r, c));
    }

    private void FillAll()
    {
        for (int i = 0; i < _grid.Capacity; i++)
            _indexBuffer.Add(i);
    }

    private void FillSquare3x3(int pivot)
    {
        var (pr, pc) = _grid.ToRowCol(pivot);
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
                if (_grid.IsValidRowCol(pr + dr, pc + dc))
                    _indexBuffer.Add(_grid.ToIndex(pr + dr, pc + dc));
    }

    private void FillCross(int pivot)
    {
        var (pr, pc) = _grid.ToRowCol(pivot);
        foreach (var (dr, dc) in CrossOffsets)
            if (_grid.IsValidRowCol(pr + dr, pc + dc))
                _indexBuffer.Add(_grid.ToIndex(pr + dr, pc + dc));
    }
}
