/// <summary>
/// 단일 공유 그리드 배치 관리(서버 BattleGrid 정합). 아군·적 한 그리드에 글로벌 TileIndex로 배치.
/// </summary>
public class BattleFieldManager
{
    public TileGroup Grid { get; }

    public BattleFieldManager(TileGroup grid)
    {
        Grid = grid;
    }

    public bool TryPlaceUnit(IPlaceable unit, int tileIndex)
    {
        return Grid.TryPlace(unit, tileIndex);
    }

    public void RemoveUnit(IPlaceable unit)
    {
        Grid.Remove(unit);
    }
}
