using SM.Contracts.TurnRPG;
using UnityEngine;

public class AllySpawner : MonoBehaviour
{
    [SerializeField]
    private PlayerUnitController _allyPrefab;

    /// <summary>(레거시/오프라인) 클라 편성 캐시로 아군 스폰.</summary>
    public void SpawnAlly(AllySetInfo allySet)
    {
        var ally = Instantiate(_allyPrefab);
        var characterData = Client.Instance.GameData?.Characters.Get(allySet.Ally.UnitId);

        var equipped = EquipmentCache.Instance.GetEquipped(allySet.Ally.UnitId);
        var stats = EquipmentStatCalculator.Calculate(allySet.Ally, equipped);

        ally.Initialize(stats, characterData);
        UnitManager.Instance.AddUnit(ally, allySet.TileIndex);
    }

    /// <summary>(온라인) 서버 권위 스냅샷 유닛으로 아군 스폰. 스탯=서버, 비주얼=TemplateId→CharacterDatabase.</summary>
    public void SpawnFromSnapshot(BattleUnitDto dto)
    {
        var ally = Instantiate(_allyPrefab);
        // TODO(id 규약): CharacterData.CharacterId(string) == TemplateId.ToString() 가정(plan/turnrpg-server 확정).
        var characterData = Client.Instance.GameData?.Characters.Get(dto.TemplateId.ToString());

        var element = characterData != null ? characterData.Element : default;
        int maxTenacity = characterData != null ? characterData.BaseTenacity : 3;
        var data = new BattleUnitServerData(dto, element, maxTenacity);

        ally.Initialize(data, characterData);
        UnitManager.Instance.AddUnit(ally, dto.TileIndex);
    }
}
