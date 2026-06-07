using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour, IInteractable, IInteractionOptionProvider, IInteractionObjectDataProvider, IInteractionTargetDataProvider
{
    private const string DefaultLockedPopupTextDataId = "text_entrance_lockeddoor";
    private const string DefaultLockedOptionButtonText = "\uD655\uC778";

    [SerializeField] private string _interactionObjectDataId;
    [SerializeField] private string _targetEntryPointId;
    [SerializeField] private string _requiredFlagId;
    [SerializeField] private string _lockedPopupTextDataId;
    [SerializeField] private float _interactionDistance = 1.5f;

    public Transform InteractionTransform
    {
        get { return transform; }
    }

    public float InteractionDistance
    {
        get { return _interactionDistance; }
    }

    public Transform InteractionMenuTransform
    {
        get { return transform; }
    }

    public string TargetEntryPointId
    {
        get { return _targetEntryPointId; }
    }

    public List<InteractionOption> GetInteractionOptions()
    {
        List<InteractionOption> optionList = InteractionOptionFactory.CreateOptionList(_interactionObjectDataId);
        if (IsLocked() == false)
        {
            return optionList;
        }

        return CreateLockedOptionList(optionList);
    }

    public InteractionObjectData GetInteractionObjectData()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so door interaction object data cannot be loaded.");
            return null;
        }

        if (string.IsNullOrEmpty(_interactionObjectDataId))
        {
            Debug.LogWarning($"{gameObject.name} has no interaction object data id for DoorTrigger.");
            return null;
        }

        return GameDataManager.Instance.GetInteractionObjectData(_interactionObjectDataId);
    }

    public string GetInteractionTargetDataId(InteractionActionType actionType)
    {
        if (actionType == InteractionActionType.ShowPopupText && IsLocked())
        {
            return GetLockedPopupTextDataId();
        }

        if (actionType != InteractionActionType.EnterStage)
        {
            return string.Empty;
        }

        if (IsLocked())
        {
            Debug.LogWarning($"{gameObject.name} is locked, so target entry point id cannot be used.");
            return string.Empty;
        }

        return GetCurrentTargetEntryPointId();
    }

    public void Interact()
    {
        if (IsLocked())
        {
            Debug.LogWarning($"{gameObject.name} is locked, so EnterStage interaction cannot be executed.");
            return;
        }

        string targetEntryPointId = GetCurrentTargetEntryPointId();
        if (string.IsNullOrEmpty(targetEntryPointId))
        {
            Debug.LogWarning($"{gameObject.name} has no target entry point id.");
            return;
        }

        if (WorldTransitionManager.Instance == null)
        {
            Debug.LogWarning("WorldTransitionManager.Instance is missing, so door EnterStage interaction cannot be executed.");
            return;
        }

        WorldTransitionManager.Instance.MovePlayerToEntryPoint(targetEntryPointId);
    }

    private bool IsLocked()
    {
        if (string.IsNullOrEmpty(_requiredFlagId))
        {
            return false;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning($"{gameObject.name} has a required flag id, but ScenarioManager.Instance is missing.");
            return true;
        }

        return ScenarioManager.Instance.HasFlag(_requiredFlagId) == false;
    }

    private List<InteractionOption> CreateLockedOptionList(List<InteractionOption> optionList)
    {
        List<InteractionOption> lockedOptionList = new List<InteractionOption>();
        bool hasEnterStageOption = false;

        if (optionList != null)
        {
            for (int i = 0; i < optionList.Count; i++)
            {
                InteractionOption interactionOption = optionList[i];
                if (interactionOption == null)
                {
                    continue;
                }

                if (interactionOption.ActionType == InteractionActionType.EnterStage)
                {
                    hasEnterStageOption = true;
                    lockedOptionList.Add(new InteractionOption(interactionOption.ButtonText, InteractionActionType.ShowPopupText));
                    continue;
                }

                lockedOptionList.Add(interactionOption);
            }
        }

        if (hasEnterStageOption == false)
        {
            Debug.LogWarning($"{gameObject.name} is locked, but it has no EnterStage interaction option. A default locked popup option will be added.");
            lockedOptionList.Add(new InteractionOption(DefaultLockedOptionButtonText, InteractionActionType.ShowPopupText));
        }

        return lockedOptionList;
    }

    private string GetLockedPopupTextDataId()
    {
        if (string.IsNullOrEmpty(_lockedPopupTextDataId) == false)
        {
            return _lockedPopupTextDataId;
        }

        return DefaultLockedPopupTextDataId;
    }

    private string GetCurrentTargetEntryPointId()
    {
        if (string.IsNullOrEmpty(_targetEntryPointId) == false)
        {
            return _targetEntryPointId;
        }

        Debug.LogWarning($"{gameObject.name} has no target entry point id.");
        return string.Empty;
    }
}
