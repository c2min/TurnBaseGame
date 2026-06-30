using System;
using System.Collections.Generic;
using SMDevLibrary.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

// 스테이지 선택 팝업. StageCatalog(plan stage_catalog) 표시 → 선택 stageId 콜백.
// 전투 진입 와이어(StageEnterRequest)는 호출처(LobbySceneController)가 담당.
[PopupPath("Stage")]
public class UIStageSelectPopup : BasePopup
{
    [SerializeField] private Transform _listContainer;
    [SerializeField] private UIStageSlot _slotPrefab;
    [SerializeField] private Button _closeButton;

    private readonly List<UIStageSlot> _slots = new();
    private Action<int> _onSelect;

    protected override void Awake()
    {
        base.Awake();
        _closeButton.onClick.AddListener(RequestClose);
    }

    public void Open(Action<int> onSelect)
    {
        _onSelect = onSelect;
        gameObject.SetActive(true);
        Rebuild();
    }

    private void Rebuild()
    {
        ClearSlots();
        StageCatalog.Instance.EnsureLoaded();

        foreach (int stageId in StageCatalog.Instance.SortedStageIds)
        {
            if (StageCatalog.Instance.TryGet(stageId, out var entry) == false) continue;

            var slot = Instantiate(_slotPrefab, _listContainer);
            slot.Bind(stageId, entry, IsUnlocked(entry), OnStageChosen);
            _slots.Add(slot);
        }
    }

    // INFO :: unlockStageId=0이면 처음부터 해금. 선행 스테이지 클리어 판정=진행도 시스템 착지 후 → 현재 전부 해금 취급
    private bool IsUnlocked(StageCatalogEntry entry) => true;

    private void OnStageChosen(int stageId)
    {
        _onSelect?.Invoke(stageId);
        RequestClose();
    }

    private void ClearSlots()
    {
        foreach (var slot in _slots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        _slots.Clear();
    }
}
