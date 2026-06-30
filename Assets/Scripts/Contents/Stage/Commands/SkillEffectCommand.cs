using Cysharp.Threading.Tasks;
using SMDevLibrary.Command;
using SMDevLibrary.Sound;
using System.Threading;
using UnityEngine;

/// <summary>
/// SkillEffect 하나를 단일 대상에게 적용하는 원자 커맨드.
/// HitVFX가 설정되어 있으면 효과 적용 전 대상 위치에서 재생하고 완료까지 대기합니다.
/// </summary>
public class SkillEffectCommand : ICommand
{
    private readonly SkillEffect _effect;
    private readonly ICombatant  _caster;
    private readonly ICombatant  _target;
    private readonly ESkillType  _skillType;

    public SkillEffectCommand(SkillEffect effect, ICombatant caster, ICombatant target, ESkillType skillType)
    {
        _effect    = effect;
        _caster    = caster;
        _target    = target;
        _skillType = skillType;
    }

    public async UniTask ExecuteAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested || !_target.IsAlive)
            return;

        Vector3 position = (_target is MonoBehaviour mb) ? mb.transform.position : Vector3.zero;

        // SFX는 논블로킹 — VFX와 동시에 시작하고 완료를 기다리지 않음
        if (_effect.HitSFX != null)
            SFXManager.Instance?.Play(_effect.HitSFX, position);

        // VFX는 완료까지 대기 후 효과 적용
        if (_effect.HitVFX != null)
            await new PlayVFXCommand(_effect.HitVFX, position).ExecuteAsync(ct);

        if (!ct.IsCancellationRequested && _target.IsAlive)
            _effect.Apply(_caster, _target, _skillType);
    }
}
