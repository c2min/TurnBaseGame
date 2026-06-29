using System.Collections.Generic;
using UnityEngine;

public static class EquipmentStatCalculator
{
    private struct StatAccumulator
    {
        public int FlatHp, FlatAttack, FlatDefense, FlatSpeed;
        public float PercentHp, PercentAttack, PercentDefense;
        public float CritRate, CritDamage, EffectHit, EffectResist;
    }

    public static ComputedStats Calculate(IUnitServerData baseData, IEnumerable<ItemInstance> items)
    {
        var accumulator = default(StatAccumulator);

        foreach (var item in items)
        {
            Accumulate(ref accumulator, item.Data.MainStat.StatType, item.Data.MainStat.Value * (1f + item.EnchantLevel * 0.1f));
            foreach (var sub in item.Data.SubStats)
            {
                Accumulate(ref accumulator, sub.StatType, sub.Value);
            }
        }

        return new ComputedStats
        {
            UnitId       = baseData.UnitId,
            Hp           = Mathf.RoundToInt(baseData.Hp * (1f + accumulator.PercentHp) + accumulator.FlatHp),
            AttackPower  = Mathf.RoundToInt(baseData.AttackPower * (1f + accumulator.PercentAttack) + accumulator.FlatAttack),
            Defense      = Mathf.RoundToInt(baseData.Defense * (1f + accumulator.PercentDefense) + accumulator.FlatDefense),
            Speed        = baseData.Speed + accumulator.FlatSpeed,
            CritRate     = accumulator.CritRate,
            CritDamage   = accumulator.CritDamage,
            EffectHit    = accumulator.EffectHit,
            EffectResist = accumulator.EffectResist,
            Element      = baseData.Element,
            MaxTenacity  = baseData.MaxTenacity,
        };
    }

    private static void Accumulate(ref StatAccumulator accumulator, EItemStat stat, float value)
    {
        switch (stat)
        {
            case EItemStat.Hp:             accumulator.FlatHp          += (int)value; break;
            case EItemStat.HpPercent:      accumulator.PercentHp       += value / 100f; break;
            case EItemStat.Attack:         accumulator.FlatAttack      += (int)value; break;
            case EItemStat.AttackPercent:  accumulator.PercentAttack   += value / 100f; break;
            case EItemStat.Defense:        accumulator.FlatDefense     += (int)value; break;
            case EItemStat.DefensePercent: accumulator.PercentDefense  += value / 100f; break;
            case EItemStat.Speed:          accumulator.FlatSpeed       += (int)value; break;
            case EItemStat.CritRate:       accumulator.CritRate        += value / 100f; break;
            case EItemStat.CritDamage:     accumulator.CritDamage      += value / 100f; break;
            case EItemStat.EffectHit:      accumulator.EffectHit       += value / 100f; break;
            case EItemStat.EffectResist:   accumulator.EffectResist    += value / 100f; break;
        }
    }
}
