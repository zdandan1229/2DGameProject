using System.Collections.Generic;
using UnityEngine;

public class NPCDialogueTrigger : MonoBehaviour, IInteractable, IInteractionOptionProvider
{
    [SerializeField] private string _interactionObjectDataId;
    [SerializeField] private string _startDialogueId;
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
        List<InteractionOption> optionList = InteractionOptionFactory.CreateOptionList(_interactionObjectDataId);
        if (optionList.Count > 0)
        {
            return optionList;
        }

        if (string.IsNullOrEmpty(_startDialogueId))
        {
            Debug.LogWarning($"{gameObject.name} has no dialogue id, so a fallback dialogue option cannot be created.");
            return optionList;
        }

        optionList.Add(new InteractionOption("\uB300\uD654\uD558\uAE30", InteractionActionType.OpenDialogue, _startDialogueId));
        return optionList;
    }

    public void Interact()
    {
        if (string.IsNullOrEmpty(_startDialogueId))
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

        UIManager.Instance.OpenDialogueUI(_startDialogueId);
    }
}
