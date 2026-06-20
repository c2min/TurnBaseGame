using System;
using System.Collections.Generic;

public enum ESkillRangeType
{
    Single,    // 단일 타겟
    Row,       // 같은 행 전체
    Column,    // 같은 열 전체
    All,       // 전체
    Square3x3, // 3x3 사각형
    Cross,     // 십자형
}

[Serializable]
public class SkillRangeData
{
    public ESkillRangeType RangeType;
    public EUnitTeam TargetTeam;
}

/// <summary>
/// 단일 공유 전투 그리드(가변 W×H). 아군·적이 한 그리드에 공존(서버 BattleGrid 정합·FFT식).
/// 글로벌 TileIndex = row*Width + col (row=Y, col=X). 서버 GridPosition.ToIndex(w)=Y*w+X와 동형.
/// ⚠️ 2026-06-20 ADR-007: 구 팀별 5×3(COL/ROW 정적) 모델 폐기 → 단일 공유·가변 치수로 수렴.
/// </summary>
public class TileGroup
{
    public int Width { get; }
    public int Height { get; }
    public int Capacity => Width * Height;

    private readonly Tile[] _tiles;

    public event Action<Tile> OnTileChanged;

    public TileGroup(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        _tiles = new Tile[Width * Height];
        for (int i = 0; i < _tiles.Length; i++)
        {
            _tiles[i] = new Tile(i);
            _tiles[i].OnOccupantChanged += t => OnTileChanged?.Invoke(t);
        }
    }

    // -------------------------------------------------------
    // 배치 / 제거

    public bool TryPlace(IPlaceable unit, int index)
    {
        if (!IsValidIndex(index)) return false;
        return _tiles[index].TryPlace(unit);
    }

    public void Remove(IPlaceable unit)
    {
        var tile = FindTile(unit);
        tile?.Remove();
    }

    public bool TryMove(IPlaceable unit, int toIndex)
    {
        if (!IsValidIndex(toIndex) || !_tiles[toIndex].IsEmpty) return false;

        var from = FindTile(unit);
        if (from == null) return false;

        from.Remove();
        return _tiles[toIndex].TryPlace(unit);
    }

    // -------------------------------------------------------
    // 조회

    public Tile GetTile(int index) => IsValidIndex(index) ? _tiles[index] : null;

    public IPlaceable GetUnit(int index) => GetTile(index)?.Occupant;

    public IEnumerable<Tile> GetAllTiles() => _tiles;

    public IEnumerable<Tile> GetOccupiedTiles()
    {
        for (int i = 0; i < _tiles.Length; i++)
            if (!_tiles[i].IsEmpty) yield return _tiles[i];
    }

    public IEnumerable<Tile> GetEmptyTiles()
    {
        for (int i = 0; i < _tiles.Length; i++)
            if (_tiles[i].IsEmpty) yield return _tiles[i];
    }

    public IEnumerable<IPlaceable> GetAllUnits()
    {
        for (int i = 0; i < _tiles.Length; i++)
            if (!_tiles[i].IsEmpty) yield return _tiles[i].Occupant;
    }

    public Tile FindTile(IPlaceable unit)
    {
        for (int i = 0; i < _tiles.Length; i++)
            if (_tiles[i].Occupant == unit) return _tiles[i];
        return null;
    }

    public List<IPlaceable> GetUnitsInRange(IEnumerable<int> indices)
    {
        var result = new List<IPlaceable>();
        foreach (int i in indices)
        {
            if (!IsValidIndex(i)) continue;
            var occupant = _tiles[i].Occupant;
            if (occupant != null) result.Add(occupant);
        }
        return result;
    }

    public Tile GetFirstEmptyTile()
    {
        for (int i = 0; i < _tiles.Length; i++)
            if (_tiles[i].IsEmpty) return _tiles[i];
        return null;
    }

    // -------------------------------------------------------
    // 좌표 유틸 (인스턴스 — 글로벌 TileIndex = row*Width + col)

    public (int row, int col) ToRowCol(int index) => (index / Width, index % Width);

    public int ToIndex(int row, int col) => row * Width + col;

    public bool IsValidRowCol(int row, int col) => row >= 0 && row < Height && col >= 0 && col < Width;

    private bool IsValidIndex(int index) => index >= 0 && index < Capacity;
}
