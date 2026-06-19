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

    private TileGroup _allyGroup;
    private TileGroup _enemyGroup;
    private TileHighlighter _highlighter;

    public void Initialize(StageInfo stageInfo)
    {
        if (PartyCache.Instance.IsValidated == false)
        {
            Debug.LogError("검증되지 않은 파티 데이터");
            return;
        }

        SetupTileGroups();
        SetupBattleFieldView();
        SetupAllies();
        SetupWave(stageInfo.Waves);
    }

    /// <summary>
    /// (온라인) 서버 권위 스냅샷으로 전투 초기화. 그리드/유닛 배치=서버 권위.
    /// ⚠️ TileIndex 매핑 = 팀별 5×3(`TileGroup` COL5×ROW3·Team별 0..14) 가정 — 서버 grid 규약 확인 필요(REQUEST).
    /// </summary>
    public void InitializeFromSnapshot(BattleSnapshotPacket snapshot)
    {
        SetupTileGroups();
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

    private void SetupTileGroups()
    {
        _allyGroup = new TileGroup(EUnitTeam.Ally);
        _enemyGroup = new TileGroup(EUnitTeam.Enemy);
        _highlighter = new TileHighlighter(_allyGroup, _enemyGroup);

        UnitManager.Instance.Initialize(_allyGroup, _enemyGroup);
    }

    private void SetupBattleFieldView()
    {
        _battleFieldView.Initialize(_highlighter, _allyGroup, _enemyGroup);
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