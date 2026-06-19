using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public abstract class UnitController : MonoBehaviour, ITurnActor, ICombatant, IPlaceable, IStatusReceiver, IElemental, IHasTenacity
{
    [SerializeField]
    protected SpineCharacterController _spine;

    public string UnitId { get; private set; }
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsAlive => CurrentHp > 0;
    public abstract EUnitTeam Team { get; }

    public bool CanAct => IsAlive && _statusEffects.CanAct;
    public int TileIndex { get; private set; } = -1;

    public float CritRate { get; private set; }
    public float CritDamage { get; private set; }
    public float EffectHit { get; private set; }
    public float EffectResist { get; private set; }
    public EElement Element { get; private set; }
    public Tenacity Tenacity { get; private set; }

    public event Action<ICombatant> OnDied;
    public event Action<ICombatant, int> OnDamaged;
    public event Action<ICombatant, int> OnHealed;

    protected StatusEffectHandler _statusEffects = new StatusEffectHandler();
    private readonly Dictionary<EStat, int> _stats = new Dictionary<EStat, int>();

    public CharacterData CharacterData { get; private set; }
    public IReadOnlyList<SkillSlotRuntime> ActiveSkills { get; private set; }
    public StatusEffectHandler StatusEffects => _statusEffects;

    public virtual void Initialize(IUnitServerData data, CharacterData characterData = null)
    {
        UnitId = data.UnitId;
        MaxHp = data.Hp;
        CurrentHp = MaxHp;
        _stats[EStat.Speed] = data.Speed;
        _stats[EStat.Attack] = data.AttackPower;
        _stats[EStat.Defense] = data.Defense;

        Element  = data.Element;
        Tenacity = new Tenacity(data.MaxTenacity);
        Tenacity.OnDepleted += OnTenacityDepleted;

        if (data is ComputedStats cs)
        {
            CritRate     = cs.CritRate;
            CritDamage   = cs.CritDamage;
            EffectHit    = cs.EffectHit;
            EffectResist = cs.EffectResist;
        }

        _statusEffects.Initialize(this);

        CharacterData = characterData;

        if (characterData != null)
        {
            var rawSkills = characterData.GetActiveSkills();
            var skills = new List<SkillSlotRuntime>(rawSkills.Length);
            for (int i = 0; i < rawSkills.Length; i++)
            {
                if (rawSkills[i] != null)
                    skills.Add(new SkillSlotRuntime(rawSkills[i]));
            }

            ActiveSkills = skills;
        }
        else
        {
            ActiveSkills = new List<SkillSlotRuntime>();
        }

        PlaySpawnSequenceAsync().Forget();
    }

    private async UniTaskVoid PlaySpawnSequenceAsync()
    {
        if (_spine == null)
            return;

        try
        {
            await _spine.PlayAndWait(UnitAnimationState.Appear, ct: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (this == false)
            return;

        _spine.PlayAnimation(UnitAnimationState.Idle);
    }

    public int Speed => GetStat(EStat.Speed);

    public virtual void TakeDamage(int amount)
    {
        int afterShield = _statusEffects.ProcessDamage(amount);
        int damage = Mathf.Max(0, afterShield - GetStat(EStat.Defense));
        CurrentHp = Mathf.Max(0, CurrentHp - damage);

        OnDamaged?.Invoke(this, damage);

        if (!IsAlive)
        {
            OnDied?.Invoke(this);
            _spine?.ClearLoopTarget();
            _spine?.PlayAnimation(UnitAnimationState.Dead);
        }
        else
        {
            _spine?.PlayAnimation(UnitAnimationState.Hit);
        }
    }

    public virtual void Heal(int amount)
    {
        int healed = Mathf.Min(amount, MaxHp - CurrentHp);
        CurrentHp += healed;
        OnHealed?.Invoke(this, healed);
    }

    public void ApplyStatus(IStatusEffect effect)
    {
        _statusEffects.Apply(effect);
    }

    public void ModifyStat(EStat stat, int delta)
        => _stats[stat] = Mathf.Max(0, _stats[stat] + delta);

    public int GetStat(EStat stat) => _stats.GetValueOrDefault(stat, 0);

    private void OnTenacityDepleted()
    {
        UnitManager.Instance.TurnOrder.ResetGauge(this);
    }

    public virtual void OnTurnStart()
    {
        Tenacity?.Restore();
        _statusEffects.OnTurnStart();
        _spine?.PlayAnimation(UnitAnimationState.Idle);
    }

    public virtual void OnTurnEnd()
    {
        _statusEffects.OnTurnEnd();
    }

    public virtual async UniTask PlayActionAnimationAsync(string stateName = UnitAnimationState.Attack, CancellationToken ct = default)
    {
        if (_spine == null)
            return;
            
        await _spine.PlayAndWait(stateName, ct: ct);
    }

    public void OnPlaced(int tileIndex) => TileIndex = tileIndex;
    public void OnRemoved() => TileIndex = -1;
}
