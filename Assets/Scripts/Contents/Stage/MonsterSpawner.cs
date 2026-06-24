using SM.Contracts.TurnRPG;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField]
    private EnemyUnitController _enemyPrefab;

    /// <summary>(레거시/오프라인) 웨이브 데이터로 적 스폰.</summary>
    public void SpawnWave(WaveInfo waveInfo)
    {
        foreach (var enemySet in waveInfo.EnemySetInfos)
        {
            var enemy = Instantiate(_enemyPrefab);
            enemy.Initialize(enemySet.Enemy);
            UnitManager.Instance.AddUnit(enemy, enemySet.TileIndex);
        }
    }

    /// <summary>
    /// (온라인) 서버 권위 스냅샷 유닛으로 적 스폰. 스탯=서버.
    /// ⚠️ 적 TemplateId=0(plan stages.json 미지정·graceful) → 비주얼 미해소(플레이스홀더), 크래시 없음.
    /// </summary>
    public void SpawnFromSnapshot(BattleUnitDto dto)
    {
        var enemy = Instantiate(_enemyPrefab);
        var characterData = dto.TemplateId > 0
            ? Client.Instance.GameData?.Characters.Get(dto.TemplateId)
            : null;

        var element = characterData != null ? characterData.Element : default;
        int maxTenacity = characterData != null ? characterData.BaseTenacity : 3;
        var data = new BattleUnitServerData(dto, element, maxTenacity);

        enemy.Initialize(data, characterData);
        UnitManager.Instance.AddUnit(enemy, dto.TileIndex);
    }
}
