using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionHoverController : MonoBehaviour
{
    private const string DefaultExitMenuButtonText = "\uB098\uAC00\uAE30";

    [SerializeField] private Player _player;
    [SerializeField] private string _exitMenuButtonText = DefaultExitMenuButtonText;
    [SerializeField] private Vector3 _menuWorldOffset = new Vector3(0f, 1.1f, 0f);
    [SerializeField] private Vector3 _menuWorldOffsetWhenNearRightEdge = new Vector3(-1.2f, 1.1f, 0f);
    [SerializeField, Range(0f, 1f)] private float _rightEdgeViewportLine = 0.75f;

    private IInteractable _currentInteractable;
    private IInteractionOptionProvider _currentOptionProvider;
    private InteractionMenuUI _openedInteractionMenuUI;

    private void Awake()
    {
        if (_player == null)
        {
            _player = GetComponent<Player>();
        }
    }

    private void Update()
    {
        if (CanUpdateInteraction() == false)
        {
            CloseInteractionMenu();
            return;
        }

        TryRequestInteractionMenu();
        TryOpenTestInteractionMenu();
    }

    private bool CanUpdateInteraction()
    {
        if (GameManager.Instance != null && GameManager.Instance.CanWorldInteract() == false)
        {
            return false;
        }

        if (Camera.main == null)
        {
            return false;
        }

        return true;
    }

    private void TryRequestInteractionMenu()
    {
        if (InputManager.GetPrimaryClickDown() == false)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        InteractionMenuUI openedInteractionMenuUI = GetOpenedInteractionMenuUI();
        if (openedInteractionMenuUI != null)
        {
            if (openedInteractionMenuUI.BlocksWorldInput)
            {
                InputManager.ConsumePrimaryClickForFrame();
            }

            return;
        }

        InteractionClickArea.InteractionClickResult clickResult = FindClickResultUnderMouse();
        if (clickResult == null || clickResult.OptionProvider == null)
        {
            CloseInteractionMenu();
            return;
        }

        IInteractionOptionProvider optionProvider = clickResult.OptionProvider;
        IInteractable interactable = optionProvider as IInteractable;
        if (interactable == null)
        {
            Debug.LogWarning("The selected interaction target does not implement IInteractable.");
            CloseInteractionMenu();
            return;
        }

        if (_player == null)
        {
            Debug.LogWarning("InteractionHoverController has no Player reference.");
            CloseInteractionMenu();
            return;
        }

        CloseInteractionMenu();
        _currentInteractable = interactable;
        _currentOptionProvider = optionProvider;
        _player.RequestInteract(interactable, clickResult.InteractionPosition, OpenInteractionMenuAfterArrive);
    }

    private InteractionMenuUI GetOpenedInteractionMenuUI()
    {
        if (_openedInteractionMenuUI != null)
        {
            if (_openedInteractionMenuUI.gameObject.activeInHierarchy == false)
            {
                _openedInteractionMenuUI = null;
                _currentInteractable = null;
                _currentOptionProvider = null;
                return null;
            }

            return _openedInteractionMenuUI;
        }

        if (UIManager.Instance == null)
        {
            return null;
        }

        if (UIManager.Instance.IsUIOpened(UIType.InteractionMenuUI) == false)
        {
            return null;
        }

        UIBase uiBase = UIManager.Instance.FindCreatedUI(UIType.InteractionMenuUI);
        return uiBase as InteractionMenuUI;
    }

    private InteractionClickArea.InteractionClickResult FindClickResultUnderMouse()
    {
        float pointerZPosition = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 mousePosition = InputManager.GetPointerScreenPosition(pointerZPosition);

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        return InteractionClickArea.FindClickResultAtWorldPosition(worldPosition, true);
    }

    private void OpenInteractionMenuAfterArrive(IInteractable arrivedInteractable)
    {
        if (arrivedInteractable == null)
        {
            Debug.LogWarning("Arrived interaction target is null, so InteractionMenuUI cannot be opened.");
            CloseInteractionMenu();
            return;
        }

        if (arrivedInteractable != _currentInteractable)
        {
            Debug.LogWarning("Arrived interaction target does not match the reserved target.");
            CloseInteractionMenu();
            return;
        }

        if (_currentOptionProvider == null)
        {
            Debug.LogWarning("Interaction option provider is null, so InteractionMenuUI cannot be opened.");
            CloseInteractionMenu();
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing, so InteractionMenuUI cannot be opened.");
            CloseInteractionMenu();
            return;
        }

        List<InteractionOption> optionList = CreateMenuOptionList(_currentOptionProvider);
        if (optionList.Count <= 0)
        {
            Debug.LogWarning("There are no interaction options to display.");
            CloseInteractionMenu();
            return;
        }

        if (TryExecuteDirectInteractionOption(optionList, _currentOptionProvider))
        {
            return;
        }

        Vector3 menuWorldPosition = GetMenuWorldPosition();
        _openedInteractionMenuUI = UIManager.Instance.OpenInteractionMenuUI(optionList, menuWorldPosition, OnClickInteractionOption);
    }

    private void TryOpenTestInteractionMenu()
    {
        if (InputManager.GetInteractionMenuTestDown() == false)
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing, so test InteractionMenuUI cannot be opened.");
            return;
        }

        CloseInteractionMenu();

        List<InteractionOption> optionList = CreateMenuOptionList(null);
        Vector3 menuWorldPosition = GetMenuWorldPosition();
        _openedInteractionMenuUI = UIManager.Instance.OpenInteractionMenuUI(optionList, menuWorldPosition, OnClickInteractionOption);
    }

    private Vector3 GetMenuWorldPosition()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Camera.main is missing, so InteractionMenuUI position cannot be calculated.");
            return transform.position + _menuWorldOffset;
        }

        Vector3 viewportPosition = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPosition.x >= _rightEdgeViewportLine)
        {
            return transform.position + _menuWorldOffsetWhenNearRightEdge;
        }

        return transform.position + _menuWorldOffset;
    }

    private List<InteractionOption> CreateMenuOptionList(IInteractionOptionProvider optionProvider)
    {
        List<InteractionOption> optionList = new List<InteractionOption>();
        optionList.Add(new InteractionOption(GetExitMenuButtonText(), InteractionActionType.ExitMenu));

        if (optionProvider == null)
        {
            return optionList;
        }

        List<InteractionOption> providedOptionList = optionProvider.GetInteractionOptions();
        if (providedOptionList == null)
        {
            return optionList;
        }

        for (int i = 0; i < providedOptionList.Count; i++)
        {
            InteractionOption interactionOption = providedOptionList[i];
            if (interactionOption == null || interactionOption.IsExecutable() == false)
            {
                Debug.LogWarning($"Interaction option at index {i} is invalid and will not be added to the menu.");
                continue;
            }

            if (interactionOption.CanShowInMenu())
            {
                optionList.Add(interactionOption);
            }
            else if (interactionOption.ShouldExecuteDirectly())
            {
                optionList.Insert(0, interactionOption);
            }
        }

        return optionList;
    }

    private string GetExitMenuButtonText()
    {
        if (string.IsNullOrEmpty(_exitMenuButtonText))
        {
            Debug.LogWarning("InteractionHoverController의 _exitMenuButtonText가 비어 있어 기본 나가기 문구를 사용합니다.");
            return DefaultExitMenuButtonText;
        }

        return _exitMenuButtonText;
    }

    private bool TryExecuteDirectInteractionOption(List<InteractionOption> optionList, IInteractionOptionProvider optionProvider)
    {
        if (optionList == null)
        {
            return false;
        }

        for (int i = 0; i < optionList.Count; i++)
        {
            InteractionOption interactionOption = optionList[i];
            if (interactionOption == null || interactionOption.ShouldExecuteDirectly() == false)
            {
                continue;
            }

            if (interactionOption.ActionType != InteractionActionType.ShowPopupText)
            {
                Debug.LogWarning($"ButtonText가 비어 있는 직접 실행 옵션은 ShowPopupText만 지원합니다. ActionType: {interactionOption.ActionType}");
                CloseInteractionMenu();
                return true;
            }

            CloseInteractionMenu();
            ExecuteInteractionOption(interactionOption, optionProvider);
            return true;
        }

        return false;
    }

    private void CloseInteractionMenu()
    {
        _currentInteractable = null;
        _currentOptionProvider = null;
        _openedInteractionMenuUI = null;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseInteractionMenuUI();
        }
    }

    private void OnClickInteractionOption(InteractionOption interactionOption)
    {
        if (interactionOption == null)
        {
            Debug.LogWarning("Interaction option to execute is null.");
            return;
        }

        if (interactionOption.ActionType == InteractionActionType.ExitMenu)
        {
            CloseInteractionMenu();
            return;
        }

        IInteractionOptionProvider selectedOptionProvider = _currentOptionProvider;
        CloseInteractionMenu();
        ExecuteInteractionOption(interactionOption, selectedOptionProvider);
    }

    private void ExecuteInteractionOption(InteractionOption interactionOption, IInteractionOptionProvider optionProvider)
    {
        if (GameManager.Instance != null && GameManager.Instance.CanWorldInteract() == false)
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing.");
            return;
        }

        switch (interactionOption.ActionType)
        {
            case InteractionActionType.OpenDialogue:
                OpenDialogue(GetTargetDataId(optionProvider, InteractionActionType.OpenDialogue), optionProvider);
                break;
            case InteractionActionType.OpenInspectObject:
                OpenInspectObject(GetTargetDataId(optionProvider, InteractionActionType.OpenInspectObject), optionProvider);
                break;
            case InteractionActionType.OpenInspectArea:
                OpenInspectArea(GetTargetDataId(optionProvider, InteractionActionType.OpenInspectArea));
                break;
            case InteractionActionType.OpenJournal:
                OpenJournal(GetTargetDataId(optionProvider, InteractionActionType.OpenJournal), optionProvider);
                break;
            case InteractionActionType.ShowPopupText:
                OpenPopupText(GetTargetDataId(optionProvider, InteractionActionType.ShowPopupText));
                break;
            case InteractionActionType.OpenStatus:
                OpenStatus(GetTargetDataId(optionProvider, InteractionActionType.OpenStatus));
                break;
            case InteractionActionType.EnterStage:
                EnterStage(GetTargetDataId(optionProvider, InteractionActionType.EnterStage));
                break;
            case InteractionActionType.PickupItem:
                Debug.LogWarning($"Interaction action is not implemented yet: {interactionOption.ActionType}");
                break;
            default:
                Debug.LogWarning($"Unknown interaction action: {interactionOption.ActionType}");
                break;
        }
    }

    private string GetTargetDataId(IInteractionOptionProvider optionProvider, InteractionActionType actionType)
    {
        IInteractionTargetDataProvider targetDataProvider = optionProvider as IInteractionTargetDataProvider;
        if (targetDataProvider != null)
        {
            string providedTargetDataId = targetDataProvider.GetInteractionTargetDataId(actionType);
            if (string.IsNullOrEmpty(providedTargetDataId) == false)
            {
                return providedTargetDataId;
            }
        }

        Debug.LogWarning($"Interaction target data id is empty. ActionType: {actionType}");
        return string.Empty;
    }

    private void EnterStage(string entryPointId)
    {
        if (string.IsNullOrEmpty(entryPointId))
        {
            Debug.LogWarning("EntryPointId is empty, so EnterStage interaction cannot be executed.");
            return;
        }

        if (WorldTransitionManager.Instance == null)
        {
            Debug.LogWarning("WorldTransitionManager.Instance is missing, so EnterStage interaction cannot be executed.");
            return;
        }

        WorldTransitionManager.Instance.MovePlayerToEntryPoint(entryPointId);
    }

    private void OpenDialogue(string dialogueId, IInteractionOptionProvider optionProvider)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            Debug.LogWarning("Dialogue id is empty.");
            return;
        }

        UIManager.Instance.OpenDialogueUI(dialogueId, string.Empty, () => MarkDialogueCompleteFlag(optionProvider));
    }

    private void MarkDialogueCompleteFlag(IInteractionOptionProvider optionProvider)
    {
        IInteractionObjectDataProvider objectDataProvider = optionProvider as IInteractionObjectDataProvider;
        if (objectDataProvider == null)
        {
            return;
        }

        InteractionObjectData interactionObjectData = objectDataProvider.GetInteractionObjectData();
        if (interactionObjectData == null || string.IsNullOrEmpty(interactionObjectData.DialogueCompleteFlagId))
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning("ScenarioManager.Instance가 없어 대화 완료 플래그를 기록할 수 없습니다.");
            return;
        }

        ScenarioManager.Instance.MarkFlag(interactionObjectData.DialogueCompleteFlagId);
    }

    private void OpenInspectObject(string inspectObjectDataId, IInteractionOptionProvider optionProvider)
    {
        if (string.IsNullOrEmpty(inspectObjectDataId))
        {
            Debug.LogWarning("Inspect object data id is empty.");
            return;
        }

        IInspectObjectCompleteHandler completeHandler = optionProvider as IInspectObjectCompleteHandler;
        UIManager.Instance.OpenInspectObjectUI(inspectObjectDataId, completeHandler);
    }

    private void OpenInspectArea(string inspectAreaDataId)
    {
        if (string.IsNullOrEmpty(inspectAreaDataId))
        {
            Debug.LogWarning("Inspect area data id is empty.");
            return;
        }

        UIManager.Instance.OpenInspectAreaUI(inspectAreaDataId);
    }

    private void OpenJournal(string journalDataId, IInteractionOptionProvider optionProvider)
    {
        if (string.IsNullOrEmpty(journalDataId))
        {
            Debug.LogWarning("Journal data id is empty.");
            return;
        }

        IJournalInspectCompleteHandler completeHandler = optionProvider as IJournalInspectCompleteHandler;
        UIManager.Instance.OpenJournalInspectUI(journalDataId, completeHandler);
    }

    private void OpenStatus(string characterDataId)
    {
        if (string.IsNullOrEmpty(characterDataId))
        {
            Debug.LogWarning("Character data id is empty.");
            return;
        }

        UIManager.Instance.OpenNPCStatusUI(characterDataId);
    }

    private void OpenPopupText(string popupTextDataId)
    {
        if (string.IsNullOrEmpty(popupTextDataId))
        {
            Debug.LogWarning("PopupText data id is empty.");
            return;
        }

        UIManager.Instance.OpenPopupTextUI(popupTextDataId);
    }
}
