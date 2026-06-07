using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : UIBase
{
    [SerializeField] private Image Image_LoadingImage;
    [SerializeField] private CanvasGroup CanvasGroup_LoadingRoot;
    [SerializeField] private Sprite[] Sprite_LoadingFrameList;
    [SerializeField] private float _loopDuration = 3f;
    [SerializeField] private float _transitionStartTime = 2.5f;
    [SerializeField] private float _screenFadeOutDuration = 0.5f;
    [SerializeField] private float _screenFadeInDuration = 0.8f;

    private Coroutine _animationCoroutine;
    private Coroutine _transitionCoroutine;
    private System.Func<bool> _onBeforeFadeOutCallback;
    private System.Action _onBlackScreenCallback;

    private void Awake()
    {
        BindMissingReferences();
        DisableRaycastTargets();
    }

    private void OnEnable()
    {
        DisableRaycastTargets();
        SetLoadingVisible(false);
    }

    private void OnDisable()
    {
        StopLoadingAnimation();
        StopTransitionSequence();
        ClearTransitionCallbacks();
    }

    public void StartLoading(System.Func<bool> onBeforeFadeOutCallback, System.Action onBlackScreenCallback)
    {
        _onBeforeFadeOutCallback = onBeforeFadeOutCallback;
        _onBlackScreenCallback = onBlackScreenCallback;

        SetLoadingVisible(true);
        StartLoadingAnimation();
        StartTransitionSequence();
    }

    private void BindMissingReferences()
    {
        Image_LoadingImage = FindChildImageIfNull(Image_LoadingImage, "Image_LoadingImage");
        CanvasGroup_LoadingRoot = FindCanvasGroupIfNull(CanvasGroup_LoadingRoot);
    }

    private void StartLoadingAnimation()
    {
        StopLoadingAnimation();

        if (TryValidateAnimationReferences() == false)
        {
            return;
        }

        _animationCoroutine = StartCoroutine(PlayLoadingAnimation());
    }

    private void StopLoadingAnimation()
    {
        if (_animationCoroutine == null)
        {
            return;
        }

        StopCoroutine(_animationCoroutine);
        _animationCoroutine = null;
    }

    private bool TryValidateAnimationReferences()
    {
        if (Image_LoadingImage == null)
        {
            Debug.LogWarning("LoadingUI의 Image_LoadingImage 참조가 비어 있어 로딩 애니메이션을 재생할 수 없습니다.");
            return false;
        }

        if (Sprite_LoadingFrameList == null || Sprite_LoadingFrameList.Length <= 0)
        {
            Debug.LogWarning("LoadingUI의 Sprite_LoadingFrameList가 비어 있어 로딩 애니메이션을 재생할 수 없습니다.");
            return false;
        }

        return true;
    }

    private IEnumerator PlayLoadingAnimation()
    {
        float elapsedTime = 0f;
        float loopDuration = Mathf.Max(0.01f, _loopDuration);

        while (true)
        {
            elapsedTime += Time.unscaledDeltaTime;
            int frameIndex = GetLoadingFrameIndex(elapsedTime, loopDuration);
            SetLoadingFrame(frameIndex);
            yield return null;
        }
    }

    private int GetLoadingFrameIndex(float elapsedTime, float loopDuration)
    {
        if (Sprite_LoadingFrameList == null || Sprite_LoadingFrameList.Length <= 0)
        {
            return 0;
        }

        float normalizedTime = Mathf.Repeat(elapsedTime, loopDuration) / loopDuration;
        int frameIndex = Mathf.FloorToInt(normalizedTime * Sprite_LoadingFrameList.Length);
        return Mathf.Clamp(frameIndex, 0, Sprite_LoadingFrameList.Length - 1);
    }

    private void SetLoadingFrame(int frameIndex)
    {
        if (Image_LoadingImage == null || Sprite_LoadingFrameList == null || Sprite_LoadingFrameList.Length <= 0)
        {
            return;
        }

        if (frameIndex < 0 || frameIndex >= Sprite_LoadingFrameList.Length)
        {
            return;
        }

        Sprite sprite = Sprite_LoadingFrameList[frameIndex];
        if (sprite == null)
        {
            return;
        }

        Image_LoadingImage.sprite = sprite;
    }

    private void StartTransitionSequence()
    {
        StopTransitionSequence();
        _transitionCoroutine = StartCoroutine(PlayTransitionSequence());
    }

    private void StopTransitionSequence()
    {
        if (_transitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = null;
    }

    private IEnumerator PlayTransitionSequence()
    {
        float transitionStartTime = Mathf.Max(0f, _transitionStartTime);
        if (transitionStartTime > 0f)
        {
            yield return new WaitForSecondsRealtime(transitionStartTime);
        }

        bool didRunBeforeFadeOut = InvokeBeforeFadeOutCallback();
        if (didRunBeforeFadeOut == false)
        {
            _transitionCoroutine = null;
            yield break;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 LoadingUI에서 화면 전환을 열 수 없습니다.");
            yield break;
        }

        ScreenTransitionUI screenTransitionUI = UIManager.Instance.OpenScreenTransitionUI();
        if (screenTransitionUI == null)
        {
            Debug.LogWarning("ScreenTransitionUI를 열 수 없어 LoadingUI를 닫지 않습니다.");
            yield break;
        }

        yield return screenTransitionUI.FadeOut(_screenFadeOutDuration);
        InvokeBlackScreenCallback();
        SetLoadingVisible(false);
        yield return screenTransitionUI.FadeIn(_screenFadeInDuration);
        _transitionCoroutine = null;
        UIManager.Instance.CloseLoadingUI();
    }

    private bool InvokeBeforeFadeOutCallback()
    {
        if (_onBeforeFadeOutCallback == null)
        {
            return true;
        }

        bool didCompleteBeforeFadeOut = _onBeforeFadeOutCallback.Invoke();
        if (didCompleteBeforeFadeOut == false)
        {
            Debug.LogWarning("LoadingUI의 페이드아웃 전 작업이 실패하여 화면 전환을 중단합니다.");
        }

        return didCompleteBeforeFadeOut;
    }

    private void InvokeBlackScreenCallback()
    {
        if (_onBlackScreenCallback == null)
        {
            return;
        }

        _onBlackScreenCallback.Invoke();
    }

    private void ClearTransitionCallbacks()
    {
        _onBeforeFadeOutCallback = null;
        _onBlackScreenCallback = null;
    }

    private void SetLoadingVisible(bool isVisible)
    {
        if (CanvasGroup_LoadingRoot == null)
        {
            CanvasGroup_LoadingRoot = FindCanvasGroupIfNull(CanvasGroup_LoadingRoot);
        }

        if (CanvasGroup_LoadingRoot == null)
        {
            return;
        }

        CanvasGroup_LoadingRoot.alpha = isVisible ? 1f : 0f;
        CanvasGroup_LoadingRoot.blocksRaycasts = false;
        CanvasGroup_LoadingRoot.interactable = false;
    }

    private void DisableRaycastTargets()
    {
        Image[] imageArr = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < imageArr.Length; i++)
        {
            if (imageArr[i] == null)
            {
                continue;
            }

            imageArr[i].raycastTarget = false;
        }
    }

    private CanvasGroup FindCanvasGroupIfNull(CanvasGroup currentCanvasGroup)
    {
        if (currentCanvasGroup != null)
        {
            return currentCanvasGroup;
        }

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            return canvasGroup;
        }

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        Debug.LogWarning("LoadingUI에 CanvasGroup이 없어 런타임에 추가했습니다.");
        return canvasGroup;
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
            Debug.LogWarning($"LoadingUI에서 {childName} 오브젝트를 찾을 수 없습니다.");
            return null;
        }

        Image image = childTransform.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"LoadingUI의 {childName} 오브젝트에 Image 컴포넌트가 없습니다.");
            return null;
        }

        return image;
    }

    private Transform FindChildTransformByName(Transform rootTransform, string childName)
    {
        if (rootTransform == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(childName))
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
