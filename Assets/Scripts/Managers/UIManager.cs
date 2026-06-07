using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private const int BgRootSortingOrder = 0;
    private const int MainRootSortingOrder = 100;
    private const int ContentRootSortingOrder = 200;
    private const int PopupRootSortingOrder = 300;
    private const int VeryFrontRootSortingOrder = 400;
    private const int ScreenTransitionRootSortingOrder = 500;

    [SerializeField] Canvas Canvas_BgRoot;
    [SerializeField] Canvas Canvas_MainRoot;
    [SerializeField] Canvas Canvas_ContentRoot;
    [SerializeField] Canvas Canvas_PopupRoot;
    [SerializeField] Canvas Canvas_VeryFrontRoot;
    [SerializeField] Canvas Canvas_ScreenTransitionUIRoot;

    public static UIManager Instance { get; set; }

    // 얘는 생성과 제거에 관한 부분 -> Instancing과 가비지컬렉터와 연관이 있는 애
    private Dictionary<UIType, UIBase> _createdUIDic = new Dictionary<UIType, UIBase>();
    // 얘는 활성과 비활성에 관한 부분 -> SetActive
    private HashSet<UIType> _openedUIDic = new HashSet<UIType>();


    private void Awake()
    {
        Instance = this;
        SetupCanvasSortingOrders();
    }

    private void Start()
    {
        // 테스트 단계에서는 시작과 동시에 자동 UI 생성 흐름을 잠시 막아둔다.
        // 매니저는 살아있되, 필요한 UI는 이후에 직접 호출해서 연다.
        // this.ShowStartupUIOnGameStart();
        this.OpenCustomCursorUI();
        PreloadUI(UIRootType.ScreenTransitionUIRoot, UIType.ScreenTransitionUI);
        this.OpenTitleUI();
    }

    private void Update()
    {
        if (InputManager.GetCloseUIDown())
        {
            RequestCloseClosableUI();
        }

        if (InputManager.GetInventoryToggleDown())
        {
            RequestToggleInventory();
        }

        if (InputManager.GetMiniMapToggleDown())
        {
            RequestToggleMiniMap();
        }

        if (InputManager.GetJournalToggleDown())
        {
            RequestToggleJournal();
        }
    }

    private void RequestToggleInventory()
    {
        if (GameManager.Instance != null && GameManager.Instance.CanUseInventoryHotkey() == false)
        {
            Debug.LogWarning("인벤토리 단축키가 잠겨 있어 입력을 처리하지 않습니다.");
            return;
        }

        if (IsUIOpened(UIType.PlayerMenuUI))
        {
            var uiBase = FindCreatedUI(UIType.PlayerMenuUI);
            if (uiBase is PlayerMenuUI playerMenuUI && playerMenuUI.CurrentTabType != PlayerMenuTabType.Inventory)
            {
                playerMenuUI.OpenInventoryTab();
            }

            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            Debug.LogWarning("게임이 일시정지 상태라 인벤토리 입력을 처리하지 않습니다.");
            return;
        }

        this.OpenInventoryPopup();
    }

    private void RequestToggleMiniMap()
    {
        if (GameManager.Instance != null && GameManager.Instance.CanUseMiniMapHotkey() == false)
        {
            Debug.LogWarning("미니맵 단축키가 잠겨 있어 입력을 처리하지 않습니다.");
            return;
        }

        if (IsUIOpened(UIType.PlayerMenuUI))
        {
            var uiBase = FindCreatedUI(UIType.PlayerMenuUI);
            if (uiBase is PlayerMenuUI playerMenuUI)
            {
                if (playerMenuUI.CurrentTabType == PlayerMenuTabType.MiniMap)
                {
                    return;
                }

                playerMenuUI.OpenMiniMapTab();
                return;
            }

            this.ClosePlayerMenuUI();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            Debug.LogWarning("게임이 일시정지 상태라 미니맵 입력을 처리하지 않습니다.");
            return;
        }

        this.OpenMiniMapUI();
    }

    private void RequestToggleJournal()
    {
        if (GameManager.Instance != null && GameManager.Instance.CanUseJournalHotkey() == false)
        {
            Debug.LogWarning("일지 단축키가 잠겨 있어 입력을 처리하지 않습니다.");
            return;
        }

        if (IsUIOpened(UIType.PlayerMenuUI))
        {
            var uiBase = FindCreatedUI(UIType.PlayerMenuUI);
            if (uiBase is PlayerMenuUI playerMenuUI)
            {
                if (playerMenuUI.CurrentTabType == PlayerMenuTabType.Journal)
                {
                    return;
                }

                playerMenuUI.OpenJournalTab();
                return;
            }

            this.ClosePlayerMenuUI();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            Debug.LogWarning("게임이 일시정지 상태라 일지 입력을 처리하지 않습니다.");
            return;
        }

        PlayerMenuUI openedPlayerMenuUI = this.OpenPlayerMenuUI();
        if (openedPlayerMenuUI == null)
        {
            return;
        }

        openedPlayerMenuUI.OpenJournalTab();
    }

    private void RequestCloseClosableUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.CanUseCloseUIHotkey() == false)
        {
            Debug.LogWarning("UI 닫기 단축키가 잠겨 있어 입력을 처리하지 않습니다.");
            return;
        }

        if (IsUIOpened(UIType.DialogueUI) || IsUIOpened(UIType.InspectObjectUI))
        {
            return;
        }

        if (IsUIOpened(UIType.InteractionMenuUI))
        {
            this.CloseInteractionMenuUI();
            return;
        }

        if (IsUIOpened(UIType.PlayerMenuUI))
        {
            this.ClosePlayerMenuUI();
            return;
        }

        if (IsUIOpened(UIType.Inventory))
        {
            CloseContentUI(UIType.Inventory);
        }
    }

    public UIBase OpenUI(UIRootType uiRootType, UIType uiType, bool isInitialHide = false)
    {
        // 딱히 요청이 있진 않고 오픈만 하면 되는 UI에서 사용

        var openedUI = GetCreatedUI(uiRootType, uiType);
        if (openedUI == null)
        {
            Debug.LogWarning($"{uiType} UI를 찾거나 생성하지 못했습니다.");
            return null;
        }

        bool isSetActiveOnOpen = (isInitialHide == false); // 열었을 때 기본적으로 숨겨서 열 것인지 체크
        if (_openedUIDic.Contains(uiType) == false)
        {
            openedUI.gameObject.SetActive(isSetActiveOnOpen);
            _openedUIDic.Add(uiType);
        }

        return openedUI;
    }

    public UIBase PreloadUI(UIRootType uiRootType, UIType uiType)
    {
        UIBase createdUI = GetCreatedUI(uiRootType, uiType);
        if (createdUI == null)
        {
            Debug.LogWarning($"{uiType} UI를 미리 생성하지 못했습니다.");
            return null;
        }

        createdUI.gameObject.SetActive(false);
        _openedUIDic.Remove(uiType);
        return createdUI;
    }

    public void CloseUI(UIRootType uiRootType, UIType uiType)
    {
        if (_openedUIDic.Contains(uiType))
        {
            if (_createdUIDic.TryGetValue(uiType, out UIBase openedUi) == false || openedUi == null)
            {
                Debug.LogWarning($"{uiType} UI 참조가 없어 닫을 수 없습니다.");
                _openedUIDic.Remove(uiType);
                return;
            }

            if (openedUi is InteractionMenuUI interactionMenuUI)
            {
                interactionMenuUI.CloseMenu();
            }

            openedUi.gameObject.SetActive(false);
            _openedUIDic.Remove(uiType);
        }
    }

    private Transform GetRootTransform(UIRootType uiRootType)
    {
        Transform root = null;
        switch (uiRootType)
        {
            case UIRootType.BackgroundUI:
                root = Canvas_BgRoot != null ? Canvas_BgRoot.transform : null;
                break;
            case UIRootType.MainUI:
                root = Canvas_MainRoot != null ? Canvas_MainRoot.transform : null;
                break;
            case UIRootType.ContentUI:
                root = Canvas_ContentRoot != null ? Canvas_ContentRoot.transform : null;
                break;
            case UIRootType.PopupUI:
                root = Canvas_PopupRoot != null ? Canvas_PopupRoot.transform : null;
                break;
            case UIRootType.VeryFrontUI:
                root = Canvas_VeryFrontRoot != null ? Canvas_VeryFrontRoot.transform : null;
                break;
            case UIRootType.ScreenTransitionUIRoot:
                root = Canvas_ScreenTransitionUIRoot != null ? Canvas_ScreenTransitionUIRoot.transform : null;
                break;
        }
        return root;
    }

    private void SetupCanvasSortingOrders()
    {
        SetupCanvasSortingOrder(Canvas_BgRoot, BgRootSortingOrder, nameof(Canvas_BgRoot));
        SetupCanvasSortingOrder(Canvas_MainRoot, MainRootSortingOrder, nameof(Canvas_MainRoot));
        SetupCanvasSortingOrder(Canvas_ContentRoot, ContentRootSortingOrder, nameof(Canvas_ContentRoot));
        SetupCanvasSortingOrder(Canvas_PopupRoot, PopupRootSortingOrder, nameof(Canvas_PopupRoot));
        SetupCanvasSortingOrder(Canvas_VeryFrontRoot, VeryFrontRootSortingOrder, nameof(Canvas_VeryFrontRoot));
        SetupCanvasSortingOrder(Canvas_ScreenTransitionUIRoot, ScreenTransitionRootSortingOrder, nameof(Canvas_ScreenTransitionUIRoot));
    }

    private void SetupCanvasSortingOrder(Canvas targetCanvas, int sortingOrder, string canvasName)
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning($"{canvasName} 참조가 비어 있어 UI 정렬 순서를 설정할 수 없습니다.");
            return;
        }

        targetCanvas.overrideSorting = true;
        targetCanvas.sortingOrder = sortingOrder;
    }

    private void CreateUI(UIRootType uiRootType, UIType uiType)
    {
        if (_createdUIDic.ContainsKey(uiType) == false)
        {
            string path = this.GetUIPath(uiRootType, uiType);
            GameObject loadedObj = (GameObject)Resources.Load(path);
            Transform root = GetRootTransform(uiRootType);
            if (loadedObj == null)
            {
                Debug.LogWarning($"{path} 경로에서 UI 프리팹을 찾을 수 없습니다.");
                return;
            }

            if (root == null)
            {
                Debug.LogWarning($"{uiRootType} Root Canvas가 비어 있어 {uiType} UI를 생성할 수 없습니다.");
                return;
            }

            GameObject gObj = Instantiate(loadedObj, root);
            if (gObj != null)
            {
                gObj.name = uiType.ToString();

                var uiBase = gObj.GetComponent<UIBase>();
                if (uiBase == null)
                {
                    Debug.LogWarning($"{uiType} 프리팹에 UIBase를 상속한 스크립트가 없습니다.");
                    return;
                }

                _createdUIDic.Add(uiType, uiBase);
            }
        }
    }

    private UIBase GetCreatedUI(UIRootType uiRootType, UIType uiType)
    {
        if (_createdUIDic.ContainsKey(uiType) == false)
        {
            CreateUI(uiRootType, uiType);
        }

        if (_createdUIDic.TryGetValue(uiType, out UIBase uiBase))
        {
            return uiBase;
        }

        return null;
    }

    public UIBase FindCreatedUI(UIType uiType)
    {
        if (_createdUIDic.TryGetValue(uiType, out UIBase uiBase))
        {
            return uiBase;
        }

        return null;
    }

    public bool IsUIOpened(UIType uiType)
    {
        return _openedUIDic.Contains(uiType);
    }


    public UIBase OpenContentUI(UIType uiType)
    {
        return OpenUI(UIRootType.ContentUI, uiType);
    }

    public UIBase OpenPopupUI(UIType uiType)
    {
        return OpenUI(UIRootType.PopupUI, uiType);
    }

    public void CloseContentUI(UIType uiType)
    {
        CloseUI(UIRootType.ContentUI, uiType);
    }

    public void ClosePopupUI(UIType uiType)
    {
        CloseUI(UIRootType.PopupUI, uiType);
    }

}
