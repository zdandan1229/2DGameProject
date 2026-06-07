using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FirstGameStartUI : UIBase
{
    private enum TutorialStartMode
    {
        Tutorial,
        Skip
    }

    [SerializeField] private Button Button_TutorialStart;
    [SerializeField] private Button Button_TutorialSkip;

    [Header("Prologue Text Effect")]
    [SerializeField] private GameObject GameObject_SelectionBoxRoot;
    [SerializeField] private GameObject GameObject_PrologueText;
    [SerializeField] private Text Text_Korean;
    [SerializeField] private Text Text_English;
    [SerializeField] private AudioSource Audio_PrologueSound;
    [SerializeField] private float _prologueSoundTargetVolume = 0.3f;
    [SerializeField] private float _prologueSoundFadeInDuration = 0.15f;
    [SerializeField] private float _prologueSoundFadeOutDuration = 1f;
    [SerializeField] private float _koreanTextInterval = 0.04f;
    [SerializeField] private float _englishTextInterval = 0.035f;
    [SerializeField] private float _englishStartDelay = 0.2f;
    [SerializeField] private float _loadingStartDelay = 1f;
    [SerializeField] private string _tutorialSkipStartEntryPointId = "entrance_tutorialend";

    private Coroutine _prologueCoroutine;
    private Coroutine _prologueSoundFadeCoroutine;
    private string _koreanFullText;
    private string _englishFullText;
    private bool _isLoadingStartRequested;
    private TutorialStartMode _requestedStartMode;

    private void Awake()
    {
        BindMissingReferences();
        DisableNonButtonRaycastTargets();
        CachePrologueText();
        ResetPrologueText();
        SetPrologueSoundVolumeImmediately(0f);
        BindButtonEvents();
    }

    private void OnDisable()
    {
        StopPrologueSequence();
        _isLoadingStartRequested = false;
        ResetPrologueText();
        SetPrologueSoundVolumeImmediately(0f);
    }

    private void OnDestroy()
    {
        UnbindButtonEvents();
    }

    private void OnClickTutorialStart()
    {
        if (_prologueCoroutine != null)
        {
            return;
        }

        _prologueCoroutine = StartCoroutine(PlayPrologueSequence());
    }

    private void OnClickTutorialSkip()
    {
        StopPrologueSequence();
        ResetPrologueText();
        SetPrologueSoundVolumeImmediately(0f);
        OpenLoadingForGameStart(TutorialStartMode.Skip);
    }

    private IEnumerator PlayPrologueSequence()
    {
        if (Text_Korean == null || Text_English == null)
        {
            Debug.LogWarning("FirstGameStartUI의 프롤로그 텍스트 참조가 비어 있어 튜토리얼 시작 연출을 재생할 수 없습니다.");
            _prologueCoroutine = null;
            yield break;
        }

        SetButtonInteractable(false);
        SetSelectionBoxActive(false);

        if (GameObject_PrologueText != null)
        {
            GameObject_PrologueText.SetActive(true);
        }

        Text_Korean.text = string.Empty;
        Text_English.text = string.Empty;
        StartPrologueSoundFade(_prologueSoundTargetVolume, _prologueSoundFadeInDuration);

        bool isKoreanComplete = false;
        bool isEnglishComplete = false;

        StartCoroutine(TypeText(Text_Korean, _koreanFullText, _koreanTextInterval, () =>
        {
            isKoreanComplete = true;
            StartPrologueSoundFade(0f, _prologueSoundFadeOutDuration);
        }));
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, _englishStartDelay));
        StartCoroutine(TypeText(Text_English, _englishFullText, _englishTextInterval, () => isEnglishComplete = true));

        while (isKoreanComplete == false || isEnglishComplete == false)
        {
            yield return null;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, _loadingStartDelay));
        _prologueCoroutine = null;
        OpenLoadingForGameStart(TutorialStartMode.Tutorial);
    }

    private IEnumerator TypeText(Text targetText, string fullText, float textInterval, System.Action onComplete)
    {
        if (targetText == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (string.IsNullOrEmpty(fullText))
        {
            targetText.text = string.Empty;
            onComplete?.Invoke();
            yield break;
        }

        textInterval = Mathf.Max(0f, textInterval);
        for (int i = 0; i < fullText.Length; i++)
        {
            targetText.text = fullText.Substring(0, i + 1);

            if (textInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(textInterval);
            }
            else
            {
                yield return null;
            }
        }

        onComplete?.Invoke();
    }

    private void OpenLoadingForGameStart(TutorialStartMode startMode)
    {
        if (_isLoadingStartRequested)
        {
            return;
        }

        _isLoadingStartRequested = true;
        _requestedStartMode = startMode;
        SetButtonInteractable(false);
        SetSelectionBoxActive(false);

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 LoadingUI를 열 수 없습니다.");
            RestoreSelectionBoxForLoadingRetry();
            return;
        }

        LoadingUI loadingUI = UIManager.Instance.OpenLoadingUI();
        if (loadingUI == null)
        {
            RestoreSelectionBoxForLoadingRetry();
            return;
        }

        loadingUI.StartLoading(PrepareRequestedGameStart, CompleteRequestedGameStartOnBlackScreen);
    }

    private void RestoreSelectionBoxForLoadingRetry()
    {
        _isLoadingStartRequested = false;
        SetButtonInteractable(true);
        SetSelectionBoxActive(true);
    }

    private void StopPrologueSequence()
    {
        if (_prologueCoroutine == null)
        {
            return;
        }

        StopCoroutine(_prologueCoroutine);
        _prologueCoroutine = null;
    }

    private bool PrepareRequestedGameStart()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance가 없어 게임 시작 세팅을 처리할 수 없습니다.");
            return false;
        }

        if (_requestedStartMode == TutorialStartMode.Skip)
        {
            return GameManager.Instance.PrepareNewGameStartAtEntryPoint(_tutorialSkipStartEntryPointId);
        }

        return GameManager.Instance.PrepareNewGameStart();
    }

    private void CompleteRequestedGameStartOnBlackScreen()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 FirstGameStartUI를 닫을 수 없습니다.");
            return;
        }

        UIManager.Instance.CloseFirstGameStartUI();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance가 없어 튜토리얼 스킵 후 게임을 재개할 수 없습니다.");
            return;
        }

        if (_requestedStartMode == TutorialStartMode.Tutorial)
        {
            GameManager.Instance.ApplyTutorialStartLocks();
        }
        else
        {
            GameManager.Instance.ApplyTutorialSkipStartLocks();
            ApplyTutorialSkipCompletedState();
        }

        GameManager.Instance.ResumeGame();
    }

    private void ApplyTutorialSkipCompletedState()
    {
        TutorialScenarioController tutorialScenarioController = FindFirstObjectByType<TutorialScenarioController>();
        if (tutorialScenarioController == null)
        {
            Debug.LogWarning("TutorialScenarioController가 없어 튜토리얼 스킵 완료 상태를 적용할 수 없습니다.");
            return;
        }

        tutorialScenarioController.ApplyTutorialSkipCompletedState();
    }

    private void BindMissingReferences()
    {
        Button_TutorialStart = FindChildButtonIfNull(Button_TutorialStart, "Button_TutorialStart");
        Button_TutorialSkip = FindChildButtonIfNull(Button_TutorialSkip, "Button_TutorialSkip");
        Text_Korean = FindChildTextIfNull(Text_Korean, "TextKorean");
        Text_English = FindChildTextIfNull(Text_English, "TextEnglish");
        Audio_PrologueSound = FindPrologueAudioSourceIfNull(Audio_PrologueSound);

        if (GameObject_SelectionBoxRoot == null)
        {
            Transform selectionBoxTransform = FindChildTransformByName(transform, "Image_SelectionBoxBorderLine");
            if (selectionBoxTransform != null)
            {
                GameObject_SelectionBoxRoot = selectionBoxTransform.gameObject;
            }
        }

        if (GameObject_PrologueText == null && Text_Korean != null)
        {
            GameObject_PrologueText = Text_Korean.transform.parent.gameObject;
        }
    }

    private void CachePrologueText()
    {
        _koreanFullText = Text_Korean != null ? Text_Korean.text : string.Empty;
        _englishFullText = Text_English != null ? Text_English.text : string.Empty;
    }

    private void ResetPrologueText()
    {
        if (GameObject_PrologueText != null)
        {
            GameObject_PrologueText.SetActive(false);
        }

        if (Text_Korean != null)
        {
            Text_Korean.text = _koreanFullText;
        }

        if (Text_English != null)
        {
            Text_English.text = _englishFullText;
        }

        SetButtonInteractable(true);
        SetSelectionBoxActive(true);
    }

    private void SetButtonInteractable(bool isInteractable)
    {
        if (Button_TutorialStart != null)
        {
            Button_TutorialStart.interactable = isInteractable;
        }

        if (Button_TutorialSkip != null)
        {
            Button_TutorialSkip.interactable = isInteractable;
        }
    }

    private void SetSelectionBoxActive(bool isActive)
    {
        if (GameObject_SelectionBoxRoot == null)
        {
            Debug.LogWarning("FirstGameStartUI의 GameObject_SelectionBoxRoot 참조가 비어 있어 선택지 박스를 제어할 수 없습니다.");
            return;
        }

        GameObject_SelectionBoxRoot.SetActive(isActive);
    }

    private void StartPrologueSoundFade(float targetVolume, float fadeDuration)
    {
        StopPrologueSoundFade();

        _prologueSoundFadeCoroutine = StartCoroutine(FadePrologueSoundVolume(targetVolume, fadeDuration));
    }

    private void StopPrologueSoundFade()
    {
        if (_prologueSoundFadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(_prologueSoundFadeCoroutine);
        _prologueSoundFadeCoroutine = null;
    }

    private IEnumerator FadePrologueSoundVolume(float targetVolume, float fadeDuration)
    {
        if (Audio_PrologueSound == null)
        {
            Audio_PrologueSound = FindPrologueAudioSourceIfNull(Audio_PrologueSound);
        }

        if (Audio_PrologueSound == null)
        {
            yield break;
        }

        Audio_PrologueSound.mute = false;

        if (Audio_PrologueSound.isPlaying == false)
        {
            Audio_PrologueSound.Play();
        }

        float startVolume = Audio_PrologueSound.volume;
        targetVolume = Mathf.Clamp01(targetVolume);
        fadeDuration = Mathf.Max(0f, fadeDuration);

        if (fadeDuration <= 0f)
        {
            Audio_PrologueSound.volume = targetVolume;
            _prologueSoundFadeCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);
            Audio_PrologueSound.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        Audio_PrologueSound.volume = targetVolume;
        _prologueSoundFadeCoroutine = null;
    }

    private void SetPrologueSoundVolumeImmediately(float volume)
    {
        if (_prologueSoundFadeCoroutine != null)
        {
            StopCoroutine(_prologueSoundFadeCoroutine);
            _prologueSoundFadeCoroutine = null;
        }

        if (Audio_PrologueSound == null)
        {
            Audio_PrologueSound = FindPrologueAudioSourceIfNull(Audio_PrologueSound);
        }

        if (Audio_PrologueSound == null)
        {
            return;
        }

        Audio_PrologueSound.mute = false;
        Audio_PrologueSound.volume = Mathf.Clamp01(volume);

        if (Audio_PrologueSound.isPlaying == false)
        {
            Audio_PrologueSound.Play();
        }
    }

    private void BindButtonEvents()
    {
        if (Button_TutorialStart == null)
        {
            Debug.LogWarning("FirstGameStartUI의 Button_TutorialStart 참조가 비어 있어 튜토리얼 시작 버튼을 연결할 수 없습니다.");
        }
        else
        {
            Button_TutorialStart.onClick.RemoveListener(OnClickTutorialStart);
            Button_TutorialStart.onClick.AddListener(OnClickTutorialStart);
        }

        if (Button_TutorialSkip == null)
        {
            Debug.LogWarning("FirstGameStartUI의 Button_TutorialSkip 참조가 비어 있어 튜토리얼 스킵 버튼을 연결할 수 없습니다.");
        }
        else
        {
            Button_TutorialSkip.onClick.RemoveListener(OnClickTutorialSkip);
            Button_TutorialSkip.onClick.AddListener(OnClickTutorialSkip);
        }
    }

    private void UnbindButtonEvents()
    {
        if (Button_TutorialStart != null)
        {
            Button_TutorialStart.onClick.RemoveListener(OnClickTutorialStart);
        }

        if (Button_TutorialSkip != null)
        {
            Button_TutorialSkip.onClick.RemoveListener(OnClickTutorialSkip);
        }
    }

    private void DisableNonButtonRaycastTargets()
    {
        Graphic[] graphicArr = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphicArr.Length; i++)
        {
            Graphic graphic = graphicArr[i];
            if (graphic == null)
            {
                continue;
            }

            if (graphic.GetComponentInParent<Button>(true) != null)
            {
                continue;
            }

            graphic.raycastTarget = false;
        }
    }

    private Button FindChildButtonIfNull(Button currentButton, string childName)
    {
        if (currentButton != null)
        {
            return currentButton;
        }

        Transform childTransform = FindChildTransformByName(transform, childName);
        if (childTransform == null)
        {
            Debug.LogWarning($"FirstGameStartUI에서 {childName} 오브젝트를 찾을 수 없습니다.");
            return null;
        }

        Button button = childTransform.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"FirstGameStartUI의 {childName} 오브젝트에 Button 컴포넌트가 없습니다.");
            return null;
        }

        return button;
    }

    private Text FindChildTextIfNull(Text currentText, string childName)
    {
        if (currentText != null)
        {
            return currentText;
        }

        Transform childTransform = FindChildTransformByName(transform, childName);
        if (childTransform == null)
        {
            Debug.LogWarning($"FirstGameStartUI에서 {childName} 오브젝트를 찾을 수 없습니다.");
            return null;
        }

        Text text = childTransform.GetComponent<Text>();
        if (text == null)
        {
            Debug.LogWarning($"FirstGameStartUI의 {childName} 오브젝트에 Text 컴포넌트가 없습니다.");
            return null;
        }

        return text;
    }

    private AudioSource FindPrologueAudioSourceIfNull(AudioSource currentAudioSource)
    {
        if (currentAudioSource != null)
        {
            return currentAudioSource;
        }

        Transform childTransform = FindChildTransformByName(transform, "PencilSound");
        if (childTransform == null)
        {
            Debug.LogWarning("FirstGameStartUI에서 PencilSound 오브젝트를 찾을 수 없습니다.");
            return null;
        }

        AudioSource audioSource = childTransform.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("FirstGameStartUI의 PencilSound 오브젝트에 AudioSource 컴포넌트가 없습니다.");
            return null;
        }

        return audioSource;
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
