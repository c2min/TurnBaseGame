public class SkillSlotRuntime : ISkill
{
    public SkillData Data { get; }

    // ⚠️ 궁극기 에너지 게이지·SP(HSR 잔재) 제거(2026-06-20 ADR-005).
    //    스킬 사용 게이팅 없음 — 사용 검증은 서버 권위(라운드 단위). 클라는 표시·송신만.
    public bool IsReady => true;

    public SkillSlotRuntime(SkillData data)
    {
        Data = data;
    }

    public void Use()
    {
        // 자원 게이지 없음 — 스킬 효과 적용은 서버 권위(OnSkillResult). 클라 Use=no-op.
    }
}
