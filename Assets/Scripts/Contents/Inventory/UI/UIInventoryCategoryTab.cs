using System;
using UnityEngine;
using UnityEngine.UI;

public class UIInventoryCategoryTab : MonoBehaviour
{
    [SerializeField]
    private Button _button;
    
    [SerializeField]
    private Image _tabImage;

    public EItemCategory Category { get; private set; }

    private Sprite _normalSprite;
    private Sprite _selectedSprite;
    private Action<EItemCategory> _onSelected;

    public void Initialize(EItemCategory category, Sprite normalSprite, Sprite selectedSprite, Action<EItemCategory> onSelected)
    {
        Category        = category;
        _normalSprite   = normalSprite;
        _selectedSprite = selectedSprite;
        _onSelected     = onSelected;
        _button.onClick.AddListener(() => _onSelected?.Invoke(Category));
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        _tabImage.sprite = selected ? _selectedSprite : _normalSprite;
    }
}
