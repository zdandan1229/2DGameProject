using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueUI : UIBase, IPointerClickHandler
{
    [Header("Name")]
    [SerializeField] private GameObject Layout_CharacterName;
    [SerializeField] private Text Text_Character;

    [Header("Dialogue")]
    [SerializeField] private Text Text_Description;
    [SerializeField] private Image Image_Portrait;

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
    private Queue<string> _descriptionQueue = new Queue<string>();
    private List<GameObject> _createdSelectionButtonList = new List<GameObject>();
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
        ClearDescriptionTyping();

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
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Z))
        {
            RequestNextDialogue();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RequestNextDialogue();
    }

    public void StartDialogue(string dialogueId)
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

        ResetDialogueUI();
        SetCharacterName(dialogueData.CharacterDataId);
        SetPortrait(dialogueData);
        PrepareDescriptionQueue(dialogueData.Description);
        ShowNextDescriptionPage();
        RefreshNextButtonVisible(true);
    }

    public void RequestNextDialogue()
    {
        if (IsSelectionShowing)
        {
            return;
        }

        if (IsDescriptionTyping())
        {
            SkipDescriptionTyping();
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 다음 다이얼로그를 진행할 수 없습니다.");
            CloseDialogueUI();
            return;
        }

        bool isNextDescriptionExist = ShowNextDescriptionPage();
        if (isNextDescriptionExist)
        {
            return;
        }

        DialogueData currentDialogueData = GameDataManager.Instance.GetDialogueData(_currentDialogueId);
        if (HasSelectionData(currentDialogueData))
        {
            ShowSelection(currentDialogueData);
            RefreshNextButtonVisible(false);
            return;
        }

        bool isNextDialogueExist = CheckAndStartNextDialogue();
        if (isNextDialogueExist == false)
        {
            CloseDialogueUI();
        }
    }

    private void ResetDialogueUI()
    {
        ClearDescriptionTyping();

        _descriptionQueue.Clear();
        ClearSelectionButtons();

        if (Layout_SelectionRoot != null)
        {
            Layout_SelectionRoot.SetActive(false);
        }

        if (Text_Description != null && TypingEffect_Description == null)
        {
            Text_Description.text = string.Empty;
        }
    }

    private void PrepareDescriptionQueue(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            _descriptionQueue.Enqueue(string.Empty);
            return;
        }

        if (description.Contains("<np>"))
        {
            string[] dialogueDescriptionList = description.Split("<np>");
            foreach (string desc in dialogueDescriptionList)
            {
                _descriptionQueue.Enqueue(desc);
            }
            return;
        }

        _descriptionQueue.Enqueue(description);
    }

    private bool ShowNextDescriptionPage()
    {
        bool isNextDescriptionExist = (_descriptionQueue.Count > 0);
        if (isNextDescriptionExist == false)
        {
            return false;
        }

        string desc = _descriptionQueue.Dequeue();
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

        string nextDialogueId = dialogueData.NextDialogueId;
        if (string.IsNullOrEmpty(nextDialogueId) == false)
        {
            DialogueData nextDialogueData = GameDataManager.Instance.GetDialogueData(nextDialogueId);
            if (nextDialogueData == null)
            {
                Debug.LogWarning($"다음 다이얼로그 데이터가 존재하지 않습니다 : {nextDialogueId}");
                return false;
            }

            StartDialogue(nextDialogueId);
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

    private void SetPortrait(DialogueData dialogueData)
    {
        if (Image_Portrait == null)
        {
            return;
        }

        string portraitPath = dialogueData.CharacterPortraitPath;

        if (string.IsNullOrEmpty(portraitPath))
        {
            CharacterData characterData = GameDataManager.Instance.GetCharacterData(dialogueData.CharacterDataId);
            if (characterData != null)
            {
                portraitPath = characterData.PortraitPath;
            }
        }

        if (string.IsNullOrEmpty(portraitPath))
        {
            Image_Portrait.gameObject.SetActive(false);
            return;
        }

        Sprite portraitSprite = GameUtil.LoadSpriteCanBeNull(portraitPath);
        if (portraitSprite == null)
        {
            Image_Portrait.gameObject.SetActive(false);
            return;
        }

        Image_Portrait.gameObject.SetActive(true);
        Image_Portrait.sprite = portraitSprite;
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

        StartDialogue(nextDialogueId);
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

    private void SetCurrentDialogueDescription(string description)
    {
        if (Text_Description == null)
        {
            Debug.LogWarning("DialogueUI의 Text_Description 참조가 누락되어 있습니다.");
            return;
        }

        if (TrySetupDescriptionTypingEffect() == false)
        {
            Text_Description.text = description ?? string.Empty;
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
            return;
        }

        if (Text_Description != null)
        {
            Text_Description.text = string.Empty;
        }
    }

    private bool IsDescriptionTyping()
    {
        return TypingEffect_Description != null && TypingEffect_Description.IsTyping;
    }

    private bool TrySetupDescriptionTypingEffect()
    {
        if (Text_Description == null)
        {
            Debug.LogWarning("DialogueUI의 Text_Description 참조가 누락되어 있습니다.");
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

        TypingEffect_Description.Initialize(Text_Description);
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
}
