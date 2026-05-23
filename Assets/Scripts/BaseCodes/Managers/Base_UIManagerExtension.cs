#if false
using UnityEngine;

public enum Base_UIRootType
{
    None = 0,
    BackgroundUI,
    MainUI,
    ContentUI,
    PopupUI,
    VeryFrontUI
}

public enum Base_UIType
{
    MainUI,
    MyProfilePopup, // 신규UI추가 1) 새로운 UIType을 추가한다
    Inventory,
    LoadingUI,
    DialogueUI,
    SJDialogueUI,
    InfoBookUI,
    ClearPopup
}

public static class Base_UIManagerExtension
{
    public static string GetUIPath(this Base_UIManager uiManager, Base_UIRootType uiRootType, Base_UIType uiType)
    {
        string path = string.Empty; // "" == string.Empty

        // 신규UI추가 2) Resources.Load를 할 경로를 직접 명시한다
        // 해당 경로는 프로젝트창에서 Resources/Prefabs/UI폴더 내에 있는 RootType 폴더명과 UIType 프리팹 이름과 동일해야 한다! (ex. ContentUI/MyProfilePopup)
        path = $"Prefabs/UI/{uiRootType}/{uiType}";
        return path;
    }

    public static void ShowStartupUIOnGameStart(this Base_UIManager uiManager)
    {
        uiManager.OpenLoadingUI();
        uiManager.OpenUI(Base_UIRootType.MainUI, Base_UIType.MainUI);
        // 게임 로비 UI를 여기서 오픈해주자 -> uiManager.
        // MainUI도
    }

    // 신규UI추가 3) 이렇게 어떤 팝업을 열고, 열때 전달해야하는 파라미터가 있다면 이렇게 전달한다.
        // 추가하기 편하게 그냥 빼둔 확장 메서드이므로, uiManager과 this는 우선 넘어가자
    public static void OpenMyProfilePopup(this Base_UIManager uiManager, string characterDataId)
    {
        // 신규UI추가 4) 이렇게 UI 타입을 던져서 UI 생성을 요청한다
        var uiBase = uiManager.OpenPopupUI(Base_UIType.MyProfilePopup);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }

        if (uiBase is Base_MyProfilePopup myProfilePopup)
        {
            myProfilePopup.RefreshCharacterUI(characterDataId);
        }
    }

    public static void OpenInventoryPopup(this Base_UIManager uiManger)
    {
        var uiBase = uiManger.OpenContentUI(Base_UIType.Inventory);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }
    }

    public static void OpenLoadingUI(this Base_UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(Base_UIRootType.VeryFrontUI, Base_UIType.LoadingUI);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseLoadingUI(this Base_UIManager uiManager)
    {
        uiManager.CloseUI(Base_UIRootType.VeryFrontUI, Base_UIType.LoadingUI);
    }

    public static void OpenDialogueUI(this Base_UIManager uiManager, string startDialogueId)
    {
        var uiBase = uiManager.OpenContentUI(Base_UIType.DialogueUI);
        if(uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }

        if (uiBase is Base_DialogueUI dialogueUi)
        {
            dialogueUi.StartDialogue(startDialogueId);
        }
    }

    public static void OpenSJDialogueUI(this Base_UIManager uiManager, string startDialogueId)
    {
        var uiBase = uiManager.OpenContentUI(Base_UIType.SJDialogueUI);
        if (uiBase == null)
        {
            Debug.LogWarning($"UI가 생성되지 않았습니다");
            return;
        }

        if (uiBase is Base_DialogueUI dialogueUi)
        {
            dialogueUi.StartDialogue(startDialogueId);
        }
    }

    public static void OpenClearPopup (this Base_UIManager uiManager, Vector3 worldPosition)
    {
        var uiBase = uiManager.OpenUI(Base_UIRootType.MainUI, Base_UIType.ClearPopup);
        if (uiBase == null)
        {
            Debug.LogWarning("ClearPopup이 생성되지 않았습니다");
            return;
        }

    }

}
#endif
