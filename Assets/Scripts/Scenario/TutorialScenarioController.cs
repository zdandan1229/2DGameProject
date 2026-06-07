using System.Collections;
using UnityEngine;

public class TutorialScenarioController : MonoBehaviour
{
    private const string TutoPilotEndDialogueFlagId = "flag_tutopilot_enddialogue";
    private const string EndInspectMapPrintFlagId = "flag_endinspect_mapprint";
    private const string EndInspectBrokenFrameFlagId = "flag_endinspect_brokenframe";
    private const string EndTutorialFlagId = "flag_endtutorial";
    private const string HasIdCardFlagId = "flag_hasidcard";
    private const string EndTutorialDialogueId = "dialogue_endtutorial_100";
    private const string TutorialEndEntryPointId = "entrance_tutorialend";
    private const string IdCardInspectObjectDataId = "inspectobject_idcard";
    private const string TutorialStartJournalDataId = "journal_001";
    private const string OmakeSetName = "OmakeSet";
    private const string FirstEnterCorridorEventEndFlagId = "flag_endfirstentercorridorevent";
    private const string FirstEnterCorridorDialogueId = "dialogue_tutofirstentercorridor";
    private const string FirstEnterManQuartersEventEndFlagId = "flag_endfirstentermanquarterevent";
    private const string FirstEnterManQuartersDialogueId = "dialogue_tutofirstentermanquarters";
    private const string HelipadStageName = "Stage_Helipad";
    private const string CorridorStageName = "Stage_Corridor";
    private const string ManQuartersStageName = "Stage_Man_Quarters";
    private const string EventNPCName = "EventNPC";

    private bool _isScenarioManagerSubscribed;
    private bool _isWorldTransitionManagerSubscribed;
    private bool _isFirstEnterCorridorDialogueOpened;
    private bool _isFirstEnterManQuartersDialogueOpened;
    private bool _isEndTutorialDialogueOpening;
    private bool _isEndTutorialSequenceRunning;
    private bool _isApplyingTutorialSkipCompletedState;
    private StageInfo _lastStageInfo;

    private void OnEnable()
    {
        TrySubscribeScenarioManager();
        TrySubscribeWorldTransitionManager();
    }

    private void Start()
    {
        TrySubscribeScenarioManager();
        TrySubscribeWorldTransitionManager();
    }

    private void OnDisable()
    {
        if (ScenarioManager.Instance != null && _isScenarioManagerSubscribed)
        {
            ScenarioManager.Instance.OnFlagMarked -= HandleFlagMarked;
            _isScenarioManagerSubscribed = false;
        }

        if (WorldTransitionManager.Instance != null && _isWorldTransitionManagerSubscribed)
        {
            WorldTransitionManager.Instance.OnStageChanged -= HandleStageChanged;
            WorldTransitionManager.Instance.OnStageTransitionCompleted -= HandleStageTransitionCompleted;
            _isWorldTransitionManagerSubscribed = false;
        }
    }

    private void TrySubscribeScenarioManager()
    {
        if (_isScenarioManagerSubscribed)
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            return;
        }

        ScenarioManager.Instance.OnFlagMarked += HandleFlagMarked;
        _isScenarioManagerSubscribed = true;
        ApplyCurrentScenarioState();
    }

    private void TrySubscribeWorldTransitionManager()
    {
        if (_isWorldTransitionManagerSubscribed)
        {
            return;
        }

        if (WorldTransitionManager.Instance == null)
        {
            return;
        }

        WorldTransitionManager.Instance.OnStageChanged += HandleStageChanged;
        WorldTransitionManager.Instance.OnStageTransitionCompleted += HandleStageTransitionCompleted;
        _isWorldTransitionManagerSubscribed = true;
        _lastStageInfo = WorldTransitionManager.Instance.CurrentStageInfo;
    }

    private void ApplyCurrentScenarioState()
    {
        if (ScenarioManager.Instance == null)
        {
            return;
        }

        if (ScenarioManager.Instance.HasFlag(TutoPilotEndDialogueFlagId))
        {
            UnlockAfterTutoPilotDialogue();
        }

        if (ScenarioManager.Instance.HasFlag(EndInspectMapPrintFlagId))
        {
            UnlockAfterMapPrintInspect();
        }

        if (ScenarioManager.Instance.HasFlag(EndTutorialFlagId))
        {
            ApplyEndTutorialWorldState();
        }

        TryOpenEndTutorialDialogue();
    }

    private void HandleFlagMarked(string flagId)
    {
        if (flagId == TutoPilotEndDialogueFlagId)
        {
            UnlockAfterTutoPilotDialogue();
        }

        if (flagId == EndInspectMapPrintFlagId)
        {
            UnlockAfterMapPrintInspect();
        }

        if (flagId == EndInspectMapPrintFlagId || flagId == EndInspectBrokenFrameFlagId)
        {
            TryOpenEndTutorialDialogue();
        }

        if (flagId == EndTutorialFlagId)
        {
            if (_isApplyingTutorialSkipCompletedState)
            {
                return;
            }

            StartEndTutorialSequence();
        }
    }

    public void ApplyTutorialSkipCompletedState()
    {
        _isApplyingTutorialSkipCompletedState = true;

        MarkScenarioFlag(EndTutorialFlagId);
        MarkScenarioFlag(HasIdCardFlagId);
        AddInventoryObject(IdCardInspectObjectDataId);
        AddJournal(TutorialStartJournalDataId);
        ApplyEndTutorialWorldState();

        _isApplyingTutorialSkipCompletedState = false;
    }

    private void HandleStageChanged(StageInfo stageInfo)
    {
        TryDisableHelipadEventNPCAfterLeaving(stageInfo);
        _lastStageInfo = stageInfo;
    }

    private void HandleStageTransitionCompleted(StageInfo stageInfo)
    {
        TryOpenFirstEnterCorridorDialogue(stageInfo);
        TryOpenFirstEnterManQuartersDialogue(stageInfo);
    }

    private void UnlockAfterTutoPilotDialogue()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance가 없어 튜토리얼 파일럿 대화 이후 잠금을 해제할 수 없습니다.");
            return;
        }

        GameManager.Instance.UnlockTutorialPilotDialogueLocks();
    }

    private void UnlockAfterMapPrintInspect()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance가 없어 지도 프린트 조사 이후 미니맵 단축키 잠금을 해제할 수 없습니다.");
            return;
        }

        GameManager.Instance.SetTutorialMiniMapHotkeyLocked(false);
    }

    private void TryOpenEndTutorialDialogue()
    {
        if (_isEndTutorialDialogueOpening)
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning("ScenarioManager.Instance가 없어 튜토리얼 종료 대화 조건을 확인할 수 없습니다.");
            return;
        }

        if (ScenarioManager.Instance.HasFlag(EndTutorialFlagId))
        {
            return;
        }

        if (ScenarioManager.Instance.HasFlag(EndInspectMapPrintFlagId) == false)
        {
            return;
        }

        if (ScenarioManager.Instance.HasFlag(EndInspectBrokenFrameFlagId) == false)
        {
            return;
        }

        StartCoroutine(OpenEndTutorialDialogueAfterInspectUIClosed());
    }

    private IEnumerator OpenEndTutorialDialogueAfterInspectUIClosed()
    {
        _isEndTutorialDialogueOpening = true;

        while (UIManager.Instance == null)
        {
            yield return null;
        }

        while (UIManager.Instance.IsUIOpened(UIType.InspectObjectUI) ||
               UIManager.Instance.IsUIOpened(UIType.DialogueUI))
        {
            yield return null;
        }

        UIManager.Instance.OpenDialogueUI(EndTutorialDialogueId);
        _isEndTutorialDialogueOpening = false;
    }

    private void StartEndTutorialSequence()
    {
        if (_isEndTutorialSequenceRunning)
        {
            return;
        }

        StartCoroutine(EndTutorialSequence());
    }

    private IEnumerator EndTutorialSequence()
    {
        _isEndTutorialSequenceRunning = true;

        while (UIManager.Instance != null && UIManager.Instance.IsUIOpened(UIType.DialogueUI))
        {
            yield return null;
        }

        while (WorldTransitionManager.Instance == null)
        {
            yield return null;
        }

        bool didStartMove = WorldTransitionManager.Instance.MovePlayerToEntryPoint(
            TutorialEndEntryPointId,
            ActivateOmakeSet
        );

        if (didStartMove == false)
        {
            Debug.LogWarning($"튜토리얼 종료 후 이동할 EntryPoint를 찾지 못했습니다 : {TutorialEndEntryPointId}");
            _isEndTutorialSequenceRunning = false;
        }
    }

    private void ActivateOmakeSet()
    {
        GameObject omakeSetObject = FindSceneObjectByNameIncludingInactive(OmakeSetName);
        if (omakeSetObject == null)
        {
            Debug.LogWarning($"{OmakeSetName} 오브젝트를 찾지 못해 튜토리얼 종료 세트를 활성화할 수 없습니다.");
            return;
        }

        omakeSetObject.SetActive(true);
    }

    private void ApplyEndTutorialWorldState()
    {
        ActivateOmakeSet();
        DisableHelipadEventNPC();
    }

    private void DisableHelipadEventNPC()
    {
        GameObject helipadStageObject = FindSceneObjectByNameIncludingInactive(HelipadStageName);
        if (helipadStageObject == null)
        {
            Debug.LogWarning($"{HelipadStageName} 오브젝트를 찾지 못해 튜토리얼 NPC를 비활성화할 수 없습니다.");
            return;
        }

        Transform eventNPCTransform = helipadStageObject.transform.Find(EventNPCName);
        if (eventNPCTransform == null)
        {
            Debug.LogWarning($"{HelipadStageName}에서 {EventNPCName} 오브젝트를 찾지 못해 튜토리얼 NPC를 비활성화할 수 없습니다.");
            return;
        }

        eventNPCTransform.gameObject.SetActive(false);
    }

    private void MarkScenarioFlag(string flagId)
    {
        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning($"ScenarioManager.Instance가 없어 튜토리얼 스킵 플래그를 등록할 수 없습니다 : {flagId}");
            return;
        }

        ScenarioManager.Instance.MarkFlag(flagId);
    }

    private void AddInventoryObject(string inspectObjectDataId)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning($"InventoryManager.Instance가 없어 튜토리얼 스킵 소지품을 등록할 수 없습니다 : {inspectObjectDataId}");
            return;
        }

        if (InventoryManager.Instance.HasInspectObject(inspectObjectDataId))
        {
            return;
        }

        InventoryManager.Instance.AddInspectObject(inspectObjectDataId);
    }

    private void AddJournal(string journalDataId)
    {
        if (JournalManager.Instance == null)
        {
            Debug.LogWarning($"JournalManager.Instance가 없어 튜토리얼 스킵 일지를 등록할 수 없습니다 : {journalDataId}");
            return;
        }

        if (JournalManager.Instance.HasJournal(journalDataId))
        {
            return;
        }

        JournalManager.Instance.AddJournal(journalDataId);
    }

    private GameObject FindSceneObjectByNameIncludingInactive(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            Debug.LogWarning("찾을 씬 오브젝트 이름이 비어 있습니다.");
            return null;
        }

        GameObject[] objectArr = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objectArr.Length; i++)
        {
            GameObject targetObject = objectArr[i];
            if (targetObject == null || targetObject.name != objectName)
            {
                continue;
            }

            if (targetObject.scene.IsValid() == false)
            {
                continue;
            }

            return targetObject;
        }

        return null;
    }

    private void TryDisableHelipadEventNPCAfterLeaving(StageInfo currentStageInfo)
    {
        if (_lastStageInfo == null || currentStageInfo == null)
        {
            return;
        }

        if (ScenarioManager.Instance == null || ScenarioManager.Instance.HasFlag(TutoPilotEndDialogueFlagId) == false)
        {
            return;
        }

        if (_lastStageInfo == currentStageInfo)
        {
            return;
        }

        if (_lastStageInfo.gameObject.name != HelipadStageName)
        {
            return;
        }

        Transform eventNPCTransform = _lastStageInfo.transform.Find(EventNPCName);
        if (eventNPCTransform == null)
        {
            Debug.LogWarning($"{HelipadStageName}에서 {EventNPCName} 오브젝트를 찾을 수 없어 튜토리얼 NPC를 비활성화할 수 없습니다.");
            return;
        }

        if (eventNPCTransform.gameObject.activeSelf == false)
        {
            return;
        }

        eventNPCTransform.gameObject.SetActive(false);
    }

    private void TryOpenFirstEnterCorridorDialogue(StageInfo stageInfo)
    {
        if (stageInfo == null)
        {
            return;
        }

        if (_isFirstEnterCorridorDialogueOpened)
        {
            return;
        }

        if (stageInfo.gameObject.name != CorridorStageName)
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning("ScenarioManager.Instance가 없어 복도 첫 진입 튜토리얼 대화 조건을 확인할 수 없습니다.");
            return;
        }

        if (ScenarioManager.Instance.HasFlag(EndTutorialFlagId))
        {
            return;
        }

        if (ScenarioManager.Instance.HasFlag(TutoPilotEndDialogueFlagId) == false)
        {
            return;
        }

        if (ScenarioManager.Instance.HasFlag(FirstEnterCorridorEventEndFlagId))
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 복도 첫 진입 튜토리얼 대화를 열 수 없습니다.");
            return;
        }

        _isFirstEnterCorridorDialogueOpened = true;
        UIManager.Instance.OpenDialogueUI(FirstEnterCorridorDialogueId);
    }

    private void TryOpenFirstEnterManQuartersDialogue(StageInfo stageInfo)
    {
        if (stageInfo == null)
        {
            return;
        }

        if (_isFirstEnterManQuartersDialogueOpened)
        {
            return;
        }

        if (stageInfo.gameObject.name != ManQuartersStageName)
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning("ScenarioManager.Instance가 없어 남자 숙소 첫 진입 튜토리얼 대화 조건을 확인할 수 없습니다.");
            return;
        }

        if (ScenarioManager.Instance.HasFlag(EndTutorialFlagId))
        {
            return;
        }

        if (ScenarioManager.Instance.HasFlag(TutoPilotEndDialogueFlagId) == false)
        {
            return;
        }

        if (ScenarioManager.Instance.HasFlag(FirstEnterManQuartersEventEndFlagId))
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 남자 숙소 첫 진입 튜토리얼 대화를 열 수 없습니다.");
            return;
        }

        _isFirstEnterManQuartersDialogueOpened = true;
        UIManager.Instance.OpenDialogueUI(FirstEnterManQuartersDialogueId);
    }
}
