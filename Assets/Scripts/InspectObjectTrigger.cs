using System.Collections.Generic;
using UnityEngine;

public class InspectObjectTrigger : MonoBehaviour, IInteractable, IInteractionOptionProvider, IInteractionObjectDataProvider, IInteractionTargetDataProvider, IInspectObjectCompleteHandler, IInspectObjectCompleteOptionProvider
{
    [SerializeField] private string _interactionObjectDataId;
    [SerializeField] private string _completeFlagId;
    [SerializeField] private bool _showCompleteDialogue = true;
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
        if (actionType == InteractionActionType.OpenInspectObject)
        {
            return GetCurrentInspectObjectDataId();
        }

        if (actionType == InteractionActionType.OpenInspectArea)
        {
            return GetCurrentInspectAreaDataId();
        }

        return string.Empty;
    }

    public void Interact()
    {
        string inspectObjectDataId = GetCurrentInspectObjectDataId();
        if (string.IsNullOrEmpty(inspectObjectDataId))
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

        UIManager.Instance.OpenInspectObjectUI(inspectObjectDataId, this);
    }

    public bool CompleteInspectObject(string inspectObjectDataId)
    {
        if (string.IsNullOrEmpty(inspectObjectDataId))
        {
            Debug.LogWarning($"{gameObject.name}의 완료 처리할 조사 오브젝트 ID가 비어 있습니다.");
            return false;
        }

        string expectedInspectObjectDataId = GetCurrentInspectObjectDataId();
        if (string.IsNullOrEmpty(expectedInspectObjectDataId))
        {
            Debug.LogWarning($"{gameObject.name}의 기준 조사 오브젝트 ID가 비어 있습니다.");
            return false;
        }

        if (inspectObjectDataId != expectedInspectObjectDataId)
        {
            Debug.LogWarning($"{gameObject.name}의 조사 오브젝트 ID가 일치하지 않습니다. 예상 ID : {expectedInspectObjectDataId}, 전달 ID : {inspectObjectDataId}");
            return false;
        }

        MarkCompleteFlag();
        gameObject.SetActive(false);
        return true;
    }

    public bool ShouldOpenCompleteDialogue()
    {
        return _showCompleteDialogue;
    }

    private void MarkCompleteFlag()
    {
        if (string.IsNullOrEmpty(_completeFlagId))
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning($"{gameObject.name}의 조사 완료 플래그를 기록할 ScenarioManager.Instance가 없습니다.");
            return;
        }

        ScenarioManager.Instance.MarkFlag(_completeFlagId);
    }

    private string GetCurrentInspectObjectDataId()
    {
        InteractionObjectData objectData = GetInteractionObjectData();
        if (objectData != null && string.IsNullOrEmpty(objectData.InspectObjectDataId) == false)
        {
            return objectData.InspectObjectDataId;
        }

        Debug.LogWarning($"{gameObject.name} has no InspectObjectDataId in InteractionObjectData.");
        return string.Empty;
    }

    private string GetCurrentInspectAreaDataId()
    {
        InteractionObjectData objectData = GetInteractionObjectData();
        if (objectData != null && string.IsNullOrEmpty(objectData.InspectAreaDataId) == false)
        {
            return objectData.InspectAreaDataId;
        }

        Debug.LogWarning($"{gameObject.name} has no InspectAreaDataId in InteractionObjectData.");
        return string.Empty;
    }
}
