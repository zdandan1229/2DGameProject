using UnityEngine;

public class NPCDialogueTrigger : MonoBehaviour, IInteractable
{
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

    public void Interact()
    {
        if (string.IsNullOrEmpty(_startDialogueId))
        {
            Debug.LogWarning($"{gameObject.name}의 시작 대화 ID가 비어 있습니다.");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.CanWorldInteract() == false)
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 존재하지 않습니다.");
            return;
        }

        UIManager.Instance.OpenDialogueUI(_startDialogueId);
    }
}
