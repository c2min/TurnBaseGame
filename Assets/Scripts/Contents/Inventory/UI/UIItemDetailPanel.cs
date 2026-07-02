using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItemDetailPanel : MonoBehaviour
{
    [Header("헤더")]
    [SerializeField]
    private Image _icon;
    [SerializeField]
    private TextMeshProUGUI _categoryTierText;
    [SerializeField]
    private TextMeshProUGUI _nameText;

    [Header("메인 스탯")]
    [SerializeField]
    private TextMeshProUGUI _mainStatTypeText;
    [SerializeField]
    private TextMeshProUGUI _mainStatValueText;

    [Header("서브 스탯")]
    [SerializeField]
    private UIStatRow[] _subStatRows;

    [Header("세트")]
    [SerializeField]
    private GameObject _setSection;
    [SerializeField]
    private Image _setIcon;
    [SerializeField]
    private TextMeshProUGUI _setNameText;
    [SerializeField]
    private TextMeshProUGUI _setEffectText;

    [Header("버튼")]
    [SerializeField] private Button _enhanceButton;

    public Action<ItemInstance> OnEnhanceClicked;

    private ItemInstance _current;

    private void Awake()
    {
        _enhanceButton.onClick.AddListener(() => OnEnhanceClicked?.Invoke(_current));
        gameObject.SetActive(false);
    }

    public void Show(ItemInstance item)
    {
        _current = item;
        gameObject.SetActive(true);

        var data = item.Data;

        _icon.sprite  = data.Icon;
        _icon.enabled = data.Icon != null;

        string tierLabel = data.Tier > 0 ? $"  T{data.Tier}" : $"  {data.Rarity.ToShortLabel()}";
        _categoryTierText.text = $"{data.Category.ToKorean()}{tierLabel}";
        _nameText.text         = data.ItemName;

        _mainStatTypeText.text  = data.MainStat.StatType.ToKorean();
        _mainStatValueText.text = data.MainStat.FormatValue();

        for (int i = 0; i < _subStatRows.Length; i++)
        {
            if (i < data.SubStats.Length)
                _subStatRows[i].Bind(data.SubStats[i]);
            else
                _subStatRows[i].Hide();
        }

        bool hasSet = !string.IsNullOrEmpty(data.SetName);
        _setSection.SetActive(hasSet);
        if (hasSet)
        {
            _setIcon.sprite    = data.SetIcon;
            _setIcon.enabled   = data.SetIcon != null;
            _setNameText.text  = $"{data.SetName} ({item.CurrentSetCount}/{data.SetPieceCount})";
            _setEffectText.text = data.SetEffect;
        }
    }

    public void Hide()
    {
        _current = null;
        gameObject.SetActive(false);
    }
}
