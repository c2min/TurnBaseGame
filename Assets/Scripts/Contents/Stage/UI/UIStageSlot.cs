using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스테이지 선택 목록의 한 항목. UIStageSelectPopup이 SortedStageIds 순회하며 Instantiate·Bind.
public class UIStageSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Button _selectButton;
    [SerializeField] private GameObject _lockOverlay;

    // 진행도(클리어) 시스템 미착지 → 로케일 소스 없어 현재 한국어 폴백 직접 해소
    private const string DefaultLocaleKey = "ko";

    private int _stageId;
    private Action<int> _onSelect;

    private void Awake() => _selectButton.onClick.AddListener(OnClicked);

    public void Bind(int stageId, StageCatalogEntry entry, bool unlocked, Action<int> onSelect)
    {
        _stageId  = stageId;
        _onSelect = onSelect;

        _nameText.text = entry.Name != null ? entry.Name.Resolve(DefaultLocaleKey) : $"Stage {stageId}";
        if (_lockOverlay != null) _lockOverlay.SetActive(!unlocked);
        _selectButton.interactable = unlocked;
        // TODO: 썸네일(entry.ThumbnailPath) = ResourceLoader path-side 로드 — 데모 썸네일 확정 후
    }

    private void OnClicked() => _onSelect?.Invoke(_stageId);
}
