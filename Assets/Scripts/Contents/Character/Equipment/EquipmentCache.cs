using System.Collections.Generic;
using SMDevLibrary.Generics;

// 캐릭터별 장착 조회 — 서버 권위. 장착 상태 정본=InventoryCache(ItemInstance.EquippedByCharacterId).
// 여기는 파생 조회만(별도 상태 보유 X·격하). characterId=long(CharacterInfo.CharacterId·ADR-006).
public class EquipmentCache : LazySingleton<EquipmentCache>
{
    public IEnumerable<ItemInstance> GetEquipped(long characterId)
    {
        foreach (var item in InventoryCache.Instance.Items)
        {
            if (item.EquippedByCharacterId == characterId)
                yield return item;
        }
    }

    /// <summary>카테고리당 1슬롯(서버 권위) 장착 맵.</summary>
    public Dictionary<EItemCategory, ItemInstance> GetSlots(long characterId)
    {
        var slots = new Dictionary<EItemCategory, ItemInstance>();
        foreach (var item in GetEquipped(characterId))
        {
            slots[item.Category] = item;
        }
        return slots;
    }
}
