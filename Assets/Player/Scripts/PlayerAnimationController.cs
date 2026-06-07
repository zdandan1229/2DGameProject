using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private const string IsMovingParameterName = "IsMoving";
    private const string MoveDirectionTypeParameterName = "MoveDirectionType";
    private const int SideFrontDirectionType = 0;
    private const int SideBackDirectionType = 1;
    private const int FrontDirectionType = 2;
    private const int BackDirectionType = 3;
    private const float DirectionThreshold = 0.01f;
    private const float FirstFootstepNormalizedTime = 0.25f;
    private const float SecondFootstepNormalizedTime = 0.75f;

    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private PlayerFootstepController _footstepController;
    [SerializeField] private float _stopDelay = 0.06f;
    [SerializeField] private float _mouseVerticalDirectionThreshold = 0.013f;
    [SerializeField] private float _moveDirectionCommitDelay = 0.04f;
    [SerializeField] private float _idleDirectionCommitDelay = 0.06f;

    private int _currentMoveDirectionType = SideFrontDirectionType;
    private int _pendingMoveDirectionType = SideFrontDirectionType;
    private int _stableIdleDirectionType = SideFrontDirectionType;
    private int _pendingIdleDirectionType = SideFrontDirectionType;
    private float _lastMoveAnimationTime;
    private float _pendingMoveDirectionStartTime;
    private float _pendingIdleDirectionStartTime;
    private int _footstepLoopIndex = -1;
    private bool _didPlayFirstFootstep;
    private bool _didPlaySecondFootstep;
    private bool _stableIdleFlipX;
    private bool _isMoving;

    private void Awake()
    {
        InitializeComponents();
    }

    private void LateUpdate()
    {
        UpdateFootstepSound();
    }

    public void PlayMoveAnimation(Vector2 moveDirection)
    {
        PlayMoveAnimation(moveDirection, false);
    }

    public void PlayMoveAnimation(Vector2 moveDirection, bool useMouseVerticalDirectionThreshold)
    {
        if (moveDirection == Vector2.zero)
        {
            RequestStopMoveAnimation();
            return;
        }

        if (CanUpdateAnimation() == false)
        {
            return;
        }

        int detectedMoveDirectionType = GetMoveDirectionType(moveDirection, useMouseVerticalDirectionThreshold);
        UpdateHorizontalFlip(moveDirection);
        UpdateMoveDirectionType(detectedMoveDirectionType);
        UpdateStableIdleDirection(detectedMoveDirectionType);

        _lastMoveAnimationTime = Time.time;
        _isMoving = true;
        _animator.SetInteger(MoveDirectionTypeParameterName, _currentMoveDirectionType);
        _animator.SetBool(IsMovingParameterName, true);
    }

    public void RequestStopMoveAnimation()
    {
        if (_isMoving == false)
        {
            return;
        }

        if (Time.time - _lastMoveAnimationTime < _stopDelay)
        {
            return;
        }

        StopMoveAnimation();
    }

    public void ForceStopMoveAnimation()
    {
        StopMoveAnimation();
    }

    public void SetIdleDirection(PlayerIdleDirection idleDirection)
    {
        if (CanUpdateAnimation() == false)
        {
            return;
        }

        switch (idleDirection)
        {
            case PlayerIdleDirection.SideFrontRight:
                SetStableIdleDirection(SideFrontDirectionType, false);
                _spriteRenderer.flipX = false;
                break;
            case PlayerIdleDirection.SideFrontLeft:
                SetStableIdleDirection(SideFrontDirectionType, true);
                _spriteRenderer.flipX = true;
                break;
            case PlayerIdleDirection.SideBackRight:
                SetStableIdleDirection(SideBackDirectionType, false);
                _spriteRenderer.flipX = false;
                break;
            case PlayerIdleDirection.SideBackLeft:
                SetStableIdleDirection(SideBackDirectionType, true);
                _spriteRenderer.flipX = true;
                break;
            case PlayerIdleDirection.Front:
                SetStableIdleDirection(FrontDirectionType, _spriteRenderer.flipX);
                break;
            case PlayerIdleDirection.Back:
                SetStableIdleDirection(BackDirectionType, _spriteRenderer.flipX);
                break;
            default:
                Debug.LogWarning($"{idleDirection} is not a supported player idle direction.");
                return;
        }

        _isMoving = false;
        ResetFootstepTiming();
        _currentMoveDirectionType = _stableIdleDirectionType;
        _animator.SetInteger(MoveDirectionTypeParameterName, _stableIdleDirectionType);
        _animator.SetBool(IsMovingParameterName, false);
    }

    private void StopMoveAnimation()
    {
        if (CanUpdateAnimation() == false)
        {
            return;
        }

        _isMoving = false;
        ResetFootstepTiming();
        _currentMoveDirectionType = _stableIdleDirectionType;
        _spriteRenderer.flipX = _stableIdleFlipX;
        _animator.SetInteger(MoveDirectionTypeParameterName, _stableIdleDirectionType);
        _animator.SetBool(IsMovingParameterName, false);
    }

    private void InitializeComponents()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_footstepController == null)
        {
            _footstepController = GetComponent<PlayerFootstepController>();
        }

        if (_footstepController == null)
        {
            _footstepController = gameObject.AddComponent<PlayerFootstepController>();
        }
    }

    private bool CanUpdateAnimation()
    {
        if (_animator == null)
        {
            Debug.LogWarning("PlayerAnimationController에 Animator 참조가 없어 플레이어 애니메이션을 갱신할 수 없습니다.");
            return false;
        }

        if (_spriteRenderer == null)
        {
            Debug.LogWarning("PlayerAnimationController에 SpriteRenderer 참조가 없어 플레이어 방향 반전을 갱신할 수 없습니다.");
            return false;
        }

        return true;
    }

    private int GetMoveDirectionType(Vector2 moveDirection, bool useMouseVerticalDirectionThreshold)
    {
        float verticalDirectionThreshold = useMouseVerticalDirectionThreshold == true
            ? _mouseVerticalDirectionThreshold
            : DirectionThreshold;

        bool isVerticalMove = Mathf.Abs(moveDirection.x) <= verticalDirectionThreshold;

        if (isVerticalMove == true)
        {
            if (moveDirection.y > DirectionThreshold)
            {
                return BackDirectionType;
            }

            if (moveDirection.y < -DirectionThreshold)
            {
                return FrontDirectionType;
            }
        }

        if (moveDirection.y > DirectionThreshold)
        {
            return SideBackDirectionType;
        }

        return SideFrontDirectionType;
    }

    private void UpdateStableIdleDirection(int moveDirectionType)
    {
        if (moveDirectionType == _stableIdleDirectionType)
        {
            _pendingIdleDirectionType = moveDirectionType;
            _pendingIdleDirectionStartTime = Time.time;
            _stableIdleFlipX = _spriteRenderer.flipX;
            return;
        }

        if (moveDirectionType != _pendingIdleDirectionType)
        {
            _pendingIdleDirectionType = moveDirectionType;
            _pendingIdleDirectionStartTime = Time.time;
            return;
        }

        if (Time.time - _pendingIdleDirectionStartTime < _idleDirectionCommitDelay)
        {
            return;
        }

        SetStableIdleDirection(moveDirectionType, _spriteRenderer.flipX);
    }

    private void SetStableIdleDirection(int moveDirectionType, bool flipX)
    {
        _currentMoveDirectionType = moveDirectionType;
        _pendingMoveDirectionType = moveDirectionType;
        _stableIdleDirectionType = moveDirectionType;
        _pendingIdleDirectionType = moveDirectionType;
        _pendingMoveDirectionStartTime = Time.time;
        _pendingIdleDirectionStartTime = Time.time;
        _stableIdleFlipX = flipX;
    }

    private void UpdateMoveDirectionType(int detectedMoveDirectionType)
    {
        if (_isMoving == false || detectedMoveDirectionType == _currentMoveDirectionType)
        {
            _currentMoveDirectionType = detectedMoveDirectionType;
            _pendingMoveDirectionType = detectedMoveDirectionType;
            _pendingMoveDirectionStartTime = Time.time;
            return;
        }

        if (detectedMoveDirectionType != _pendingMoveDirectionType)
        {
            _pendingMoveDirectionType = detectedMoveDirectionType;
            _pendingMoveDirectionStartTime = Time.time;
            return;
        }

        if (Time.time - _pendingMoveDirectionStartTime < _moveDirectionCommitDelay)
        {
            return;
        }

        _currentMoveDirectionType = detectedMoveDirectionType;
    }

    private void UpdateHorizontalFlip(Vector2 moveDirection)
    {
        if (moveDirection.x > DirectionThreshold)
        {
            _spriteRenderer.flipX = false;
            return;
        }

        if (moveDirection.x < -DirectionThreshold)
        {
            _spriteRenderer.flipX = true;
        }
    }

    private void UpdateFootstepSound()
    {
        if (_isMoving == false)
        {
            return;
        }

        if (_animator == null || _footstepController == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        int currentLoopIndex = Mathf.FloorToInt(stateInfo.normalizedTime);
        float loopTime = Mathf.Repeat(stateInfo.normalizedTime, 1f);

        if (currentLoopIndex != _footstepLoopIndex)
        {
            _footstepLoopIndex = currentLoopIndex;
            _didPlayFirstFootstep = false;
            _didPlaySecondFootstep = false;
        }

        if (_didPlayFirstFootstep == false && loopTime >= FirstFootstepNormalizedTime)
        {
            _footstepController.PlayFootstep();
            _didPlayFirstFootstep = true;
        }

        if (_didPlaySecondFootstep == false && loopTime >= SecondFootstepNormalizedTime)
        {
            _footstepController.PlayFootstep();
            _didPlaySecondFootstep = true;
        }
    }

    private void ResetFootstepTiming()
    {
        _footstepLoopIndex = -1;
        _didPlayFirstFootstep = false;
        _didPlaySecondFootstep = false;
    }
}
