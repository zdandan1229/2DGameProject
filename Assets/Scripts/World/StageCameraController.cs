using UnityEngine;

public class StageCameraController : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Transform _player;
    [SerializeField] private SpriteRenderer _stageRenderer;
    [SerializeField] private bool _followX = true;
    [SerializeField] private bool _followY;
    [SerializeField] private bool _clampX = true;
    [SerializeField] private bool _clampY;
    [SerializeField] private float _smoothTime = 0.18f;
    [SerializeField] private float _activeBoundsPadding = 0.5f;

    private Vector3 _cameraVelocity;
    private bool _didWarnMissingCamera;
    private bool _didWarnMissingPlayer;
    private bool _didWarnMissingStageRenderer;
    private bool _didWarnNonOrthographicCamera;

    private void Awake()
    {
        TryAssignCamera();
        TryAssignStageRenderer();
    }

    private void LateUpdate()
    {
        if (CanUpdateCamera() == false)
        {
            return;
        }

        if (IsPlayerInStageBounds() == false)
        {
            _cameraVelocity = Vector3.zero;
            return;
        }

        Vector3 targetPosition = GetTargetCameraPosition();
        Vector3 nextPosition = Vector3.SmoothDamp(
            _mainCamera.transform.position,
            targetPosition,
            ref _cameraVelocity,
            Mathf.Max(0f, _smoothTime)
        );

        _mainCamera.transform.position = ClampCameraPosition(nextPosition);
    }

    private bool CanUpdateCamera()
    {
        TryAssignCamera();
        TryAssignPlayer();
        TryAssignStageRenderer();

        if (_mainCamera == null)
        {
            LogWarningOnce(ref _didWarnMissingCamera, "StageCameraController has no Main Camera reference.");
            return false;
        }

        if (_mainCamera.orthographic == false)
        {
            LogWarningOnce(ref _didWarnNonOrthographicCamera, "StageCameraController only supports an orthographic camera.");
            return false;
        }

        if (_player == null)
        {
            LogWarningOnce(ref _didWarnMissingPlayer, "StageCameraController has no Player reference.");
            return false;
        }

        if (_stageRenderer == null)
        {
            LogWarningOnce(ref _didWarnMissingStageRenderer, "StageCameraController has no stage SpriteRenderer reference.");
            return false;
        }

        return true;
    }

    private Vector3 GetTargetCameraPosition()
    {
        Vector3 targetPosition = _mainCamera.transform.position;

        if (_followX == true)
        {
            targetPosition.x = _player.position.x;
        }

        if (_followY == true)
        {
            targetPosition.y = _player.position.y;
        }

        return ClampCameraPosition(targetPosition);
    }

    private Vector3 ClampCameraPosition(Vector3 cameraPosition)
    {
        Bounds stageBounds = _stageRenderer.bounds;
        float cameraHalfHeight = _mainCamera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * _mainCamera.aspect;

        if (_clampX == true)
        {
            cameraPosition.x = ClampAxis(
                cameraPosition.x,
                stageBounds.min.x + cameraHalfWidth,
                stageBounds.max.x - cameraHalfWidth,
                stageBounds.center.x
            );
        }

        if (_clampY == true)
        {
            cameraPosition.y = ClampAxis(
                cameraPosition.y,
                stageBounds.min.y + cameraHalfHeight,
                stageBounds.max.y - cameraHalfHeight,
                stageBounds.center.y
            );
        }

        return cameraPosition;
    }

    private float ClampAxis(float value, float min, float max, float fallbackValue)
    {
        if (min > max)
        {
            return fallbackValue;
        }

        return Mathf.Clamp(value, min, max);
    }

    private bool IsPlayerInStageBounds()
    {
        Bounds stageBounds = _stageRenderer.bounds;
        Vector3 playerPosition = _player.position;

        return playerPosition.x >= stageBounds.min.x - _activeBoundsPadding
            && playerPosition.x <= stageBounds.max.x + _activeBoundsPadding
            && playerPosition.y >= stageBounds.min.y - _activeBoundsPadding
            && playerPosition.y <= stageBounds.max.y + _activeBoundsPadding;
    }

    private void TryAssignCamera()
    {
        if (_mainCamera != null)
        {
            return;
        }

        _mainCamera = Camera.main;
    }

    private void TryAssignPlayer()
    {
        if (_player != null)
        {
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            _player = player.transform;
        }
    }

    private void TryAssignStageRenderer()
    {
        if (_stageRenderer != null)
        {
            return;
        }

        _stageRenderer = GetComponentInParent<SpriteRenderer>();
    }

    private void LogWarningOnce(ref bool didWarn, string message)
    {
        if (didWarn == true)
        {
            return;
        }

        Debug.LogWarning(message);
        didWarn = true;
    }
}
