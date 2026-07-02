using System;
using UnityEngine;

[Serializable]
public struct StatEntry
{
    public EItemStat StatType;
    public float Value;
    public bool IsPercent;
}

[CreateAssetMenu(fileName = "ItemData", menuName = "TurnBase/Item/ItemData")]
public class ItemData : ScriptableObject
{
    // 콘텐츠 템플릿 id(int·ADR-006) — EquipmentDto.TemplateId 해석 키(plan equipment.json int-키 미러).
    public int TemplateId;
    public string ItemName;
    public Sprite Icon;
    public EItemCategory Category;
    public EItemRarity Rarity;
    public int Tier;

    [Header("Stats")]
    public StatEntry MainStat;
    public StatEntry[] SubStats;

    [Header("Set")]
    public Sprite SetIcon;
    public string SetName;
    public int SetPieceCount;
    [TextArea(1, 3)] public string SetEffect;
}
