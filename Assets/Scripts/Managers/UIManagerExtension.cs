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
    VeryFrontUI,
    ScreenTransitionUIRoot
}

public enum UIType
{
    Inventory,
    DialogueUI,
    InspectObjectUI,
    InteractionMenuUI,
    MiniMapUI,
    PlayerMenuUI,
    ScreenTransitionUI,
    CustomCursorUI,
    PopupTextUI,
    TitleUI,
    FirstGameStartUI,
    LoadingUI,
    NPCStatusUI
}

public static class UIManagerExtension
{
    public static string GetUIPath(this UIManager uiManager, UIRootType uiRootType, UIType uiType)
    {
        if (uiRootType == UIRootType.ScreenTransitionUIRoot && uiType == UIType.ScreenTransitionUI)
        {
            return $"Prefabs/UI/{UIRootType.VeryFrontUI}/{uiType}";
        }

        return $"Prefabs/UI/{uiRootType}/{uiType}";
    }

    public static TitleUI OpenTitleUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.MainUI, UIType.TitleUI);
        if (uiBase == null)
        {
            Debug.LogWarning("TitleUI를 생성하지 못했습니다.");
            return null;
        }

        if (uiBase is TitleUI titleUI)
        {
            return titleUI;
        }

        Debug.LogWarning("생성된 UI가 TitleUI 타입이 아닙니다.");
        return null;
    }

    public static void CloseTitleUI(this UIManager uiManager)
    {
        uiManager.CloseUI(UIRootType.MainUI, UIType.TitleUI);
    }

    public static FirstGameStartUI OpenFirstGameStartUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.FirstGameStartUI);
        if (uiBase == null)
        {
            Debug.LogWarning("FirstGameStartUI를 생성하지 못했습니다.");
            return null;
        }

        if (uiBase is FirstGameStartUI firstGameStartUI)
        {
            return firstGameStartUI;
        }

        Debug.LogWarning("생성된 UI가 FirstGameStartUI 타입이 아닙니다.");
        return null;
    }

    public static void CloseFirstGameStartUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.FirstGameStartUI);
    }

    public static LoadingUI OpenLoadingUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.VeryFrontUI, UIType.LoadingUI);
        if (uiBase == null)
        {
            Debug.LogWarning("LoadingUI를 생성하지 못했습니다.");
            return null;
        }

        if (uiBase is LoadingUI loadingUI)
        {
            return loadingUI;
        }

        Debug.LogWarning("생성된 UI가 LoadingUI 타입이 아닙니다.");
        return null;
    }

    public static void CloseLoadingUI(this UIManager uiManager)
    {
        uiManager.CloseUI(UIRootType.VeryFrontUI, UIType.LoadingUI);
    }

    public static void OpenInventoryPopup(this UIManager uiManager)
    {
        uiManager.OpenInventoryPopup(string.Empty);
    }

    public static void OpenInventoryPopup(this UIManager uiManager, string selectedInspectObjectDataId)
    {
        PlayerMenuUI playerMenuUI = uiManager.OpenPlayerMenuUI();
        if (playerMenuUI == null)
        {
            return;
        }

        playerMenuUI.OpenInventoryTab(selectedInspectObjectDataId);
    }

    public static void OpenDialogueUI(this UIManager uiManager, string startDialogueId)
    {
        uiManager.OpenDialogueUI(startDialogueId, string.Empty);
    }

    public static void OpenDialogueUI(this UIManager uiManager, string startDialogueId, string objectName)
    {
        uiManager.OpenDialogueUI(startDialogueId, objectName, null);
    }

    public static void OpenDialogueUI(this UIManager uiManager, string startDialogueId, string objectName, Action onCompleteCallback)
    {
        var uiBase = uiManager.OpenContentUI(UIType.DialogueUI);
        if (uiBase == null)
        {
            Debug.LogWarning("DialogueUI가 생성되지 않았습니다.");
            return;
        }

        if (uiBase is DialogueUI dialogueUi)
        {
            dialogueUi.StartDialogue(startDialogueId, objectName, onCompleteCallback);
            return;
        }

        Debug.LogWarning("생성된 UI가 DialogueUI 타입이 아닙니다.");
    }

    public static void OpenInspectObjectUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenContentUI(UIType.InspectObjectUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InspectObjectUI가 생성되지 않았습니다.");
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
            return;
        }

        Debug.LogWarning("생성된 UI가 InspectObjectUI 타입이 아닙니다.");
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
            return;
        }

        Debug.LogWarning("생성된 UI가 InspectObjectUI 타입이 아닙니다.");
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
            return;
        }

        Debug.LogWarning("생성된 UI가 InspectObjectUI 타입이 아닙니다.");
    }

    public static void OpenInspectAreaUI(this UIManager uiManager, string inspectAreaDataId)
    {
        var uiBase = uiManager.OpenContentUI(UIType.InspectObjectUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InspectObjectUI가 생성되지 않았습니다.");
            return;
        }

        if (uiBase is InspectObjectUI inspectObjectUI)
        {
            inspectObjectUI.StartInspectArea(inspectAreaDataId);
            return;
        }

        Debug.LogWarning("생성된 UI가 InspectObjectUI 타입이 아닙니다.");
    }

    public static void OpenJournalInspectUI(this UIManager uiManager, string journalDataId, IJournalInspectCompleteHandler completeHandler)
    {
        var uiBase = uiManager.OpenContentUI(UIType.InspectObjectUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InspectObjectUI가 생성되지 않았습니다.");
            return;
        }

        if (uiBase is InspectObjectUI inspectObjectUI)
        {
            inspectObjectUI.StartJournalInspect(journalDataId, completeHandler);
            return;
        }

        Debug.LogWarning("생성된 UI가 InspectObjectUI 타입이 아닙니다.");
    }

    public static void OpenJournalTab(this UIManager uiManager, string journalDataId, string noticeMessage)
    {
        PlayerMenuUI playerMenuUI = uiManager.OpenPlayerMenuUI();
        if (playerMenuUI == null)
        {
            return;
        }

        playerMenuUI.OpenJournalTab(journalDataId, noticeMessage);
    }

    public static NPCStatusUI OpenNPCStatusUI(this UIManager uiManager, string characterDataId)
    {
        if (string.IsNullOrEmpty(characterDataId))
        {
            Debug.LogWarning("NPCStatusUI를 열 캐릭터 데이터 ID가 비어 있습니다.");
            return null;
        }

        var uiBase = uiManager.OpenContentUI(UIType.NPCStatusUI);
        if (uiBase == null)
        {
            Debug.LogWarning("NPCStatusUI가 생성되지 않았습니다.");
            return null;
        }

        if (uiBase is NPCStatusUI npcStatusUI)
        {
            npcStatusUI.OpenCharacterStatus(characterDataId);
            return npcStatusUI;
        }

        Debug.LogWarning("생성된 UI가 NPCStatusUI 타입이 아닙니다.");
        return null;
    }

    public static void CloseNPCStatusUI(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.NPCStatusUI);
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

    public static InteractionMenuUI OpenInteractionMenuUIWithoutMoveLock(this UIManager uiManager, List<InteractionOption> optionList, Transform followTarget, Vector3 followWorldOffset, Vector3 followWorldOffsetWhenNearRightEdge, float followRightEdgeViewportLine, Action<InteractionOption> onClickOptionCallback)
    {
        var uiBase = uiManager.OpenUI(UIRootType.MainUI, UIType.InteractionMenuUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InteractionMenuUI could not be created.");
            return null;
        }

        if (uiBase is InteractionMenuUI interactionMenuUI)
        {
            interactionMenuUI.OpenFollowMenuWithoutMoveLock(optionList, followTarget, followWorldOffset, followWorldOffsetWhenNearRightEdge, followRightEdgeViewportLine, onClickOptionCallback);
            return interactionMenuUI;
        }

        Debug.LogWarning("Created UI is not InteractionMenuUI.");
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

    public static MiniMapUI OpenMiniMapUI(this UIManager uiManager)
    {
        PlayerMenuUI playerMenuUI = uiManager.OpenPlayerMenuUI();
        if (playerMenuUI == null)
        {
            return null;
        }

        playerMenuUI.OpenMiniMapTab();
        return null;
    }

    public static void CloseMiniMapUI(this UIManager uiManager)
    {
        uiManager.ClosePlayerMenuUI();
    }

    public static PlayerMenuUI OpenPlayerMenuUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenContentUI(UIType.PlayerMenuUI);
        if (uiBase == null)
        {
            Debug.LogWarning("PlayerMenuUI가 생성되지 않았습니다.");
            return null;
        }

        if (uiBase is PlayerMenuUI playerMenuUI)
        {
            return playerMenuUI;
        }

        Debug.LogWarning("생성된 UI가 PlayerMenuUI 타입이 아닙니다.");
        return null;
    }

    public static void ClosePlayerMenuUI(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.PlayerMenuUI);
    }

    public static ScreenTransitionUI OpenScreenTransitionUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.ScreenTransitionUIRoot, UIType.ScreenTransitionUI);
        if (uiBase == null)
        {
            Debug.LogWarning("ScreenTransitionUI could not be created.");
            return null;
        }

        if (uiBase is ScreenTransitionUI screenTransitionUI)
        {
            screenTransitionUI.gameObject.SetActive(true);
            screenTransitionUI.transform.SetAsLastSibling();
            return screenTransitionUI;
        }

        Debug.LogWarning("Created UI is not ScreenTransitionUI.");
        return null;
    }

    public static PopupTextUI OpenPopupTextUI(this UIManager uiManager, string popupTextDataId)
    {
        if (string.IsNullOrEmpty(popupTextDataId))
        {
            Debug.LogWarning("PopupText data id is empty, so PopupTextUI cannot be opened.");
            return null;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so PopupTextUI cannot load text data.");
            return null;
        }

        PopupTextData popupTextData = GameDataManager.Instance.GetPopupTextData(popupTextDataId);
        if (popupTextData == null)
        {
            Debug.LogWarning($"PopupText data was not found: {popupTextDataId}");
            return null;
        }

        return uiManager.OpenPopupTextUIWithText(popupTextData.Description);
    }

    private static PopupTextUI OpenPopupTextUIWithText(this UIManager uiManager, string popupText)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.PopupTextUI);
        if (uiBase == null)
        {
            Debug.LogWarning("PopupTextUI가 생성되지 않았습니다.");
            return null;
        }

        if (uiBase is PopupTextUI popupTextUI)
        {
            popupTextUI.ShowPopupText(popupText);
            return popupTextUI;
        }

        Debug.LogWarning("생성된 UI가 PopupTextUI 타입이 아닙니다.");
        return null;
    }

    public static CustomCursorUI OpenCustomCursorUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.VeryFrontUI, UIType.CustomCursorUI);
        if (uiBase == null)
        {
            Debug.LogWarning("CustomCursorUI could not be created.");
            return null;
        }

        if (uiBase is CustomCursorUI customCursorUI)
        {
            return customCursorUI;
        }

        Debug.LogWarning("Created UI is not CustomCursorUI.");
        return null;
    }
}
