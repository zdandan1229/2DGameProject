using UnityEngine;

public abstract class ScenarioObjectEventReaction : MonoBehaviour
{
    [SerializeField] private string _requiredFlagId;
    [SerializeField] private string _stateFlagId;
    [SerializeField] private string _inactiveOptionText;
    [SerializeField] private string _activeOptionText;

    public string RequiredFlagId
    {
        get { return _requiredFlagId; }
    }

    public string StateFlagId
    {
        get { return _stateFlagId; }
    }

    public bool CanShowEventOption()
    {
        if (ValidateScenarioManager() == false)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_requiredFlagId))
        {
            Debug.LogWarning($"{name} has no required scenario flag id.");
            return false;
        }

        return ScenarioManager.Instance.HasFlag(_requiredFlagId);
    }

    public string GetEventOptionText()
    {
        if (IsEventStateActive())
        {
            return _activeOptionText;
        }

        return _inactiveOptionText;
    }

    public bool IsEventStateActive()
    {
        if (ValidateScenarioManager() == false)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_stateFlagId))
        {
            return false;
        }

        return ScenarioManager.Instance.HasFlag(_stateFlagId);
    }

    public void RestoreCurrentState()
    {
        if (ValidateScenarioManager() == false)
        {
            return;
        }

        ApplyState(IsEventStateActive());
    }

    public void TryExecuteEvent()
    {
        if (CanShowEventOption() == false)
        {
            return;
        }

        bool isActive = IsEventStateActive();
        ExecuteEvent(isActive);
    }

    protected void MarkEventStateActive()
    {
        if (ValidateStateFlagId() == false)
        {
            return;
        }

        ScenarioManager.Instance.MarkFlag(_stateFlagId);
    }

    protected void MarkEventStateInactive()
    {
        if (ValidateStateFlagId() == false)
        {
            return;
        }

        ScenarioManager.Instance.RemoveFlag(_stateFlagId);
    }

    protected abstract void ExecuteEvent(bool isActive);
    protected abstract void ApplyState(bool isActive);

    private bool ValidateStateFlagId()
    {
        if (ValidateScenarioManager() == false)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_stateFlagId))
        {
            Debug.LogWarning($"{name} has no scenario state flag id.");
            return false;
        }

        return true;
    }

    private bool ValidateScenarioManager()
    {
        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning($"{name} cannot use scenario event reaction because ScenarioManager.Instance is missing.");
            return false;
        }

        return true;
    }
}
