using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharInfoPanel : MonoBehaviour
{
    [Header("스탯 텍스트")]
    [SerializeField]
    private TextMeshProUGUI _nameText;
    [SerializeField]
    private TextMeshProUGUI _levelText;
    [SerializeField]
    private TextMeshProUGUI _hpText;
    [SerializeField]
    private TextMeshProUGUI _attackText;
    [SerializeField]
    private TextMeshProUGUI _defenseText;
    [SerializeField]
    private TextMeshProUGUI _speedText;
    [SerializeField]
    private TextMeshProUGUI _critRateText;
    [SerializeField]
    private TextMeshProUGUI _critDamageText;

    [Header("스파인 미리보기")]
    [SerializeField]
    private SpineCharacterPreview _preview;

    [Header("버튼")]
    [SerializeField]
    private Button _levelUpButton;
    [SerializeField]
    private Button _confirmButton;

    public Action OnLevelUp;
    public Action OnConfirm;

    private void Awake()
    {
        _levelUpButton.onClick.AddListener(() => OnLevelUp?.Invoke());
        _confirmButton.onClick.AddListener(() => OnConfirm?.Invoke());
    }

    public void Bind(AllyInfo ally, CharacterData charData)
    {
        // 스탯=서버 전산출(AllyInfo=CharacterInfo 최종값·장비 반영). 클라 계산 supersede.
        _nameText.text      = ally.Name;
        _levelText.text     = $"Lv.{ally.Level}";
        _hpText.text        = ally.Hp.ToString();
        _attackText.text    = ally.AttackPower.ToString();
        _defenseText.text   = ally.Defense.ToString();
        _speedText.text     = ally.Speed.ToString();
        _critRateText.text  = "-"; // 서버 CharacterInfo 미운반(후속)
        _critDamageText.text = "-";

        _preview?.Show(charData);
    }

    public void Clear()
    {
        _nameText.text       = string.Empty;
        _levelText.text      = string.Empty;
        _hpText.text         = string.Empty;
        _attackText.text     = string.Empty;
        _defenseText.text    = string.Empty;
        _speedText.text      = string.Empty;
        _critRateText.text   = string.Empty;
        _critDamageText.text = string.Empty;

        _preview?.Clear();
    }
}
