using TMPro;
using UnityEngine;

public class UIStatRow : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _typeText;
    [SerializeField]
    private TextMeshProUGUI _valueText;

    public void Bind(StatEntry stat)
    {
        gameObject.SetActive(true);
        _typeText.text  = stat.StatType.ToKorean();
        _valueText.text = stat.FormatValue();
    }

    public void Hide() => gameObject.SetActive(false);
}
