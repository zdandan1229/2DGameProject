using System.Collections.Generic;
using UnityEngine;

public class NPCDialogueTrigger : MonoBehaviour, IInteractable, IInteractionOptionProvider, IInteractionObjectDataProvider, IInteractionTargetDataProvider
{
    [SerializeField] private string _interactionObjectDataId;
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

    public List<InteractionOption> GetInteractionOptions()
    {
        return InteractionOptionFactory.CreateOptionList(_interactionObjectDataId);
    }

    public InteractionObjectData GetInteractionObjectData()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so interaction object data cannot be loaded.");
            return null;
        }

        if (string.IsNullOrEmpty(_interactionObjectDataId))
        {
            return null;
        }

        return GameDataManager.Instance.GetInteractionObjectData(_interactionObjectDataId);
    }

    public string GetInteractionTargetDataId(InteractionActionType actionType)
    {
        InteractionObjectData interactionObjectData = GetInteractionObjectData();
        if (interactionObjectData == null)
        {
            Debug.LogWarning($"{gameObject.name} has no interaction object data.");
            return string.Empty;
        }

        if (actionType == InteractionActionType.OpenStatus)
        {
            if (string.IsNullOrEmpty(interactionObjectData.CharacterDataId))
            {
                Debug.LogWarning($"{interactionObjectData.Id} has no CharacterDataId.");
                return string.Empty;
            }

            return interactionObjectData.CharacterDataId;
        }

        if (actionType != InteractionActionType.OpenDialogue)
        {
            return string.Empty;
        }

        return GetCurrentDialogueDataId(interactionObjectData);
    }

    public void Interact()
    {
        InteractionObjectData interactionObjectData = GetInteractionObjectData();
        string dialogueDataId = GetCurrentDialogueDataId(interactionObjectData);
        if (string.IsNullOrEmpty(dialogueDataId))
        {
            Debug.LogWarning($"{gameObject.name} has no dialogue id.");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.CanWorldInteract() == false)
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing.");
            return;
        }

        UIManager.Instance.OpenDialogueUI(dialogueDataId, string.Empty, () => MarkDialogueCompleteFlag(interactionObjectData));
    }

    private string GetCurrentDialogueDataId()
    {
        return GetCurrentDialogueDataId(GetInteractionObjectData());
    }

    private string GetCurrentDialogueDataId(InteractionObjectData interactionObjectData)
    {
        if (interactionObjectData == null)
        {
            Debug.LogWarning($"{gameObject.name} has no interaction object data.");
            return string.Empty;
        }

        bool isFirstDialogueComplete = IsDialogueComplete(interactionObjectData);
        if (isFirstDialogueComplete && string.IsNullOrEmpty(interactionObjectData.RepeatDialogueDataId) == false)
        {
            return interactionObjectData.RepeatDialogueDataId;
        }

        if (string.IsNullOrEmpty(interactionObjectData.DialogueDataId))
        {
            Debug.LogWarning($"{interactionObjectData.Id} has no DialogueDataId.");
            return string.Empty;
        }

        return interactionObjectData.DialogueDataId;
    }

    private bool IsDialogueComplete(InteractionObjectData interactionObjectData)
    {
        if (interactionObjectData == null || string.IsNullOrEmpty(interactionObjectData.DialogueCompleteFlagId))
        {
            return false;
        }

        if (ScenarioManager.Instance == null)
        {
            return false;
        }

        return ScenarioManager.Instance.HasFlag(interactionObjectData.DialogueCompleteFlagId);
    }

    private void MarkDialogueCompleteFlag(InteractionObjectData interactionObjectData)
    {
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
}
