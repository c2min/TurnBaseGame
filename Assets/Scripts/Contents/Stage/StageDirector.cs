using SM.Contracts.TurnRPG;
using UnityEngine;

public class StageDirector : MonoBehaviour
{
    [SerializeField]
    private WaveController _waveController;
    
    [SerializeField]
    private AllySpawner _allySpawner;
    
    [SerializeField]
    private MonsterSpawner _monsterSpawner;
    
    [SerializeField]
    private BattleFieldView _battleFieldView;

    [SerializeField]
    private BattleController _battleController;

    // 오프라인/레거시 폴백 기본 그리드 치수(온라인=GridDto). ⚠️ 임시값 — @plan 오프라인 스펙 시 조정.
    private const int OfflineGridWidth = 6;
    private const int OfflineGridHeight = 6;

    private TileGroup _grid;
    private TileHighlighter _highlighter;

    public void Initialize(StageInfo stageInfo)
    {
        if (PartyCache.Instance.IsValidated == false)
        {
            Debug.LogError("검증되지 않은 파티 데이터");
            return;
        }

        SetupGrid(OfflineGridWidth, OfflineGridHeight);
        SetupBattleFieldView();
        SetupAllies();
        SetupWave(stageInfo.Waves);
    }

    /// <summary>
    /// (온라인) 서버 권위 스냅샷으로 전투 초기화. 그리드 치수=GridDto, 유닛 배치=글로벌 TileIndex(서버 권위).
    /// 단일 공유 그리드(아군·적 공존, ADR-007 — turnrpg-server 규약 확정: TileIndex=Y*Width+X).
    /// </summary>
    public void InitializeFromSnapshot(BattleSnapshotPacket snapshot)
    {
        int width = snapshot.Grid != null ? snapshot.Grid.Width : OfflineGridWidth;
        int height = snapshot.Grid != null ? snapshot.Grid.Height : OfflineGridHeight;
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

    /// <summary>(온라인) 스냅샷 초기화 후 턴 루프 시작(서버 CurrentUnitId). 웨이브 미사용.</summary>
    public void StartBattle(string currentUnitId)
    {
        _battleController.StartLoop(currentUnitId);
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

    private void SetupAllies()
    {
        foreach (var allySet in PartyCache.Instance.AllySetInfos)
        {
            _allySpawner.SpawnAlly(allySet);
        }    
    }

    private void SetupWave(WaveInfo[] waves)
    {
        _waveController.Initialize(waves);
        _waveController.OnWaveStart += HandleWaveStart;
        _waveController.OnWaveEnd += HandleWaveEnd;
        _waveController.OnAllWavesCleared += HandleStageClear;
    }

    public void StartStage(string firstActorId)
    {
        _battleController.StartLoop(firstActorId);
        _waveController.StartWave();
    }

    public void NotifyNextActor(string nextUnitId)
    {
        _battleController.NotifyNextActor(nextUnitId);
    }

    private void HandleWaveStart(int index) 
    {
        Debug.Log($"웨이브 {index + 1} 시작");
    }

    private void HandleWaveEnd(int index) => Debug.Log($"웨이브 {index + 1} 클리어");

    private void HandleStageClear()
    {
        PartyCache.Instance.Clear();
        Debug.Log("스테이지 클리어");
    }

    private void OnDestroy()
    {
        _waveController.OnWaveStart -= HandleWaveStart;
        _waveController.OnWaveEnd -= HandleWaveEnd;
        _waveController.OnAllWavesCleared -= HandleStageClear;
    }
}