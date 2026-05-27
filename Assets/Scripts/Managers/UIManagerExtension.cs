using System;
using System.Collections.Generic;
using UnityEngine;

public enum UIRootType
{
    None = 0,
    BackgroundUI,
    MainUI,
    ContentUI,
    PopupUI,
    VeryFrontUI
}

public enum UIType
{
    MainUI,
    MyProfilePopup, // 신규UI추가 1) 새로운 UIType을 추가한다
    Inventory,
    LoadingUI,
    DialogueUI,
    InspectObjectUI,
    SJDialogueUI,
    InfoBookUI,
    ClearPopup,
    InteractionMenuUI
}

public static class UIManagerExtension
{
    public static string GetUIPath(this UIManager uiManager, UIRootType uiRootType, UIType uiType)
    {
        string path = string.Empty; // "" == string.Empty

        // 신규UI추가 2) Resources.Load를 할 경로를 직접 명시한다
        // 해당 경로는 프로젝트창에서 Resources/Prefabs/UI폴더 내에 있는 RootType 폴더명과 UIType 프리팹 이름과 동일해야 한다! (ex. ContentUI/MyProfilePopup)
        path = $"Prefabs/UI/{uiRootType}/{uiType}";
        return path;
    }

    public static void ShowStartupUIOnGameStart(this UIManager uiManager)
    {
        // 테스트 단계에서는 시작 UI 프리팹이 아직 없어 null Instantiate 오류가 나므로 잠시 막아둔다.
        // uiManager.OpenLoadingUI();
        // uiManager.OpenUI(UIRootType.MainUI, UIType.MainUI);
        // 게임 로비 UI를 여기서 오픈해주자 -> uiManager.
        // MainUI도
    }

    // 신규UI추가 3) 이렇게 어떤 팝업을 열고, 열때 전달해야하는 파라미터가 있다면 이렇게 전달한다.
    // 추가하기 편하게 그냥 빼둔 확장 메서드이므로, uiManager과 this는 우선 넘어가자
    public static void OpenMyProfilePopup(this UIManager uiManager, string characterDataId)
    {
        // 신규UI추가 4) 이렇게 UI 타입을 던져서 UI 생성을 요청한다
        var uiBase = uiManager.OpenPopupUI(UIType.MyProfilePopup);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }
    }

    public static void OpenInventoryPopup(this UIManager uiManger)
    {
        uiManger.OpenInventoryPopup(string.Empty);
    }

    public static void OpenInventoryPopup(this UIManager uiManger, string selectedInspectObjectDataId)
    {
        var uiBase = uiManger.OpenContentUI(UIType.Inventory);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }

        if (uiBase is InventoryUI inventoryUI)
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("InventoryManager.Instance가 존재하지 않아 인벤토리 목록을 갱신할 수 없습니다.");
                inventoryUI.RefreshInventory(new List<string>());
                RestoreInventorySelection(inventoryUI, selectedInspectObjectDataId);
                return;
            }

            inventoryUI.RefreshInventory(InventoryManager.Instance.GetInspectObjectDataIdList());
            RestoreInventorySelection(inventoryUI, selectedInspectObjectDataId);
        }
    }

    private static void RestoreInventorySelection(InventoryUI inventoryUI, string selectedInspectObjectDataId)
    {
        if (inventoryUI == null || string.IsNullOrEmpty(selectedInspectObjectDataId))
        {
            return;
        }

        inventoryUI.SelectInspectObject(selectedInspectObjectDataId);
    }

    public static void OpenLoadingUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.VeryFrontUI, UIType.LoadingUI);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseLoadingUI(this UIManager uiManager)
    {
        uiManager.CloseUI(UIRootType.VeryFrontUI, UIType.LoadingUI);
    }

    public static void OpenDialogueUI(this UIManager uiManager, string startDialogueId)
    {
        uiManager.OpenDialogueUI(startDialogueId, string.Empty);
    }

    public static void OpenDialogueUI(this UIManager uiManager, string startDialogueId, string objectName)
    {
        var uiBase = uiManager.OpenContentUI(UIType.DialogueUI);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }

        if (uiBase is DialogueUI dialogueUi)
        {
            dialogueUi.StartDialogue(startDialogueId, objectName);
        }
    }

    public static void OpenInspectObjectUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenContentUI(UIType.InspectObjectUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InspectObjectUI가 생성되지 않았습니다.");
            return;
        }
    }

    public static void OpenInspectObjectUI(this UIManager uiManager, GameObject objectPrefab, string description)
    {
        var uiBase = uiManager.OpenContentUI(UIType.InspectObjectUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InspectObjectUI가 생성되지 않았습니다.");
            return;
        }

        if (uiBase is InspectObjectUI inspectObjectUI)
        {
            inspectObjectUI.StartInspectObject(objectPrefab, description);
        }
    }

    public static void OpenInspectObjectUI(this UIManager uiManager, string inspectObjectDataId)
    {
        uiManager.OpenInspectObjectUI(inspectObjectDataId, null);
    }

    public static void OpenInspectObjectUIFromInventory(this UIManager uiManager, string inspectObjectDataId)
    {
        var uiBase = uiManager.OpenContentUI(UIType.InspectObjectUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InspectObjectUI가 생성되지 않았습니다.");
            return;
        }

        if (uiBase is InspectObjectUI inspectObjectUI)
        {
            inspectObjectUI.StartInspectObjectFromInventory(inspectObjectDataId);
        }
    }

    public static void OpenInspectObjectUI(this UIManager uiManager, string inspectObjectDataId, IInspectObjectCompleteHandler completeHandler)
    {
        var uiBase = uiManager.OpenContentUI(UIType.InspectObjectUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InspectObjectUI가 생성되지 않았습니다.");
            return;
        }

        if (uiBase is InspectObjectUI inspectObjectUI)
        {
            inspectObjectUI.StartInspectObject(inspectObjectDataId, completeHandler);
        }
    }

    public static void OpenSJDialogueUI(this UIManager uiManager, string startDialogueId)
    {
        var uiBase = uiManager.OpenContentUI(UIType.SJDialogueUI);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }

        if (uiBase is DialogueUI dialogueUi)
        {
            dialogueUi.StartDialogue(startDialogueId);
        }
    }

    public static void OpenClearPopup(this UIManager uiManager, Vector3 worldPosition)
    {
        var uiBase = uiManager.OpenUI(UIRootType.MainUI, UIType.ClearPopup);
        if (uiBase == null)
        {
            Debug.LogWarning("ClearPopup이 생성되지 않았습니다");
            return;
        }
    }

    public static InteractionMenuUI OpenInteractionMenuUI(this UIManager uiManager, List<InteractionOption> optionList, Vector3 worldPosition, Action<InteractionOption> onClickOptionCallback)
    {
        var uiBase = uiManager.OpenUI(UIRootType.MainUI, UIType.InteractionMenuUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InteractionMenuUI가 생성되지 않았습니다.");
            return null;
        }

        if (uiBase is InteractionMenuUI interactionMenuUI)
        {
            interactionMenuUI.OpenMenu(optionList, worldPosition, onClickOptionCallback);
            return interactionMenuUI;
        }

        Debug.LogWarning("생성된 UI가 InteractionMenuUI 타입이 아닙니다.");
        return null;
    }

    public static void CloseInteractionMenuUI(this UIManager uiManager)
    {
        var uiBase = uiManager.FindCreatedUI(UIType.InteractionMenuUI);
        if (uiBase is InteractionMenuUI interactionMenuUI)
        {
            interactionMenuUI.CloseMenu();
        }

        uiManager.CloseUI(UIRootType.MainUI, UIType.InteractionMenuUI);
    }
}
