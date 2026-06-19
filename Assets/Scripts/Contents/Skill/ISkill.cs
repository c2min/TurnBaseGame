public interface ISkill
{
    SkillData Data { get; }
    bool IsReady { get; }
    void Use();
}
