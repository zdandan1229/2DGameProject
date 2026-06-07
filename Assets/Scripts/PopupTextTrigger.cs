using System.Collections.Generic;
using UnityEngine;

public class PopupTextTrigger : MonoBehaviour, IInteractable, IInteractionOptionProvider, IInteractionObjectDataProvider, IInteractionTargetDataProvider, IInteractionPositionProvider
{
    [SerializeField] private string _interactionObjectDataId;
    [SerializeField] private string _completeFlagId;
    [SerializeField] private float _interactionDistance = 1.5f;

    private Collider2D _interactionCollider;

    public Transform InteractionTransform
    {
        get { return transform; }
    }

    public float InteractionDistance
    {
        get { return _interactionDistance; }
    }

    public Vector3 InteractionPosition
    {
        get { return GetInteractionPosition(); }
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
        if (actionType != InteractionActionType.ShowPopupText)
        {
            return string.Empty;
        }

        return GetCurrentPopupTextDataId();
    }

    public void Interact()
    {
        string popupTextDataId = GetCurrentPopupTextDataId();
        if (string.IsNullOrEmpty(popupTextDataId))
        {
            Debug.LogWarning($"{gameObject.name} has no popup text data id.");
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

        PopupTextUI popupTextUI = UIManager.Instance.OpenPopupTextUI(popupTextDataId);
        if (popupTextUI == null)
        {
            return;
        }

        MarkCompleteFlag();
    }

    private void MarkCompleteFlag()
    {
        if (string.IsNullOrEmpty(_completeFlagId))
        {
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning($"{gameObject.name}의 팝업 텍스트 완료 플래그를 기록할 ScenarioManager.Instance가 없습니다.");
            return;
        }

        ScenarioManager.Instance.MarkFlag(_completeFlagId);
    }

    private string GetCurrentPopupTextDataId()
    {
        InteractionObjectData objectData = GetInteractionObjectData();
        if (objectData != null && string.IsNullOrEmpty(objectData.PopupTextDataId) == false)
        {
            return objectData.PopupTextDataId;
        }

        Debug.LogWarning($"{gameObject.name} has no PopupTextDataId in InteractionObjectData.");
        return string.Empty;
    }

    private Vector3 GetInteractionPosition()
    {
        if (_interactionCollider == null)
        {
            _interactionCollider = GetComponent<Collider2D>();
        }

        if (_interactionCollider != null)
        {
            return _interactionCollider.bounds.center;
        }

        Debug.LogWarning($"{gameObject.name} has no Collider2D, so transform position will be used as popup text interaction position.");
        return transform.position;
    }
}
