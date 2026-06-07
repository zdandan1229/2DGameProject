using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenTransitionUI : UIBase
{
    [SerializeField] private Image Image_Screen;
    [SerializeField] private CanvasGroup CanvasGroup_ScreenRoot;
    [SerializeField] private bool _useSmoothFade = true;

    private void Awake()
    {
        InitializeReferences();
        SetAlpha(0f);
    }

    public IEnumerator FadeOut(float duration)
    {
        gameObject.SetActive(true);
        yield return FadeAlpha(0f, 1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return FadeAlpha(1f, 0f, duration);
        gameObject.SetActive(false);
    }

    public void SetAlpha(float alpha)
    {
        InitializeReferences();

        if (CanvasGroup_ScreenRoot == null)
        {
            Debug.LogWarning("ScreenTransitionUI is missing CanvasGroup_ScreenRoot, so alpha cannot be updated.");
            return;
        }

        float clampedAlpha = Mathf.Clamp01(alpha);
        CanvasGroup_ScreenRoot.alpha = clampedAlpha;
        CanvasGroup_ScreenRoot.blocksRaycasts = clampedAlpha > 0f;
        CanvasGroup_ScreenRoot.interactable = false;
    }

    private IEnumerator FadeAlpha(float startAlpha, float targetAlpha, float duration)
    {
        InitializeReferences();

        if (CanvasGroup_ScreenRoot == null)
        {
            Debug.LogWarning("ScreenTransitionUI is missing CanvasGroup_ScreenRoot, so fade cannot be played.");
            yield break;
        }

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsedTime = 0f;
        SetAlpha(startAlpha);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, GetFadeProgress(progress)));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private float GetFadeProgress(float progress)
    {
        if (_useSmoothFade == false)
        {
            return progress;
        }

        return Mathf.SmoothStep(0f, 1f, progress);
    }

    private void InitializeReferences()
    {
        if (Image_Screen == null)
        {
            Image_Screen = GetComponentInChildren<Image>(true);
        }

        if (CanvasGroup_ScreenRoot == null)
        {
            CanvasGroup_ScreenRoot = GetComponent<CanvasGroup>();
            if (CanvasGroup_ScreenRoot == null)
            {
                CanvasGroup_ScreenRoot = gameObject.AddComponent<CanvasGroup>();
                Debug.LogWarning("ScreenTransitionUI had no CanvasGroup, so one was added at runtime.");
            }
        }
    }

}
