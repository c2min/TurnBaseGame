using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharEquipSlot : MonoBehaviour
{
    private static readonly Color EmptyColor = new Color(0.25f, 0.25f, 0.25f);
    private static readonly Color[] RarityColors =
    {
        new Color(0.55f, 0.55f, 0.55f), // Normal
        new Color(0.20f, 0.55f, 1.00f), // Magic
        new Color(0.55f, 0.20f, 1.00f), // Rare
        new Color(0.90f, 0.60f, 0.10f), // Epic
        new Color(1.00f, 0.30f, 0.20f), // Legend
    };

    [SerializeField]
    private Button _button;
    [SerializeField]
    private Image _background;
    [SerializeField]
    private Image _icon;
    [SerializeField]
    private TextMeshProUGUI _enchantText;

    private EItemCategory _category;
    private ItemInstance _item;

    public Action<EItemCategory, ItemInstance> OnClicked;

    private void Awake()
    {
        _button.onClick.AddListener(() => OnClicked?.Invoke(_category, _item));
    }

    public void Bind(EItemCategory category, ItemInstance item)
    {
        _category = category;
        _item     = item;

        if (item == null)
        {
            _icon.enabled       = false;
            _enchantText.text   = string.Empty;
            _background.color   = EmptyColor;
            return;
        }

        _icon.sprite  = item.Data.Icon;
        _icon.enabled = item.Data.Icon != null;

        _enchantText.text = item.EnchantLevel > 0 ? $"+{item.EnchantLevel}" : string.Empty;

        int idx = Mathf.Clamp((int)item.Data.Rarity, 0, RarityColors.Length - 1);
        _background.color = RarityColors[idx];
    }

    public void Clear()
    {
        _item             = null;
        _icon.enabled     = false;
        _enchantText.text = string.Empty;
        _background.color = EmptyColor;
    }
}
