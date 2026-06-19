using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISkillSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float LongPressDuration = 0.5f;

    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _notReadyOverlay;
    [SerializeField] private Image _selectedBorder;
    [SerializeField] private TextMeshProUGUI _skillNameText;

    public SkillSlotRuntime BoundSkill => _skill;

    private SkillSlotRuntime _skill;
    private Action<SkillSlotRuntime> _onClick;
    private Action<SkillSlotRuntime> _onLongPressStart;
    private Action _onLongPressEnd;
    private UltimateGauge _boundGauge;

    private Coroutine _longPressCoroutine;
    private bool _longPressTriggered;

    public void Bind(SkillSlotRuntime skill, Action<SkillSlotRuntime> onClick,
                     Action<SkillSlotRuntime> onLongPressStart = null, Action onLongPressEnd = null)
    {
        UnbindGauge();

        _skill = skill;
        _onClick = onClick;
        _onLongPressStart = onLongPressStart;
        _onLongPressEnd = onLongPressEnd;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() =>
        {
            if (!_longPressTriggered)
                _onClick?.Invoke(_skill);
        });

        if (skill?.Data != null)
        {
            _icon.sprite = skill.Data.Icon;
            _icon.enabled = skill.Data.Icon != null;
            _skillNameText.text = skill.Data.SkillName;
        }

        // 궁극기는 게이지가 찼을 때 자동으로 버튼 활성화되도록 구독
        if (skill?.Data.SkillType == ESkillType.Ultimate && skill.UltimateGauge != null)
        {
            _boundGauge = skill.UltimateGauge;
            _boundGauge.OnGaugeChanged += Refresh;
        }

        SetSelected(false);
        gameObject.SetActive(skill != null);
        Refresh();
    }

    public void SetSelected(bool selected)
    {
        if (_selectedBorder != null)
            _selectedBorder.enabled = selected;
    }

    public void Refresh()
    {
        if (_skill == null)
            return;

        bool ready = _skill.IsReady;

        _notReadyOverlay.enabled = !ready;
        _button.interactable = ready;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_button.interactable) return;

        _longPressTriggered = false;
        _longPressCoroutine = StartCoroutine(LongPressRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_longPressTriggered)
            _onLongPressEnd?.Invoke();

        CancelLongPress();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_longPressTriggered)
            _onLongPressEnd?.Invoke();

        CancelLongPress();
    }

    private IEnumerator LongPressRoutine()
    {
        yield return new WaitForSecondsRealtime(LongPressDuration);
        _longPressTriggered = true;
        _onLongPressStart?.Invoke(_skill);
    }

    private void CancelLongPress()
    {
        if (_longPressCoroutine != null)
        {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
        }

        _longPressTriggered = false;
    }

    private void UnbindGauge()
    {
        if (_boundGauge == null)
            return;

        _boundGauge.OnGaugeChanged -= Refresh;
        _boundGauge = null;
    }

    private void OnDestroy()
    {
        CancelLongPress();
        UnbindGauge();
    }
}
