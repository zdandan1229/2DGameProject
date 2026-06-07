using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    private enum PlayerMovePurpose
    {
        None,
        Move,
        Approach
    }

    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _mouseStuckMoveThreshold = 0.005f;
    [SerializeField] private float _mouseStuckStopTime = 0.2f;

    private Rigidbody2D _rigidbody;
    private PlayerAnimationController _animationController;
    private PlayerTransitionPoint _playerTransitionPoint;
    private Transform _playerApprochPoint;
    private Vector2 _moveDirection;
    private Vector2 _targetPosition;
    private Vector2 _prevMouseMovePosition;
    private float _mouseStuckTimer;
    private bool _hasPrevMouseMovePosition;
    private PlayerMovePurpose _movePurpose;
    private IInteractable _reservedInteractable;
    private Action<IInteractable> _onArriveInteractableCallback;
    private Vector3 _reservedInteractionPosition;
    private bool _hasReservedInteractionPosition;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animationController = GetComponent<PlayerAnimationController>();
        _playerTransitionPoint = GetComponentInChildren<PlayerTransitionPoint>();
        _playerApprochPoint = transform.Find("PlayerApprochPoint");

        if (_playerTransitionPoint == null)
        {
            Debug.LogError("Player child object with PlayerTransitionPoint is missing. Move movement cannot work without it.");
        }

        if (_playerApprochPoint == null)
        {
            Debug.LogError("Player child object named PlayerApprochPoint is missing. Approach movement cannot work without it.");
        }
    }

    private void Update()
    {
        if (CanUpdateMove() == false)
        {
            ClearMoveState();
            return;
        }

        SetMoveDirection();
        SetMouseTarget();
    }

    private void FixedUpdate()
    {
        if (CanUpdateMove() == false)
        {
            return;
        }

        MovePlayer();
    }

    private bool CanUpdateMove()
    {
        if (GameManager.Instance == null)
        {
            return true;
        }

        return GameManager.Instance.CanPlayerMove();
    }

    private void ClearMoveState()
    {
        _moveDirection = Vector2.zero;
        _movePurpose = PlayerMovePurpose.None;
        _reservedInteractable = null;
        _onArriveInteractableCallback = null;
        _hasReservedInteractionPosition = false;
        ResetMouseStuckState();
        ForceStopMoveAnimation();
    }

    private void SetMoveDirection()
    {
        _moveDirection = InputManager.GetMoveDirection();

        if (_moveDirection != Vector2.zero)
        {
            _movePurpose = PlayerMovePurpose.None;
            _reservedInteractable = null;
            _onArriveInteractableCallback = null;
            _hasReservedInteractionPosition = false;
            ResetMouseStuckState();
        }
    }

    private void SetMouseTarget()
    {
        if (InputManager.GetPrimaryClickDown() == false)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Camera.main == null)
        {
            return;
        }

        float pointerZPosition = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 mousePosition = InputManager.GetPointerScreenPosition(pointerZPosition);

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        IInteractable interactable = FindInteractableAtPosition(worldPosition);
        if (interactable != null)
        {
            return;
        }

        RequestMoveToWorldPosition(new Vector2(worldPosition.x, worldPosition.y));
    }

    private void MovePlayer()
    {
        if (_moveDirection != Vector2.zero)
        {
            MoveToDirection(_moveDirection);

            return;
        }

        switch (_movePurpose)
        {
            case PlayerMovePurpose.Move:
                MoveToWorldTarget();
                return;
            case PlayerMovePurpose.Approach:
                ApproachInteractionTarget();
                return;
        }

        RequestStopMoveAnimation();
    }

    private void MoveToDirection(Vector2 moveDirection)
    {
        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 이동할 수 없습니다.");
            ForceStopMoveAnimation();
            return;
        }

        Vector2 nextPosition = _rigidbody.position + moveDirection * _moveSpeed * Time.fixedDeltaTime;

        _rigidbody.MovePosition(nextPosition);
        PlayMoveAnimation(moveDirection, false);
    }

    private void MoveToWorldTarget()
    {
        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 마우스 목표 지점으로 이동할 수 없습니다.");
            _movePurpose = PlayerMovePurpose.None;
            ForceStopMoveAnimation();
            return;
        }

        if (_playerTransitionPoint == null)
        {
            Debug.LogError("PlayerTransitionPoint is missing, so move movement cannot continue.");
            StopMouseTargetMove();
            return;
        }

        if (IsMouseMoveStuck() == true)
        {
            StopMouseTargetMove();
            return;
        }

        Vector2 currentMoveBasePosition = GetTransitionBasePosition();
        Vector2 targetRigidbodyPosition = GetRigidbodyPositionForTransitionPoint(_targetPosition);
        Vector2 moveDirection = _targetPosition - currentMoveBasePosition;

        if (moveDirection.magnitude <= 0.05f)
        {
            StopMouseTargetMove();
            return;
        }

        moveDirection.Normalize();

        Vector2 nextPosition = Vector2.MoveTowards(
            _rigidbody.position,
            targetRigidbodyPosition,
            _moveSpeed * Time.fixedDeltaTime
        );

        _rigidbody.MovePosition(nextPosition);
        PlayMoveAnimation(moveDirection, true);
    }

    private void ApproachInteractionTarget()
    {
        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 상호작용 대상에게 접근할 수 없습니다.");
            _movePurpose = PlayerMovePurpose.None;
            ForceStopMoveAnimation();
            return;
        }

        if (_reservedInteractable == null)
        {
            StopMouseTargetMove();
            return;
        }

        if (_playerApprochPoint == null)
        {
            Debug.LogError("PlayerApprochPoint is missing, so approach movement cannot continue.");
            StopMouseTargetMove();
            return;
        }

        UpdateInteractionTarget();

        if (CanInteractReservedTarget())
        {
            ExecuteReservedInteractable();
            return;
        }

        if (IsMouseMoveStuck() == true)
        {
            StopMouseTargetMove();
            return;
        }

        Vector2 currentMoveBasePosition = GetApproachBasePosition();
        Vector2 targetRigidbodyPosition = GetRigidbodyPositionForApprochPoint(_targetPosition);
        Vector2 moveDirection = _targetPosition - currentMoveBasePosition;

        if (moveDirection.magnitude <= 0.05f)
        {
            StopMouseTargetMove();
            return;
        }

        moveDirection.Normalize();

        Vector2 nextPosition = Vector2.MoveTowards(
            _rigidbody.position,
            targetRigidbodyPosition,
            _moveSpeed * Time.fixedDeltaTime
        );

        _rigidbody.MovePosition(nextPosition);
        PlayMoveAnimation(moveDirection, true);
    }

    private IInteractable FindInteractableAtPosition(Vector3 worldPosition)
    {
        IInteractionOptionProvider optionProvider = InteractionClickArea.FindOptionProviderAtWorldPosition(worldPosition, false);
        return optionProvider as IInteractable;
    }

    public void RequestInteract(IInteractable interactable)
    {
        RequestInteract(interactable, null);
    }

    public void RequestInteract(IInteractable interactable, Action<IInteractable> onArriveInteractableCallback)
    {
        RequestInteract(interactable, Vector3.zero, false, onArriveInteractableCallback);
    }

    public void RequestInteract(IInteractable interactable, Vector3 interactionPosition, Action<IInteractable> onArriveInteractableCallback)
    {
        RequestInteract(interactable, interactionPosition, true, onArriveInteractableCallback);
    }

    private void RequestInteract(IInteractable interactable, Vector3 interactionPosition, bool hasInteractionPosition, Action<IInteractable> onArriveInteractableCallback)
    {
        if (CanUpdateMove() == false)
        {
            ClearMoveState();
            return;
        }

        if (interactable == null)
        {
            Debug.LogWarning("상호작용 대상이 비어 있어 이동 후 상호작용을 예약할 수 없습니다.");
            return;
        }

        if (interactable.InteractionTransform == null)
        {
            Debug.LogWarning("상호작용 대상의 InteractionTransform이 비어 있습니다.");
            return;
        }

        _reservedInteractable = interactable;
        _onArriveInteractableCallback = onArriveInteractableCallback;
        _reservedInteractionPosition = interactionPosition;
        _hasReservedInteractionPosition = hasInteractionPosition;
        UpdateInteractionTarget();
        _movePurpose = PlayerMovePurpose.Approach;
        ResetMouseStuckState();
    }

    private void UpdateInteractionTarget()
    {
        if (_reservedInteractable == null || _reservedInteractable.InteractionTransform == null)
        {
            _reservedInteractable = null;
            _onArriveInteractableCallback = null;
            _hasReservedInteractionPosition = false;
            _movePurpose = PlayerMovePurpose.None;
            ResetMouseStuckState();
            return;
        }

        Vector3 interactionPosition = GetInteractablePosition(_reservedInteractable);
        _targetPosition = new Vector2(interactionPosition.x, interactionPosition.y);
    }

    private bool CanInteractReservedTarget()
    {
        if (_reservedInteractable == null || _reservedInteractable.InteractionTransform == null)
        {
            return false;
        }

        if (_playerApprochPoint == null)
        {
            Debug.LogError("PlayerApprochPoint is missing, so interaction distance cannot be checked.");
            return false;
        }

        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 상호작용 거리를 확인할 수 없습니다.");
            return false;
        }

        float distance = Vector2.Distance(GetApproachBasePosition(), GetInteractablePosition(_reservedInteractable));
        return distance <= _reservedInteractable.InteractionDistance;
    }

    private Vector3 GetInteractablePosition(IInteractable interactable)
    {
        if (_hasReservedInteractionPosition)
        {
            return _reservedInteractionPosition;
        }

        if (interactable is IInteractionPositionProvider positionProvider)
        {
            return positionProvider.InteractionPosition;
        }

        return interactable.InteractionTransform.position;
    }

    private void ExecuteReservedInteractable()
    {
        IInteractable interactable = _reservedInteractable;
        Action<IInteractable> arriveCallback = _onArriveInteractableCallback;

        _reservedInteractable = null;
        _onArriveInteractableCallback = null;
        _hasReservedInteractionPosition = false;
        _movePurpose = PlayerMovePurpose.None;
        _moveDirection = Vector2.zero;
        ResetMouseStuckState();
        ForceStopMoveAnimation();

        if (interactable == null)
        {
            return;
        }

        if (arriveCallback != null)
        {
            arriveCallback.Invoke(interactable);
            return;
        }

        interactable.Interact();
    }

    private void RequestMoveToWorldPosition(Vector2 targetPosition)
    {
        _targetPosition = targetPosition;
        _movePurpose = PlayerMovePurpose.Move;
        _reservedInteractable = null;
        _onArriveInteractableCallback = null;
        _hasReservedInteractionPosition = false;
        ResetMouseStuckState();
    }

    private Vector2 GetTransitionBasePosition()
    {
        return _playerTransitionPoint.transform.position;
    }

    private Vector2 GetApproachBasePosition()
    {
        return _playerApprochPoint.position;
    }

    private Vector2 GetRigidbodyPositionForTransitionPoint(Vector2 targetPosition)
    {
        Vector2 transitionPointOffset = (Vector2)_playerTransitionPoint.transform.position - _rigidbody.position;
        return targetPosition - transitionPointOffset;
    }

    private Vector2 GetRigidbodyPositionForApprochPoint(Vector2 targetPosition)
    {
        Vector2 approchPointOffset = (Vector2)_playerApprochPoint.position - _rigidbody.position;
        return targetPosition - approchPointOffset;
    }

    public bool MoveTransitionPointToPosition(Vector3 targetPosition)
    {
        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 PlayerTransitionPoint 기준으로 위치를 이동할 수 없습니다.");
            return false;
        }

        if (_playerTransitionPoint == null)
        {
            Debug.LogError("PlayerTransitionPoint is missing, so transition point movement cannot continue.");
            return false;
        }

        Vector2 targetRigidbodyPosition = GetRigidbodyPositionForTransitionPoint(targetPosition);
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.position = targetRigidbodyPosition;

        Vector3 nextPosition = transform.position;
        nextPosition.x = targetRigidbodyPosition.x;
        nextPosition.y = targetRigidbodyPosition.y;
        transform.position = nextPosition;

        ClearMoveState();
        return true;
    }

    private bool IsMouseMoveStuck()
    {
        if (_hasPrevMouseMovePosition == false)
        {
            _prevMouseMovePosition = _rigidbody.position;
            _hasPrevMouseMovePosition = true;
            _mouseStuckTimer = 0f;
            return false;
        }

        float movedDistance = Vector2.Distance(_prevMouseMovePosition, _rigidbody.position);
        _prevMouseMovePosition = _rigidbody.position;

        if (movedDistance > _mouseStuckMoveThreshold)
        {
            _mouseStuckTimer = 0f;
            return false;
        }

        _mouseStuckTimer += Time.fixedDeltaTime;
        return _mouseStuckTimer >= _mouseStuckStopTime;
    }

    private void StopMouseTargetMove()
    {
        _movePurpose = PlayerMovePurpose.None;
        _reservedInteractable = null;
        _onArriveInteractableCallback = null;
        _hasReservedInteractionPosition = false;
        ResetMouseStuckState();
        ForceStopMoveAnimation();
    }

    private void ResetMouseStuckState()
    {
        _prevMouseMovePosition = Vector2.zero;
        _mouseStuckTimer = 0f;
        _hasPrevMouseMovePosition = false;
    }

    private void PlayMoveAnimation(Vector2 moveDirection)
    {
        PlayMoveAnimation(moveDirection, false);
    }

    private void PlayMoveAnimation(Vector2 moveDirection, bool useMouseVerticalDirectionThreshold)
    {
        if (_animationController == null)
        {
            Debug.LogWarning("Player에 PlayerAnimationController가 없어 이동 애니메이션을 재생할 수 없습니다.");
            return;
        }

        _animationController.PlayMoveAnimation(moveDirection, useMouseVerticalDirectionThreshold);
    }

    private void RequestStopMoveAnimation()
    {
        if (_animationController == null)
        {
            Debug.LogWarning("Player에 PlayerAnimationController가 없어 이동 애니메이션을 정지할 수 없습니다.");
            return;
        }

        _animationController.RequestStopMoveAnimation();
    }

    private void ForceStopMoveAnimation()
    {
        if (_animationController == null)
        {
            Debug.LogWarning("Player에 PlayerAnimationController가 없어 이동 애니메이션을 강제로 정지할 수 없습니다.");
            return;
        }

        _animationController.ForceStopMoveAnimation();
    }

    public void LookToDirection(Vector2 lookDirection)
    {
        if (lookDirection == Vector2.zero)
        {
            Debug.LogWarning("Player가 바라볼 방향이 비어 있어 회전할 수 없습니다.");
            return;
        }

        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 회전할 수 없습니다.");
            return;
        }

        float targetAngle = GetAngleFromDirection(lookDirection);
        _rigidbody.rotation = targetAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    public void SetIdleDirection(PlayerIdleDirection idleDirection)
    {
        if (_animationController == null)
        {
            Debug.LogWarning("Player is missing PlayerAnimationController, so idle direction cannot be updated.");
            return;
        }

        _animationController.SetIdleDirection(idleDirection);
    }

    public void SetPlayerScale(float playerScale)
    {
        if (playerScale <= 0f)
        {
            Debug.LogWarning("Player scale must be greater than 0.");
            return;
        }

        transform.localScale = Vector3.one * playerScale;
    }

    public void SetMoveSpeed(float moveSpeed)
    {
        if (moveSpeed <= 0f)
        {
            Debug.LogWarning("Player move speed must be greater than 0.");
            return;
        }

        _moveSpeed = moveSpeed;
    }

    private float GetAngleFromDirection(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
    }

}
