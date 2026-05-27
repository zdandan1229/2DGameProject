using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionHoverController : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private Vector3 _menuWorldOffset = new Vector3(0f, 1.1f, 0f);

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

        IInteractionOptionProvider optionProvider = FindOptionProviderUnderMouse();
        if (optionProvider == null)
        {
            CloseInteractionMenu();
            return;
        }

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
        _player.RequestInteract(interactable, OpenInteractionMenuAfterArrive);
    }

    private IInteractionOptionProvider FindOptionProviderUnderMouse()
    {
        float pointerZPosition = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 mousePosition = InputManager.GetPointerScreenPosition(pointerZPosition);

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Collider2D collider = Physics2D.OverlapPoint(worldPosition);
        if (collider == null)
        {
            return null;
        }

        IInteractionOptionProvider optionProvider = collider.GetComponentInParent<IInteractionOptionProvider>();
        if (optionProvider == null)
        {
            optionProvider = collider.GetComponent<IInteractionOptionProvider>();
        }

        return optionProvider;
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

        Vector3 menuWorldPosition = transform.position + _menuWorldOffset;
        _openedInteractionMenuUI = UIManager.Instance.OpenInteractionMenuUI(optionList, menuWorldPosition, OnClickInteractionOption);
    }

    private List<InteractionOption> CreateMenuOptionList(IInteractionOptionProvider optionProvider)
    {
        List<InteractionOption> optionList = new List<InteractionOption>();
        optionList.Add(new InteractionOption("\uB098\uAC00\uAE30", InteractionActionType.ExitMenu, string.Empty));

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
            if (interactionOption == null || interactionOption.IsValid() == false)
            {
                Debug.LogWarning($"Interaction option at index {i} is invalid and will not be added to the menu.");
                continue;
            }

            optionList.Add(interactionOption);
        }

        return optionList;
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
                OpenDialogue(interactionOption.TargetDataId);
                break;
            case InteractionActionType.OpenInspectObject:
                OpenInspectObject(interactionOption.TargetDataId, optionProvider);
                break;
            case InteractionActionType.OpenStatus:
            case InteractionActionType.PickupItem:
            case InteractionActionType.EnterDoor:
                Debug.LogWarning($"Interaction action is not implemented yet: {interactionOption.ActionType}");
                break;
            default:
                Debug.LogWarning($"Unknown interaction action: {interactionOption.ActionType}");
                break;
        }
    }

    private void OpenDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            Debug.LogWarning("Dialogue id is empty.");
            return;
        }

        UIManager.Instance.OpenDialogueUI(dialogueId);
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
}
