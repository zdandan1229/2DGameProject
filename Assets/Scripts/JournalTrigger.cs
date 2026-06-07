using System.Collections.Generic;
using UnityEngine;

public class JournalTrigger : MonoBehaviour, IInteractable, IInteractionOptionProvider, IInteractionObjectDataProvider, IInteractionTargetDataProvider, IJournalInspectCompleteHandler
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
        if (actionType != InteractionActionType.OpenJournal)
        {
            return string.Empty;
        }

        return GetCurrentJournalDataId();
    }

    public void Interact()
    {
        string journalDataId = GetCurrentJournalDataId();
        if (string.IsNullOrEmpty(journalDataId))
        {
            Debug.LogWarning($"{gameObject.name} has no journal id.");
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

        UIManager.Instance.OpenJournalInspectUI(journalDataId, this);
    }

    public void CompleteJournalInspect(string journalDataId)
    {
        if (string.IsNullOrEmpty(journalDataId))
        {
            Debug.LogWarning($"{gameObject.name}의 완료 처리용 JournalData Id가 비어 있습니다.");
            return;
        }

        string expectedJournalDataId = GetCurrentJournalDataId();
        if (string.IsNullOrEmpty(expectedJournalDataId))
        {
            Debug.LogWarning($"{gameObject.name}의 기준 JournalData Id가 비어 있습니다.");
            return;
        }

        if (journalDataId != expectedJournalDataId)
        {
            Debug.LogWarning($"{gameObject.name}의 JournalData Id가 일치하지 않습니다. 예상 ID : {expectedJournalDataId}, 전달 ID : {journalDataId}");
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 일지 데이터를 확인할 수 없습니다.");
            return;
        }

        JournalData journalData = GameDataManager.Instance.GetJournalData(journalDataId);
        if (journalData == null)
        {
            Debug.LogWarning($"일지 데이터가 존재하지 않습니다 : {journalDataId}");
            return;
        }

        JournalManager journalManager = JournalManager.Instance;
        if (journalManager == null)
        {
            Debug.LogWarning("JournalManager.Instance가 없어 일지를 등록할 수 없습니다. ManagerContainer에 JournalManager를 배치했는지 확인하세요.");
            return;
        }

        journalManager.AddJournal(journalDataId);

        gameObject.SetActive(false);
    }

    private string GetCurrentJournalDataId()
    {
        InteractionObjectData objectData = GetInteractionObjectData();
        if (objectData != null && string.IsNullOrEmpty(objectData.JournalDataId) == false)
        {
            return objectData.JournalDataId;
        }

        Debug.LogWarning($"{gameObject.name} has no JournalDataId in InteractionObjectData.");
        return string.Empty;
    }
}
