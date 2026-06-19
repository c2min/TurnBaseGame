using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "TurnBase/Skill/SkillData")]
public class SkillData : ScriptableObject
{
    public string SkillId;
    public string SkillName;
    [TextArea(2, 4)] public string Description;
    public Sprite Icon;
    public ESkillType SkillType;
    [Min(0)] public int GaugeCharge;
    public ESkillRangeType RangeType;
    public EUnitTeam TargetTeam;

    [Header("Effects")]
    public SkillEffect[] Effects;
}
