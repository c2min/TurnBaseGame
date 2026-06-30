using SMDevLibrary.Sound;
using SMDevLibrary.VFX;
using UnityEngine;

/// <summary>
/// 스킬 효과 1개 단위 — ScriptableObject로 직렬화해 SkillData.Effects[]에 조합
/// </summary>
public abstract class SkillEffect : ScriptableObject
{
    public EEffectTarget Target;

    [Header("VFX / SFX")]
    [Tooltip("효과 적용 전 대상 위치에서 재생할 VFX (없으면 생략)")]
    public VFXClip HitVFX;

    [Tooltip("효과 적용 시 재생할 SFX (없으면 생략)")]
    public SFXClip HitSFX;

    public abstract void Apply(ICombatant caster, ICombatant target, ESkillType skillType);

    // 실제 상태를 변경하지 않고 적용 후 예상 HP를 반환 (HP에 영향 없는 효과는 그대로 반환)
    public virtual int SimulateHp(ICombatant caster, ICombatant target, int currentProjectedHp)
        => currentProjectedHp;
}
