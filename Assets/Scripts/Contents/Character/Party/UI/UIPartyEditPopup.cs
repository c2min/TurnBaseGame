using System.Collections.Generic;
using SMDevLibrary.Managers;
using SMDevLibrary.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[PopupPath("Character")]
public class UIPartyEditPopup : BasePopup
{
    private const int SlotCount = 15;

    [SerializeField]
    private Transform _slotRoot;
    [SerializeField]
    private TextMeshProUGUI _partyLabel;
    [SerializeField]
    private Button _prevButton;
    [SerializeField]
    private Button _nextButton;
    [SerializeField]
    private Button[] _partyButtons;
    [SerializeField]
    private Button _confirmButton;

    private UIPartySlot[] _slots;
    private int _currentPartyIndex;
    private readonly AllyInfo[][] _parties = new AllyInfo[PartyCache.MaxPresets][];

    protected override void Awake()
    {
        base.Awake();

        _slots = _slotRoot.GetComponentsInChildren<UIPartySlot>();

        for (int i = 0; i < PartyCache.MaxPresets; i++)
            _parties[i] = new AllyInfo[SlotCount];

        _prevButton.onClick.AddListener(OnPrevParty);
        _nextButton.onClick.AddListener(OnNextParty);
        _confirmButton.onClick.AddListener(OnConfirmClicked);

        for (int i = 0; i < _partyButtons.Length; i++)
        {
            int idx = i;
            _partyButtons[i].onClick.AddListener(() => SelectParty(idx));
        }

        LoadAllPresets();
        SelectParty(0);
    }

    private void LoadAllPresets()
    {
        for (int p = 0; p < PartyCache.MaxPresets; p++)
        {
            foreach (var setInfo in PartyCache.Instance.GetPreset(p))
                _parties[p][setInfo.TileIndex] = setInfo.Ally;
        }
    }

    private void SelectParty(int index)
    {
        _currentPartyIndex = index;
        _partyLabel.text = $"Party{index + 1}";
        RefreshSlots();
        RefreshPartyButtons();
    }

    // 파티 전환 시: SetData로 콜백까지 재초기화
    private void RefreshSlots()
    {
        var party = _parties[_currentPartyIndex];
        for (int i = 0; i < _slots.Length; i++)
            _slots[i].SetData(i, party[i], GetPortrait(party[i]), OnSlotClicked, OnSlotSwapped);
    }

    // 스왑·캐릭터 확정 시: 비주얼만 갱신 (콜백은 이미 설정됨)
    private void RefreshSlot(int slotIndex, AllyInfo ally)
    {
        _slots[slotIndex].Refresh(ally, GetPortrait(ally));
    }

    private void RefreshPartyButtons()
    {
        for (int i = 0; i < _partyButtons.Length; i++)
        {
            var colors = _partyButtons[i].colors;
            colors.normalColor = i == _currentPartyIndex ? Color.yellow : Color.white;
            _partyButtons[i].colors = colors;
        }
    }

    private Sprite GetPortrait(AllyInfo ally)
    {
        if (ally == null) return null;
        return Client.Instance.GameData?.Characters.Get(ally.TemplateId)?.Portrait;
    }

    private void OnPrevParty() =>
        SelectParty((_currentPartyIndex - 1 + PartyCache.MaxPresets) % PartyCache.MaxPresets);

    private void OnNextParty() =>
        SelectParty((_currentPartyIndex + 1) % PartyCache.MaxPresets);

    private async void OnSlotClicked(int tileIndex)
    {
        var popup = await UIManager.Instance.Show<UICharacterSelectPopup>();
        popup.Open(
            tileIndex,
            _parties[_currentPartyIndex][tileIndex],
            Client.Instance.UserInfo.HaveCharacters,
            OnCharacterConfirmed
        );
    }

    private void OnCharacterConfirmed(int tileIndex, AllyInfo newAlly)
    {
        var party = _parties[_currentPartyIndex];

        if (newAlly != null)
        {
            // 선택한 캐릭터가 현재 파티의 다른 슬롯에 이미 있으면 스왑
            int sourceIndex = FindSlotWithUnit(newAlly.UnitId);
            if (sourceIndex >= 0 && sourceIndex != tileIndex)
            {
                // 타겟 슬롯의 기존 캐릭터를 소스 슬롯으로 보냄
                party[sourceIndex] = party[tileIndex];
                RefreshSlot(sourceIndex, party[sourceIndex]);
            }
        }

        party[tileIndex] = newAlly;
        RefreshSlot(tileIndex, newAlly);
        SaveCurrentPreset();
    }

    private int FindSlotWithUnit(string unitId)
    {
        var party = _parties[_currentPartyIndex];
        for (int i = 0; i < SlotCount; i++)
        {
            if (party[i] != null && party[i].UnitId == unitId)
                return i;
        }
        return -1;
    }

    private void OnSlotSwapped(int fromIndex, int toIndex)
    {
        var party = _parties[_currentPartyIndex];
        (party[fromIndex], party[toIndex]) = (party[toIndex], party[fromIndex]);
        RefreshSlot(fromIndex, party[fromIndex]);
        RefreshSlot(toIndex, party[toIndex]);
        SaveCurrentPreset();
    }

    private void SaveCurrentPreset()
    {
        var list = BuildPresetList(_currentPartyIndex);
        PartyCache.Instance.SetPreset(_currentPartyIndex, list);
    }

    private void OnConfirmClicked()
    {
        var list = BuildPresetList(_currentPartyIndex);
        PartyCache.Instance.SetPreset(_currentPartyIndex, list);
        PartyCache.Instance.SetParty(list);
        RequestClose();
    }

    private List<AllySetInfo> BuildPresetList(int partyIndex)
    {
        var list = new List<AllySetInfo>();
        var party = _parties[partyIndex];
        for (int i = 0; i < SlotCount; i++)
        {
            if (party[i] != null)
                list.Add(new AllySetInfo { Ally = party[i], TileIndex = i });
        }
        return list;
    }

}
