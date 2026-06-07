using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueUI : UIBase, IPointerClickHandler, IInspectObjectCompleteHandler, IInspectObjectCompleteOptionProvider
{
    private const char PlayerNarrationPrefix = '#';
    private const string PlayerNarrationName = "나";

    [Header("Background")]
    [SerializeField] private GameObject Layout_BG;

    [Header("Name")]
    [SerializeField] private GameObject Layout_CharacterName;
    [SerializeField] private Text Text_Character;

    [Header("Dialogue")]
    [SerializeField] private GameObject Layout_Description;
    [SerializeField] private Text Text_Description_Portrait;
    [SerializeField] private Text Text_Description_WithoutPortrait;
    [SerializeField] private Image Image_Portrait;
    [SerializeField] private RectTransform Rect_PortraitBox;

    [Header("Choice")]
    [SerializeField] private GameObject Layout_SelectionRoot;
    [SerializeField] private Transform SelectionSlot_1;
    [SerializeField] private Transform SelectionSlot_2;
    [SerializeField] private Transform SelectionSlot_3;
    [SerializeField] private Button Prefab_SelectionButton;

    [Header("Optional")]
    [SerializeField] private Button Button_Next;
    [SerializeField] private TextTypingEffect TypingEffect_Description;

    private string _currentDialogueId;
    private string _objectNameTokenValue;
    private Action _onCompleteCallback;
    private bool _isDialogueSoundStateStarted;
    private Queue<string> _descriptionQueue = new Queue<string>();
    private List<GameObject> _createdSelectionButtonList = new List<GameObject>();
    private Text _currentDescriptionText;
    private bool _isTextWindowHidden;
    private bool _isWaitingInspectObject;
    private bool _isCurrentDialogueJournalNoticeQueued;
    private bool _wasBackgroundActiveBeforeHide;
    private bool _wasCharacterNameActiveBeforeHide;
    private bool _wasPortraitActiveBeforeHide;
    private bool _wasDescriptionActiveBeforeHide;
    private bool _wasNextButtonActiveBeforeHide;
    private string _pendingInspectNextDialogueId;
    private readonly int[][] _selectionSlotIndexMap =
    {
        null,
        new[] { 1 },
        new[] { 0, 2 },
        new[] { 0, 1, 2 }
    };

    private bool IsSelectionShowing
    {
        get
        {
            return Layout_SelectionRoot != null && Layout_SelectionRoot.activeSelf;
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 존재하지 않아 게임 정지를 요청하지 못했습니다.");
        }

        if (Button_Next != null)
        {
            Button_Next.onClick.AddListener(RequestNextDialogue);
        }
    }

    private void OnDisable()
    {
        RestoreDialogueSoundStateIfStarted();
        RestoreTextWindowIfHidden();
        ClearDescriptionTyping();
        _isTextWindowHidden = false;
        _isWaitingInspectObject = false;
        _isCurrentDialogueJournalNoticeQueued = false;
        _pendingInspectNextDialogueId = string.Empty;

        if (Button_Next != null)
        {
            Button_Next.onClick.RemoveListener(RequestNextDialogue);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 존재하지 않아 게임 재개를 요청하지 못했습니다.");
        }
    }

    private void Update()
    {
        if (_isWaitingInspectObject)
        {
            return;
        }

        if (InputManager.GetTextWindowToggleDown())
        {
            ToggleTextWindowVisible();
            return;
        }

        if (_isTextWindowHidden)
        {
            return;
        }

        if (InputManager.GetDialogueNextDown())
        {
            RequestNextDialogue();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isWaitingInspectObject)
        {
            return;
        }

        if (_isTextWindowHidden)
        {
            return;
        }

        RequestNextDialogue();
    }

    public void StartDialogue(string dialogueId)
    {
        StartDialogue(dialogueId, string.Empty);
    }

    public void StartDialogue(string dialogueId, string objectName)
    {
        StartDialogue(dialogueId, objectName, null);
    }

    public void StartDialogue(string dialogueId, string objectName, Action onCompleteCallback)
    {
        _onCompleteCallback = onCompleteCallback;
        StartDialogueSoundState();
        StartDialogueInternal(dialogueId, objectName);
    }

    private void StartDialogueInternal(string dialogueId, string objectName)
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 다이얼로그를 시작할 수 없습니다.");
            CloseDialogueUI();
            return;
        }

        DialogueData dialogueData = GameDataManager.Instance.GetDialogueData(dialogueId);
        if (dialogueData == null)
        {
            Debug.LogWarning($"다이얼로그 데이터가 존재하지 않습니다 : {dialogueId}");
            CloseDialogueUI();
            return;
        }

        _currentDialogueId = dialogueId;
        _objectNameTokenValue = objectName ?? string.Empty;
        _isCurrentDialogueJournalNoticeQueued = false;
        ResetDialogueUI();
        ApplyDialogueAffinity(dialogueData);
        ApplyDialogueBgmCommand(dialogueData);
        bool isPortraitShown = SetPortrait(dialogueData);
        SetDescriptionTextByPortrait(isPortraitShown);

        if (ShouldOpenInspectObjectImmediately(dialogueData))
        {
            RefreshNextButtonVisible(false);
            TryOpenInspectObjectFromDialogue(dialogueData);
            return;
        }

        PrepareDescriptionQueue(dialogueData.Description);
        ShowNextDescriptionPage();
        RefreshNextButtonVisible(true);
    }

    public void RequestNextDialogue()
    {
        if (_isWaitingInspectObject)
        {
            return;
        }

        if (IsSelectionShowing)
        {
            return;
        }

        if (IsDescriptionTyping())
        {
            SkipDescriptionTyping();
            return;
        }

        bool isNextDescriptionExist = ShowNextDescriptionPage();
        if (isNextDescriptionExist)
        {
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 다음 다이얼로그를 진행할 수 없습니다.");
            CloseDialogueUI();
            return;
        }

        DialogueData currentDialogueData = GameDataManager.Instance.GetDialogueData(_currentDialogueId);
        if (TryQueueJournalNoticeForDialogue(currentDialogueData))
        {
            return;
        }

        if (HasSelectionData(currentDialogueData))
        {
            MarkDialogueFlag(currentDialogueData);
            ShowSelection(currentDialogueData);
            RefreshNextButtonVisible(false);
            return;
        }

        if (TryOpenInspectObjectFromDialogue(currentDialogueData))
        {
            return;
        }

        MarkDialogueFlag(currentDialogueData);
        bool isNextDialogueExist = CheckAndStartNextDialogue();
        if (isNextDialogueExist == false)
        {
            CompleteDialogueAndCloseUI();
        }
    }

    private void ResetDialogueUI()
    {
        RestoreTextWindowIfHidden();
        ClearDescriptionTyping();
        _isTextWindowHidden = false;

        _descriptionQueue.Clear();
        ClearSelectionButtons();
        _isWaitingInspectObject = false;
        _pendingInspectNextDialogueId = string.Empty;

        if (Layout_SelectionRoot != null)
        {
            Layout_SelectionRoot.SetActive(false);
        }

        ClearDescriptionText(Text_Description_Portrait);
        ClearDescriptionText(Text_Description_WithoutPortrait);
        SetDescriptionTextVisible(Text_Description_Portrait, false);
        SetDescriptionTextVisible(Text_Description_WithoutPortrait, false);
        _currentDescriptionText = null;
    }

    private void ToggleTextWindowVisible()
    {
        if (_isTextWindowHidden)
        {
            RestoreTextWindowVisible();
            return;
        }

        HideTextWindow();
    }

    private void RestoreTextWindowIfHidden()
    {
        if (_isTextWindowHidden == false)
        {
            return;
        }

        RestoreTextWindowVisible();
    }

    private void HideTextWindow()
    {
        _wasBackgroundActiveBeforeHide = IsGameObjectActive(Layout_BG);
        _wasCharacterNameActiveBeforeHide = IsGameObjectActive(Layout_CharacterName);
        _wasPortraitActiveBeforeHide = Image_Portrait != null && Image_Portrait.gameObject.activeSelf;
        _wasDescriptionActiveBeforeHide = IsGameObjectActive(Layout_Description);
        _wasNextButtonActiveBeforeHide = Button_Next != null && Button_Next.gameObject.activeSelf;

        SetGameObjectActive(Layout_BG, false);
        SetGameObjectActive(Layout_CharacterName, false);
        SetGameObjectActive(Image_Portrait != null ? Image_Portrait.gameObject : null, false);
        SetGameObjectActive(Layout_Description, false);
        SetGameObjectActive(Button_Next != null ? Button_Next.gameObject : null, false);

        _isTextWindowHidden = true;
    }

    private void RestoreTextWindowVisible()
    {
        SetGameObjectActive(Layout_BG, _wasBackgroundActiveBeforeHide);
        SetGameObjectActive(Layout_CharacterName, _wasCharacterNameActiveBeforeHide);
        SetGameObjectActive(Image_Portrait != null ? Image_Portrait.gameObject : null, _wasPortraitActiveBeforeHide);
        SetGameObjectActive(Layout_Description, _wasDescriptionActiveBeforeHide);
        SetGameObjectActive(Button_Next != null ? Button_Next.gameObject : null, _wasNextButtonActiveBeforeHide);

        _isTextWindowHidden = false;
    }

    private bool IsGameObjectActive(GameObject targetObject)
    {
        return targetObject != null && targetObject.activeSelf;
    }

    private void SetGameObjectActive(GameObject targetObject, bool isActive)
    {
        if (targetObject == null)
        {
            return;
        }

        targetObject.SetActive(isActive);
    }

    private void PrepareDescriptionQueue(string description)
    {
        EnqueueDescription(description);
    }

    private void EnqueueDescription(string description)
    {
        string replacedDescription = ReplaceDescriptionToken(description);
        if (string.IsNullOrEmpty(description))
        {
            _descriptionQueue.Enqueue(string.Empty);
            return;
        }

        if (replacedDescription.Contains("<np>"))
        {
            string[] dialogueDescriptionList = replacedDescription.Split("<np>");
            for (int i = 0; i < dialogueDescriptionList.Length; i++)
            {
                _descriptionQueue.Enqueue(dialogueDescriptionList[i]);
            }
            return;
        }

        _descriptionQueue.Enqueue(replacedDescription);
    }

    private string ReplaceDescriptionToken(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(_objectNameTokenValue))
        {
            return description;
        }

        return description.Replace("{ObjectName}", _objectNameTokenValue);
    }

    private bool ShowNextDescriptionPage()
    {
        bool isNextDescriptionExist = (_descriptionQueue.Count > 0);
        if (isNextDescriptionExist == false)
        {
            return false;
        }

        string desc = _descriptionQueue.Dequeue();
        RefreshCharacterNameForDescriptionPage(desc);
        desc = RemovePlayerNarrationPrefix(desc);
        SetCurrentDialogueDescription(desc);
        return true;
    }

    private bool CheckAndStartNextDialogue()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 다음 다이얼로그 데이터를 확인할 수 없습니다.");
            return false;
        }

        DialogueData dialogueData = GameDataManager.Instance.GetDialogueData(_currentDialogueId);
        if (dialogueData == null)
        {
            Debug.LogWarning($"다이얼로그 데이터가 존재하지 않습니다 : {_currentDialogueId}");
            return false;
        }

        return TryStartDialogue(dialogueData.NextDialogueId);
    }

    private bool TryQueueJournalNoticeForDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null || _isCurrentDialogueJournalNoticeQueued)
        {
            return false;
        }

        if (string.IsNullOrEmpty(dialogueData.CompleteJournalDataId))
        {
            return false;
        }

        JournalData journalData = GetDialogueJournalData(dialogueData.CompleteJournalDataId);
        if (journalData == null)
        {
            _isCurrentDialogueJournalNoticeQueued = true;
            return false;
        }

        if (JournalManager.Instance == null)
        {
            Debug.LogWarning("JournalManager.Instance가 없어 대화 완료 일지를 등록할 수 없습니다.");
            _isCurrentDialogueJournalNoticeQueued = true;
            return false;
        }

        bool isAdded = JournalManager.Instance.AddJournal(journalData.Id);
        if (isAdded == false && JournalManager.Instance.HasJournal(journalData.Id) == false)
        {
            _isCurrentDialogueJournalNoticeQueued = true;
            return false;
        }

        _isCurrentDialogueJournalNoticeQueued = true;
        EnqueueDescription($"{journalData.Title}의 내용이 일지에 등록되었다.");
        ShowNextDescriptionPage();
        return true;
    }

    private JournalData GetDialogueJournalData(string journalDataId)
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 대화 완료 일지 데이터를 조회할 수 없습니다.");
            return null;
        }

        JournalData journalData = GameDataManager.Instance.GetJournalData(journalDataId);
        if (journalData == null)
        {
            Debug.LogWarning($"대화 완료에 연결된 일지 데이터가 존재하지 않습니다 : {journalDataId}");
            return null;
        }

        return journalData;
    }

    private bool TryStartDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId) == false)
        {
            if (GameDataManager.Instance == null)
            {
                Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 다이얼로그를 진행할 수 없습니다.");
                return false;
            }

            DialogueData nextDialogueData = GameDataManager.Instance.GetDialogueData(dialogueId);
            if (nextDialogueData == null)
            {
                Debug.LogWarning($"다음 다이얼로그 데이터가 존재하지 않습니다 : {dialogueId}");
                return false;
            }

            StartDialogueInternal(dialogueId, _objectNameTokenValue);
            return true;
        }

        return false;
    }

    private void SetCharacterName(string characterDataId)
    {
        bool isActive = string.IsNullOrEmpty(characterDataId) == false;

        if (Layout_CharacterName != null)
        {
            Layout_CharacterName.SetActive(isActive);
        }

        if (isActive == false || Text_Character == null)
        {
            return;
        }

        CharacterData characterData = GameDataManager.Instance.GetCharacterData(characterDataId);
        if (characterData != null)
        {
            Text_Character.text = characterData.Name;
        }
    }

    private void SetCharacterNameText(string characterName)
    {
        bool isActive = string.IsNullOrEmpty(characterName) == false;

        if (Layout_CharacterName != null)
        {
            Layout_CharacterName.SetActive(isActive);
        }

        if (isActive == false || Text_Character == null)
        {
            return;
        }

        Text_Character.text = characterName;
    }

    private void RefreshCharacterNameForDescriptionPage(string description)
    {
        if (IsPlayerNarrationPage(description))
        {
            SetCharacterNameText(PlayerNarrationName);
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 현재 대사의 캐릭터 이름을 확인할 수 없습니다.");
            SetCharacterNameText(string.Empty);
            return;
        }

        DialogueData dialogueData = GameDataManager.Instance.GetDialogueData(_currentDialogueId);
        if (dialogueData == null)
        {
            Debug.LogWarning($"다이얼로그 데이터가 존재하지 않아 캐릭터 이름을 표시할 수 없습니다 : {_currentDialogueId}");
            SetCharacterNameText(string.Empty);
            return;
        }

        SetCharacterName(dialogueData.CharacterDataId);
    }

    private bool IsPlayerNarrationPage(string description)
    {
        return string.IsNullOrEmpty(description) == false && description[0] == PlayerNarrationPrefix;
    }

    private string RemovePlayerNarrationPrefix(string description)
    {
        if (IsPlayerNarrationPage(description) == false)
        {
            return description;
        }

        return description.Substring(1);
    }

    private bool SetPortrait(DialogueData dialogueData)
    {
        if (Image_Portrait == null)
        {
            Debug.LogWarning("DialogueUI의 Image_Portrait 참조가 누락되어 초상화를 표시할 수 없습니다.");
            return false;
        }

        string portraitPath = dialogueData.CharacterPortraitPath;

        if (string.IsNullOrEmpty(portraitPath))
        {
            portraitPath = GetDefaultDialoguePortraitPath(dialogueData.CharacterDataId);
        }

        if (string.IsNullOrEmpty(portraitPath))
        {
            Image_Portrait.gameObject.SetActive(false);
            Image_Portrait.sprite = null;
            return false;
        }

        Sprite portraitSprite = GameUtil.LoadSpriteCanBeNull(portraitPath);
        if (portraitSprite == null)
        {
            Image_Portrait.gameObject.SetActive(false);
            Image_Portrait.sprite = null;
            return false;
        }

        Image_Portrait.gameObject.SetActive(true);
        Image_Portrait.sprite = portraitSprite;
        FitPortraitToBox(portraitSprite);
        return true;
    }

    private void ApplyDialogueAffinity(DialogueData dialogueData)
    {
        if (dialogueData == null || dialogueData.AddLikeAbility == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(dialogueData.CharacterDataId))
        {
            Debug.LogWarning($"호감도 증감값이 있지만 CharacterDataId가 비어 있어 적용할 수 없습니다 : {dialogueData.Id}");
            return;
        }

        if (NPCStatusController.Instance == null)
        {
            Debug.LogWarning("NPCStatusController.Instance가 없어 호감도 증감값을 적용할 수 없습니다.");
            return;
        }

        NPCStatusController.Instance.AddAffinity(dialogueData.CharacterDataId, dialogueData.AddLikeAbility);
    }

    private void MarkDialogueFlag(DialogueData dialogueData)
    {
        if (dialogueData == null || string.IsNullOrEmpty(dialogueData.DialogueFlagId))
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning($"ScenarioManager.Instance가 없어 다이얼로그 플래그를 기록할 수 없습니다 : {dialogueData.DialogueFlagId}");
            return;
        }

        ScenarioManager.Instance.MarkFlag(dialogueData.DialogueFlagId);
    }

    private string GetDefaultDialoguePortraitPath(string characterDataId)
    {
        if (string.IsNullOrEmpty(characterDataId))
        {
            return string.Empty;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 기본 대화 초상화를 확인할 수 없습니다.");
            return string.Empty;
        }

        CharacterData characterData = GameDataManager.Instance.GetCharacterData(characterDataId);
        if (characterData == null)
        {
            Debug.LogWarning($"캐릭터 데이터가 없어 기본 대화 초상화를 확인할 수 없습니다 : {characterDataId}");
            return string.Empty;
        }

        return GetDialoguePortraitPathByCharacterState(characterData);
    }

    private string GetDialoguePortraitPathByCharacterState(CharacterData characterData)
    {
        if (characterData == null)
        {
            return string.Empty;
        }

        if (NPCStatusController.Instance == null)
        {
            return characterData.DialoguePortraitDefault;
        }

        string characterDataId = characterData.Id;
        if (NPCStatusController.Instance.IsMadness(characterDataId) && string.IsNullOrEmpty(characterData.DialoguePortraitMadnessDefault) == false)
        {
            return characterData.DialoguePortraitMadnessDefault;
        }

        if (NPCStatusController.Instance.IsSick(characterDataId) && string.IsNullOrEmpty(characterData.DialoguePortraitSickDefault) == false)
        {
            return characterData.DialoguePortraitSickDefault;
        }

        if (NPCStatusController.Instance.IsLike(characterDataId) && string.IsNullOrEmpty(characterData.DialoguePortraitLikeDefault) == false)
        {
            return characterData.DialoguePortraitLikeDefault;
        }

        if (NPCStatusController.Instance.IsHate(characterDataId) && string.IsNullOrEmpty(characterData.DialoguePortraitHateDefault) == false)
        {
            return characterData.DialoguePortraitHateDefault;
        }

        return characterData.DialoguePortraitDefault;
    }

    private void FitPortraitToBox(Sprite portraitSprite)
    {
        if (portraitSprite == null)
        {
            Debug.LogWarning("초상화 스프라이트가 비어 있어 초상화 크기를 맞출 수 없습니다.");
            return;
        }

        RectTransform portraitRectTransform = Image_Portrait.rectTransform;
        if (portraitRectTransform == null)
        {
            Debug.LogWarning("Image_Portrait에 RectTransform이 없어 초상화 크기를 맞출 수 없습니다.");
            return;
        }

        RectTransform portraitBoxRectTransform = GetPortraitBoxRectTransform(portraitRectTransform);
        if (portraitBoxRectTransform == null)
        {
            Debug.LogWarning("초상화가 들어갈 Layout_Portrait 박스를 찾을 수 없어 초상화 크기를 맞출 수 없습니다.");
            return;
        }

        Vector2 boxSize = portraitBoxRectTransform.rect.size;
        if (boxSize.x <= 0f || boxSize.y <= 0f)
        {
            Debug.LogWarning($"초상화 박스 크기가 올바르지 않습니다. 현재 크기 : {boxSize}");
            return;
        }

        Image_Portrait.preserveAspect = true;
        Image_Portrait.SetNativeSize();

        Vector2 portraitSize = portraitRectTransform.rect.size;
        if (portraitSize.x <= 0f || portraitSize.y <= 0f)
        {
            Debug.LogWarning($"초상화 이미지 크기가 올바르지 않습니다. 현재 크기 : {portraitSize}");
            return;
        }

        float widthScale = boxSize.x / portraitSize.x;
        float heightScale = boxSize.y / portraitSize.y;
        float fitScale = Mathf.Min(1f, widthScale, heightScale);

        portraitRectTransform.localScale = Vector3.one * fitScale;
    }

    private RectTransform GetPortraitBoxRectTransform(RectTransform portraitRectTransform)
    {
        if (Rect_PortraitBox != null)
        {
            return Rect_PortraitBox;
        }

        return portraitRectTransform.parent as RectTransform;
    }

    private void ShowSelection(DialogueData dialogueData)
    {
        if (HasSelectionData(dialogueData) == false)
        {
            if (Layout_SelectionRoot != null)
            {
                Layout_SelectionRoot.SetActive(false);
            }
            return;
        }

        if (Layout_SelectionRoot == null || Prefab_SelectionButton == null)
        {
            Debug.LogWarning("선택지 UI 참조가 누락되어 있습니다.");
            CloseDialogueUI();
            return;
        }

        int selectionCount = dialogueData.SelectionNameList.Count;
        if (selectionCount <= 0 || selectionCount >= _selectionSlotIndexMap.Length)
        {
            Debug.LogWarning($"현재 선택지는 1~3개까지만 지원합니다. 입력 개수 : {selectionCount}");
            CloseDialogueUI();
            return;
        }

        List<Transform> selectionSlotList = GetSelectionSlotList();
        if (selectionSlotList.Count < 3)
        {
            Debug.LogWarning("SelectionSlot_1, SelectionSlot_2, SelectionSlot_3 구성이 올바르지 않습니다.");
            CloseDialogueUI();
            return;
        }

        Layout_SelectionRoot.SetActive(true);

        int[] slotIndexArr = _selectionSlotIndexMap[selectionCount];
        for (int i = 0; i < selectionSlotList.Count; i++)
        {
            bool isUsedSlot = System.Array.IndexOf(slotIndexArr, i) >= 0;
            selectionSlotList[i].gameObject.SetActive(isUsedSlot);
        }

        for (int i = 0; i < selectionCount; i++)
        {
            string selectionName = dialogueData.SelectionNameList[i];
            string nextDialogueId = dialogueData.SelectionDialogueIdList[i];
            Transform targetSlot = selectionSlotList[slotIndexArr[i]];

            Button createdButton = Instantiate(Prefab_SelectionButton, targetSlot);
            SetSelectionButtonText(createdButton, selectionName);
            SetSelectionButtonEvent(createdButton, nextDialogueId);

            _createdSelectionButtonList.Add(createdButton.gameObject);
        }
    }

    public void OnClickSelection(string nextDialogueId)
    {
        if (string.IsNullOrEmpty(nextDialogueId))
        {
            Debug.LogWarning("선택지에 연결된 다음 다이얼로그 ID가 비어 있습니다.");
            CloseDialogueUI();
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 선택지 다이얼로그를 진행할 수 없습니다.");
            CloseDialogueUI();
            return;
        }

        DialogueData nextDialogueData = GameDataManager.Instance.GetDialogueData(nextDialogueId);
        if (nextDialogueData == null)
        {
            Debug.LogWarning($"선택지에 연결된 다이얼로그 데이터가 존재하지 않습니다 : {nextDialogueId}");
            CloseDialogueUI();
            return;
        }

        StartDialogueInternal(nextDialogueId, _objectNameTokenValue);
    }

    private void ClearSelectionButtons()
    {
        for (int i = 0; i < _createdSelectionButtonList.Count; i++)
        {
            if (_createdSelectionButtonList[i] != null)
            {
                Destroy(_createdSelectionButtonList[i]);
            }
        }

        _createdSelectionButtonList.Clear();
    }

    private bool HasSelectionData(DialogueData dialogueData)
    {
        if (dialogueData == null)
        {
            return false;
        }

        return
            dialogueData.SelectionNameList != null &&
            dialogueData.SelectionDialogueIdList != null &&
            dialogueData.SelectionNameList.Count > 0 &&
            dialogueData.SelectionNameList.Count == dialogueData.SelectionDialogueIdList.Count;
    }

    private List<Transform> GetSelectionSlotList()
    {
        List<Transform> selectionSlotList = new List<Transform>();

        if (Layout_SelectionRoot == null)
        {
            return selectionSlotList;
        }

        AddSelectionSlotIfExist(selectionSlotList, SelectionSlot_1);
        AddSelectionSlotIfExist(selectionSlotList, SelectionSlot_2);
        AddSelectionSlotIfExist(selectionSlotList, SelectionSlot_3);

        return selectionSlotList;
    }

    private void AddSelectionSlotIfExist(List<Transform> selectionSlotList, Transform slotTransform)
    {
        if (slotTransform != null)
        {
            selectionSlotList.Add(slotTransform);
        }
    }

    private void SetSelectionButtonText(Button selectionButton, string buttonText)
    {
        if (selectionButton == null)
        {
            return;
        }

        Text textComponent = selectionButton.GetComponentInChildren<Text>(true);
        if (textComponent == null)
        {
            Debug.LogWarning("선택지 버튼의 Legacy Text를 찾지 못했습니다.");
            return;
        }

        textComponent.text = buttonText;
    }

    private void SetSelectionButtonEvent(Button selectionButton, string nextDialogueId)
    {
        if (selectionButton == null)
        {
            return;
        }

        DialogueSelectionButton selectionButtonHandler = selectionButton.GetComponent<DialogueSelectionButton>();
        if (selectionButtonHandler == null)
        {
            selectionButtonHandler = selectionButton.gameObject.AddComponent<DialogueSelectionButton>();
        }

        selectionButtonHandler.Initialize(this, nextDialogueId);
    }

    private void RefreshNextButtonVisible(bool isVisible)
    {
        if (Button_Next == null)
        {
            return;
        }

        Button_Next.gameObject.SetActive(isVisible);
    }

    private bool ShouldOpenInspectObjectImmediately(DialogueData dialogueData)
    {
        if (dialogueData == null)
        {
            return false;
        }

        return string.IsNullOrEmpty(dialogueData.Description) && string.IsNullOrEmpty(dialogueData.OpenInspectObjectDataId) == false;
    }

    private bool TryOpenInspectObjectFromDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null || string.IsNullOrEmpty(dialogueData.OpenInspectObjectDataId))
        {
            return false;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 대화 중 조사 오브젝트 데이터를 확인할 수 없습니다.");
            CloseDialogueUI();
            return true;
        }

        InspectObjectData inspectObjectData = GameDataManager.Instance.GetInspectObjectData(dialogueData.OpenInspectObjectDataId);
        if (inspectObjectData == null)
        {
            Debug.LogWarning($"대화 중 열 조사 오브젝트 데이터가 존재하지 않습니다 : {dialogueData.OpenInspectObjectDataId}");
            CloseDialogueUI();
            return true;
        }

        if (string.IsNullOrEmpty(inspectObjectData.PrefabPath))
        {
            Debug.LogWarning($"대화 중 열 조사 오브젝트 프리팹 경로가 비어 있습니다 : {dialogueData.OpenInspectObjectDataId}");
            CloseDialogueUI();
            return true;
        }

        GameObject inspectObjectPrefab = Resources.Load<GameObject>(inspectObjectData.PrefabPath);
        if (inspectObjectPrefab == null)
        {
            Debug.LogWarning($"대화 중 열 조사 오브젝트 프리팹을 찾을 수 없습니다 : {inspectObjectData.PrefabPath}");
            CloseDialogueUI();
            return true;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 대화 중 조사 UI를 열 수 없습니다.");
            CloseDialogueUI();
            return true;
        }

        MarkDialogueFlag(dialogueData);
        _isWaitingInspectObject = true;
        _pendingInspectNextDialogueId = dialogueData.NextDialogueId ?? string.Empty;
        HideDialogueForInspectObject();
        UIManager.Instance.OpenInspectObjectUI(dialogueData.OpenInspectObjectDataId, this);
        return true;
    }

    private void HideDialogueForInspectObject()
    {
        ClearDescriptionTyping();

        if (_isTextWindowHidden == false)
        {
            HideTextWindow();
        }
    }

    public bool CompleteInspectObject(string inspectObjectDataId)
    {
        if (_isWaitingInspectObject == false)
        {
            Debug.LogWarning($"DialogueUI가 조사 완료 대기 상태가 아닌데 조사 완료 콜백을 받았습니다 : {inspectObjectDataId}");
            return true;
        }

        ContinueDialogueAfterInspectObject();
        return true;
    }

    public bool ShouldOpenCompleteDialogue()
    {
        return _isWaitingInspectObject == false;
    }

    private void ContinueDialogueAfterInspectObject()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 없어 조사 UI 이후 다이얼로그 정지 상태를 복구하지 못했습니다.");
        }

        _isWaitingInspectObject = false;
        RestoreTextWindowIfHidden();

        string nextDialogueId = _pendingInspectNextDialogueId;
        _pendingInspectNextDialogueId = string.Empty;

        if (TryStartDialogue(nextDialogueId))
        {
            return;
        }

        CompleteDialogueAndCloseUI();
    }

    private void SetDescriptionTextByPortrait(bool isPortraitShown)
    {
        Text selectedDescriptionText = isPortraitShown ? Text_Description_Portrait : Text_Description_WithoutPortrait;
        Text fallbackDescriptionText = isPortraitShown ? Text_Description_WithoutPortrait : Text_Description_Portrait;

        if (selectedDescriptionText == null)
        {
            Debug.LogWarning($"DialogueUI의 {(isPortraitShown ? nameof(Text_Description_Portrait) : nameof(Text_Description_WithoutPortrait))} 참조가 누락되어 있습니다.");
            selectedDescriptionText = fallbackDescriptionText;
        }

        if (selectedDescriptionText == null)
        {
            Debug.LogWarning("DialogueUI의 설명 Text 참조가 모두 누락되어 대사를 표시할 수 없습니다.");
            _currentDescriptionText = null;
            SetDescriptionTextVisible(Text_Description_Portrait, false);
            SetDescriptionTextVisible(Text_Description_WithoutPortrait, false);
            return;
        }

        _currentDescriptionText = selectedDescriptionText;
        SetDescriptionTextVisible(Text_Description_Portrait, Text_Description_Portrait == _currentDescriptionText);
        SetDescriptionTextVisible(Text_Description_WithoutPortrait, Text_Description_WithoutPortrait == _currentDescriptionText);
    }

    private void SetDescriptionTextVisible(Text descriptionText, bool isVisible)
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.gameObject.SetActive(isVisible);
    }

    private void ClearDescriptionText(Text descriptionText)
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.text = string.Empty;
    }

    private void SetCurrentDialogueDescription(string description)
    {
        if (_currentDescriptionText == null)
        {
            Debug.LogWarning("DialogueUI의 현재 설명 Text 참조가 누락되어 있습니다.");
            return;
        }

        if (TrySetupDescriptionTypingEffect() == false)
        {
            _currentDescriptionText.text = description ?? string.Empty;
            return;
        }

        TypingEffect_Description.Play(description);
    }

    private void SkipDescriptionTyping()
    {
        if (TypingEffect_Description == null)
        {
            return;
        }

        TypingEffect_Description.Skip();
    }

    private void ClearDescriptionTyping()
    {
        if (TypingEffect_Description != null)
        {
            TypingEffect_Description.Clear();
        }

        ClearDescriptionText(Text_Description_Portrait);
        ClearDescriptionText(Text_Description_WithoutPortrait);
    }

    private bool IsDescriptionTyping()
    {
        return TypingEffect_Description != null && TypingEffect_Description.IsTyping;
    }

    private bool TrySetupDescriptionTypingEffect()
    {
        if (_currentDescriptionText == null)
        {
            Debug.LogWarning("DialogueUI의 현재 설명 Text 참조가 누락되어 있습니다.");
            return false;
        }

        if (TypingEffect_Description == null)
        {
            TypingEffect_Description = GetComponent<TextTypingEffect>();
            if (TypingEffect_Description == null)
            {
                TypingEffect_Description = gameObject.AddComponent<TextTypingEffect>();
            }
        }

        TypingEffect_Description.Initialize(_currentDescriptionText);
        return true;
    }

    private void CloseDialogueUI()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 존재하지 않아 DialogueUI를 닫을 수 없습니다.");
            return;
        }

        UIManager.Instance.CloseContentUI(UIType.DialogueUI);
    }

    private void StartDialogueSoundState()
    {
        if (_isDialogueSoundStateStarted)
        {
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager.Instance가 존재하지 않아 다이얼로그 사운드 상태를 시작할 수 없습니다.");
            return;
        }

        SoundManager.Instance.BeginDialogueSoundState();
        _isDialogueSoundStateStarted = true;
    }

    private void ApplyDialogueBgmCommand(DialogueData dialogueData)
    {
        if (dialogueData == null)
        {
            Debug.LogWarning("다이얼로그 데이터가 비어 있어 BGM 명령을 확인할 수 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(dialogueData.BGM))
        {
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning($"SoundManager.Instance가 존재하지 않아 다이얼로그 BGM 명령을 실행할 수 없습니다 : {dialogueData.Id}");
            return;
        }

        SoundManager.Instance.ApplyDialogueBgmCommand(dialogueData.BGM);
    }

    private void RestoreDialogueSoundStateIfStarted()
    {
        if (_isDialogueSoundStateStarted == false)
        {
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager.Instance가 존재하지 않아 다이얼로그 사운드 상태를 복구할 수 없습니다.");
            _isDialogueSoundStateStarted = false;
            return;
        }

        SoundManager.Instance.RestoreDialogueSoundState();
        _isDialogueSoundStateStarted = false;
    }

    private void CompleteDialogueAndCloseUI()
    {
        if (_onCompleteCallback != null)
        {
            Action onCompleteCallback = _onCompleteCallback;
            _onCompleteCallback = null;
            onCompleteCallback.Invoke();
        }

        RestoreDialogueSoundStateIfStarted();
        CloseDialogueUI();
    }
}
