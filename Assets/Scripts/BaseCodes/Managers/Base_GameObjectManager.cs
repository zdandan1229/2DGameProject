#if false
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Base_GameObjectManager : MonoBehaviour
{
    // 생성할 몬스터의 프리팹
    [SerializeField] private GameObject Prefab_Enemy;
    [SerializeField] private Transform Root_Enemy;

    public static Base_GameObjectManager Inst { get; set; }

    // 생성된 오브젝트의 키가 됨
    private int _objectInstanceKeyGenerator = 0;

    // 생성된 오브젝트의 생명을 보관
    private Dictionary<int, GameObject> _createdGameObjectContainer = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> _fieldObjectContainer = new Dictionary<int, GameObject>();

    private void Awake()
    {
        Inst = this;
    }

    public void RequestSpawnEnemy()
    {
        if(Prefab_Enemy == null)
        {
            Debug.LogWarning("프리팹이 등록되지 않은 오브젝트 입니다.");
            return;
        }

        var gObj = Instantiate(Prefab_Enemy, Root_Enemy);
        if(gObj == null)
        {
            Debug.LogWarning("생성에 실패한 게임 오브젝트 입니다.");
            return;
        }

        // 1-1 생성에 성공했다면, 미리 Key를 발급한다.
        _objectInstanceKeyGenerator++;

        // 1-2 Dictionary에 추가하기 전에 미리 키 검사한다
        if (_createdGameObjectContainer.ContainsKey(_objectInstanceKeyGenerator) == true)
        {
            Debug.LogWarning("이미 동일한 키가 발급된 게임 오브젝트가 존재합니다");
            return;
        }

        // 1-3 동적생성(실체화)된 오브젝트를 게임 오브젝트 매니저의 자료구조(Dictionary)에 보관하자!
        _createdGameObjectContainer.Add(_objectInstanceKeyGenerator, gObj);

        Debug.Log($"키: {_objectInstanceKeyGenerator}의 객체 {gObj.name}이 호출되었습니다.");
    }


    public GameObject GetEntityObjectCanBeNull(int instanceId)
    {
        if(_createdGameObjectContainer.ContainsKey(instanceId) == false)
        {
            Debug.LogWarning($"{instanceId}는 존재하지 않습니다.");
            return null;
        }

        // 2-1 실체화하면서 등록된 게임 오브젝트가 있다면 반환
        return _createdGameObjectContainer[instanceId];
    } 

    public void RequestDestroyEntityObject(int instanceId)
    {
        var gObj = GetEntityObjectCanBeNull(instanceId);
        if(gObj == null)
        {
            return;
        }

        // 3-1 요청된 객체를 제거함
        _createdGameObjectContainer.Remove(instanceId);
        Destroy(gObj);
    }






    //[필드 오브젝트] ====================================================================================================

    public async UniTaskVoid CreateFieldObject(string fieldObjectDataId, Transform spawnSpot)
    {
        var fieldObject = Base_GameDataManager.Instance.GetFieldObjectData(fieldObjectDataId);
        if (fieldObject != null)
        {
            var createdObj = await Base_ResourceManager.Inst.InstantiateAsync(fieldObject.PrefabPath, Root_Enemy, true);
            createdObj.transform.position = spawnSpot.position;
            AddFieldObjectOnCreate(createdObj, fieldObjectDataId);
        }
    }

    private void AddFieldObjectOnCreate(GameObject createdObject, string fieldObjectDataId)
    {
        _objectInstanceKeyGenerator++;
        var generatedInstanceId = _objectInstanceKeyGenerator;
        _fieldObjectContainer.Add(generatedInstanceId, createdObject);
    }

    public void RequestDestroyFieldObject(int instanceId)
    {
        var fieldObjectComponent = GetFieldObjectByInstanceId(instanceId);
        if (fieldObjectComponent == null)
        {
            return;
        }

        // 요청된 필드 오브젝트를 제거함
        _fieldObjectContainer.Remove(instanceId);
        Destroy(fieldObjectComponent);
    }

    public GameObject GetFieldObjectByInstanceId(int fieldObjectInstanceId)
    {
        if(_fieldObjectContainer.ContainsKey(fieldObjectInstanceId) == false)
        {
            Debug.LogError($"{fieldObjectInstanceId} 찾으려는 필드 오브젝트가 유효하지 않습니다");
            return null;
        }

        return _fieldObjectContainer[fieldObjectInstanceId];
    } 
}
#endif
