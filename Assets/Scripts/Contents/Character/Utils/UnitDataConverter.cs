using System;
using SM.Contracts.TurnRPG;

public interface IUnitServerData
{
    string UnitId { get; }
    int Hp { get; }
    int Speed { get; }
    int AttackPower { get; }
    int Defense { get; }
    EElement Element { get; }
    int MaxTenacity { get; }
}

[Serializable]
public class AllyInfo : IUnitServerData
{
    public string UnitId { get; set; }
    /// <summary>콘텐츠 템플릿 id(int·ADR-006) — 비주얼/정의 해소 키(CharacterDatabase). UnitId(인스턴스)와 별개.</summary>
    public int TemplateId { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int Hp { get; set; }
    public int Speed { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public EElement Element { get; set; }
    public int MaxTenacity { get; set; } = 3;
}

[Serializable]
public class EnemyInfo : IUnitServerData
{
    public string UnitId { get; set; }
    public string Name { get; set; }
    public int Hp { get; set; }
    public int Speed { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public EElement Element { get; set; }
    public int MaxTenacity { get; set; } = 3;
}

/// <summary>
/// 계약 BattleUnitDto(서버 권위 스냅샷 유닛)를 클라 유닛 초기화 인터페이스로 변환.
/// 스탯·UnitId=서버 권위. Element/MaxTenacity는 와이어 미운반 → 콘텐츠(CharacterData)에서 주입(미해소=기본값).
/// </summary>
public class BattleUnitServerData : IUnitServerData
{
    public string UnitId { get; }
    public int Hp { get; }
    public int Speed { get; }
    public int AttackPower { get; }
    public int Defense { get; }
    public EElement Element { get; }
    public int MaxTenacity { get; }

    public BattleUnitServerData(BattleUnitDto dto, EElement element, int maxTenacity)
    {
        UnitId = dto.UnitId;
        Hp = dto.Hp;
        Speed = dto.Speed;
        AttackPower = dto.AttackPower;
        Defense = dto.Defense;
        Element = element;
        MaxTenacity = maxTenacity;
    }
}