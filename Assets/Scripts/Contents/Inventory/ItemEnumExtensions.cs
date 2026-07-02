public static class ItemEnumExtensions
{
    public static string ToKorean(this EItemCategory category) => category switch
    {
        EItemCategory.All    => "전체",
        EItemCategory.Weapon => "무기",
        EItemCategory.Gloves => "장갑",
        EItemCategory.Armor  => "방어구",
        EItemCategory.Necklace => "목걸이",
        EItemCategory.Ring   => "반지",
        EItemCategory.Boots  => "신발",
        _                    => category.ToString(),
    };

    public static string ToKorean(this EItemStat stat) => stat switch
    {
        EItemStat.Attack         => "공격력",
        EItemStat.AttackPercent  => "공격력",
        EItemStat.Hp             => "생명력",
        EItemStat.HpPercent      => "생명력",
        EItemStat.Defense        => "방어력",
        EItemStat.DefensePercent => "방어력",
        EItemStat.Speed          => "속도",
        EItemStat.CritRate       => "치명타 확률",
        EItemStat.CritDamage     => "치명타 피해",
        EItemStat.EffectHit      => "효과 적중",
        EItemStat.EffectResist   => "효과 저항",
        _                        => stat.ToString(),
    };

    public static string FormatValue(this StatEntry entry)
    {
        return entry.IsPercent ? $"{entry.Value:F2}%" : $"{(int)entry.Value}";
    }

    public static string ToShortLabel(this EItemRarity rarity) => rarity switch
    {
        EItemRarity.Normal => "N",
        EItemRarity.Magic  => "M",
        EItemRarity.Rare   => "R",
        EItemRarity.Epic   => "E",
        EItemRarity.Legend => "L",
        _                  => "?",
    };
}
