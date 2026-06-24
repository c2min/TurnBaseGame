using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "TurnBase/Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    // 콘텐츠 정본 id = int(ADR-006, plan+turnrpg-server 확정). 계약 CharacterInfo/BattleUnitDto.TemplateId와 직매핑.
    public int TemplateId;
    public string CharacterName;
    public Sprite Portrait;
    public GameObject SpinePrefab;

    [Header("Element")]
    public EElement Element;

    [Header("Base Stats")]
    public int BaseHp = 100;
    public int BaseAttack = 10;
    public int BaseDefense = 5;
    public int BaseSpeed = 10;
    public int BaseTenacity = 3;

    [Header("Skills")]
    public SkillData PassiveSkill;
    public SkillData NormalAttack;
    public SkillData Skill;
    public SkillData UltimateSkill;

    public SkillData[] GetActiveSkills()
        => new[] { NormalAttack, Skill, UltimateSkill };
}
