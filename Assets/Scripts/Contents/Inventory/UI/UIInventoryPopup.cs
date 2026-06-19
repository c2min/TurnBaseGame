using SMDevLibrary.Managers;
// 로컬 EItemCategory 등과 충돌 회피 → 필요한 Core 타입만 타깃 별칭.
using ENetworkStatusCode = SM.Contracts.Core.ENetworkStatusCode;
using SMDevLibrary.Network.Utility;
using SMDevLibrary.UI.Popup;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[PopupPath("Inventory")]
public class UIInventoryPopup : BasePopup
{
    [SerializeField]
    private Button _closeButton;

    [Header("카테고리 탭 — EItemCategory 순서대로 연결")]
    [SerializeField]
    private UIInventoryCategoryTab[] _categoryTabs;
    [SerializeField]
    private Sprite _tabNormalSprite;
    
    [SerializeField]
    private Sprite _tabSelectedSprite;

    [Header("정렬")]
    [SerializeField]
    private Button _sortButton;
    
    [SerializeField]
    private TextMeshProUGUI _sortLabel;

    [Header("그리드")]
    [SerializeField]
    private UIInventoryScrollView _scrollView;

    [Header("상세 패널")]
    [SerializeField]
    private UIItemDetailPanel _detailPanel;

    [Header("하단 바")]
    [SerializeField]
    private Button _sellButton;
    [SerializeField]
    private TextMeshProUGUI _countText;

    private readonly List<ItemInstance> _filtered = new();
    private EItemCategory _category = EItemCategory.All;
    private EItemSortType _sortType = EItemSortType.Tier;
    private ItemInstance _selectedItem;

    private static readonly EItemSortType[] SortCycle =
    {
        EItemSortType.Tier,
        EItemSortType.Rarity,
        EItemSortType.EnchantLevel,
    };
    private static readonly string[] SortLabels = { "장비 티어", "희귀도", "강화 수치" };

    private int _sortIndex;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < _categoryTabs.Length; i++)
        {
            var category = (EItemCategory)i;
            _categoryTabs[i].Initialize(category, _tabNormalSprite, _tabSelectedSprite, OnCategorySelected);
        }

        _sortButton.onClick.AddListener(OnSortClicked);
        _sellButton.onClick.AddListener(OnSellClicked);
        _closeButton.onClick.AddListener(RequestClose);

        _detailPanel.OnEnhanceClicked = OnEnhanceClicked;
    }

    public void Open()
    {
        _selectedItem   = null;
        _sortIndex      = 0;
        _sortType       = SortCycle[0];
        _sortLabel.text = SortLabels[0];

        _detailPanel.Hide();
        gameObject.SetActive(true);
        SelectCategory(EItemCategory.All);
    }

    public void Refresh()
    {
        _filtered.Clear();

        foreach (var item in InventoryCache.Instance.Items)
        {
            if (_category == EItemCategory.All || item.Data.Category == _category)
                _filtered.Add(item);
        }

        _filtered.Sort((a, b) => _sortType switch
        {
            EItemSortType.Rarity       => b.Data.Rarity.CompareTo(a.Data.Rarity),
            EItemSortType.EnchantLevel => b.EnchantLevel.CompareTo(a.EnchantLevel),
            _                          => b.Data.Tier.CompareTo(a.Data.Tier),
        });

        _scrollView.SetItems(_filtered, OnItemSelected);
        UpdateCountText();
    }

    private void OnCategorySelected(EItemCategory category) => SelectCategory(category);

    private void SelectCategory(EItemCategory category)
    {
        _category = category;

        for (int i = 0; i < _categoryTabs.Length; i++)
        {
            _categoryTabs[i].SetSelected((EItemCategory)i == category);
        }

        _selectedItem = null;
        _detailPanel.Hide();
        Refresh();
    }

    private void OnSortClicked()
    {
        _sortIndex      = (_sortIndex + 1) % SortCycle.Length;
        _sortType       = SortCycle[_sortIndex];
        _sortLabel.text = SortLabels[_sortIndex];
        Refresh();
    }

    private void OnItemSelected(ItemInstance item)
    {
        _selectedItem = item;
        _detailPanel.Show(item);
    }

    private void OnSellClicked()
    {
        if (_selectedItem == null || _selectedItem.IsLocked) return;

        UnityNetworkBridge.Instance.SendPacket(new RequestSellItem { InstanceId = _selectedItem.InstanceId });
        _selectedItem = null;
        _detailPanel.Hide();
    }

    public void OnSellResponse(ResponseSellItem res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

        InventoryCache.Instance.Remove(res.InstanceId);
        Refresh();
    }

    public void OnEquipResponse(ResponseEquipItem res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

        var equipped = InventoryCache.Instance.Find(res.EquippedInstanceId);
        if (equipped != null)
            EquipmentCache.Instance.Equip(res.UnitId, equipped);

        Refresh();
    }

    public void OnUnequipResponse(ResponseUnequipItem res)
    {
        if (res.Code != ENetworkStatusCode.Success) return;

        var item = InventoryCache.Instance.Find(res.InstanceId);
        if (item != null)
            EquipmentCache.Instance.Unequip(res.UnitId, item.Data.Category);

        Refresh();
    }

    private void OnEnhanceClicked(ItemInstance item)
    {
        // TODO: 강화 팝업 연결
        Debug.Log($"[Inventory] 강화 요청: {item.Data.ItemName} +{item.EnchantLevel}");
    }

    private void UpdateCountText()
    {
        _countText.text = $"{InventoryCache.Instance.Items.Count} / {InventoryCache.Instance.MaxCount}";
    }
}
