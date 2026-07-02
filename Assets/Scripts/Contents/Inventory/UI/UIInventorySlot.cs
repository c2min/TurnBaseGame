using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SMDevLibrary.UI.Layout;

public class UIInventorySlot : MonoBehaviour, IRecycleElement<ItemInstance>
{
    [SerializeField]
    private Button _button;
    
    [SerializeField]
    private Image _background;
    
    [SerializeField]
    private Image _icon;
    
    [SerializeField]
    private Image _selectedBorder;
    
    [SerializeField]
    private Image _lockIcon;
    
    [SerializeField]
    private Image _favoriteIcon;
    
    [SerializeField]
    private Image _setIcon;
    
    [SerializeField]
    private TextMeshProUGUI _tierText;
    
    [SerializeField]
    private TextMeshProUGUI _enchantText;

    private static readonly Color[] RarityColors =
    {
        new Color(0.55f, 0.55f, 0.55f), // Normal  - 회색
        new Color(0.20f, 0.55f, 1.00f), // Magic   - 파랑
        new Color(0.55f, 0.20f, 1.00f), // Rare    - 보라
        new Color(0.90f, 0.60f, 0.10f), // Epic    - 금색
        new Color(1.00f, 0.30f, 0.20f), // Legend  - 빨강
    };

    public ItemInstance Item { get; private set; }

    private Action<ItemInstance> _onClick;

    private void Awake()
    {
        _button.onClick.AddListener(() => _onClick?.Invoke(Item));
    }

    public void SetOnClicked(Action<ItemInstance> onClick) => _onClick = onClick;

    public void Bind(ItemInstance item)
    {
        Item = item;

        _icon.sprite  = item.Data.Icon;
        _icon.enabled = item.Data.Icon != null;

        _tierText.text = item.Data.Tier > 0 ? $"T{item.Data.Tier}" : item.Data.Rarity.ToShortLabel();

        bool hasEnchant = item.EnchantLevel > 0;
        _enchantText.gameObject.SetActive(hasEnchant);
        if (hasEnchant)
        {
            _enchantText.text = $"+{item.EnchantLevel}";
        }

        _lockIcon.gameObject.SetActive(item.IsLocked);
        _favoriteIcon.gameObject.SetActive(item.IsFavorite);

        bool hasSet = item.Data.SetIcon != null;
        _setIcon.gameObject.SetActive(hasSet);
        if (hasSet)
        {
            _setIcon.sprite = item.Data.SetIcon;
        }

        int rarityIdx = Mathf.Clamp((int)item.Data.Rarity, 0, RarityColors.Length - 1);
        _background.color = RarityColors[rarityIdx];

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (_selectedBorder != null)
            _selectedBorder.enabled = selected;
    }
}
