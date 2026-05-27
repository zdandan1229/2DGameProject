using System.Collections.Generic;
using UnityEngine;

public class InspectObjectTrigger : MonoBehaviour, IInteractable, IInteractionOptionProvider, IInspectObjectCompleteHandler
{
    [SerializeField] private string _interactionObjectDataId;
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

        if (string.IsNullOrEmpty(_inspectObjectDataId))
        {
            Debug.LogWarning($"{gameObject.name} has no inspect object id, so a fallback inspect option cannot be created.");
            return optionList;
        }

        optionList.Add(new InteractionOption("\uC870\uC0AC\uD558\uAE30", InteractionActionType.OpenInspectObject, _inspectObjectDataId));
        return optionList;
    }

    public void Interact()
    {
        if (string.IsNullOrEmpty(_inspectObjectDataId))
        {
            Debug.LogWarning($"{gameObject.name} has no inspect object id.");
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

        UIManager.Instance.OpenInspectObjectUI(_inspectObjectDataId, this);
    }

    public void CompleteInspectObject(string inspectObjectDataId)
    {
        if (string.IsNullOrEmpty(inspectObjectDataId))
        {
            Debug.LogWarning($"{gameObject.name}의 완료 처리할 조사 오브젝트 ID가 비어 있습니다.");
            return;
        }

        if (inspectObjectDataId != _inspectObjectDataId)
        {
            Debug.LogWarning($"{gameObject.name}의 조사 오브젝트 ID가 일치하지 않습니다. 예상 ID : {_inspectObjectDataId}, 전달 ID : {inspectObjectDataId}");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance가 존재하지 않아 조사 오브젝트를 소지품에 추가할 수 없습니다.");
            return;
        }

        bool isAdded = InventoryManager.Instance.AddInspectObject(inspectObjectDataId);
        if (isAdded == false && InventoryManager.Instance.HasInspectObject(inspectObjectDataId) == false)
        {
            return;
        }

        gameObject.SetActive(false);
    }
}
