using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "TurnBase/Skill/SkillData")]
public class SkillData : ScriptableObject
{
    // 콘텐츠 정본 id = int(ADR-006). 계약 BattleSkillUseRequestPacket.SkillId(int)와 직매핑.
    public int SkillId;
    public string SkillName;
    [TextArea(2, 4)] public string Description;
    public Sprite Icon;
    public ESkillType SkillType;
    public ESkillRangeType RangeType;
    public EUnitTeam TargetTeam;

    [Header("Effects")]
    public SkillEffect[] Effects;
}
