using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InspectObjectUI : UIBase, IPointerClickHandler
{
    private class InspectProgressState
    {
        public int InspectPointCount;
        public bool IsCompleteDescriptionShown;
        public HashSet<InspectPoint> InspectedPointSet = new HashSet<InspectPoint>();

        public void Reset()
        {
            InspectPointCount = 0;
            IsCompleteDescriptionShown = false;
            InspectedPointSet.Clear();
        }
    }

    [Header("Inspect Object")]
    [SerializeField] private RectTransform Layout_ObjectRoot;
    [SerializeField] private RectTransform Layout_AreaRoot;
    [SerializeField] private GameObject Prefab_Object;

    [Header("Description")]
    [SerializeField] private GameObject Layout_DescriptionWindow;
    [SerializeField] private Text Text_Description;
    [SerializeField] private TextTypingEffect TypingEffect_Description;

    [Header("Next")]
    [SerializeField] private Button Button_Next;

    [Header("Exit")]
    [SerializeField] private Button Button_Exit;
    [SerializeField] private string _completeInspectDescription = "살펴볼 수 있는 부분을 모두 확인했어.";
    [SerializeField] private string _defaultEndDialogueId = "dialogue_default_endinspect";

    private GameObject _createdObject;
    private GameObject _createdArea;
    private InspectObjectData _currentInspectObjectData;
    private JournalData _currentJournalData;
    private IInspectObjectCompleteHandler _completeHandler;
    private IJournalInspectCompleteHandler _journalCompleteHandler;
    private Queue<string> _descriptionQueue = new Queue<string>();
    private InspectProgressState _objectProgress = new InspectProgressState();
    private InspectProgressState _areaProgress = new InspectProgressState();
    private bool _isInspectTextShowing;
    private bool _isOpenedFromInventory;
    private bool _isOpenedFromArea;
    private bool _isWaitingJournalOpen;
    private bool _isTextWindowHidden;
    private bool _wasDescriptionWindowActiveBeforeHide;
    private bool _wasNextButtonActiveBeforeHide;
    private bool _wasExitButtonActiveBeforeHide;
    private string _pendingJournalDataId;
    private string _returnInventoryInspectObjectDataId;
    private HashSet<string> _pendingInspectPointFlagIdSet = new HashSet<string>();

    private void OnEnable()
    {
        if (Button_Next != null)
        {
            Button_Next.onClick.AddListener(RequestNextDescription);
        }
        else
        {
            Debug.LogWarning("InspectObjectUI의 Button_Next 참조가 누락되어 있습니다.");
        }

        if (Button_Exit != null)
        {
            Button_Exit.onClick.AddListener(RequestExitInspectObjectUI);
        }
        else
        {
            Debug.LogWarning("InspectObjectUI의 Button_Exit 참조가 누락되어 있습니다.");
        }

        RefreshNextButton(false);
        RefreshExitButton(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 존재하지 않아 조사 UI에서 게임 정지를 요청하지 못했습니다.");
        }

        ShowDefaultObject();
    }

    private void OnDisable()
    {
        RestoreTextWindowIfHidden();
        ClearDescriptionTyping();

        if (Button_Next != null)
        {
            Button_Next.onClick.RemoveListener(RequestNextDescription);
        }

        if (Button_Exit != null)
        {
            Button_Exit.onClick.RemoveListener(RequestExitInspectObjectUI);
        }

        ClearObject();
        ClearArea();
        _currentInspectObjectData = null;
        _currentJournalData = null;
        _completeHandler = null;
        _journalCompleteHandler = null;
        _isOpenedFromInventory = false;
        _isOpenedFromArea = false;
        _isWaitingJournalOpen = false;
        _isTextWindowHidden = false;
        _pendingJournalDataId = string.Empty;
        _returnInventoryInspectObjectDataId = string.Empty;
        ClearPendingInspectPointFlags();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 존재하지 않아 조사 UI에서 게임 재개를 요청하지 못했습니다.");
        }
    }

    public void StartInspectObject(GameObject objectPrefab, string description)
    {
        RestoreTextWindowIfHidden();
        _currentInspectObjectData = null;
        _currentJournalData = null;
        _completeHandler = null;
        _journalCompleteHandler = null;
        _isOpenedFromInventory = false;
        _isOpenedFromArea = false;
        _isWaitingJournalOpen = false;
        _isTextWindowHidden = false;
        _pendingJournalDataId = string.Empty;
        _returnInventoryInspectObjectDataId = string.Empty;
        ClearPendingInspectPointFlags();
        ShowObject(objectPrefab);
        StartDescription(description);
    }

    private void Update()
    {
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
            RequestNextDescription();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isTextWindowHidden)
        {
            return;
        }

        RequestNextDescription();
    }

    public void StartInspectObject(string inspectObjectDataId)
    {
        StartInspectObject(inspectObjectDataId, null);
    }

    public void StartInspectObject(string inspectObjectDataId, IInspectObjectCompleteHandler completeHandler)
    {
        StartInspectObjectInternal(inspectObjectDataId, completeHandler, false, false);
    }

    public void StartInspectObjectFromInventory(string inspectObjectDataId)
    {
        StartInspectObjectInternal(inspectObjectDataId, null, true, false);
    }

    public void StartInspectArea(string inspectAreaDataId)
    {
        RestoreTextWindowIfHidden();

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so inspect area data cannot be loaded.");
            CloseInspectObjectUI();
            return;
        }

        InspectAreaData inspectAreaData = GameDataManager.Instance.GetInspectAreaData(inspectAreaDataId);
        if (inspectAreaData == null)
        {
            Debug.LogWarning($"Inspect area data was not found: {inspectAreaDataId}");
            CloseInspectObjectUI();
            return;
        }

        if (string.IsNullOrEmpty(inspectAreaData.PrefabPath))
        {
            Debug.LogWarning($"Inspect area prefab path is empty: {inspectAreaDataId}");
            CloseInspectObjectUI();
            return;
        }

        GameObject areaPrefab = Resources.Load<GameObject>(inspectAreaData.PrefabPath);
        if (areaPrefab == null)
        {
            Debug.LogWarning($"Inspect area prefab was not found: {inspectAreaData.PrefabPath}");
            CloseInspectObjectUI();
            return;
        }

        _currentInspectObjectData = null;
        _currentJournalData = null;
        _completeHandler = null;
        _journalCompleteHandler = null;
        _isOpenedFromInventory = false;
        _isOpenedFromArea = false;
        _isWaitingJournalOpen = false;
        _isTextWindowHidden = false;
        _pendingJournalDataId = string.Empty;
        _returnInventoryInspectObjectDataId = string.Empty;
        ClearPendingInspectPointFlags();

        ClearObject();
        ShowArea(areaPrefab);

        if (string.IsNullOrEmpty(inspectAreaData.Description) == false)
        {
            StartDescription(inspectAreaData.Description);
        }

        HideTextWindow();
    }

    public void StartJournalInspect(string journalDataId, IJournalInspectCompleteHandler completeHandler)
    {
        RestoreTextWindowIfHidden();

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 일지 데이터를 조회할 수 없습니다.");
            CloseInspectObjectUI();
            return;
        }

        JournalData journalData = GameDataManager.Instance.GetJournalData(journalDataId);
        if (journalData == null)
        {
            Debug.LogWarning($"일지 데이터가 존재하지 않습니다 : {journalDataId}");
            CloseInspectObjectUI();
            return;
        }

        _currentInspectObjectData = null;
        _currentJournalData = journalData;
        _completeHandler = null;
        _journalCompleteHandler = completeHandler;
        _isOpenedFromInventory = false;
        _isOpenedFromArea = false;
        _isWaitingJournalOpen = false;
        _isTextWindowHidden = false;
        _pendingJournalDataId = string.Empty;
        _returnInventoryInspectObjectDataId = string.Empty;
        ClearPendingInspectPointFlags();

        ClearObject();
        StartDescription(GetJournalInspectDescription(journalData));
        RefreshExitButton(false);
    }

    public void OpenInspectObjectFromArea(string inspectObjectDataId)
    {
        StartInspectObjectInternal(inspectObjectDataId, null, false, true);
    }

    public void OpenInspectObjectFromArea(string inspectObjectDataId, InspectPoint sourceInspectPoint)
    {
        RegisterPendingInspectPointFlag(sourceInspectPoint);
        RegisterInspectedPoint(sourceInspectPoint);
        StartInspectObjectInternal(inspectObjectDataId, null, false, true);
    }

    private void StartInspectObjectInternal(string inspectObjectDataId, IInspectObjectCompleteHandler completeHandler, bool isOpenedFromInventory, bool isOpenedFromArea)
    {
        RestoreTextWindowIfHidden();

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 조사 오브젝트 데이터를 조회할 수 없습니다.");
            CloseInspectObjectUI();
            return;
        }

        InspectObjectData inspectObjectData = GameDataManager.Instance.GetInspectObjectData(inspectObjectDataId);
        if (inspectObjectData == null)
        {
            Debug.LogWarning($"조사 오브젝트 데이터가 존재하지 않습니다 : {inspectObjectDataId}");
            CloseInspectObjectUI();
            return;
        }

        if (string.IsNullOrEmpty(inspectObjectData.PrefabPath))
        {
            Debug.LogWarning($"조사 오브젝트 프리팹 경로가 비어 있습니다 : {inspectObjectDataId}");
            CloseInspectObjectUI();
            return;
        }

        GameObject objectPrefab = Resources.Load<GameObject>(inspectObjectData.PrefabPath);
        if (objectPrefab == null)
        {
            Debug.LogWarning($"조사 오브젝트 프리팹을 찾을 수 없습니다 : {inspectObjectData.PrefabPath}");
            CloseInspectObjectUI();
            return;
        }

        _currentInspectObjectData = inspectObjectData;
        _completeHandler = completeHandler;
        _isOpenedFromInventory = isOpenedFromInventory;
        _isOpenedFromArea = isOpenedFromArea;
        _returnInventoryInspectObjectDataId = isOpenedFromInventory ? inspectObjectDataId : string.Empty;

        if (isOpenedFromArea == false)
        {
            ClearPendingInspectPointFlags();
            ClearArea();
        }

        ShowObject(objectPrefab);
        ShowStartInspectText(inspectObjectData);

        if (_isOpenedFromInventory)
        {
            RefreshExitButton(true);
        }
    }

    public void ShowInspectText(InspectPoint inspectPoint)
    {
        if (inspectPoint == null)
        {
            Debug.LogWarning("조사 포인트 참조가 비어 있습니다.");
            return;
        }

        bool isTextShown = ShowInspectText(inspectPoint.InspectTextId);
        if (isTextShown == false)
        {
            return;
        }

        RegisterPendingInspectPointFlag(inspectPoint);
        RegisterInspectPointJournal(inspectPoint);
        RegisterInspectedPoint(inspectPoint);
    }

    public void RegisterPendingInspectPointFlag(InspectPoint inspectPoint)
    {
        if (inspectPoint == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(inspectPoint.CompleteFlagId))
        {
            return;
        }

        _pendingInspectPointFlagIdSet.Add(inspectPoint.CompleteFlagId);
    }

    private void RegisterInspectPointJournal(InspectPoint inspectPoint)
    {
        if (inspectPoint == null || string.IsNullOrEmpty(inspectPoint.CompleteJournalDataId))
        {
            return;
        }

        JournalData journalData = GetJournalDataForInspectPoint(inspectPoint.CompleteJournalDataId);
        if (journalData == null)
        {
            return;
        }

        if (JournalManager.Instance == null)
        {
            Debug.LogWarning("JournalManager.Instance가 없어 조사 포인트 일지를 등록할 수 없습니다.");
            return;
        }

        bool isAdded = JournalManager.Instance.AddJournal(journalData.Id);
        if (isAdded == false && JournalManager.Instance.HasJournal(journalData.Id) == false)
        {
            return;
        }

        EnqueueDescription($"{journalData.Title}의 내용이 일지에 등록되었다.");
    }

    private JournalData GetJournalDataForInspectPoint(string journalDataId)
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 조사 포인트 일지 데이터를 조회할 수 없습니다.");
            return null;
        }

        JournalData journalData = GameDataManager.Instance.GetJournalData(journalDataId);
        if (journalData == null)
        {
            Debug.LogWarning($"조사 포인트에 연결된 일지 데이터가 존재하지 않습니다 : {journalDataId}");
            return null;
        }

        return journalData;
    }

    public bool ShowInspectText(string inspectTextId)
    {
        if (string.IsNullOrEmpty(inspectTextId))
        {
            Debug.LogWarning("출력할 조사 텍스트 ID가 비어 있습니다.");
            return false;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 조사 텍스트 데이터를 조회할 수 없습니다.");
            return false;
        }

        InspectTextData inspectTextData = GameDataManager.Instance.GetInspectTextData(inspectTextId);
        if (inspectTextData == null)
        {
            Debug.LogWarning($"조사 텍스트 데이터가 존재하지 않습니다 : {inspectTextId}");
            return false;
        }

        StartDescription(inspectTextData.Description);
        return true;
    }

    public void SetDescription(string description)
    {
        if (Text_Description == null)
        {
            Debug.LogWarning("InspectObjectUI의 Text_Description 참조가 누락되어 있습니다.");
            return;
        }

        if (TrySetupDescriptionTypingEffect() == false)
        {
            Text_Description.text = description ?? string.Empty;
            return;
        }

        TypingEffect_Description.Play(description);
    }

    public void CloseInspectObjectUI()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 존재하지 않아 InspectObjectUI를 닫을 수 없습니다.");
            return;
        }

        UIManager.Instance.CloseContentUI(UIType.InspectObjectUI);
    }

    private void RequestExitInspectObjectUI()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 존재하지 않아 InspectObjectUI 종료 처리를 할 수 없습니다.");
            return;
        }

        if (_currentInspectObjectData == null)
        {
            if (_currentJournalData != null)
            {
                OpenPendingJournalTab();
                return;
            }

            ApplyPendingInspectPointFlags();
            CloseInspectObjectUI();
            return;
        }

        if (_isOpenedFromInventory)
        {
            string returnInspectObjectDataId = _returnInventoryInspectObjectDataId;
            ApplyPendingInspectPointFlags();
            UIManager.Instance.CloseContentUI(UIType.InspectObjectUI);
            UIManager.Instance.OpenInventoryPopup(returnInspectObjectDataId);
            return;
        }

        if (_isOpenedFromArea)
        {
            if (CompleteCurrentInspectObject() == false)
            {
                return;
            }

            ReturnToInspectArea();
            return;
        }

        if (ShouldReturnToExternalCompleteFlow())
        {
            if (CompleteCurrentInspectObjectAndReturnToExternalFlow() == false)
            {
                return;
            }

            return;
        }

        string objectName = GetInspectObjectName(_currentInspectObjectData);
        if (string.IsNullOrEmpty(objectName))
        {
            return;
        }

        if (string.IsNullOrEmpty(_defaultEndDialogueId))
        {
            Debug.LogWarning("조사 종료 후 출력할 기본 다이얼로그 ID가 비어 있습니다.");
            return;
        }

        if (CompleteCurrentInspectObject() == false)
        {
            return;
        }

        UIManager.Instance.CloseContentUI(UIType.InspectObjectUI);
        UIManager.Instance.OpenDialogueUI(_defaultEndDialogueId, objectName);
    }

    private void ReturnToInspectArea()
    {
        RestoreTextWindowIfHidden();
        ClearDescriptionTyping();
        _descriptionQueue.Clear();
        SetDescription(string.Empty);
        RefreshNextButton(false);
        ClearObject();
        _currentInspectObjectData = null;
        _completeHandler = null;
        _isOpenedFromInventory = false;
        _isOpenedFromArea = false;
        _returnInventoryInspectObjectDataId = string.Empty;
        _isInspectTextShowing = false;
        CheckCompleteInspect();
    }

    private bool CompleteCurrentInspectObject()
    {
        if (_currentInspectObjectData == null)
        {
            Debug.LogWarning("완료 처리할 조사 오브젝트 데이터가 없습니다.");
            return false;
        }

        if (string.IsNullOrEmpty(_currentInspectObjectData.Id))
        {
            Debug.LogWarning("완료 처리할 조사 오브젝트 ID가 비어 있습니다.");
            return false;
        }

        if (_completeHandler == null)
        {
            if (ApplyCompleteReward(_currentInspectObjectData) == false)
            {
                return false;
            }

            ApplyPendingInspectPointFlags();
            return true;
        }

        if (ApplyCompleteReward(_currentInspectObjectData) == false)
        {
            return false;
        }

        ApplyPendingInspectPointFlags();
        return _completeHandler.CompleteInspectObject(_currentInspectObjectData.Id);
    }

    private bool CompleteCurrentInspectObjectAndReturnToExternalFlow()
    {
        if (_currentInspectObjectData == null)
        {
            Debug.LogWarning("완료 처리할 조사 오브젝트 데이터가 없습니다.");
            return false;
        }

        if (string.IsNullOrEmpty(_currentInspectObjectData.Id))
        {
            Debug.LogWarning("완료 처리할 조사 오브젝트 ID가 비어 있습니다.");
            return false;
        }

        if (_completeHandler == null)
        {
            Debug.LogWarning("조사 완료 후 복귀할 완료 핸들러가 없습니다.");
            return false;
        }

        if (ApplyCompleteReward(_currentInspectObjectData) == false)
        {
            return false;
        }

        string completedInspectObjectDataId = _currentInspectObjectData.Id;
        IInspectObjectCompleteHandler completeHandler = _completeHandler;

        ApplyPendingInspectPointFlags();
        UIManager.Instance.CloseContentUI(UIType.InspectObjectUI);
        return completeHandler.CompleteInspectObject(completedInspectObjectDataId);
    }

    private bool ApplyCompleteReward(InspectObjectData inspectObjectData)
    {
        if (inspectObjectData == null)
        {
            Debug.LogWarning("완료 보상을 처리할 조사 오브젝트 데이터가 없습니다.");
            return false;
        }

        string rewardType = GetCompleteRewardType(inspectObjectData);
        if (string.Equals(rewardType, "None", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(rewardType, "Inventory", System.StringComparison.OrdinalIgnoreCase))
        {
            return AddCompleteRewardToInventory(inspectObjectData.Id);
        }

        if (string.Equals(rewardType, "Journal", System.StringComparison.OrdinalIgnoreCase))
        {
            return AddCompleteRewardToJournal(inspectObjectData);
        }

        Debug.LogWarning($"알 수 없는 조사 완료 보상 타입입니다 : {rewardType}. InspectObjectData Id : {inspectObjectData.Id}");
        return false;
    }

    private string GetCompleteRewardType(InspectObjectData inspectObjectData)
    {
        if (inspectObjectData == null || string.IsNullOrEmpty(inspectObjectData.CompleteRewardType))
        {
            return "Inventory";
        }

        return inspectObjectData.CompleteRewardType;
    }

    private bool AddCompleteRewardToInventory(string inspectObjectDataId)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance가 존재하지 않아 조사 오브젝트를 소지품에 추가할 수 없습니다.");
            return false;
        }

        bool isAdded = InventoryManager.Instance.AddInspectObject(inspectObjectDataId);
        if (isAdded == false && InventoryManager.Instance.HasInspectObject(inspectObjectDataId) == false)
        {
            return false;
        }

        return true;
    }

    private bool AddCompleteRewardToJournal(InspectObjectData inspectObjectData)
    {
        if (inspectObjectData == null || string.IsNullOrEmpty(inspectObjectData.CompleteJournalDataId))
        {
            Debug.LogWarning($"조사 완료 보상이 Journal이지만 CompleteJournalDataId가 비어 있습니다. InspectObjectData Id : {inspectObjectData?.Id}");
            return false;
        }

        if (JournalManager.Instance == null)
        {
            Debug.LogWarning("JournalManager.Instance가 없어 조사 완료 보상 일지를 등록할 수 없습니다.");
            return false;
        }

        bool isAdded = JournalManager.Instance.AddJournal(inspectObjectData.CompleteJournalDataId);
        if (isAdded == false && JournalManager.Instance.HasJournal(inspectObjectData.CompleteJournalDataId) == false)
        {
            return false;
        }

        return true;
    }

    private bool ShouldOpenCompleteDialogue()
    {
        IInspectObjectCompleteOptionProvider optionProvider = _completeHandler as IInspectObjectCompleteOptionProvider;
        if (optionProvider == null)
        {
            return true;
        }

        return optionProvider.ShouldOpenCompleteDialogue();
    }

    private bool ShouldReturnToExternalCompleteFlow()
    {
        return _completeHandler != null && ShouldOpenCompleteDialogue() == false;
    }

    private void ApplyPendingInspectPointFlags()
    {
        if (_pendingInspectPointFlagIdSet.Count <= 0)
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning("ScenarioManager.Instance가 없어 조사 포인트 완료 플래그를 기록할 수 없습니다.");
            return;
        }

        foreach (string flagId in _pendingInspectPointFlagIdSet)
        {
            ScenarioManager.Instance.MarkFlag(flagId);
        }

        ClearPendingInspectPointFlags();
    }

    private void ClearPendingInspectPointFlags()
    {
        _pendingInspectPointFlagIdSet.Clear();
    }

    private void CompleteJournal(string journalDataId, IJournalInspectCompleteHandler journalCompleteHandler)
    {
        if (string.IsNullOrEmpty(journalDataId))
        {
            Debug.LogWarning("완료 처리할 일지 ID가 비어 있습니다.");
            return;
        }

        if (journalCompleteHandler == null)
        {
            return;
        }

        journalCompleteHandler.CompleteJournalInspect(journalDataId);
    }

    private void ShowJournalRegisteredNotice()
    {
        if (_currentJournalData == null)
        {
            Debug.LogWarning("등록 안내를 출력할 일지 데이터가 없습니다.");
            return;
        }

        _pendingJournalDataId = _currentJournalData.Id;
        CompleteJournal(_pendingJournalDataId, _journalCompleteHandler);

        _isWaitingJournalOpen = true;
        SetDescriptionImmediate($"{_currentJournalData.Title}의 내용이 일지에 등록되었다.");
        RefreshNextButton(true);
        RefreshExitButton(false);
    }

    private void OpenPendingJournalTab()
    {
        if (_isWaitingJournalOpen == false)
        {
            return;
        }

        if (string.IsNullOrEmpty(_pendingJournalDataId))
        {
            Debug.LogWarning("열어야 할 일지 ID가 비어 있습니다.");
            CloseInspectObjectUI();
            return;
        }

        string journalDataId = _pendingJournalDataId;
        UIManager.Instance.CloseContentUI(UIType.InspectObjectUI);
        UIManager.Instance.OpenJournalTab(journalDataId, string.Empty);
    }

    private string GetInspectObjectName(InspectObjectData inspectObjectData)
    {
        if (inspectObjectData == null)
        {
            Debug.LogWarning("조사 종료 다이얼로그에 전달할 조사 오브젝트 데이터가 없습니다.");
            return string.Empty;
        }

        if (string.IsNullOrEmpty(inspectObjectData.Name))
        {
            Debug.LogWarning($"조사 오브젝트 Name이 비어 있어 종료 다이얼로그의 ObjectName을 채울 수 없습니다 : {inspectObjectData.Id}");
            return string.Empty;
        }

        return inspectObjectData.Name;
    }

    private void ShowDefaultObject()
    {
        if (Prefab_Object == null)
        {
            return;
        }

        ShowObject(Prefab_Object);
    }

    private void ShowStartInspectText(InspectObjectData inspectObjectData)
    {
        if (inspectObjectData == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(inspectObjectData.StartInspectTextId) == false)
        {
            InspectTextData inspectTextData = GameDataManager.Instance.GetInspectTextData(inspectObjectData.StartInspectTextId);
            if (inspectTextData != null)
            {
                StartDescription(inspectTextData.Description);
                return;
            }

            Debug.LogWarning($"시작 조사 텍스트 데이터가 존재하지 않습니다 : {inspectObjectData.StartInspectTextId}");
        }

        StartDescription(inspectObjectData.Description);
    }

    private string GetJournalInspectDescription(JournalData journalData)
    {
        if (journalData == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(journalData.InspectText) == false)
        {
            return journalData.InspectText;
        }

        if (string.IsNullOrEmpty(journalData.InspectTextId) == false && GameDataManager.Instance != null)
        {
            InspectTextData inspectTextData = GameDataManager.Instance.GetInspectTextData(journalData.InspectTextId);
            if (inspectTextData != null)
            {
                return inspectTextData.Description;
            }

            Debug.LogWarning($"일지 조사 텍스트 데이터가 존재하지 않습니다 : {journalData.InspectTextId}");
        }

        if (string.IsNullOrEmpty(journalData.InspectDescription) == false)
        {
            return journalData.InspectDescription;
        }

        return journalData.Description;
    }

    private void ShowObject(GameObject objectPrefab)
    {
        if (Layout_ObjectRoot == null)
        {
            Debug.LogWarning("InspectObjectUI의 Layout_ObjectRoot 참조가 누락되어 확대 오브젝트를 생성할 수 없습니다.");
            return;
        }

        if (objectPrefab == null)
        {
            Debug.LogWarning("생성할 확대 오브젝트 프리팹이 비어 있습니다.");
            return;
        }

        ClearObject();
        _createdObject = Instantiate(objectPrefab, Layout_ObjectRoot);
        PrepareInspectPointProgress(_createdObject, _objectProgress);
    }

    private void ShowArea(GameObject areaPrefab)
    {
        if (Layout_AreaRoot == null)
        {
            Debug.LogWarning("InspectObjectUI Layout_AreaRoot reference is missing, so inspect area cannot be created.");
            return;
        }

        if (areaPrefab == null)
        {
            Debug.LogWarning("Inspect area prefab is missing.");
            return;
        }

        ClearArea();
        _createdArea = Instantiate(areaPrefab, Layout_AreaRoot);
        PrepareInspectPointProgress(_createdArea, _areaProgress);
    }

    private void ClearObject()
    {
        if (_createdObject == null)
        {
            return;
        }

        Destroy(_createdObject);
        _createdObject = null;
        ResetInspectPointProgress(_objectProgress);
    }

    private void ClearArea()
    {
        if (_createdArea == null)
        {
            return;
        }

        Destroy(_createdArea);
        _createdArea = null;
        ResetInspectPointProgress(_areaProgress);
    }

    private void PrepareInspectPointProgress(GameObject createdObject, InspectProgressState progressState)
    {
        ResetInspectPointProgress(progressState);

        if (createdObject == null)
        {
            Debug.LogWarning("생성된 조사 오브젝트가 없어 조사 포인트를 확인할 수 없습니다.");
            RefreshExitButton(true);
            return;
        }

        InspectPoint[] inspectPointArr = createdObject.GetComponentsInChildren<InspectPoint>(true);
        progressState.InspectPointCount = inspectPointArr.Length;

        if (progressState.InspectPointCount <= 0)
        {
            Debug.LogWarning($"{createdObject.name} 안에 InspectPoint가 없어 바로 나갈 수 있도록 처리합니다.");
            RefreshExitButton(true);
        }
    }

    private void ResetInspectPointProgress(InspectProgressState progressState)
    {
        ClearDescriptionTyping();

        _descriptionQueue.Clear();
        _isInspectTextShowing = false;
        progressState?.Reset();
        RefreshNextButton(false);
        RefreshExitButton(false);
    }

    private void RegisterInspectedPoint(InspectPoint inspectPoint)
    {
        if (inspectPoint == null)
        {
            return;
        }

        InspectProgressState progressState = GetProgressStateForInspectPoint(inspectPoint);
        if (progressState == null)
        {
            return;
        }

        progressState.InspectedPointSet.Add(inspectPoint);
    }

    private bool IsObjectInspectPoint(InspectPoint inspectPoint)
    {
        if (inspectPoint == null || Layout_ObjectRoot == null)
        {
            return false;
        }

        return inspectPoint.transform.IsChildOf(Layout_ObjectRoot);
    }

    private bool IsAreaInspectPoint(InspectPoint inspectPoint)
    {
        if (inspectPoint == null || Layout_AreaRoot == null)
        {
            return false;
        }

        return inspectPoint.transform.IsChildOf(Layout_AreaRoot);
    }

    private InspectProgressState GetProgressStateForInspectPoint(InspectPoint inspectPoint)
    {
        if (IsObjectInspectPoint(inspectPoint))
        {
            return _objectProgress;
        }

        if (IsAreaInspectPoint(inspectPoint))
        {
            return _areaProgress;
        }

        Debug.LogWarning($"{inspectPoint.gameObject.name}이 Layout_ObjectRoot 또는 Layout_AreaRoot 아래에 없어 조사 진행도에 등록할 수 없습니다.");
        return null;
    }

    private InspectProgressState GetCurrentProgressStateForCompletion()
    {
        if (_currentInspectObjectData != null)
        {
            return _objectProgress;
        }

        if (_createdArea != null)
        {
            return _areaProgress;
        }

        return null;
    }

    private void CheckCompleteInspect()
    {
        if (_isOpenedFromInventory)
        {
            return;
        }

        InspectProgressState progressState = GetCurrentProgressStateForCompletion();
        if (progressState == null)
        {
            return;
        }

        if (progressState.IsCompleteDescriptionShown)
        {
            return;
        }

        if (_isInspectTextShowing)
        {
            return;
        }

        if (progressState.InspectPointCount <= 0)
        {
            return;
        }

        if (progressState.InspectedPointSet.Count < progressState.InspectPointCount)
        {
            return;
        }

        progressState.IsCompleteDescriptionShown = true;

        string completeDescription = GetCurrentCompleteInspectDescription();
        if (string.IsNullOrEmpty(completeDescription) == false)
        {
            SetDescription(completeDescription);
        }

        RefreshExitButton(true);
    }

    private string GetCurrentCompleteInspectDescription()
    {
        if (_currentInspectObjectData != null)
        {
            return GetCompleteInspectDescription();
        }

        return _completeInspectDescription;
    }

    private string GetCompleteInspectDescription()
    {
        if (_currentInspectObjectData == null || string.IsNullOrEmpty(_currentInspectObjectData.CompleteInspectDescription))
        {
            return _completeInspectDescription;
        }

        string completeDescriptionRule = _currentInspectObjectData.CompleteInspectDescription.Trim();
        if (string.Equals(completeDescriptionRule, "o", System.StringComparison.OrdinalIgnoreCase))
        {
            return _completeInspectDescription;
        }

        if (string.Equals(completeDescriptionRule, "x", System.StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return _currentInspectObjectData.CompleteInspectDescription;
    }

    private void RefreshExitButton(bool isActive)
    {
        if (Button_Exit == null)
        {
            return;
        }

        if (_isTextWindowHidden)
        {
            _wasExitButtonActiveBeforeHide = isActive;
            return;
        }

        Button_Exit.gameObject.SetActive(isActive);
    }

    private void StartDescription(string description)
    {
        if (_isTextWindowHidden)
        {
            RestoreTextWindowVisible();
        }

        PrepareDescriptionQueue(description);
        _isInspectTextShowing = true;
        ShowNextDescriptionPage();
        RefreshNextButton(true);
    }

    private void PrepareDescriptionQueue(string description)
    {
        _descriptionQueue.Clear();
        EnqueueDescription(description);
    }

    private void EnqueueDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            _descriptionQueue.Enqueue(string.Empty);
            return;
        }

        if (description.Contains("<np>"))
        {
            string[] descriptionArr = description.Split("<np>");
            for (int i = 0; i < descriptionArr.Length; i++)
            {
                _descriptionQueue.Enqueue(descriptionArr[i]);
            }
            return;
        }

        _descriptionQueue.Enqueue(description);
    }

    private bool ShowNextDescriptionPage()
    {
        if (_descriptionQueue.Count <= 0)
        {
            return false;
        }

        SetDescription(_descriptionQueue.Dequeue());
        return true;
    }

    private void RequestNextDescription()
    {
        if (_isWaitingJournalOpen)
        {
            OpenPendingJournalTab();
            return;
        }

        if (_isInspectTextShowing == false)
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

        _isInspectTextShowing = false;
        SetDescription(string.Empty);
        RefreshNextButton(false);
        CheckCompleteJournal();
        CheckCompleteInspect();
    }

    private void CheckCompleteJournal()
    {
        if (_currentJournalData == null)
        {
            return;
        }

        ShowJournalRegisteredNotice();
    }

    private void RefreshNextButton(bool isActive)
    {
        if (Button_Next == null)
        {
            return;
        }

        if (_isTextWindowHidden)
        {
            _wasNextButtonActiveBeforeHide = isActive;
            return;
        }

        Button_Next.gameObject.SetActive(isActive);
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
        GameObject descriptionWindowObject = GetDescriptionWindowObject();

        _wasDescriptionWindowActiveBeforeHide = descriptionWindowObject != null && descriptionWindowObject.activeSelf;
        _wasNextButtonActiveBeforeHide = Button_Next != null && Button_Next.gameObject.activeSelf;
        _wasExitButtonActiveBeforeHide = Button_Exit != null && Button_Exit.gameObject.activeSelf;

        SetGameObjectActive(descriptionWindowObject, false);
        SetGameObjectActive(Button_Next != null ? Button_Next.gameObject : null, false);
        SetGameObjectActive(Button_Exit != null ? Button_Exit.gameObject : null, false);

        _isTextWindowHidden = true;
    }

    private void RestoreTextWindowVisible()
    {
        SetGameObjectActive(GetDescriptionWindowObject(), _wasDescriptionWindowActiveBeforeHide);
        SetGameObjectActive(Button_Next != null ? Button_Next.gameObject : null, _wasNextButtonActiveBeforeHide);
        SetGameObjectActive(Button_Exit != null ? Button_Exit.gameObject : null, _wasExitButtonActiveBeforeHide);

        _isTextWindowHidden = false;
    }

    private GameObject GetDescriptionWindowObject()
    {
        if (Layout_DescriptionWindow != null)
        {
            return Layout_DescriptionWindow;
        }

        if (Text_Description != null)
        {
            Debug.LogWarning("InspectObjectUI의 Layout_DescriptionWindow 참조가 비어 있어 Text_Description만 토글합니다.");
            return Text_Description.gameObject;
        }

        return null;
    }

    private void SetGameObjectActive(GameObject targetObject, bool isActive)
    {
        if (targetObject == null)
        {
            return;
        }

        targetObject.SetActive(isActive);
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
            Debug.LogWarning("InspectObjectUI의 Text_Description 참조가 누락되어 있습니다.");
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

    private void SetDescriptionImmediate(string description)
    {
        if (Text_Description == null)
        {
            Debug.LogWarning("InspectObjectUI의 Text_Description 참조가 누락되어 있습니다.");
            return;
        }

        if (TypingEffect_Description != null)
        {
            TypingEffect_Description.Clear();
        }

        Text_Description.text = description ?? string.Empty;
    }
}
