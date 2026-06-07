using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FoggedBlackScreenEffect : MonoBehaviour
{
    [SerializeField] private Image Image1;
    [SerializeField] private Image Image2;
    [SerializeField] private float _minAlpha = 50f;
    [SerializeField] private float _maxAlpha = 255f;
    [SerializeField] private float _image1FadeDuration = 2.4f;
    [SerializeField] private float _image2FadeDuration = 3.6f;
    [SerializeField] private float _image2StartDelay = 0.8f;

    private Coroutine _image1Coroutine;
    private Coroutine _image2Coroutine;

    private void Awake()
    {
        BindMissingReferences();
    }

    private void OnEnable()
    {
        StartFogEffect();
    }

    private void OnDisable()
    {
        StopFogEffect();
    }

    private void BindMissingReferences()
    {
        Image1 = FindChildImageIfNull(Image1, "Image1");
        Image1 = FindChildImageIfNull(Image1, "Image");
        Image2 = FindChildImageIfNull(Image2, "Image2");
    }

    private void StartFogEffect()
    {
        StopFogEffect();

        if (Image1 == null && Image2 == null)
        {
            Debug.LogWarning("FoggedBlackScreenEffect has no Image1 or Image2, so fog effect cannot be played.");
            return;
        }

        if (Image1 != null)
        {
            _image1Coroutine = StartCoroutine(PlayFogAlphaLoop(Image1, _image1FadeDuration, 0f));
        }

        if (Image2 != null)
        {
            _image2Coroutine = StartCoroutine(PlayFogAlphaLoop(Image2, _image2FadeDuration, _image2StartDelay));
        }
    }

    private void StopFogEffect()
    {
        StopFogCoroutine(ref _image1Coroutine);
        StopFogCoroutine(ref _image2Coroutine);
    }

    private void StopFogCoroutine(ref Coroutine coroutine)
    {
        if (coroutine == null)
        {
            return;
        }

        StopCoroutine(coroutine);
        coroutine = null;
    }

    private IEnumerator PlayFogAlphaLoop(Image image, float fadeDuration, float startDelay)
    {
        if (image == null)
        {
            yield break;
        }

        SetImageAlpha(image, _minAlpha);

        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }

        float duration = Mathf.Max(0.01f, fadeDuration);
        while (true)
        {
            yield return FadeImageAlpha(image, _minAlpha, _maxAlpha, duration);
            yield return FadeImageAlpha(image, _maxAlpha, _minAlpha, duration);
        }
    }

    private IEnumerator FadeImageAlpha(Image image, float startAlpha, float targetAlpha, float duration)
    {
        if (image == null)
        {
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, progress));
            SetImageAlpha(image, alpha);
            yield return null;
        }

        SetImageAlpha(image, targetAlpha);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp(alpha, 0f, 255f) / 255f;
        image.color = color;
    }

    private Image FindChildImageIfNull(Image currentImage, string childName)
    {
        if (currentImage != null)
        {
            return currentImage;
        }

        Transform childTransform = FindChildTransformByName(transform, childName);
        if (childTransform == null)
        {
            return null;
        }

        Image image = childTransform.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"FoggedBlackScreenEffect child {childName} has no Image component.");
            return null;
        }

        return image;
    }

    private Transform FindChildTransformByName(Transform rootTransform, string childName)
    {
        if (rootTransform == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        Transform[] childTransforms = rootTransform.GetComponentsInChildren<Transform>(true);
        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform != null && childTransform.name == childName)
            {
                return childTransform;
            }
        }

        return null;
    }
}
