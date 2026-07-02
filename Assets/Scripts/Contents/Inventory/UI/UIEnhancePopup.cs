using SMDevLibrary.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 장비 강화 팝업(확정 강화·서버 권위). 선행 셸 — 표시 + 강화 버튼.
// ⚠️ 실동작(EnhanceRequest 송신·비용/재료 표시)은 계약 배포(turnrpg-server→bridge) + O7/경제 표면 착지 후.
// 설계=EQUIP_GROWTH_DESIGN.md(강화=메인스탯만·재화+재료·확정·스탯 서버 전산출).
[PopupPath("Enhance")]
public class UIEnhancePopup : BasePopup
{
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [SerializeField] private TextMeshProUGUI _enchantText;   // "+N → +N+1"
    [SerializeField] private Button _enhanceButton;          // 확정 강화(성공률 100%)
    [SerializeField] private Button _closeButton;

    private ItemInstance _item;

    protected override void Awake()
    {
        base.Awake();
        _closeButton.onClick.AddListener(RequestClose);
        _enhanceButton.onClick.AddListener(OnEnhanceConfirm);
    }

    public void Open(ItemInstance item)
    {
        _item = item;
        gameObject.SetActive(true);
        Refresh();
    }

    private void Refresh()
    {
        if (_item == null) return;
        _itemNameText.text = _item.Data != null ? _item.Data.ItemName : "-";
        _enchantText.text  = $"+{_item.EnchantLevel} → +{_item.EnchantLevel + 1}";
        // TODO(계약/데이터 배포 후): 강화 비용(재화+강화재료) 표시 = 서버/plan 데이터. 확정강화라 성공률 표시 없음.
    }

    // INFO :: 확정 강화 실행. 서버 권위(소유·비용 검증·차감·EnchantLevel+1)·스탯 서버 전산출.
    private void OnEnhanceConfirm()
    {
        // TODO(계약 배포 후·O7 경제 표면): EnhanceRequest{instanceId} 송신 → EnhanceResponse 반영.
        Debug.Log($"<color=#00C853>[UI/UIEnhancePopup]</color> :> 강화 요청(계약 대기): {_item?.Data?.ItemName} +{_item?.EnchantLevel}");
    }
}
