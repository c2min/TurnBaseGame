using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SMDevLibrary.UI.Layout;

public class UICharacterElement : MonoBehaviour, IRecycleElement<AllyInfo>
{
    [SerializeField]
    private Image _thumbnail;
    [SerializeField]
    private Image _highlight;
    [SerializeField]
    private TextMeshProUGUI _levelText;
    [SerializeField]
    private Button _button;

    public AllyInfo Info { get; private set; }

    private Action<AllyInfo> _onClicked;

    private void Awake()
    {
        _button.onClick.AddListener(() => _onClicked?.Invoke(Info));
    }

    public void Bind(AllyInfo data)
    {
        Info = data;

        if (_levelText != null)
            _levelText.text = $"Lv.{data.Level}";

        if (_thumbnail != null)
        {
            Sprite portrait = null;
            var charData = Client.Instance.GameData?.Characters.Get(data.TemplateId);
            if (charData != null)
                portrait = charData.Portrait;
                
            _thumbnail.sprite  = portrait;
            _thumbnail.enabled = portrait != null;
        }

        SetSelected(false);
    }

    public void SetOnClicked(Action<AllyInfo> onClicked)
    {
        _onClicked = onClicked;
    }

    public void SetSelected(bool selected)
    {
        _highlight.gameObject.SetActive(selected);
    }

    public bool IsAlly(AllyInfo ally) =>
        Info != null && ally != null && Info.UnitId == ally.UnitId;
}
