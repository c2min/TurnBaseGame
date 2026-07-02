using SM.Contracts.TurnRPG;
using UnityEngine;

// 오프라인 전투 폐지(2026-07-02) — 전투 초기화는 서버 권위 스냅샷(BattleSnapshot) 단일 경로.
public class StageDirector : MonoBehaviour
{
    [SerializeField]
    private AllySpawner _allySpawner;

    [SerializeField]
    private MonsterSpawner _monsterSpawner;

    [SerializeField]
    private BattleFieldView _battleFieldView;

    [SerializeField]
    private BattleController _battleController;

    // GridDto null 시 폴백 기본 치수(온라인=GridDto 서버 권위).
    private const int FallbackGridWidth = 6;
    private const int FallbackGridHeight = 6;

    private TileGroup _grid;
    private TileHighlighter _highlighter;

    /// <summary>
    /// 서버 권위 스냅샷으로 전투 초기화. 그리드 치수=GridDto, 유닛 배치=글로벌 TileIndex(서버 권위).
    /// 단일 공유 그리드(아군·적 공존, ADR-007 — turnrpg-server 규약: TileIndex=Y*Width+X).
    /// </summary>
    public void InitializeFromSnapshot(BattleSnapshotPacket snapshot)
    {
        int width = snapshot.Grid != null ? snapshot.Grid.Width : FallbackGridWidth;
        int height = snapshot.Grid != null ? snapshot.Grid.Height : FallbackGridHeight;
        SetupGrid(width, height);
        SetupBattleFieldView();

        if (snapshot.Units != null)
        {
            foreach (var dto in snapshot.Units)
            {
                if (dto.Team == ETeam.Ally)
                    _allySpawner.SpawnFromSnapshot(dto);
                else
                    _monsterSpawner.SpawnFromSnapshot(dto);
            }
        }
    }

    /// <summary>스냅샷 초기화 후 턴 루프 시작(서버 CurrentUnitId).</summary>
    public void StartBattle(string currentUnitId)
    {
        _battleController.StartLoop(currentUnitId);
    }

    public void NotifyNextActor(string nextUnitId)
    {
        _battleController.NotifyNextActor(nextUnitId);
    }

    private void SetupGrid(int width, int height)
    {
        _grid = new TileGroup(width, height);
        _highlighter = new TileHighlighter(_grid);

        UnitManager.Instance.Initialize(_grid);
    }

    private void SetupBattleFieldView()
    {
        _battleFieldView.Initialize(_highlighter, _grid);
        _battleController.SetBattleFieldView(_battleFieldView);
    }
}
