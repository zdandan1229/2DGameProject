using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WorldTransitionManager : MonoBehaviour
{
    private const string CorridorCameraCenterName = "CameraCenter";

    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Player _playerPrefab;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private float _miniMapFadeOutDuration = 0.5f;
    [SerializeField] private float _miniMapBlackScreenWaitDuration = 0.5f;
    [SerializeField] private float _miniMapFadeInDuration = 0.8f;

    private readonly Dictionary<string, StageEntryPoint> _stageEntryPointDic = new Dictionary<string, StageEntryPoint>();
    private StageEntryPoint _startSpawnPoint;
    private bool _isTransitioning;

    public static WorldTransitionManager Instance { get; private set; }
    public StageInfo CurrentStageInfo { get; private set; }
    public event Action<StageInfo> OnStageChanged;
    public event Action<StageInfo> OnStageTransitionCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("WorldTransitionManager Instance already exists. Duplicate WorldTransitionManager will be disabled.");
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RegisterAllStageEntryPoints();
    }

    private void RegisterAllStageEntryPoints()
    {
        StageEntryPoint[] stageEntryPoints = FindObjectsByType<StageEntryPoint>(FindObjectsSortMode.None);
        if (stageEntryPoints == null || stageEntryPoints.Length == 0)
        {
            Debug.LogWarning("No StageEntryPoint was found in the scene.");
            return;
        }

        foreach (StageEntryPoint stageEntryPoint in stageEntryPoints)
        {
            RegisterStageEntryPoint(stageEntryPoint);
        }
    }

    public void RegisterStageEntryPoint(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null)
        {
            Debug.LogWarning("Cannot register an empty StageEntryPoint.");
            return;
        }

        string entryPointId = stageEntryPoint.EntryPointId;
        if (string.IsNullOrEmpty(entryPointId))
        {
            Debug.LogWarning($"{stageEntryPoint.name} has no EntryPointId, so it cannot be registered.");
            return;
        }

        if (_stageEntryPointDic.ContainsKey(entryPointId))
        {
            Debug.LogWarning($"{entryPointId} StageEntryPoint is already registered. It will be replaced.");
        }

        _stageEntryPointDic[entryPointId] = stageEntryPoint;
        CacheStartSpawnPoint(stageEntryPoint);
    }

    public void UnregisterStageEntryPoint(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null || string.IsNullOrEmpty(stageEntryPoint.EntryPointId))
        {
            return;
        }

        if (_stageEntryPointDic.TryGetValue(stageEntryPoint.EntryPointId, out StageEntryPoint registeredPoint) == false)
        {
            return;
        }

        if (registeredPoint != stageEntryPoint)
        {
            return;
        }

        _stageEntryPointDic.Remove(stageEntryPoint.EntryPointId);

        if (_startSpawnPoint == stageEntryPoint)
        {
            _startSpawnPoint = null;
        }
    }

    private void CacheStartSpawnPoint(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null || stageEntryPoint.IsStartSpawnPoint == false)
        {
            return;
        }

        if (_startSpawnPoint != null && _startSpawnPoint != stageEntryPoint)
        {
            Debug.LogWarning($"{stageEntryPoint.name} is also marked as StartSpawnPoint. The first registered StartSpawnPoint will be used.");
            return;
        }

        _startSpawnPoint = stageEntryPoint;
    }

    public bool StartNewGame()
    {
        if (_stageEntryPointDic.Count <= 0)
        {
            RegisterAllStageEntryPoints();
        }

        return SpawnPlayerAtEntryPoint(_startSpawnPoint, "StartSpawnPoint");
    }

    public bool StartNewGameAtEntryPoint(string entryPointId)
    {
        if (_stageEntryPointDic.Count <= 0)
        {
            RegisterAllStageEntryPoints();
        }

        if (TryGetStageEntryPoint(entryPointId, out StageEntryPoint stageEntryPoint) == false)
        {
            return false;
        }

        return SpawnPlayerAtEntryPoint(stageEntryPoint, entryPointId);
    }

    private bool SpawnPlayerAtEntryPoint(StageEntryPoint startEntryPoint, string startEntryPointName)
    {
        if (startEntryPoint == null)
        {
            Debug.LogWarning($"{startEntryPointName} is missing, so player cannot be spawned at game start.");
            return false;
        }

        Transform existingPlayerTransform = GetPlayerTransform();
        if (existingPlayerTransform != null)
        {
            SetPlayerEntranceState(startEntryPoint);
            bool moveSucceeded = MovePlayerToPosition(startEntryPoint.transform.position);
            if (moveSucceeded == false)
            {
                return false;
            }

            MoveCameraToStageEntryPointRoom(startEntryPoint);
            NotifyStageChanged(startEntryPoint);
            return true;
        }

        if (_playerPrefab == null)
        {
            Debug.LogError("WorldTransitionManager has no Player prefab, so player cannot be spawned at game start.");
            return false;
        }

        Player player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
        if (player == null)
        {
            Debug.LogError("Player prefab instantiation failed, so player cannot be spawned at game start.");
            return false;
        }

        _playerTransform = player.transform;
        SetPlayerEntranceState(startEntryPoint);
        bool createdPlayerMoveSucceeded = MovePlayerToPosition(startEntryPoint.transform.position);
        if (createdPlayerMoveSucceeded == false)
        {
            return false;
        }

        MoveCameraToStageEntryPointRoom(startEntryPoint);
        NotifyStageChanged(startEntryPoint);
        return true;
    }

    public bool MovePlayerToEntryPoint(string entryPointId)
    {
        return MovePlayerToEntryPoint(entryPointId, null);
    }

    public bool MovePlayerToEntryPoint(string entryPointId, Action onBlackScreenBeforeMove)
    {
        if (TryGetStageEntryPoint(entryPointId, out StageEntryPoint stageEntryPoint) == false)
        {
            return false;
        }

        if (_isTransitioning == true)
        {
            Debug.LogWarning("World transition is already in progress.");
            return false;
        }

        StartCoroutine(MovePlayerToEntryPointWithTransition(stageEntryPoint, onBlackScreenBeforeMove));
        return true;
    }

    public bool MovePlayerToPosition(Vector3 targetPosition)
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            Debug.LogWarning("Player Transform is missing, so world transition cannot be completed.");
            return false;
        }

        Player player = playerTransform.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogWarning("Player component is missing, so world transition cannot be completed.");
            return false;
        }

        return player.MoveTransitionPointToPosition(targetPosition);
    }

    private bool TryGetStageEntryPoint(string entryPointId, out StageEntryPoint stageEntryPoint)
    {
        stageEntryPoint = null;

        if (string.IsNullOrEmpty(entryPointId))
        {
            Debug.LogWarning("EntryPointId is empty, so player cannot be moved.");
            return false;
        }

        if (_stageEntryPointDic.TryGetValue(entryPointId, out stageEntryPoint) == false || stageEntryPoint == null)
        {
            Debug.LogWarning($"{entryPointId} StageEntryPoint could not be found.");
            return false;
        }

        return true;
    }

    private IEnumerator MovePlayerToEntryPointWithTransition(StageEntryPoint stageEntryPoint, Action onBlackScreenBeforeMove)
    {
        _isTransitioning = true;

        bool didPauseGame = RequestPauseGameForTransition();
        ScreenTransitionUI screenTransitionUI = OpenScreenTransitionUI();

        if (screenTransitionUI != null)
        {
            yield return screenTransitionUI.FadeOut(_miniMapFadeOutDuration);
        }

        ClosePlayerMenuUIOnBlackScreen();
        didPauseGame = RequestPauseGameForTransition() || didPauseGame;
        onBlackScreenBeforeMove?.Invoke();

        bool didMove = MovePlayerToEntryPointImmediately(stageEntryPoint);
        if (didMove)
        {
            NotifyStageChanged(stageEntryPoint);
        }

        if (_miniMapBlackScreenWaitDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(_miniMapBlackScreenWaitDuration);
        }

        RequestResumeGameForTransition(didPauseGame);

        if (screenTransitionUI != null)
        {
            yield return screenTransitionUI.FadeIn(_miniMapFadeInDuration);
        }

        _isTransitioning = false;

        if (didMove)
        {
            NotifyStageTransitionCompleted(stageEntryPoint);
        }
    }

    private bool MovePlayerToEntryPointImmediately(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null)
        {
            Debug.LogWarning("StageEntryPoint is missing, so player cannot be moved.");
            return false;
        }

        SetPlayerEntranceState(stageEntryPoint);

        bool moveSucceeded = MovePlayerToPosition(stageEntryPoint.transform.position);
        if (moveSucceeded == false)
        {
            return false;
        }

        MoveCameraToStageEntryPointRoom(stageEntryPoint);
        return true;
    }

    private void NotifyStageChanged(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null)
        {
            Debug.LogWarning("StageEntryPoint is missing, so stage change cannot be notified.");
            return;
        }

        StageInfo stageInfo = stageEntryPoint.GetComponentInParent<StageInfo>();
        if (stageInfo == null)
        {
            Debug.LogWarning($"{stageEntryPoint.name} has no parent StageInfo, so stage change cannot be notified.");
            return;
        }

        CurrentStageInfo = stageInfo;
        OnStageChanged?.Invoke(stageInfo);
    }

    private void NotifyStageTransitionCompleted(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null)
        {
            Debug.LogWarning("StageEntryPoint is missing, so stage transition completion cannot be notified.");
            return;
        }

        StageInfo stageInfo = stageEntryPoint.GetComponentInParent<StageInfo>();
        if (stageInfo == null)
        {
            Debug.LogWarning($"{stageEntryPoint.name} has no parent StageInfo, so stage transition completion cannot be notified.");
            return;
        }

        OnStageTransitionCompleted?.Invoke(stageInfo);
    }

    private bool RequestPauseGameForTransition()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is missing, so transition pause cannot be requested.");
            return false;
        }

        if (GameManager.Instance.IsGamePaused == true)
        {
            return false;
        }

        GameManager.Instance.PauseGame();
        return true;
    }

    private void RequestResumeGameForTransition(bool didPauseGame)
    {
        if (didPauseGame == false)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is missing, so transition pause cannot be released.");
            return;
        }

        GameManager.Instance.ResumeGame();
    }

    private ScreenTransitionUI OpenScreenTransitionUI()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing, so screen transition cannot be opened.");
            return null;
        }

        return UIManager.Instance.OpenScreenTransitionUI();
    }

    private void ClosePlayerMenuUIOnBlackScreen()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing, so PlayerMenuUI cannot be closed on black screen.");
            return;
        }

        UIManager.Instance.ClosePlayerMenuUI();
    }

    private Transform GetPlayerTransform()
    {
        if (_playerTransform != null)
        {
            return _playerTransform;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player == null)
        {
            return null;
        }

        _playerTransform = player.transform;
        return _playerTransform;
    }

    private void SetPlayerEntranceState(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null)
        {
            Debug.LogWarning("StageEntryPoint is missing, so player entrance state cannot be set.");
            return;
        }

        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            Debug.LogWarning("Player Transform is missing, so player entrance state cannot be set.");
            return;
        }

        Player player = playerTransform.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogWarning("Player component is missing, so player entrance state cannot be set.");
            return;
        }

        player.SetIdleDirection(stageEntryPoint.PlayerIdleDirection);
        ApplyPlayerOptionByStageType(player, stageEntryPoint);
        ApplyFootstepStageType(stageEntryPoint);
    }

    private void ApplyPlayerOptionByStageType(Player player, StageEntryPoint stageEntryPoint)
    {
        if (player == null)
        {
            Debug.LogWarning("Player is missing, so player stage options cannot be applied.");
            return;
        }

        if (stageEntryPoint == null)
        {
            Debug.LogWarning("StageEntryPoint is missing, so player stage options cannot be applied.");
            return;
        }

        StageInfo stageInfo = stageEntryPoint.GetComponentInParent<StageInfo>();
        if (stageInfo == null)
        {
            Debug.LogWarning($"{stageEntryPoint.name} has no parent StageInfo, so player stage options cannot be applied.");
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so player stage options cannot be loaded.");
            return;
        }

        PlayerOptionByStageTypeData playerOptionData = GameDataManager.Instance.GetPlayerOptionByStageTypeData(stageInfo.StageType);
        if (playerOptionData == null)
        {
            Debug.LogWarning($"{stageInfo.StageType} PlayerOptionByStageTypeData could not be found.");
            return;
        }

        player.SetPlayerScale(playerOptionData.PlayerScale);
        player.SetMoveSpeed(playerOptionData.MoveSpeed);
    }

    private void ApplyFootstepStageType(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null)
        {
            Debug.LogWarning("StageEntryPoint is missing, so footstep stage sound cannot be applied.");
            return;
        }

        StageInfo stageInfo = stageEntryPoint.GetComponentInParent<StageInfo>();
        if (stageInfo == null)
        {
            Debug.LogWarning($"{stageEntryPoint.name} has no parent StageInfo, so footstep stage sound cannot be applied.");
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager.Instance is missing, so footstep stage sound cannot be applied.");
            return;
        }

        SoundManager.Instance.ApplyFootstepStageType(stageInfo.StageType);
    }

    private void MoveCameraToStageEntryPointRoom(StageEntryPoint stageEntryPoint)
    {
        if (stageEntryPoint == null)
        {
            Debug.LogWarning("StageEntryPoint is missing, so camera position cannot be updated.");
            return;
        }

        StageInfo stageInfo = stageEntryPoint.GetComponentInParent<StageInfo>();
        if (stageInfo == null)
        {
            Debug.LogWarning($"{stageEntryPoint.name} has no parent StageInfo, so camera position cannot be updated.");
            return;
        }

        Camera mainCamera = GetMainCamera();
        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera is missing, so camera position cannot be updated.");
            return;
        }

        Vector3 cameraPosition = mainCamera.transform.position;
        cameraPosition.x = stageInfo.transform.position.x;
        cameraPosition.y = stageInfo.transform.position.y;

        if (stageInfo.StageType == StageType.Corridor)
        {
            cameraPosition = GetCorridorCameraPosition(cameraPosition, stageEntryPoint, mainCamera);
        }

        mainCamera.transform.position = cameraPosition;
    }

    private Vector3 GetCorridorCameraPosition(Vector3 cameraPosition, StageEntryPoint stageEntryPoint, Camera mainCamera)
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            Debug.LogWarning("Player Transform is missing, so Corridor camera position cannot be aligned to player.");
            return cameraPosition;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera is missing, so Corridor camera position cannot be calculated.");
            return cameraPosition;
        }

        if (mainCamera.orthographic == false)
        {
            Debug.LogWarning("Main Camera is not orthographic, so Corridor camera bounds cannot be calculated.");
            return cameraPosition;
        }

        StageInfo stageInfo = stageEntryPoint.GetComponentInParent<StageInfo>();
        if (stageInfo == null)
        {
            Debug.LogWarning($"{stageEntryPoint.name} has no parent StageInfo, so Corridor camera bounds cannot be calculated.");
            return cameraPosition;
        }

        SpriteRenderer stageRenderer = GetLargestStageSpriteRenderer(stageInfo);
        if (stageRenderer == null)
        {
            Debug.LogWarning($"{stageInfo.name} has no child SpriteRenderer, so Corridor camera bounds cannot be calculated.");
            return cameraPosition;
        }

        Bounds stageBounds = stageRenderer.bounds;
        float cameraHalfHeight = mainCamera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * mainCamera.aspect;

        cameraPosition.x = ClampCameraAxis(
            playerTransform.position.x,
            stageBounds.min.x + cameraHalfWidth,
            stageBounds.max.x - cameraHalfWidth,
            stageBounds.center.x
        );

        Transform cameraCenter = FindChildTransformByName(stageInfo.transform, CorridorCameraCenterName);
        if (cameraCenter != null)
        {
            cameraPosition.y = cameraCenter.position.y;
        }
        else
        {
            Debug.LogWarning($"{stageInfo.name} has no {CorridorCameraCenterName}, so Corridor camera Y uses stage bounds center.");
            cameraPosition.y = stageBounds.center.y;
        }

        return cameraPosition;
    }

    private SpriteRenderer GetLargestStageSpriteRenderer(StageInfo stageInfo)
    {
        if (stageInfo == null)
        {
            return null;
        }

        SpriteRenderer[] spriteRenderers = stageInfo.GetComponentsInChildren<SpriteRenderer>();
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return null;
        }

        SpriteRenderer largestSpriteRenderer = null;
        float largestArea = 0f;

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            Bounds bounds = spriteRenderer.bounds;
            float area = bounds.size.x * bounds.size.y;
            if (largestSpriteRenderer == null || area > largestArea)
            {
                largestSpriteRenderer = spriteRenderer;
                largestArea = area;
            }
        }

        return largestSpriteRenderer;
    }

    private Transform FindChildTransformByName(Transform rootTransform, string childName)
    {
        if (rootTransform == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(childName) == true)
        {
            return null;
        }

        Transform[] childTransforms = rootTransform.GetComponentsInChildren<Transform>(true);
        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform != null && childTransform.name == childName)
            {
                return childTransform;
            }
        }

        return null;
    }

    private float ClampCameraAxis(float value, float min, float max, float fallbackValue)
    {
        if (min > max)
        {
            return fallbackValue;
        }

        return Mathf.Clamp(value, min, max);
    }

    private Camera GetMainCamera()
    {
        if (_mainCamera != null)
        {
            return _mainCamera;
        }

        _mainCamera = Camera.main;
        return _mainCamera;
    }
}
