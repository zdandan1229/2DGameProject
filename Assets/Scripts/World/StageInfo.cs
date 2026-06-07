using UnityEngine;

public enum StageType
{
    Outside,
    SmallRoom,
    LargeRoom,
    Corridor
}

public class StageInfo : MonoBehaviour
{
    [SerializeField] private StageType _stageType = StageType.SmallRoom;

    public StageType StageType
    {
        get { return _stageType; }
    }
}
