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
        if (Input.GetMouseButtonDown(0) == false)
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
            Debug.LogWarning("선택된 상호작용 대상이 IInteractable을 구현하지 않았습니다.");
            CloseInteractionMenu();
            return;
        }

        if (_player == null)
        {
            Debug.LogWarning("InteractionHoverController의 Player 참조가 비어 있습니다.");
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
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);

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
            Debug.LogWarning("도착한 상호작용 대상이 비어 있어 InteractionMenuUI를 열 수 없습니다.");
            CloseInteractionMenu();
            return;
        }

        if (arrivedInteractable != _currentInteractable)
        {
            Debug.LogWarning("도착한 상호작용 대상과 예약된 대상이 다릅니다.");
            CloseInteractionMenu();
            return;
        }

        if (_currentOptionProvider == null)
        {
            Debug.LogWarning("상호작용 옵션 제공자가 비어 있어 InteractionMenuUI를 열 수 없습니다.");
            CloseInteractionMenu();
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 존재하지 않아 InteractionMenuUI를 열 수 없습니다.");
            CloseInteractionMenu();
            return;
        }

        List<InteractionOption> optionList = CreateMenuOptionList(_currentOptionProvider);
        if (optionList.Count <= 0)
        {
            Debug.LogWarning("표시할 상호작용 옵션이 없습니다.");
            CloseInteractionMenu();
            return;
        }

        Vector3 menuWorldPosition = transform.position + _menuWorldOffset;
        _openedInteractionMenuUI = UIManager.Instance.OpenInteractionMenuUI(optionList, menuWorldPosition, OnClickInteractionOption);
    }

    private List<InteractionOption> CreateMenuOptionList(IInteractionOptionProvider optionProvider)
    {
        List<InteractionOption> optionList = new List<InteractionOption>();
        optionList.Add(new InteractionOption("나가기", InteractionActionType.ExitMenu, string.Empty));

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
                Debug.LogWarning($"상호작용 옵션 {i}번이 올바르지 않아 메뉴에 추가하지 않습니다.");
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
            Debug.LogWarning("실행할 상호작용 옵션이 비어 있습니다.");
            return;
        }

        if (interactionOption.ActionType == InteractionActionType.ExitMenu)
        {
            CloseInteractionMenu();
            return;
        }

        IInteractable interactable = _currentInteractable;
        CloseInteractionMenu();

        if (interactable == null)
        {
            Debug.LogWarning("실행할 상호작용 대상이 비어 있습니다.");
            return;
        }

        interactable.Interact();
    }
}
