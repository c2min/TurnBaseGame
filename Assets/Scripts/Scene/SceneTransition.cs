using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using SMDevLibrary.SceneManagement;

/// <summary>
/// 씬 전환시 트랜지션 핸들러
/// </summary>
public class SceneTransition : MonoBehaviour, ISceneTransition
{
    [SerializeField]
    private Image _fadeImage;

    [SerializeField]
    private float _duration = 0.5f;

    [SerializeField]
    private Ease _easeType = Ease.InOutQuad;

    private void Awake()
    {
        _fadeImage.color = new Color(0, 0, 0, 0);
    }

    public async UniTask FadeOutAsync()
    {
        _fadeImage.DOKill();
        await _fadeImage.DOFade(1f, _duration)
            .SetEase(_easeType)
            .SetUpdate(true).ToUniTask();
    }

    public async UniTask FadeInAsync()
    {
        _fadeImage.DOKill();
        await _fadeImage.DOFade(0f, _duration)
            .SetEase(_easeType)
            .SetUpdate(true).ToUniTask();
    }

    public void FadeIn(Action onComplete)
    {
        _fadeImage.DOKill();
        _fadeImage.DOFade(0f, _duration)
            .SetEase(_easeType)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void FadeOut(Action onComplete)
    {
        _fadeImage.DOKill();
        _fadeImage.DOFade(1f, _duration)
            .SetEase(_easeType)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }
}