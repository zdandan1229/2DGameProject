#if false
using UnityEngine;

public enum Base_SpawnSpotType
{
    None = 0,
    Harvest,
    DropItem,
    Dialogue,
    Monster
}

public enum Base_StartSpawnType
{
    None = 0,
    OnAwake,
    OnEnable,
    OnRange,
    // UniTask나 코루틴으로 일정 시간마다 랜덤 생성도 구현해보자
}

public class Base_SpawnSpot : MonoBehaviour
{
    [SerializeField] private Base_SpawnSpotType _spawnSpotType;
    [SerializeField] private Base_StartSpawnType _startSpawnType;

    [SerializeField] private string _spawnObjectDataId;
    [SerializeField] private Collider2D Collider_OnSpawnStart;

    private void Awake()
    {
        if(_startSpawnType == Base_StartSpawnType.OnAwake)
        {
            StartSpawn();
        }
    }

    private void Start()
    {
        if (_startSpawnType == Base_StartSpawnType.OnEnable)
        {
            StartSpawn();
        }


        if (Collider_OnSpawnStart != null)
        {
            Collider_OnSpawnStart.enabled = (_startSpawnType == Base_StartSpawnType.OnRange);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player") == true)
        {
            StartSpawn();
        }
    }

    private void StartSpawn()
    {
        // TODO - 개선점
        // 이미 스폰된 객체가 있다면, 해당 객체가 사라질때까지 추가적인 스폰을 하지 않도록 추가 처리해야한다

        switch (_spawnSpotType)
        {
            case Base_SpawnSpotType.Harvest:
            case Base_SpawnSpotType.DropItem:
                Base_GameObjectManager.Inst.CreateFieldObject(_spawnObjectDataId, this.transform).Forget();
                // 추가처리가 들어가기 까지는 해당 스폰스팟이 더이상 동작하지 않게 비활성화 한다
                this.gameObject.SetActive(false);
                break;
            case Base_SpawnSpotType.Monster:
                break;
            case Base_SpawnSpotType.Dialogue:
                // 다이얼로그 발생 유형은 시작 시 이 스폰스팟을 더이상 사용하지 않게 비활성화 한다 (제거도 무관)
                Base_UIManager.Instance.OpenDialogueUI(_spawnObjectDataId);
                this.gameObject.SetActive(false);
                break;
        }
    }

}
#endif
