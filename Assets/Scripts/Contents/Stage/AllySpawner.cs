using SM.Contracts.TurnRPG;
using UnityEngine;

public class AllySpawner : MonoBehaviour
{
    [SerializeField]
    private PlayerUnitController _allyPrefab;

    /// <summary>서버 권위 스냅샷 유닛으로 아군 스폰. 스탯=서버, 비주얼=TemplateId→CharacterDatabase.</summary>
    public void SpawnFromSnapshot(BattleUnitDto dto)
    {
        var ally = Instantiate(_allyPrefab);
        var characterData = Client.Instance.GameData?.Characters.Get(dto.TemplateId);

        var element = characterData != null ? characterData.Element : default;
        int maxTenacity = characterData != null ? characterData.BaseTenacity : 3;
        var data = new BattleUnitServerData(dto, element, maxTenacity);

        ally.Initialize(data, characterData);
        UnitManager.Instance.AddUnit(ally, dto.TileIndex);
    }
}
