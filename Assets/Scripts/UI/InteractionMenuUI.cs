using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionMenuUI : UIBase, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform[] ButtonRootList;
    [SerializeField] private InteractionMenuButton Prefab_ButtonInteraction;
    [SerializeField] private Vector2 _screenOffset = Vector2.zero;

    private List<InteractionMenuButton> _createdButtonList = new List<InteractionMenuButton>();
    private Action<InteractionOption> _onClickOptionCallback;
    private bool _isPointerInside;

    public bool IsPointerInside
    {
        get { return _isPointerInside; }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
    }

    public void OpenMenu(List<InteractionOption> optionList, Vector3 worldPosition, Action<InteractionOption> onClickOptionCallback)
    {
        _onClickOptionCallback = onClickOptionCallback;
        _isPointerInside = false;

        SetMenuPosition(worldPosition);
        RefreshButtons(optionList);
    }

    public void CloseMenu()
    {
        _isPointerInside = false;
        _onClickOptionCallback = null;
        ClearButtons();
        RefreshButtonRoots(false);
    }

    private void SetMenuPosition(Vector3 worldPosition)
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Camera.main이 없어 InteractionMenuUI 위치를 설정할 수 없습니다.");
            return;
        }

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            Debug.LogWarning("InteractionMenuUI의 RectTransform을 찾지 못했습니다.");
            return;
        }

        RectTransform parentRectTransform = rectTransform.parent as RectTransform;
        if (parentRectTransform == null)
        {
            Debug.LogWarning("InteractionMenuUI의 부모 RectTransform을 찾지 못했습니다.");
            return;
        }

        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        screenPosition += _screenOffset;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = parentCanvas.worldCamera;
        }

        Vector2 localPosition;
        bool isConverted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRectTransform,
            screenPosition,
            uiCamera,
            out localPosition
        );

        if (isConverted == false)
        {
            Debug.LogWarning("InteractionMenuUI의 화면 좌표를 UI 좌표로 변환하지 못했습니다.");
            return;
        }

        rectTransform.anchoredPosition = localPosition;
    }

    private void RefreshButtons(List<InteractionOption> optionList)
    {
        ClearButtons();
        RefreshButtonRoots(false);

        if (optionList == null || optionList.Count <= 0)
        {
            Debug.LogWarning("표시할 상호작용 옵션이 없습니다.");
            return;
        }

        if (ButtonRootList == null || ButtonRootList.Length <= 0)
        {
            Debug.LogWarning("InteractionMenuUI의 ButtonRootList가 비어 있습니다.");
            return;
        }

        if (Prefab_ButtonInteraction == null)
        {
            Debug.LogWarning("InteractionMenuUI의 Prefab_ButtonInteraction 참조가 비어 있습니다.");
            return;
        }

        int buttonCount = Mathf.Min(optionList.Count, ButtonRootList.Length);
        if (optionList.Count > ButtonRootList.Length)
        {
            Debug.LogWarning($"상호작용 옵션은 {ButtonRootList.Length}개까지만 표시됩니다. 입력 개수 : {optionList.Count}");
        }

        for (int i = 0; i < buttonCount; i++)
        {
            int buttonRootIndex = ButtonRootList.Length - 1 - i;
            Transform buttonRoot = ButtonRootList[buttonRootIndex];
            if (buttonRoot == null)
            {
                Debug.LogWarning($"InteractionMenuUI의 ButtonRootList {buttonRootIndex}번 슬롯이 비어 있습니다.");
                continue;
            }

            InteractionOption interactionOption = optionList[i];
            if (interactionOption == null || interactionOption.IsValid() == false)
            {
                Debug.LogWarning($"상호작용 옵션 {i}번이 올바르지 않아 버튼 생성을 건너뜁니다.");
                continue;
            }

            buttonRoot.gameObject.SetActive(true);

            InteractionMenuButton createdButton = Instantiate(Prefab_ButtonInteraction, buttonRoot);
            createdButton.Initialize(interactionOption, OnClickOption);
            _createdButtonList.Add(createdButton);
        }
    }

    private void RefreshButtonRoots(bool isActive)
    {
        if (ButtonRootList == null)
        {
            return;
        }

        for (int i = 0; i < ButtonRootList.Length; i++)
        {
            if (ButtonRootList[i] != null)
            {
                ButtonRootList[i].gameObject.SetActive(isActive);
            }
        }
    }

    private void ClearButtons()
    {
        for (int i = 0; i < _createdButtonList.Count; i++)
        {
            if (_createdButtonList[i] != null)
            {
                Destroy(_createdButtonList[i].gameObject);
            }
        }

        _createdButtonList.Clear();
    }

    private void OnClickOption(InteractionOption interactionOption)
    {
        if (_onClickOptionCallback == null)
        {
            Debug.LogWarning("상호작용 옵션 클릭 콜백이 비어 있습니다.");
            return;
        }

        _onClickOptionCallback.Invoke(interactionOption);
    }
}
