using System.Collections.Generic;
using System;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }
    public event Action<string> OnFlagMarked;

    private HashSet<string> _activeFlagIdSet = new HashSet<string>();
    private HashSet<string> _completedInspectObjectDataIdSet = new HashSet<string>();
    private HashSet<string> _completedScenarioEventIdSet = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("ScenarioManager Instance가 이미 존재합니다. 중복 생성된 ScenarioManager를 비활성화합니다.");
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
    }

    public bool MarkFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            Debug.LogWarning("등록할 시나리오 플래그 ID가 비어 있습니다.");
            return false;
        }

        bool isAdded = _activeFlagIdSet.Add(flagId);
        if (isAdded)
        {
            OnFlagMarked?.Invoke(flagId);
        }

        return isAdded;
    }

    public bool RemoveFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            Debug.LogWarning("제거할 시나리오 플래그 ID가 비어 있습니다.");
            return false;
        }

        return _activeFlagIdSet.Remove(flagId);
    }

    public bool HasFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            return false;
        }

        return _activeFlagIdSet.Contains(flagId);
    }

    public bool MarkInspectComplete(string inspectObjectDataId)
    {
        if (string.IsNullOrEmpty(inspectObjectDataId))
        {
            Debug.LogWarning("완료 처리할 조사 오브젝트 ID가 비어 있습니다.");
            return false;
        }

        return _completedInspectObjectDataIdSet.Add(inspectObjectDataId);
    }

    public bool IsInspectComplete(string inspectObjectDataId)
    {
        if (string.IsNullOrEmpty(inspectObjectDataId))
        {
            return false;
        }

        return _completedInspectObjectDataIdSet.Contains(inspectObjectDataId);
    }

    public bool AreAllInspectsComplete(IEnumerable<string> inspectObjectDataIdList)
    {
        if (inspectObjectDataIdList == null)
        {
            Debug.LogWarning("확인할 조사 오브젝트 ID 목록이 비어 있습니다.");
            return false;
        }

        foreach (string inspectObjectDataId in inspectObjectDataIdList)
        {
            if (string.IsNullOrEmpty(inspectObjectDataId))
            {
                Debug.LogWarning("조사 완료 조건 목록에 비어 있는 ID가 있습니다.");
                return false;
            }

            if (IsInspectComplete(inspectObjectDataId) == false)
            {
                return false;
            }
        }

        return true;
    }

    public bool MarkScenarioEventComplete(string scenarioEventId)
    {
        if (string.IsNullOrEmpty(scenarioEventId))
        {
            Debug.LogWarning("완료 처리할 시나리오 이벤트 ID가 비어 있습니다.");
            return false;
        }

        return _completedScenarioEventIdSet.Add(scenarioEventId);
    }

    public bool IsScenarioEventComplete(string scenarioEventId)
    {
        if (string.IsNullOrEmpty(scenarioEventId))
        {
            return false;
        }

        return _completedScenarioEventIdSet.Contains(scenarioEventId);
    }

    public List<string> GetActiveFlagIdList()
    {
        return new List<string>(_activeFlagIdSet);
    }

    public List<string> GetCompletedInspectObjectDataIdList()
    {
        return new List<string>(_completedInspectObjectDataIdSet);
    }

    public List<string> GetCompletedScenarioEventIdList()
    {
        return new List<string>(_completedScenarioEventIdSet);
    }

    public void ClearScenarioState()
    {
        _activeFlagIdSet.Clear();
        _completedInspectObjectDataIdSet.Clear();
        _completedScenarioEventIdSet.Clear();
    }
}
