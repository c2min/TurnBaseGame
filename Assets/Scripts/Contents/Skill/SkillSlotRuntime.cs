using UnityEngine;

public class SkillSlotRuntime : ISkill
{
    public SkillData Data { get; }
    public UltimateGauge UltimateGauge { get; }

    private readonly float _chargeEfficiency;
    private bool IsUltimate => Data.SkillType == ESkillType.Ultimate;

    // ⚠️ 스킬포인트(SP) 게이팅 제거(HSR 잔재, 2026-06-20 ADR-005). 궁극기만 게이지 게이팅, 그 외 항상 사용 가능.
    public bool IsReady => IsUltimate
        ? (UltimateGauge?.IsFull ?? false)
        : true;

    public SkillSlotRuntime(SkillData data, UltimateGauge ultimateGauge = null, float chargeEfficiency = 1f)
    {
        Data = data;
        UltimateGauge = ultimateGauge;
        _chargeEfficiency = chargeEfficiency;
    }

    public void Use()
    {
        if (Data.GaugeCharge > 0)
        {
            int charge = Mathf.RoundToInt(Data.GaugeCharge * _chargeEfficiency);
            UltimateGauge?.Charge(charge);
        }

        if (IsUltimate)
            UltimateGauge?.Use();
    }
}
