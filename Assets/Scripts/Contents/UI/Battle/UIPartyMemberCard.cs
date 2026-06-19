using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPartyMemberCard : MonoBehaviour
{
    [SerializeField] private Image _portrait;
    [SerializeField] private Image _gaugeBar;
    [SerializeField] private Image _turnIndicator;
    [SerializeField] private Image _elementIcon;
    [SerializeField] private ElementIconSet _elementIconSet;
    [SerializeField] private UIStatusEffectIcon _statusIconPrefab;
    [SerializeField] private Transform _statusIconContainer;

    public UnitController Unit => _unit;

    private UnitController _unit;
    private readonly List<UIStatusEffectIcon> _statusIcons = new();

    public void SetTurnActive(bool active)
    {
        if (_turnIndicator != null)
            _turnIndicator.enabled = active;
    }

    public void Bind(UnitController unit)
    {
        Unbind();
        _unit = unit;
        SetTurnActive(false);

        if (_unit.CharacterData != null)
            _portrait.sprite = _unit.CharacterData.Portrait;

        _elementIconSet?.Apply(_elementIcon, _unit.Element);

        _unit.StatusEffects.OnEffectsChanged += RefreshStatusIcons;

        RefreshStatusIcons();
    }

    private void RefreshStatusIcons()
    {
        var effects = _unit?.StatusEffects.ActiveEffects;
        int count = effects?.Count ?? 0;

        EnsureIconCount(count);

        for (int i = 0; i < count; i++)
        {
            _statusIcons[i].Bind(effects[i]);
        }

        for (int i = count; i < _statusIcons.Count; i++)
        {
            _statusIcons[i].Hide();
        }
    }

    private void EnsureIconCount(int required)
    {
        while (_statusIcons.Count < required)
        {
            _statusIcons.Add(Instantiate(_statusIconPrefab, _statusIconContainer));
        }
    }

    private void Unbind()
    {
        if (_unit == null) return;

        _unit.StatusEffects.OnEffectsChanged -= RefreshStatusIcons;
        _unit = null;
    }

    private void OnDestroy() => Unbind();
}
