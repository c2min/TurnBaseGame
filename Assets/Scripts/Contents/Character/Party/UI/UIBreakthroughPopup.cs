using SMDevLibrary.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 캐릭터 돌파 팝업(레벨캡 상향·서버 권위). 선행 셸 — 표시 + 돌파 버튼.
// ⚠️ 실동작(BreakthroughRequest 송신·단계/레벨캡/재료 표시)은 계약 배포 + O7(per-character 영속·성장상태) 착지 후.
// 설계=EQUIP_GROWTH_DESIGN.md(성장=레벨/exp+돌파·스탯 서버 전산출·각성/스킬 추후).
[PopupPath("Breakthrough")]
public class UIBreakthroughPopup : BasePopup
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _levelText;      // "Lv.N"
    [SerializeField] private TextMeshProUGUI _stageText;      // "돌파 N → N+1" (서버 성장상태·O7 후)
    [SerializeField] private Button _breakthroughButton;
    [SerializeField] private Button _closeButton;

    private AllyInfo _ally;

    protected override void Awake()
    {
        base.Awake();
        _closeButton.onClick.AddListener(RequestClose);
        _breakthroughButton.onClick.AddListener(OnBreakthroughConfirm);
    }

    public void Open(AllyInfo ally)
    {
        _ally = ally;
        gameObject.SetActive(true);
        Refresh();
    }

    private void Refresh()
    {
        if (_ally == null) return;
        _nameText.text  = _ally.Name;
        _levelText.text = $"Lv.{_ally.Level}";
        // TODO(O7/계약 배포 후): 돌파 단계·레벨캡·재료 = 서버 per-character 성장상태 + 데이터. 현재 미제공.
        _stageText.text = "—";
    }

    // INFO :: 돌파 실행. 서버 권위(재료 검증·차감·레벨캡↑·단계+1)·스탯 서버 전산출.
    private void OnBreakthroughConfirm()
    {
        // TODO(계약 배포 후·O7): BreakthroughRequest{characterId} 송신 → BreakthroughResponse 반영.
        Debug.Log($"<color=#00C853>[UI/UIBreakthroughPopup]</color> :> 돌파 요청(계약 대기): {_ally?.Name}");
    }
}
