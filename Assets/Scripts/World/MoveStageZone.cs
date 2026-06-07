using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MoveStageZone : MonoBehaviour
{
    [SerializeField] private string _targetEntryPointId;
    [SerializeField] private string _interactionOptionDataId = "option_enterstage_next";
    [SerializeField] private Vector3 _menuWorldOffset = new Vector3(1.3f, 1.8f, 0f);
    [SerializeField] private Vector3 _menuWorldOffsetWhenNearRightEdge = new Vector3(-1.3f, 1.8f, 0f);
    [SerializeField, Range(0f, 1f)] private float _rightEdgeViewportLine = 0.85f;

    private Player _currentPlayer;
    private InteractionMenuUI _openedInteractionMenuUI;
    private int _enteredPlayerContactCount;

    private void Reset()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            collider2D.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        CloseMoveStageMenu();
        _enteredPlayerContactCount = 0;
        _currentPlayer = null;
    }

    private void Update()
    {
        if (_currentPlayer == null)
        {
            return;
        }

        if (CanOpenMoveStageMenu() == false)
        {
            CloseMoveStageMenu();
            return;
        }

        if (_openedInteractionMenuUI == null)
        {
            OpenMoveStageMenu();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = FindPlayer(other);
        if (player == null)
        {
            return;
        }

        if (_currentPlayer != null && _currentPlayer != player)
        {
            Debug.LogWarning($"{gameObject.name} is already tracking another player.");
            return;
        }

        _enteredPlayerContactCount++;
        if (_currentPlayer == player)
        {
            return;
        }

        _currentPlayer = player;
        OpenMoveStageMenu();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = FindPlayer(other);
        if (player == null || player != _currentPlayer)
        {
            return;
        }

        _enteredPlayerContactCount = Mathf.Max(0, _enteredPlayerContactCount - 1);
        if (_enteredPlayerContactCount > 0)
        {
            return;
        }

        CloseMoveStageMenu();
        _currentPlayer = null;
    }

    private Player FindPlayer(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        return other.GetComponentInParent<Player>();
    }

    private void OpenMoveStageMenu()
    {
        if (CanOpenMoveStageMenu() == false)
        {
            return;
        }

        if (_currentPlayer == null)
        {
            Debug.LogWarning($"{gameObject.name} has no player, so move stage menu cannot be opened.");
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing, so move stage menu cannot be opened.");
            return;
        }

        List<InteractionOption> optionList = CreateOptionList();
        if (optionList == null || optionList.Count <= 0)
        {
            Debug.LogWarning($"{gameObject.name} has no move stage option to display.");
            return;
        }

        _openedInteractionMenuUI = UIManager.Instance.OpenInteractionMenuUIWithoutMoveLock(
            optionList,
            _currentPlayer.transform,
            _menuWorldOffset,
            _menuWorldOffsetWhenNearRightEdge,
            _rightEdgeViewportLine,
            OnClickMoveStageOption
        );
    }

    private List<InteractionOption> CreateOptionList()
    {
        List<InteractionOption> optionList = new List<InteractionOption>();

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so move stage option cannot be loaded.");
            return optionList;
        }

        if (string.IsNullOrEmpty(_interactionOptionDataId))
        {
            Debug.LogWarning($"{gameObject.name} has no interaction option data id.");
            return optionList;
        }

        InteractionOptionData optionData = GameDataManager.Instance.GetInteractionOptionData(_interactionOptionDataId);
        if (optionData == null)
        {
            Debug.LogWarning($"InteractionOption data was not found: {_interactionOptionDataId}");
            return optionList;
        }

        if (GameUtil.TryParseEnumText(optionData.ActionType, out InteractionActionType actionType) == false)
        {
            Debug.LogWarning($"InteractionOption has an invalid ActionType. id: {_interactionOptionDataId}, ActionType: {optionData.ActionType}");
            return optionList;
        }

        InteractionOption interactionOption = new InteractionOption(optionData.ButtonText, actionType);
        if (interactionOption.CanShowInMenu() == false)
        {
            Debug.LogWarning($"InteractionOption cannot be shown in move stage menu: {_interactionOptionDataId}");
            return optionList;
        }

        optionList.Add(interactionOption);
        return optionList;
    }

    private void OnClickMoveStageOption(InteractionOption interactionOption)
    {
        if (interactionOption == null)
        {
            Debug.LogWarning("Move stage option is null.");
            return;
        }

        if (interactionOption.ActionType != InteractionActionType.EnterStage)
        {
            Debug.LogWarning($"MoveStageZone only supports EnterStage action. ActionType: {interactionOption.ActionType}");
            return;
        }

        MoveToTargetStage();
    }

    private void MoveToTargetStage()
    {
        if (CanOpenMoveStageMenu() == false)
        {
            Debug.LogWarning($"{gameObject.name} cannot move stage because direct stage movement is locked.");
            return;
        }

        if (string.IsNullOrEmpty(_targetEntryPointId))
        {
            Debug.LogWarning($"{gameObject.name} has no target stage entry point id.");
            return;
        }

        if (WorldTransitionManager.Instance == null)
        {
            Debug.LogWarning("WorldTransitionManager.Instance is missing, so player cannot move to target stage.");
            return;
        }

        CloseMoveStageMenu();
        WorldTransitionManager.Instance.MovePlayerToEntryPoint(_targetEntryPointId);
    }

    private void CloseMoveStageMenu()
    {
        _openedInteractionMenuUI = null;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseInteractionMenuUI();
        }
    }

    private bool CanOpenMoveStageMenu()
    {
        if (GameManager.Instance == null)
        {
            return true;
        }

        return GameManager.Instance.CanDirectStageMove();
    }
}
