using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotateSpeed = 360f;

    private Rigidbody2D _rigidbody;
    private Vector2 _moveDirection;
    private Vector2 _targetPosition;
    private bool _isMovingToMouseTarget;
    private IInteractable _reservedInteractable;

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
    }

    private void SetMoveDirection()
    {
        _moveDirection = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            _moveDirection += Vector2.up;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            _moveDirection += Vector2.down;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            _moveDirection += Vector2.left;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            _moveDirection += Vector2.right;
        }

        _moveDirection.Normalize();

        if (_moveDirection != Vector2.zero)
        {
            _isMovingToMouseTarget = false;
            _reservedInteractable = null;
        }
    }

    private void SetMouseTarget()
    {
        if (Input.GetMouseButtonDown(0) == false)
        {
            return;
        }

        if (Camera.main == null)
        {
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        IInteractable interactable = FindInteractableAtPosition(worldPosition);
        if (interactable != null)
        {
            RequestInteract(interactable);
            return;
        }

        SetTargetPosition(new Vector2(worldPosition.x, worldPosition.y));
        _reservedInteractable = null;
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
        Vector2 nextPosition = _rigidbody.position + moveDirection * _moveSpeed * Time.fixedDeltaTime;

        _rigidbody.MovePosition(nextPosition);
    }

    private void MoveToMouseTarget()
    {
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

    private void RequestInteract(IInteractable interactable)
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
        UpdateInteractionTarget();
        _isMovingToMouseTarget = true;
    }

    private void UpdateInteractionTarget()
    {
        if (_reservedInteractable == null || _reservedInteractable.InteractionTransform == null)
        {
            _reservedInteractable = null;
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

        float distance = Vector2.Distance(_rigidbody.position, _reservedInteractable.InteractionTransform.position);
        return distance <= _reservedInteractable.InteractionDistance;
    }

    private void ExecuteReservedInteractable()
    {
        IInteractable interactable = _reservedInteractable;

        _reservedInteractable = null;
        _isMovingToMouseTarget = false;
        _moveDirection = Vector2.zero;

        if (interactable == null)
        {
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
        float targetAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;

        float nextAngle = Mathf.MoveTowardsAngle(
            _rigidbody.rotation,
            targetAngle,
            _rotateSpeed * Time.fixedDeltaTime
        );

        _rigidbody.MoveRotation(nextAngle);
    }
}
