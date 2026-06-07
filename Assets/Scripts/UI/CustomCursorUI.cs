using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CustomCursorType
{
    Default,
    Inspect,
    Talk,
    EnterDoor
}

public class CustomCursorUI : UIBase
{
    [SerializeField] private RectTransform Rect_CursorRoot;
    [SerializeField] private GameObject Cursor_Default;
    [SerializeField] private GameObject Cursor_Inspect;
    [SerializeField] private GameObject Cursor_Talk;
    [SerializeField] private GameObject Cursor_EnterDoor;

    private CustomCursorType _currentCursorType = CustomCursorType.Default;
    private bool _isCursorInitialized;
    private bool _isCameraWarningShown;
    private bool _isRootWarningShown;
    private PointerEventData _pointerEventData;
    private List<RaycastResult> _uiRaycastResultList = new List<RaycastResult>();

    private void Awake()
    {
        InitializeReferences();
        DisableRaycastTargets();
        ShowCursor(CustomCursorType.Default);
    }

    private void OnEnable()
    {
        Cursor.visible = false;
        ShowCursor(CustomCursorType.Default);
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void Update()
    {
        UpdateCursorPosition();
        UpdateCursorType();
    }

    private void InitializeReferences()
    {
        if (Rect_CursorRoot == null)
        {
            Rect_CursorRoot = GetComponent<RectTransform>();
        }

        if (Cursor_Default == null)
        {
            Cursor_Default = transform.Find("Cursor_Default")?.gameObject;
        }

        if (Cursor_Inspect == null)
        {
            Cursor_Inspect = transform.Find("Cursor_Inspect")?.gameObject;
        }

        if (Cursor_Talk == null)
        {
            Cursor_Talk = transform.Find("Cursor_Talk")?.gameObject;
        }

        if (Cursor_EnterDoor == null)
        {
            Cursor_EnterDoor = transform.Find("Cursor_EnterDoor")?.gameObject;
        }
    }

    private void DisableRaycastTargets()
    {
        Image[] imageArr = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < imageArr.Length; i++)
        {
            imageArr[i].raycastTarget = false;
        }
    }

    private void UpdateCursorPosition()
    {
        if (Rect_CursorRoot == null)
        {
            if (_isRootWarningShown == false)
            {
                Debug.LogWarning("CustomCursorUI의 Rect_CursorRoot 참조가 누락되어 커서 위치를 갱신할 수 없습니다.");
                _isRootWarningShown = true;
            }

            return;
        }

        Rect_CursorRoot.position = InputManager.GetPointerScreenPosition(0f);
    }

    private void UpdateCursorType()
    {
        if (IsPointerOverUI())
        {
            if (IsPointerOverInspectPoint())
            {
                ShowCursor(CustomCursorType.Inspect);
                return;
            }

            ShowCursor(CustomCursorType.Default);
            return;
        }

        IInteractionOptionProvider optionProvider = FindOptionProviderUnderMouse();
        CustomCursorType cursorType = GetCursorType(optionProvider);
        ShowCursor(cursorType);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsPointerOverInspectPoint()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (_pointerEventData == null)
        {
            _pointerEventData = new PointerEventData(EventSystem.current);
        }

        _pointerEventData.position = InputManager.GetPointerScreenPosition(0f);
        _uiRaycastResultList.Clear();
        EventSystem.current.RaycastAll(_pointerEventData, _uiRaycastResultList);

        if (_uiRaycastResultList.Count <= 0)
        {
            return false;
        }

        GameObject raycastObject = _uiRaycastResultList[0].gameObject;
        if (raycastObject == null)
        {
            return false;
        }

        InspectPoint inspectPoint = raycastObject.GetComponent<InspectPoint>();
        return inspectPoint != null && inspectPoint.HasInspectTarget();
    }

    private IInteractionOptionProvider FindOptionProviderUnderMouse()
    {
        if (Camera.main == null)
        {
            if (_isCameraWarningShown == false)
            {
                Debug.LogWarning("Camera.main이 없어 커서 상호작용 대상을 확인할 수 없습니다.");
                _isCameraWarningShown = true;
            }

            return null;
        }

        Vector3 pointerScreenPosition = InputManager.GetPointerScreenPosition(Mathf.Abs(Camera.main.transform.position.z));
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(pointerScreenPosition);
        return InteractionClickArea.FindOptionProviderAtWorldPosition(worldPosition, false);
    }

    private CustomCursorType GetCursorType(IInteractionOptionProvider optionProvider)
    {
        if (optionProvider == null)
        {
            return CustomCursorType.Default;
        }

        List<InteractionOption> optionList = optionProvider.GetInteractionOptions();
        if (optionList == null)
        {
            return CustomCursorType.Default;
        }

        for (int i = 0; i < optionList.Count; i++)
        {
            InteractionOption interactionOption = optionList[i];
            if (interactionOption == null)
            {
                continue;
            }

            if (interactionOption.ActionType == InteractionActionType.OpenDialogue)
            {
                return CustomCursorType.Talk;
            }

            if (interactionOption.ActionType == InteractionActionType.EnterStage)
            {
                return CustomCursorType.EnterDoor;
            }

            if (interactionOption.ActionType == InteractionActionType.OpenInspectObject ||
                interactionOption.ActionType == InteractionActionType.OpenInspectArea ||
                interactionOption.ActionType == InteractionActionType.OpenJournal ||
                interactionOption.ActionType == InteractionActionType.ShowPopupText)
            {
                return CustomCursorType.Inspect;
            }
        }

        return CustomCursorType.Default;
    }

    private void ShowCursor(CustomCursorType cursorType)
    {
        if (_isCursorInitialized &&
            _currentCursorType == cursorType &&
            Cursor_Default != null &&
            Cursor_Inspect != null &&
            Cursor_Talk != null &&
            Cursor_EnterDoor != null)
        {
            return;
        }

        _isCursorInitialized = true;
        _currentCursorType = cursorType;
        SetCursorActive(Cursor_Default, cursorType == CustomCursorType.Default);
        SetCursorActive(Cursor_Inspect, cursorType == CustomCursorType.Inspect);
        SetCursorActive(Cursor_Talk, cursorType == CustomCursorType.Talk);
        SetCursorActive(Cursor_EnterDoor, cursorType == CustomCursorType.EnterDoor);
    }

    private void SetCursorActive(GameObject cursorObject, bool isActive)
    {
        if (cursorObject == null)
        {
            Debug.LogWarning("CustomCursorUI의 커서 이미지 참조가 누락되어 커서 표시를 변경할 수 없습니다.");
            return;
        }

        cursorObject.SetActive(isActive);
    }
}
