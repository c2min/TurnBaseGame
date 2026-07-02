using System;
using System.Collections.Generic;
using SM.Contracts.TurnRPG;
using SMDevLibrary.Managers;
using SMDevLibrary.Network.Utility;
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
        // 캐릭터 성장(돌파) 팝업 열기(셸). ⚠️실 돌파 송신=계약 배포+O7(성장상태 영속) 착지 후(EQUIP_GROWTH_DESIGN.md).
        if (_pendingAlly == null) return;
        UIManager.Instance.Show<UIBreakthroughPopup>(p => p.Open(_pendingAlly));
    }

    private void OnSkillLevelUpClicked()
    {
        // TODO: 스킬 강화 팝업 연결
        Debug.Log($"[CharSelect] 스킬 강화: {(_pendingAlly != null ? _pendingAlly.Name : "없음")}");
    }

    private void OnEquipSwapClicked()
    {
        // 인벤토리 팝업 열기(전체 카테고리 브라우즈) — 실제 장착은 장착 계약(미커버) 착지 후 배선
        UIManager.Instance.Show<UIInventoryPopup>(p => p.Open());
    }

    private void OnEquipEnhanceClicked()
    {
        // TODO: 장비 강화 팝업 연결
        Debug.Log($"[CharSelect] 장비 강화: {(_pendingAlly != null ? _pendingAlly.Name : "없음")}");
    }

    private void OnEquipSlotClicked(EItemCategory category, ItemInstance item)
    {
        // 장착된 슬롯 클릭 → 해제 요청(서버 권위: Affected 인벤 반영 + Character 최종스탯 재산출).
        if (item != null)
        {
            UnityNetworkBridge.Instance.SendPacket(new EquipmentUnequipRequestPacket { EquipmentId = item.EquipmentId });
            return;
        }
        // TODO(장착 트리거·후속): 빈 슬롯 → 인벤에서 카테고리 장비 선택 후 equip.req(charId, equipmentId).
        //   아이템→캐릭 선택 플로우(대상 charId 스레딩) 필요 — UI 플로우 결정 대기.
        UIManager.Instance.Show<UIInventoryPopup>(p => p.Open(category));
    }
}
