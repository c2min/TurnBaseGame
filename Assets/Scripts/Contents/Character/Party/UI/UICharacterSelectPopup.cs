using System;
using System.Collections.Generic;
using SMDevLibrary.Managers;
using SMDevLibrary.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

[PopupPath("Character")]
public class UICharacterSelectPopup : BasePopup
{
    private readonly Image[] _tabImages = new Image[4];

    [Header("캐릭터 목록")]
    [SerializeField]
    private UICharacterList _characterList;

    [Header("탭 버튼 — 정보/스킬/장비/돌파 순서")]
    [SerializeField]
    private Button[] _tabButtons;
    
    [SerializeField]
    private Sprite _tabNormalSprite;
    
    [SerializeField]
    private Sprite _tabSelectedSprite;

    [Header("탭 패널")]
    [SerializeField]
    private UICharInfoPanel _infoPanel;
    
    [SerializeField]
    private UICharSkillPanel _skillPanel;
    
    [SerializeField]
    private UICharEquipPanel _equipPanel;
    
    [SerializeField]
    private GameObject _breakthroughPanel;

    [Header("닫기")]
    [SerializeField] private Button _closeButton;

    private int _tileIndex;
    private int _activeTab;
    private AllyInfo _pendingAlly;
    private Action<int, AllyInfo> _onConfirm;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            int idx = i;
            _tabButtons[i].onClick.AddListener(() => SelectTab(idx));
            _tabImages[i] = _tabButtons[i].GetComponent<Image>();
        }

        if (_closeButton != null)
            _closeButton.onClick.AddListener(RequestClose);

        _infoPanel.OnConfirm  = OnConfirmClicked;
        _infoPanel.OnLevelUp  = OnLevelUpClicked;

        _skillPanel.OnLevelUp = OnSkillLevelUpClicked;

        _equipPanel.OnSwap    = OnEquipSwapClicked;
        _equipPanel.OnEnhance = OnEquipEnhanceClicked;
        _equipPanel.OnSlotClicked = OnEquipSlotClicked;
    }

    // ── 공개 API ──────────────────────────────────────────────────────────────

    public void Open(int tileIndex, AllyInfo currentAlly,
                     List<AllyInfo> characters, Action<int, AllyInfo> onConfirm)
    {
        _tileIndex   = tileIndex;
        _pendingAlly = currentAlly;
        _onConfirm   = onConfirm;

        _characterList.SetData(characters, OnCharacterSelected);

        SelectTab(0);

        if (currentAlly != null)
        {
            _characterList.SetSelected(currentAlly);
            BindActivePanel(currentAlly);
        }
        else
        {
            ClearPanels();
        }
    }

    // ── 캐릭터 선택 ───────────────────────────────────────────────────────────

    private void OnCharacterSelected(AllyInfo ally)
    {
        _pendingAlly = ally;
        _characterList.SetSelected(ally);
        BindActivePanel(ally);
    }

    // ── 탭 전환 ───────────────────────────────────────────────────────────────

    private void SelectTab(int index)
    {
        _activeTab = index;

        _infoPanel.gameObject.SetActive(index == 0);
        _skillPanel.gameObject.SetActive(index == 1);
        _equipPanel.gameObject.SetActive(index == 2);
        if (_breakthroughPanel != null)
            _breakthroughPanel.SetActive(index == 3);

        RefreshTabVisuals();

        if (_pendingAlly != null)
            BindActivePanel(_pendingAlly);
    }

    private void RefreshTabVisuals()
    {
        for (int i = 0; i < _tabImages.Length; i++)
        {
            if (_tabImages[i] == null)
                continue;

            _tabImages[i].sprite = i == _activeTab ? _tabSelectedSprite : _tabNormalSprite;
        }
    }

    // ── 패널 바인딩 ───────────────────────────────────────────────────────────

    private void BindActivePanel(AllyInfo ally)
    {
        var charData = Client.Instance.GameData?.Characters.Get(ally.TemplateId);

        switch (_activeTab)
        {
            case 0: _infoPanel.Bind(ally, charData);  break;
            case 1: _skillPanel.Bind(ally, charData); break;
            case 2: _equipPanel.Bind(ally);           break;
        }
    }

    private void ClearPanels()
    {
        _infoPanel.Clear();
        _skillPanel.Clear();
        _equipPanel.Clear();
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────────────────────

    private void OnConfirmClicked()
    {
        _onConfirm?.Invoke(_tileIndex, _pendingAlly);
        RequestClose();
    }

    private void OnLevelUpClicked()
    {
        // TODO: 캐릭터 강화 팝업 연결
        Debug.Log($"[CharSelect] 캐릭터 강화: {(_pendingAlly != null ? _pendingAlly.Name : "없음")}");
    }

    private void OnSkillLevelUpClicked()
    {
        // TODO: 스킬 강화 팝업 연결
        Debug.Log($"[CharSelect] 스킬 강화: {(_pendingAlly != null ? _pendingAlly.Name : "없음")}");
    }

    private void OnEquipSwapClicked()
    {
        // TODO: 인벤토리 팝업 연결 (슬롯 카테고리 전달)
        Debug.Log($"[CharSelect] 장비 교체: {(_pendingAlly != null ? _pendingAlly.Name : "없음")}");
    }

    private void OnEquipEnhanceClicked()
    {
        // TODO: 장비 강화 팝업 연결
        Debug.Log($"[CharSelect] 장비 강화: {(_pendingAlly != null ? _pendingAlly.Name : "없음")}");
    }

    private void OnEquipSlotClicked(EItemCategory category, ItemInstance item)
    {
        // TODO: 슬롯별 장비 교체 팝업 연결
        string itemName = item != null ? item.Data.ItemName : "비어있음";
        Debug.Log($"[CharSelect] 슬롯 클릭: {category} / {itemName}");
    }
}
