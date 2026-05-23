using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Inst { get; set; }

    private void Awake()
    {
        Inst = this;
    }

    // 로드된 에셋들을 관리하기 위한 캐시 (메모리 해제 시 필요)
    private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();

    //// 1. 에셋 로드 함수 (제네릭 사용)
    public void LoadAsset<T>(string address, System.Action<T> callback) where T : UnityEngine.Object
    {
        // 이미 로드된 에셋인지 확인
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle))
        {
            callback?.Invoke(handle.Result as T);
            return;
        }

        // 어드레서블 로드 실행
        AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(address);

        // 비동기 처리를 위한 한시적 람다 사용
        loadHandle.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                _handles[address] = op; // 핸들 저장
                callback?.Invoke(op.Result);
            }
            else
            {
                Debug.LogError($"에셋 로드 실패: {address}");
            }
        };
    }

    public async UniTask<T> LoadAsset<T>(string address) where T : UnityEngine.Object
    {
        // 1. 이미 로드된 에셋인지 확인 (캐싱 확인)
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle))
        {
            // 이미 완료된 핸들이라면 즉시 결과 반환
            return handle.Result as T;
        }

        // 2. 어드레서블 로드 실행 (UniTask로 변환)
        // ToUniTask()를 사용하여 await 가능하게 만듭니다.
        AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(address);

        try
        {
            T result = await loadHandle.ToUniTask();

            // 3. 핸들 저장 (성공 시)
            _handles[address] = loadHandle;
            return result;
        }
        catch (System.Exception e)
        {
            // 4. 실패 시 예외 처리
            Debug.LogError($"에셋 로드 실패: {address} / Error: {e.Message}");

            // 실패한 핸들도 메모리 해제가 필요할 수 있으므로 상황에 따라 Release 처리
            if (loadHandle.IsValid())
                Addressables.Release(loadHandle);

            return null;
        }
    }

    public async UniTask<GameObject> InstantiateAsync(string address, Transform parent = null, bool instantiateInWorldSpace = false)
    {
        // 1. 생성 시도
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent, instantiateInWorldSpace);

        try
        {
            // 2. UniTask로 변환하여 비동기 대기
            GameObject instance = await handle.ToUniTask();

            // 3. 성공 시 생성된 오브젝트 반환
            return instance;
        }
        catch (System.Exception e)
        {
            // 4. 실패 시 예외 처리 및 핸들 해제
            Debug.LogError($"프리팹 생성 실패: {address} / Error: {e.Message}");

            if (handle.IsValid())
                Addressables.Release(handle);

            return null;
        }
    }

    // 2-1. 스프라이트 로드 함수
    public void LoadSprite(string address, System.Action<Sprite> callback)
    {
        // 이미 로드된 스프라이트인지 확인 (캐시 활용)
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle))
        {
            callback?.Invoke(handle.Result as Sprite);
            return;
        }

        // 스프라이트 형식으로 로드
        AsyncOperationHandle<Sprite> handleOrigin = Addressables.LoadAssetAsync<Sprite>(address);

        handleOrigin.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                _handles[address] = op; // 핸들 저장 (나중에 Release하기 위함)
                callback?.Invoke(op.Result);
            }
            else
            {
                Debug.LogError($"스프라이트 로드 실패: {address}");
            }
        };
    }

    public async UniTask<Sprite> LoadSprite(string address)
    {
        // 1. 이미 로드된 스프라이트인지 확인 (캐시 활용)
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle))
        {
            // 결과가 Sprite인지 확인 후 반환
            return handle.Result as Sprite;
        }

        // 2. 스프라이트 형식으로 로드 실행
        AsyncOperationHandle<Sprite> handleOrigin = Addressables.LoadAssetAsync<Sprite>(address);

        try
        {
            // ToUniTask()를 통해 비동기 대기
            Sprite result = await handleOrigin.ToUniTask();

            // 3. 핸들 저장 (나중에 Release하기 위함)
            _handles[address] = handleOrigin;

            return result;
        }
        catch (System.Exception)
        {
            // 4. 로드 실패 시 처리
            Debug.LogError($"스프라이트 로드 실패: {address}");

            if (handleOrigin.IsValid())
                Addressables.Release(handleOrigin);

            return null;
        }
    }

    // 3. 메모리 해제 함수 (중요!)
    public void Release(string address)
    {
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
            _handles.Remove(address);
            Debug.Log($"에셋 메모리 해제 완료: {address}");
        }
    }
}
