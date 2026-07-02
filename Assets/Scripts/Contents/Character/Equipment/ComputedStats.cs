public class ComputedStats : IUnitServerData
{
    public string UnitId { get; set; }
    public int Hp { get; set; }
    public int Speed { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public float CritRate { get; set; }
    public float CritDamage { get; set; }
    public float EffectHit { get; set; }
    public float EffectResist { get; set; }
    public EElement Element { get; set; }
    public int MaxTenacity { get; set; }
}
