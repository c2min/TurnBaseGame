using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharEquipPanel : MonoBehaviour
{
    [Header("장비 합산 스탯")]
    [SerializeField]
    private TextMeshProUGUI _hpText;
    [SerializeField]
    private TextMeshProUGUI _attackText;
    [SerializeField]
    private TextMeshProUGUI _defenseText;
    [SerializeField]
    private TextMeshProUGUI _speedText;
    [SerializeField]
    private TextMeshProUGUI _critRateText;
    [SerializeField]
    private TextMeshProUGUI _critDamageText;

    [Header("장비 슬롯 — Weapon/Gloves/Armor/Boots/Ring/Necklace 순서")]
    [SerializeField]
    private UICharEquipSlot[] _equipSlots;

    [Header("버튼")]
    [SerializeField]
    private Button _swapButton;
    [SerializeField]
    private Button _enhanceButton;

    // 슬롯 클릭 시 외부에서 처리 (장비 교체 팝업 연결용)
    public Action<EItemCategory, ItemInstance> OnSlotClicked;
    public Action OnSwap;
    public Action OnEnhance;

    private static readonly EItemCategory[] SlotOrder =
    {
        EItemCategory.Weapon,
        EItemCategory.Gloves,
        EItemCategory.Armor,
        EItemCategory.Boots,
        EItemCategory.Ring,
        EItemCategory.Necklace,
    };

    private void Awake()
    {
        _swapButton.onClick.AddListener(() => OnSwap?.Invoke());
        _enhanceButton.onClick.AddListener(() => OnEnhance?.Invoke());
    }

    // AllyInfo.UnitId = CharacterId.ToString()(Client.ToAllyInfo) → 장착 characterId=long(ADR-006).
    private static long ParseCharacterId(AllyInfo ally)
        => ally != null && long.TryParse(ally.UnitId, out var id) ? id : 0;

    public void Bind(AllyInfo ally)
    {
        // 스탯=서버 전산출(CharacterInfo→AllyInfo, 장비 반영 최종값). 클라 계산(EquipmentStatCalculator) supersede.
        _hpText.text         = ally.Hp.ToString();
        _attackText.text     = ally.AttackPower.ToString();
        _defenseText.text    = ally.Defense.ToString();
        _speedText.text      = ally.Speed.ToString();
        _critRateText.text   = "-"; // 서버 CharacterInfo 미운반(후속: crit 필드 요청 시)
        _critDamageText.text = "-";

        long charId = ParseCharacterId(ally);
        var slots = EquipmentCache.Instance.GetSlots(charId);

        for (int i = 0; i < _equipSlots.Length; i++)
        {
            var cat = SlotOrder[i];
            slots.TryGetValue(cat, out var item);

            _equipSlots[i].OnClicked = OnSlotClicked;
            _equipSlots[i].Bind(cat, item);
        }
    }

    public void Clear()
    {
        _hpText.text         = string.Empty;
        _attackText.text     = string.Empty;
        _defenseText.text    = string.Empty;
        _speedText.text      = string.Empty;
        _critRateText.text   = string.Empty;
        _critDamageText.text = string.Empty;

        foreach (var slot in _equipSlots)
        {
            slot.Clear();
        }
    }
}
