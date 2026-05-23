using UnityEngine;

public class InspectObjectTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private string _inspectObjectDataId;
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
        if (string.IsNullOrEmpty(_inspectObjectDataId))
        {
            Debug.LogWarning($"{gameObject.name}의 조사 오브젝트 데이터 ID가 비어 있습니다.");
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

        UIManager.Instance.OpenInspectObjectUI(_inspectObjectDataId);
    }
}
