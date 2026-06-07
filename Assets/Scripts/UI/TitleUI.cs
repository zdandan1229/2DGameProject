using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleUI : UIBase
{
    [SerializeField] private Button Button_NewGame;
    [SerializeField] private Button Button_LoadGame;
    [SerializeField] private Button Button_Exit;

    [Header("BG2 Alpha Effect")]
    [SerializeField] private Image Image_BG2;
    [SerializeField] private float _bg2MinAlpha = 110f;
    [SerializeField] private float _bg2MaxAlpha = 255f;
    [SerializeField] private float _bg2FadeInDuration = 2f;
    [SerializeField] private float _bg2FadeOutDuration = 2f;

    [Header("DNA Rotation Effect")]
    [SerializeField] private Image Image_DNA;
    [SerializeField] private float _dnaRotationDuration = 5f;

    [Header("Button Hover Effect")]
    [SerializeField] private Image[] Image_ButtonHoverList;
    [SerializeField] private float _buttonHoverMaxAlpha = 255f;
    [SerializeField] private float _buttonHoverFadeInDuration = 0.08f;
    [SerializeField] private float _buttonHoverScaleMultiplier = 1.1f;

    [Header("Portrait Glitch Effect")]
    [SerializeField] private Image Image_PortraitR;
    [SerializeField] private Image Image_PortraitG;
    [SerializeField] private Image Image_PortraitB;
    [SerializeField] private RectTransform[] Rect_PortraitMoveList;
    [SerializeField] private float _portraitGlitchIntervalMin = 0.4f;
    [SerializeField] private float _portraitGlitchIntervalMax = 2.2f;
    [SerializeField] private float _portraitGlitchDuration = 0.08f;
    [SerializeField] private float _portraitMoveJitterDistance = 4f;
    [SerializeField] private float _portraitRgbJitterDistance = 8f;
    [SerializeField] private float _portraitScaleJitter = 0.03f;

    [Header("Portrait Glitch Sound")]
    [SerializeField] private AudioSource Audio_PortraitGlitchSound;

    [Header("Title BGM Fade In")]
    [SerializeField] private AudioSource Audio_TitleBGM;
    [SerializeField] private float _titleBGMTargetVolume = 0.25f;
    [SerializeField] private float _titleBGMFadeInDuration = 1f;

    private Coroutine _bg2AlphaCoroutine;
    private Coroutine _dnaRotationCoroutine;
    private Coroutine _portraitGlitchCoroutine;
    private Coroutine _titleBGMFadeInCoroutine;
    private float[] _buttonHoverOriginalAlphaList;
    private Vector3[] _buttonHoverOriginalScaleList;
    private Coroutine[] _buttonHoverCoroutineList;
    private RectTransform[] _portraitRgbRectList;
    private Vector2[] _portraitMoveOriginalPositionList;
    private Vector3[] _portraitMoveOriginalScaleList;
    private int[] _portraitMoveOriginalSiblingIndexList;
    private Vector2[] _portraitRgbOriginalPositionList;
    private Vector3[] _portraitRgbOriginalScaleList;
    private int[] _portraitRgbOriginalSiblingIndexList;
    private bool[] _portraitRgbOriginalActiveList;

    private void Awake()
    {
        BindMissingReferences();
        BindButtonEvents();
        BindButtonHoverEvents();
    }

    private void OnEnable()
    {
        RequestPauseGame();
        StartBG2AlphaEffect();
        StartDNARotationEffect();
        StartPortraitGlitchEffect();
        StartTitleBGMFadeIn();
    }

    private void OnDisable()
    {
        StopBG2AlphaEffect();
        StopDNARotationEffect();
        StopPortraitGlitchEffect(false);
        StopTitleBGMFadeIn();
    }

    private void OnDestroy()
    {
        UnbindButtonEvents();
    }

    private void OnClickNewGame()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 FirstGameStartUI를 열 수 없습니다.");
            return;
        }

        FirstGameStartUI firstGameStartUI = UIManager.Instance.OpenFirstGameStartUI();
        if (firstGameStartUI == null)
        {
            return;
        }

        UIManager.Instance.CloseTitleUI();
    }

    private void OnClickLoadGame()
    {
        Debug.LogWarning("Load Game은 아직 구현되지 않았습니다.");
    }

    private void OnClickExit()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance가 없어 게임 종료를 요청할 수 없습니다.");
            return;
        }

        GameManager.Instance.ExitGame();
    }

    private void RequestPauseGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance가 없어 TitleUI에서 게임 일시정지를 요청할 수 없습니다.");
            return;
        }

        GameManager.Instance.PauseGame();
    }

    private void BindButtonEvents()
    {
        if (Button_NewGame == null)
        {
            Debug.LogWarning("TitleUI의 Button_NewGame 참조가 비어 있어 새 게임 버튼을 연결할 수 없습니다.");
        }
        else
        {
            Button_NewGame.onClick.RemoveListener(OnClickNewGame);
            Button_NewGame.onClick.AddListener(OnClickNewGame);
        }

        if (Button_LoadGame == null)
        {
            Debug.LogWarning("TitleUI의 Button_LoadGame 참조가 비어 있어 로드 버튼을 연결할 수 없습니다.");
        }
        else
        {
            Button_LoadGame.onClick.RemoveListener(OnClickLoadGame);
            Button_LoadGame.onClick.AddListener(OnClickLoadGame);
        }

        if (Button_Exit == null)
        {
            Debug.LogWarning("TitleUI의 Button_Exit 참조가 비어 있어 종료 버튼을 연결할 수 없습니다.");
        }
        else
        {
            Button_Exit.onClick.RemoveListener(OnClickExit);
            Button_Exit.onClick.AddListener(OnClickExit);
        }
    }

    private void UnbindButtonEvents()
    {
        if (Button_NewGame != null)
        {
            Button_NewGame.onClick.RemoveListener(OnClickNewGame);
        }

        if (Button_LoadGame != null)
        {
            Button_LoadGame.onClick.RemoveListener(OnClickLoadGame);
        }

        if (Button_Exit != null)
        {
            Button_Exit.onClick.RemoveListener(OnClickExit);
        }
    }

    private void BindMissingReferences()
    {
        Button_NewGame = FindChildButtonIfNull(Button_NewGame, "Button_NewGame");
        Button_LoadGame = FindChildButtonIfNull(Button_LoadGame, "Button_LoadGame");
        Button_Exit = FindChildButtonIfNull(Button_Exit, "Button_Exit");
        Image_BG2 = FindChildImageIfNull(Image_BG2, "BG2");
        Image_DNA = FindChildImageIfNull(Image_DNA, "Image_DNA");
        BindButtonHoverImagesIfEmpty();
        BindPortraitGlitchReferencesIfEmpty();
        Audio_PortraitGlitchSound = FindPortraitGlitchAudioSourceIfNull(Audio_PortraitGlitchSound);
        Audio_TitleBGM = FindTitleBGMAudioSourceIfNull(Audio_TitleBGM);
        SetPortraitGlitchSoundMute(true);
    }

    private void StartBG2AlphaEffect()
    {
        StopBG2AlphaEffect();

        if (Image_BG2 == null)
        {
            Image_BG2 = FindChildImageIfNull(Image_BG2, "BG2");
        }

        if (Image_BG2 == null)
        {
            Debug.LogWarning("TitleUI의 Image_BG2 참조가 비어 있어 BG2 투명도 연출을 재생할 수 없습니다.");
            return;
        }

        _bg2AlphaCoroutine = StartCoroutine(PlayBG2AlphaEffect());
    }

    private void StopBG2AlphaEffect()
    {
        if (_bg2AlphaCoroutine == null)
        {
            return;
        }

        StopCoroutine(_bg2AlphaCoroutine);
        _bg2AlphaCoroutine = null;
    }

    private IEnumerator PlayBG2AlphaEffect()
    {
        SetBG2Alpha(_bg2MinAlpha);

        while (true)
        {
            yield return FadeBG2Alpha(_bg2MinAlpha, _bg2MaxAlpha, _bg2FadeInDuration);
            yield return FadeBG2Alpha(_bg2MaxAlpha, _bg2MinAlpha, _bg2FadeOutDuration);
        }
    }

    private IEnumerator FadeBG2Alpha(float startAlpha, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetBG2Alpha(targetAlpha);
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, progress));
            SetBG2Alpha(alpha);
            yield return null;
        }

        SetBG2Alpha(targetAlpha);
    }

    private void SetBG2Alpha(float alpha)
    {
        if (Image_BG2 == null)
        {
            return;
        }

        Color color = Image_BG2.color;
        color.a = Mathf.Clamp(alpha, 0f, 255f) / 255f;
        Image_BG2.color = color;
    }

    private void StartDNARotationEffect()
    {
        StopDNARotationEffect();

        if (Image_DNA == null)
        {
            Image_DNA = FindChildImageIfNull(Image_DNA, "Image_DNA");
        }

        if (Image_DNA == null)
        {
            Debug.LogWarning("TitleUI is missing Image_DNA, so DNA rotation effect cannot be played.");
            return;
        }

        _dnaRotationCoroutine = StartCoroutine(PlayDNARotationEffect());
    }

    private void StopDNARotationEffect()
    {
        if (_dnaRotationCoroutine == null)
        {
            return;
        }

        StopCoroutine(_dnaRotationCoroutine);
        _dnaRotationCoroutine = null;
    }

    private IEnumerator PlayDNARotationEffect()
    {
        while (true)
        {
            yield return RotateDNAOnce();
        }
    }

    private IEnumerator RotateDNAOnce()
    {
        if (Image_DNA == null)
        {
            yield break;
        }

        RectTransform dnaRectTransform = Image_DNA.rectTransform;
        if (dnaRectTransform == null)
        {
            Debug.LogWarning("Image_DNA has no RectTransform, so DNA rotation effect cannot be played.");
            yield break;
        }

        if (_dnaRotationDuration <= 0f)
        {
            SetDNARotationX(0f);
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < _dnaRotationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / _dnaRotationDuration);
            SetDNARotationX(Mathf.Lerp(0f, 360f, progress));
            yield return null;
        }

        SetDNARotationX(0f);
    }

    private void SetDNARotationX(float rotationX)
    {
        if (Image_DNA == null)
        {
            return;
        }

        Image_DNA.rectTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    private void BindButtonHoverImagesIfEmpty()
    {
        if (Image_ButtonHoverList != null && Image_ButtonHoverList.Length > 0)
        {
            InitializeButtonHoverAlphaList();
            return;
        }

        List<Image> imageList = new List<Image>();
        AddButtonHoverImageIfExists(imageList, Button_NewGame);
        AddButtonHoverImageIfExists(imageList, Button_LoadGame);
        AddButtonHoverImageIfExists(imageList, Button_Exit);

        Image_ButtonHoverList = imageList.ToArray();
        InitializeButtonHoverAlphaList();
    }

    private void AddButtonHoverImageIfExists(List<Image> imageList, Button button)
    {
        if (imageList == null)
        {
            return;
        }

        Image image = FindButtonChildImage(button);
        if (image == null)
        {
            return;
        }

        imageList.Add(image);
    }

    private Image FindButtonChildImage(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Image[] childImages = button.GetComponentsInChildren<Image>(true);
        if (childImages == null || childImages.Length <= 0)
        {
            Debug.LogWarning($"{button.name} 아래에 hover 연출용 Image가 없습니다.");
            return null;
        }

        Image buttonRootImage = button.GetComponent<Image>();
        foreach (Image childImage in childImages)
        {
            if (childImage == null || childImage == buttonRootImage)
            {
                continue;
            }

            return childImage;
        }

        Debug.LogWarning($"{button.name} 아래에서 버튼 루트가 아닌 hover 연출용 Image를 찾지 못했습니다.");
        return null;
    }

    private void InitializeButtonHoverAlphaList()
    {
        if (Image_ButtonHoverList == null)
        {
            _buttonHoverOriginalAlphaList = null;
            _buttonHoverOriginalScaleList = null;
            _buttonHoverCoroutineList = null;
            return;
        }

        _buttonHoverOriginalAlphaList = new float[Image_ButtonHoverList.Length];
        _buttonHoverOriginalScaleList = new Vector3[Image_ButtonHoverList.Length];
        _buttonHoverCoroutineList = new Coroutine[Image_ButtonHoverList.Length];

        for (int i = 0; i < Image_ButtonHoverList.Length; i++)
        {
            if (Image_ButtonHoverList[i] == null)
            {
                continue;
            }

            _buttonHoverOriginalAlphaList[i] = Image_ButtonHoverList[i].color.a * 255f;
            _buttonHoverOriginalScaleList[i] = Image_ButtonHoverList[i].rectTransform.localScale;
        }
    }

    private void BindButtonHoverEvents()
    {
        BindButtonHoverEvent(Button_NewGame, 0);
        BindButtonHoverEvent(Button_LoadGame, 1);
        BindButtonHoverEvent(Button_Exit, 2);
    }

    private void BindButtonHoverEvent(Button button, int imageIndex)
    {
        if (button == null)
        {
            return;
        }

        EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }

        AddButtonHoverTrigger(eventTrigger, EventTriggerType.PointerEnter, () => OnEnterButtonHoverImage(imageIndex));
        AddButtonHoverTrigger(eventTrigger, EventTriggerType.PointerExit, () => OnExitButtonHoverImage(imageIndex));
    }

    private void AddButtonHoverTrigger(EventTrigger eventTrigger, EventTriggerType eventTriggerType, UnityEngine.Events.UnityAction callback)
    {
        if (eventTrigger == null || callback == null)
        {
            return;
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventTriggerType;
        entry.callback.AddListener((BaseEventData eventData) => callback());
        eventTrigger.triggers.Add(entry);
    }

    private void OnEnterButtonHoverImage(int imageIndex)
    {
        if (IsButtonHoverImageIndexValid(imageIndex) == false)
        {
            return;
        }

        StopButtonHoverCoroutine(imageIndex);
        _buttonHoverCoroutineList[imageIndex] = StartCoroutine(PlayButtonHoverEnterEffect(imageIndex));
    }

    private void OnExitButtonHoverImage(int imageIndex)
    {
        if (IsButtonHoverImageIndexValid(imageIndex) == false)
        {
            return;
        }

        StopButtonHoverCoroutine(imageIndex);
        SetButtonHoverImageAlpha(imageIndex, _buttonHoverOriginalAlphaList[imageIndex]);
        SetButtonHoverImageScale(imageIndex, _buttonHoverOriginalScaleList[imageIndex]);
    }

    private IEnumerator PlayButtonHoverEnterEffect(int imageIndex)
    {
        if (IsButtonHoverImageIndexValid(imageIndex) == false)
        {
            yield break;
        }

        Vector3 targetScale = _buttonHoverOriginalScaleList[imageIndex] * _buttonHoverScaleMultiplier;
        if (_buttonHoverFadeInDuration <= 0f)
        {
            SetButtonHoverImageAlpha(imageIndex, _buttonHoverMaxAlpha);
            SetButtonHoverImageScale(imageIndex, targetScale);
            yield break;
        }

        float startAlpha = Image_ButtonHoverList[imageIndex].color.a * 255f;
        Vector3 startScale = Image_ButtonHoverList[imageIndex].rectTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < _buttonHoverFadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / _buttonHoverFadeInDuration);
            float alpha = Mathf.Lerp(startAlpha, _buttonHoverMaxAlpha, progress);
            Vector3 scale = Vector3.Lerp(startScale, targetScale, progress);
            SetButtonHoverImageAlpha(imageIndex, alpha);
            SetButtonHoverImageScale(imageIndex, scale);
            yield return null;
        }

        SetButtonHoverImageAlpha(imageIndex, _buttonHoverMaxAlpha);
        SetButtonHoverImageScale(imageIndex, targetScale);
        _buttonHoverCoroutineList[imageIndex] = null;
    }

    private void StopButtonHoverCoroutine(int imageIndex)
    {
        if (_buttonHoverCoroutineList == null || imageIndex < 0 || imageIndex >= _buttonHoverCoroutineList.Length)
        {
            return;
        }

        if (_buttonHoverCoroutineList[imageIndex] == null)
        {
            return;
        }

        StopCoroutine(_buttonHoverCoroutineList[imageIndex]);
        _buttonHoverCoroutineList[imageIndex] = null;
    }

    private bool IsButtonHoverImageIndexValid(int imageIndex)
    {
        if (Image_ButtonHoverList == null || _buttonHoverOriginalAlphaList == null || _buttonHoverOriginalScaleList == null || _buttonHoverCoroutineList == null)
        {
            Debug.LogWarning("TitleUI의 버튼 hover 이미지 목록이 준비되지 않았습니다.");
            return false;
        }

        if (imageIndex < 0 || imageIndex >= Image_ButtonHoverList.Length)
        {
            Debug.LogWarning($"TitleUI의 버튼 hover 이미지 인덱스가 범위를 벗어났습니다: {imageIndex}");
            return false;
        }

        if (Image_ButtonHoverList[imageIndex] == null)
        {
            Debug.LogWarning($"TitleUI의 버튼 hover 이미지 {imageIndex}번 참조가 비어 있습니다.");
            return false;
        }

        return true;
    }

    private void SetButtonHoverImageAlpha(int imageIndex, float alpha)
    {
        if (Image_ButtonHoverList == null || imageIndex < 0 || imageIndex >= Image_ButtonHoverList.Length)
        {
            return;
        }

        Image image = Image_ButtonHoverList[imageIndex];
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp(alpha, 0f, 255f) / 255f;
        image.color = color;
    }

    private void SetButtonHoverImageScale(int imageIndex, Vector3 scale)
    {
        if (Image_ButtonHoverList == null || imageIndex < 0 || imageIndex >= Image_ButtonHoverList.Length)
        {
            return;
        }

        Image image = Image_ButtonHoverList[imageIndex];
        if (image == null)
        {
            return;
        }

        image.rectTransform.localScale = scale;
    }

    private void BindPortraitGlitchReferencesIfEmpty()
    {
        Image_PortraitR = FindChildImageIfNull(Image_PortraitR, "PortraitImageR");
        Image_PortraitG = FindChildImageIfNull(Image_PortraitG, "PortraitImageG");
        Image_PortraitB = FindChildImageIfNull(Image_PortraitB, "PortraitImageB");

        if (Rect_PortraitMoveList == null || Rect_PortraitMoveList.Length <= 0)
        {
            List<RectTransform> portraitMoveList = new List<RectTransform>();
            AddPortraitMoveIfExists(portraitMoveList, "PortraitMove1");
            AddPortraitMoveIfExists(portraitMoveList, "PortraitMove2");
            Rect_PortraitMoveList = portraitMoveList.ToArray();
        }

        InitializePortraitGlitchOriginalState();
    }

    private void AddPortraitMoveIfExists(List<RectTransform> portraitMoveList, string childName)
    {
        if (portraitMoveList == null)
        {
            return;
        }

        Transform childTransform = FindChildTransformByName(transform, childName);
        if (childTransform == null)
        {
            Debug.LogWarning($"TitleUI에서 {childName} 오브젝트를 찾을 수 없어 Portrait Glitch 연출에서 제외합니다.");
            return;
        }

        RectTransform rectTransform = childTransform.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning($"TitleUI의 {childName} 오브젝트에 RectTransform이 없어 Portrait Glitch 연출에서 제외합니다.");
            return;
        }

        portraitMoveList.Add(rectTransform);
    }

    private void InitializePortraitGlitchOriginalState()
    {
        _portraitRgbRectList = new RectTransform[]
        {
            GetImageRectTransform(Image_PortraitR),
            GetImageRectTransform(Image_PortraitG),
            GetImageRectTransform(Image_PortraitB)
        };

        InitializePortraitMoveOriginalState();
        InitializePortraitRgbOriginalState();
    }

    private void InitializePortraitMoveOriginalState()
    {
        if (Rect_PortraitMoveList == null)
        {
            _portraitMoveOriginalPositionList = null;
            _portraitMoveOriginalScaleList = null;
            _portraitMoveOriginalSiblingIndexList = null;
            return;
        }

        _portraitMoveOriginalPositionList = new Vector2[Rect_PortraitMoveList.Length];
        _portraitMoveOriginalScaleList = new Vector3[Rect_PortraitMoveList.Length];
        _portraitMoveOriginalSiblingIndexList = new int[Rect_PortraitMoveList.Length];

        for (int i = 0; i < Rect_PortraitMoveList.Length; i++)
        {
            RectTransform rectTransform = Rect_PortraitMoveList[i];
            if (rectTransform == null)
            {
                continue;
            }

            _portraitMoveOriginalPositionList[i] = rectTransform.anchoredPosition;
            _portraitMoveOriginalScaleList[i] = rectTransform.localScale;
            _portraitMoveOriginalSiblingIndexList[i] = rectTransform.GetSiblingIndex();
        }
    }

    private void InitializePortraitRgbOriginalState()
    {
        if (_portraitRgbRectList == null)
        {
            _portraitRgbOriginalPositionList = null;
            _portraitRgbOriginalScaleList = null;
            _portraitRgbOriginalSiblingIndexList = null;
            _portraitRgbOriginalActiveList = null;
            return;
        }

        _portraitRgbOriginalPositionList = new Vector2[_portraitRgbRectList.Length];
        _portraitRgbOriginalScaleList = new Vector3[_portraitRgbRectList.Length];
        _portraitRgbOriginalSiblingIndexList = new int[_portraitRgbRectList.Length];
        _portraitRgbOriginalActiveList = new bool[_portraitRgbRectList.Length];

        for (int i = 0; i < _portraitRgbRectList.Length; i++)
        {
            RectTransform rectTransform = _portraitRgbRectList[i];
            if (rectTransform == null)
            {
                continue;
            }

            _portraitRgbOriginalPositionList[i] = rectTransform.anchoredPosition;
            _portraitRgbOriginalScaleList[i] = rectTransform.localScale;
            _portraitRgbOriginalSiblingIndexList[i] = rectTransform.GetSiblingIndex();
            _portraitRgbOriginalActiveList[i] = rectTransform.gameObject.activeSelf;
        }
    }

    private RectTransform GetImageRectTransform(Image image)
    {
        if (image == null)
        {
            return null;
        }

        return image.rectTransform;
    }

    private void StartPortraitGlitchEffect()
    {
        StopPortraitGlitchEffect();

        if (HasPortraitGlitchTarget() == false)
        {
            Debug.LogWarning("TitleUI의 Portrait Glitch 대상 참조가 비어 있어 연출을 재생할 수 없습니다.");
            return;
        }

        _portraitGlitchCoroutine = StartCoroutine(PlayPortraitGlitchEffect());
    }

    private void StopPortraitGlitchEffect()
    {
        StopPortraitGlitchEffect(true);
    }

    private void StopPortraitGlitchEffect(bool shouldRestoreState)
    {
        if (_portraitGlitchCoroutine != null)
        {
            StopCoroutine(_portraitGlitchCoroutine);
            _portraitGlitchCoroutine = null;
        }

        if (shouldRestoreState && gameObject.activeInHierarchy)
        {
            RestorePortraitGlitchState();
        }

        SetPortraitGlitchSoundMute(true);
    }

    private bool HasPortraitGlitchTarget()
    {
        bool hasMoveTarget = Rect_PortraitMoveList != null && Rect_PortraitMoveList.Length > 0;
        bool hasRgbTarget = _portraitRgbRectList != null && _portraitRgbRectList.Length > 0;
        return hasMoveTarget || hasRgbTarget;
    }

    private IEnumerator PlayPortraitGlitchEffect()
    {
        while (true)
        {
            float waitTime = Random.Range(_portraitGlitchIntervalMin, _portraitGlitchIntervalMax);
            waitTime = Mathf.Max(0.01f, waitTime);
            yield return new WaitForSecondsRealtime(waitTime);

            yield return PlayPortraitGlitchOnce();
            RestorePortraitGlitchState();
        }
    }

    private IEnumerator PlayPortraitGlitchOnce()
    {
        float duration = Mathf.Max(0.01f, _portraitGlitchDuration);
        float elapsedTime = 0f;
        SetPortraitGlitchSoundMute(false);

        while (elapsedTime < duration)
        {
            ApplyPortraitMoveGlitch();
            ApplyPortraitRgbGlitch();

            float frameTime = Random.Range(0.015f, 0.035f);
            elapsedTime += frameTime;
            yield return new WaitForSecondsRealtime(frameTime);
        }

        SetPortraitGlitchSoundMute(true);
    }

    private void ApplyPortraitMoveGlitch()
    {
        if (Rect_PortraitMoveList == null || _portraitMoveOriginalPositionList == null || _portraitMoveOriginalScaleList == null)
        {
            return;
        }

        for (int i = 0; i < Rect_PortraitMoveList.Length; i++)
        {
            RectTransform rectTransform = Rect_PortraitMoveList[i];
            if (rectTransform == null)
            {
                continue;
            }

            rectTransform.anchoredPosition = _portraitMoveOriginalPositionList[i] + GetRandomJitter(_portraitMoveJitterDistance);
            rectTransform.localScale = _portraitMoveOriginalScaleList[i] * GetRandomScaleMultiplier();

            if (Random.value < 0.35f)
            {
                rectTransform.SetAsLastSibling();
            }
        }
    }

    private void ApplyPortraitRgbGlitch()
    {
        if (_portraitRgbRectList == null || _portraitRgbOriginalPositionList == null || _portraitRgbOriginalScaleList == null)
        {
            return;
        }

        for (int i = 0; i < _portraitRgbRectList.Length; i++)
        {
            RectTransform rectTransform = _portraitRgbRectList[i];
            if (rectTransform == null)
            {
                continue;
            }

            bool shouldShow = Random.value > 0.2f;
            rectTransform.gameObject.SetActive(shouldShow);
            rectTransform.anchoredPosition = _portraitRgbOriginalPositionList[i] + GetRandomJitter(_portraitRgbJitterDistance);
            rectTransform.localScale = _portraitRgbOriginalScaleList[i] * GetRandomScaleMultiplier();

            if (Random.value < 0.5f)
            {
                rectTransform.SetAsLastSibling();
            }
        }
    }

    private Vector2 GetRandomJitter(float distance)
    {
        float jitterDistance = Mathf.Max(0f, distance);
        return new Vector2(Random.Range(-jitterDistance, jitterDistance), Random.Range(-jitterDistance, jitterDistance));
    }

    private float GetRandomScaleMultiplier()
    {
        float scaleJitter = Mathf.Max(0f, _portraitScaleJitter);
        return Random.Range(1f - scaleJitter, 1f + scaleJitter);
    }

    private void RestorePortraitGlitchState()
    {
        RestorePortraitMoveState();
        RestorePortraitRgbState();
    }

    private void RestorePortraitMoveState()
    {
        if (Rect_PortraitMoveList == null || _portraitMoveOriginalPositionList == null || _portraitMoveOriginalScaleList == null || _portraitMoveOriginalSiblingIndexList == null)
        {
            return;
        }

        for (int i = 0; i < Rect_PortraitMoveList.Length; i++)
        {
            RectTransform rectTransform = Rect_PortraitMoveList[i];
            if (rectTransform == null)
            {
                continue;
            }

            rectTransform.anchoredPosition = _portraitMoveOriginalPositionList[i];
            rectTransform.localScale = _portraitMoveOriginalScaleList[i];
            rectTransform.SetSiblingIndex(_portraitMoveOriginalSiblingIndexList[i]);
        }
    }

    private void RestorePortraitRgbState()
    {
        if (_portraitRgbRectList == null || _portraitRgbOriginalPositionList == null || _portraitRgbOriginalScaleList == null || _portraitRgbOriginalSiblingIndexList == null || _portraitRgbOriginalActiveList == null)
        {
            return;
        }

        for (int i = 0; i < _portraitRgbRectList.Length; i++)
        {
            RectTransform rectTransform = _portraitRgbRectList[i];
            if (rectTransform == null)
            {
                continue;
            }

            rectTransform.anchoredPosition = _portraitRgbOriginalPositionList[i];
            rectTransform.localScale = _portraitRgbOriginalScaleList[i];
            rectTransform.SetSiblingIndex(_portraitRgbOriginalSiblingIndexList[i]);
            rectTransform.gameObject.SetActive(_portraitRgbOriginalActiveList[i]);
        }
    }

    private void StartTitleBGMFadeIn()
    {
        StopTitleBGMFadeIn();

        if (Audio_TitleBGM == null)
        {
            Audio_TitleBGM = FindTitleBGMAudioSourceIfNull(Audio_TitleBGM);
        }

        if (Audio_TitleBGM == null)
        {
            Debug.LogWarning("TitleUI의 Audio_TitleBGM 참조가 비어 있어 BGM 페이드인을 재생할 수 없습니다.");
            return;
        }

        _titleBGMFadeInCoroutine = StartCoroutine(PlayTitleBGMFadeIn());
    }

    private void StopTitleBGMFadeIn()
    {
        if (_titleBGMFadeInCoroutine == null)
        {
            return;
        }

        StopCoroutine(_titleBGMFadeInCoroutine);
        _titleBGMFadeInCoroutine = null;
    }

    private IEnumerator PlayTitleBGMFadeIn()
    {
        if (Audio_TitleBGM == null)
        {
            yield break;
        }

        float targetVolume = Mathf.Clamp01(_titleBGMTargetVolume);
        Audio_TitleBGM.volume = 0f;

        if (Audio_TitleBGM.isPlaying == false)
        {
            Audio_TitleBGM.Play();
        }

        if (_titleBGMFadeInDuration <= 0f)
        {
            Audio_TitleBGM.volume = targetVolume;
            _titleBGMFadeInCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < _titleBGMFadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / _titleBGMFadeInDuration);
            Audio_TitleBGM.volume = Mathf.Lerp(0f, targetVolume, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        Audio_TitleBGM.volume = targetVolume;
        _titleBGMFadeInCoroutine = null;
    }

    private void SetPortraitGlitchSoundMute(bool isMute)
    {
        if (Audio_PortraitGlitchSound == null)
        {
            Audio_PortraitGlitchSound = FindPortraitGlitchAudioSourceIfNull(Audio_PortraitGlitchSound);
        }

        if (Audio_PortraitGlitchSound == null)
        {
            return;
        }

        if (isMute)
        {
            Audio_PortraitGlitchSound.mute = true;
            return;
        }

        if (Audio_PortraitGlitchSound.enabled == false || Audio_PortraitGlitchSound.gameObject.activeInHierarchy == false)
        {
            return;
        }

        if (Audio_PortraitGlitchSound.isPlaying == false)
        {
            Audio_PortraitGlitchSound.Play();
        }

        Audio_PortraitGlitchSound.mute = false;
    }

    private AudioSource FindTitleBGMAudioSourceIfNull(AudioSource currentAudioSource)
    {
        if (currentAudioSource != null)
        {
            return currentAudioSource;
        }

        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null && audioSource.mute == false)
            {
                return audioSource;
            }
        }

        Debug.LogWarning("TitleUI에서 BGM용 AudioSource를 찾을 수 없습니다.");
        return null;
    }

    private AudioSource FindPortraitGlitchAudioSourceIfNull(AudioSource currentAudioSource)
    {
        if (currentAudioSource != null)
        {
            return currentAudioSource;
        }

        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null && audioSource.mute == true)
            {
                return audioSource;
            }
        }

        Debug.LogWarning("TitleUI에서 Portrait Glitch Sound용 muted AudioSource를 찾을 수 없습니다.");
        return null;
    }

    private AudioSource FindAudioSourceIfNull(AudioSource currentAudioSource)
    {
        if (currentAudioSource != null)
        {
            return currentAudioSource;
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("TitleUI 오브젝트에 AudioSource 컴포넌트가 없습니다.");
            return null;
        }

        return audioSource;
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
            Debug.LogWarning($"TitleUI에서 {childName} 오브젝트를 찾을 수 없습니다.");
            return null;
        }

        Image image = childTransform.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"TitleUI의 {childName} 오브젝트에 Image 컴포넌트가 없습니다.");
            return null;
        }

        return image;
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
            Debug.LogWarning($"TitleUI에서 {childName} 오브젝트를 찾을 수 없습니다.");
            return null;
        }

        Button button = childTransform.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"TitleUI의 {childName} 오브젝트에 Button 컴포넌트가 없습니다.");
            return null;
        }

        return button;
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
