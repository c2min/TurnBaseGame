public enum EItemCategory
{
    All,
    Weapon,
    Gloves,
    Armor,
    Necklace,   // 구: Helmet
    Ring,
    Boots,
}

public enum EItemRarity
{
    Normal,
    Magic,
    Rare,
    Epic,
    Legend,
}

public enum EItemStat
{
    Attack,
    AttackPercent,
    Hp,
    HpPercent,
    Defense,
    DefensePercent,
    Speed,
    CritRate,
    CritDamage,
    EffectHit,
    EffectResist,
}

public enum EItemSortType
{
    Tier,
    Rarity,
    EnchantLevel,
}
