using System;

// 장비 인스턴스 — 계약 EquipmentDto(서버 권위) 소비 모델.
// EquipmentId(long)=서버 발급 인스턴스 id / Data=TemplateId(int)로 ItemDatabase 해석 / EnchantLevel=강화 / EquippedByCharacterId=장착 캐릭(null=미장착).
[Serializable]
public class ItemInstance
{
    public long EquipmentId;
    public ItemData Data;
    public int EnchantLevel;
    public long? EquippedByCharacterId;

    // 클라 로컬(장비 와이어 무운반 — 판매/즐겨찾기는 별 계약·미존재)
    public bool IsLocked;
    public bool IsFavorite;
    public int CurrentSetCount;

    public bool IsEquipped => EquippedByCharacterId.HasValue;
    public EItemCategory Category => Data != null ? Data.Category : default;
}
