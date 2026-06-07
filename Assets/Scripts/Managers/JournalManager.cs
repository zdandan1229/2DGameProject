using System.Collections.Generic;
using UnityEngine;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance { get; private set; }

    private List<string> _journalDataIdList = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("JournalManager Instance is already set. The duplicate JournalManager will be disabled.");
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
    }

    public bool AddJournal(string journalDataId)
    {
        if (string.IsNullOrEmpty(journalDataId))
        {
            Debug.LogWarning("일지에 추가할 JournalData Id가 비어 있습니다.");
            return false;
        }

        if (HasJournal(journalDataId))
        {
            return false;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 일지 데이터를 확인할 수 없습니다.");
            return false;
        }

        JournalData journalData = GameDataManager.Instance.GetJournalData(journalDataId);
        if (journalData == null)
        {
            Debug.LogWarning($"일지 데이터가 존재하지 않아 추가할 수 없습니다 : {journalDataId}");
            return false;
        }

        _journalDataIdList.Add(journalDataId);
        return true;
    }

    public bool HasJournal(string journalDataId)
    {
        if (string.IsNullOrEmpty(journalDataId))
        {
            return false;
        }

        return _journalDataIdList.Contains(journalDataId);
    }

    public List<string> GetJournalDataIdList()
    {
        List<string> copiedList = new List<string>();

        for (int i = 0; i < _journalDataIdList.Count; i++)
        {
            copiedList.Add(_journalDataIdList[i]);
        }

        return copiedList;
    }
}
