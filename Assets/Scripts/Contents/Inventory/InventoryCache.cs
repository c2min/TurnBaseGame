using System.Collections.Generic;
using SM.Contracts.TurnRPG;
using SMDevLibrary.Generics;

// 계정 보유 장비 캐시 — 계약 EquipmentDto(서버 권위) 소비. 장착 상태(EquippedByCharacterId)도 여기가 정본.
public class InventoryCache : LazySingleton<InventoryCache>
{
    private readonly List<ItemInstance> _items = new();
    public IReadOnlyList<ItemInstance> Items => _items;
    public int MaxCount { get; private set; } = 1000;

    /// <summary>inventory.resp{Equipments} 전체 반영. Data=TemplateId로 ItemDatabase 해석.</summary>
    public void SetFromDtos(IEnumerable<EquipmentDto> dtos, ItemDatabase database, int maxCount = 1000)
    {
        _items.Clear();
        MaxCount = maxCount;
        if (dtos == null) return;

        foreach (var dto in dtos)
            _items.Add(FromDto(dto, database));
    }

    /// <summary>equip/unequip.resp의 Affected(장착분+자동해제분) upsert.</summary>
    public void ApplyAffected(IEnumerable<EquipmentDto> affected, ItemDatabase database)
    {
        if (affected == null) return;

        foreach (var dto in affected)
        {
            var existing = Find(dto.EquipmentId);
            if (existing != null)
            {
                existing.EnchantLevel          = dto.EnhanceLevel;
                existing.EquippedByCharacterId = dto.EquippedByCharacterId;
            }
            else
            {
                _items.Add(FromDto(dto, database));
            }
        }
    }

    private static ItemInstance FromDto(EquipmentDto dto, ItemDatabase database)
        => new ItemInstance
        {
            EquipmentId           = dto.EquipmentId,
            Data                  = database?.Get(dto.TemplateId),
            EnchantLevel          = dto.EnhanceLevel,
            EquippedByCharacterId = dto.EquippedByCharacterId,
        };

    public ItemInstance Find(long equipmentId)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].EquipmentId == equipmentId)
                return _items[i];
        }
        return null;
    }

    // 클라 로컬 표시 상태(판매/잠금/즐겨찾기 계약 미존재 — 후속)
    public void Remove(long equipmentId)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].EquipmentId == equipmentId)
            {
                _items.RemoveAt(i);
                return;
            }
        }
    }

    public void SetLock(long equipmentId, bool locked)
    {
        var item = Find(equipmentId);
        if (item != null) item.IsLocked = locked;
    }

    public void SetFavorite(long equipmentId, bool favorite)
    {
        var item = Find(equipmentId);
        if (item != null) item.IsFavorite = favorite;
    }
}
