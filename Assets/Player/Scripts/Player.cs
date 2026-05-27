using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotateSpeed = 360f;

    private Rigidbody2D _rigidbody;
    private Vector2 _moveDirection;
    private Vector2 _targetPosition;
    private bool _isMovingToMouseTarget;
    private IInteractable _reservedInteractable;
    private Action<IInteractable> _onArriveInteractableCallback;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
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
        _isMovingToMouseTarget = false;
        _reservedInteractable = null;
        _onArriveInteractableCallback = null;
    }

    private void SetMoveDirection()
    {
        _moveDirection = InputManager.GetMoveDirection();

        if (_moveDirection != Vector2.zero)
        {
            _isMovingToMouseTarget = false;
            _reservedInteractable = null;
            _onArriveInteractableCallback = null;
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

        SetTargetPosition(new Vector2(worldPosition.x, worldPosition.y));
        _reservedInteractable = null;
        _onArriveInteractableCallback = null;
    }

    private void MovePlayer()
    {
        if (_moveDirection != Vector2.zero)
        {
            RotateSmoothly(_moveDirection);
            MoveToDirection(_moveDirection);

            return;
        }

        if (_isMovingToMouseTarget == true)
        {
            MoveToMouseTarget();
        }
    }

    private void MoveToDirection(Vector2 moveDirection)
    {
        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 이동할 수 없습니다.");
            return;
        }

        Vector2 nextPosition = _rigidbody.position + moveDirection * _moveSpeed * Time.fixedDeltaTime;

        _rigidbody.MovePosition(nextPosition);
    }

    private void MoveToMouseTarget()
    {
        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 마우스 목표 지점으로 이동할 수 없습니다.");
            _isMovingToMouseTarget = false;
            return;
        }

        if (_reservedInteractable != null)
        {
            UpdateInteractionTarget();
        }

        Vector2 moveDirection = _targetPosition - _rigidbody.position;

        if (_reservedInteractable != null && CanInteractReservedTarget())
        {
            ExecuteReservedInteractable();
            return;
        }

        if (moveDirection.magnitude <= 0.05f)
        {
            _isMovingToMouseTarget = false;
            return;
        }

        moveDirection.Normalize();

        RotateSmoothly(moveDirection);

        Vector2 nextPosition = Vector2.MoveTowards(
            _rigidbody.position,
            _targetPosition,
            _moveSpeed * Time.fixedDeltaTime
        );

        _rigidbody.MovePosition(nextPosition);
    }

    private IInteractable FindInteractableAtPosition(Vector3 worldPosition)
    {
        Collider2D collider = Physics2D.OverlapPoint(worldPosition);
        if (collider == null)
        {
            return null;
        }

        IInteractable interactable = collider.GetComponentInParent<IInteractable>();
        if (interactable == null)
        {
            interactable = collider.GetComponent<IInteractable>();
        }

        return interactable;
    }

    public void RequestInteract(IInteractable interactable)
    {
        RequestInteract(interactable, null);
    }

    public void RequestInteract(IInteractable interactable, Action<IInteractable> onArriveInteractableCallback)
    {
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
        UpdateInteractionTarget();
        _isMovingToMouseTarget = true;
    }

    private void UpdateInteractionTarget()
    {
        if (_reservedInteractable == null || _reservedInteractable.InteractionTransform == null)
        {
            _reservedInteractable = null;
            _onArriveInteractableCallback = null;
            _isMovingToMouseTarget = false;
            return;
        }

        Vector3 interactionPosition = _reservedInteractable.InteractionTransform.position;
        _targetPosition = new Vector2(interactionPosition.x, interactionPosition.y);
    }

    private bool CanInteractReservedTarget()
    {
        if (_reservedInteractable == null || _reservedInteractable.InteractionTransform == null)
        {
            return false;
        }

        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 상호작용 거리를 확인할 수 없습니다.");
            return false;
        }

        float distance = Vector2.Distance(_rigidbody.position, _reservedInteractable.InteractionTransform.position);
        return distance <= _reservedInteractable.InteractionDistance;
    }

    private void ExecuteReservedInteractable()
    {
        IInteractable interactable = _reservedInteractable;
        Action<IInteractable> arriveCallback = _onArriveInteractableCallback;

        _reservedInteractable = null;
        _onArriveInteractableCallback = null;
        _isMovingToMouseTarget = false;
        _moveDirection = Vector2.zero;

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

    private void SetTargetPosition(Vector2 targetPosition)
    {
        _targetPosition = targetPosition;
        _isMovingToMouseTarget = true;
    }

    private void RotateSmoothly(Vector2 moveDirection)
    {
        if (_rigidbody == null)
        {
            Debug.LogWarning("Player의 Rigidbody2D가 비어 있어 회전할 수 없습니다.");
            return;
        }

        float targetAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;

        float nextAngle = Mathf.MoveTowardsAngle(
            _rigidbody.rotation,
            targetAngle,
            _rotateSpeed * Time.fixedDeltaTime
        );

        _rigidbody.MoveRotation(nextAngle);
    }
}
