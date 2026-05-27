using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private List<string> _inspectObjectDataIdList = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("InventoryManager Instance가 이미 존재합니다. 중복 생성된 InventoryManager를 비활성화합니다.");
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
    }

    public bool AddInspectObject(string inspectObjectDataId)
    {
        if (string.IsNullOrEmpty(inspectObjectDataId))
        {
            Debug.LogWarning("소지품에 추가할 조사 오브젝트 ID가 비어 있습니다.");
            return false;
        }

        if (HasInspectObject(inspectObjectDataId))
        {
            Debug.LogWarning($"이미 소지품에 추가된 조사 오브젝트입니다 : {inspectObjectDataId}");
            return false;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 조사 오브젝트 데이터를 확인할 수 없습니다.");
            return false;
        }

        InspectObjectData inspectObjectData = GameDataManager.Instance.GetInspectObjectData(inspectObjectDataId);
        if (inspectObjectData == null)
        {
            Debug.LogWarning($"소지품에 추가할 조사 오브젝트 데이터가 존재하지 않습니다 : {inspectObjectDataId}");
            return false;
        }

        _inspectObjectDataIdList.Add(inspectObjectDataId);
        return true;
    }

    public bool HasInspectObject(string inspectObjectDataId)
    {
        if (string.IsNullOrEmpty(inspectObjectDataId))
        {
            return false;
        }

        return _inspectObjectDataIdList.Contains(inspectObjectDataId);
    }

    public List<string> GetInspectObjectDataIdList()
    {
        List<string> copiedList = new List<string>();

        for (int i = 0; i < _inspectObjectDataIdList.Count; i++)
        {
            copiedList.Add(_inspectObjectDataIdList[i]);
        }

        return copiedList;
    }
}
