using UnityEngine;

public enum PlayerIdleDirection
{
    SideFrontRight,
    SideFrontLeft,
    SideBackRight,
    SideBackLeft,
    Front,
    Back
}

public class StageEntryPoint : MonoBehaviour
{
    [SerializeField] private string _entryPointId;
    [SerializeField] private bool _isStartSpawnPoint;
    [SerializeField] private PlayerIdleDirection _playerIdleDirection = PlayerIdleDirection.SideFrontRight;

    public string EntryPointId
    {
        get
        {
            if (string.IsNullOrEmpty(_entryPointId) == false)
            {
                return _entryPointId;
            }

            StageInfo stageInfo = GetComponentInParent<StageInfo>();
            if (stageInfo != null && gameObject.name == "StageEntryPoint")
            {
                return stageInfo.name;
            }

            return gameObject.name;
        }
    }

    public PlayerIdleDirection PlayerIdleDirection => _playerIdleDirection;
    public bool IsStartSpawnPoint => _isStartSpawnPoint;

    private void OnEnable()
    {
        if (WorldTransitionManager.Instance == null)
        {
            return;
        }

        WorldTransitionManager.Instance.RegisterStageEntryPoint(this);
    }

    private void OnDisable()
    {
        if (WorldTransitionManager.Instance == null)
        {
            return;
        }

        WorldTransitionManager.Instance.UnregisterStageEntryPoint(this);
    }
}
