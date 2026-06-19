using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISkillInfoPanel : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _skillPointText;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(SkillData skill)
    {
        if (skill == null) return;

        _icon.sprite = skill.Icon;
        _icon.enabled = skill.Icon != null;
        _nameText.text = skill.SkillName;
        _descriptionText.text = skill.Description;
        _skillPointText.text = skill.SkillType switch
        {
            ESkillType.Ultimate => "궁극기",
            ESkillType.Passive  => "패시브",
            _                   => string.Empty,
        };

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
