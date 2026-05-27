using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] Canvas Canvas_BgRoot;
    [SerializeField] Canvas Canvas_MainRoot;
    [SerializeField] Canvas Canvas_ContentRoot;
    [SerializeField] Canvas Canvas_PopupRoot;
    [SerializeField] Canvas Canvas_VeryFrontRoot;

    public static UIManager Instance { get; set; }

    // 얘는 생성과 제거에 관한 부분 -> Instancing과 가비지컬렉터와 연관이 있는 애
    private Dictionary<UIType, UIBase> _createdUIDic = new Dictionary<UIType, UIBase>();
    // 얘는 활성과 비활성에 관한 부분 -> SetActive
    private HashSet<UIType> _openedUIDic = new HashSet<UIType>();


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 테스트 단계에서는 시작과 동시에 자동 UI 생성 흐름을 잠시 막아둔다.
        // 매니저는 살아있되, 필요한 UI는 이후에 직접 호출해서 연다.
        // this.ShowStartupUIOnGameStart();
    }

    private void Update()
    {
        if (InputManager.GetInventoryToggleDown())
        {
            RequestToggleInventory();
        }
    }

    private void RequestToggleInventory()
    {
        if (IsUIOpened(UIType.Inventory))
        {
            CloseContentUI(UIType.Inventory);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            Debug.LogWarning("게임이 일시정지 상태라 인벤토리 입력을 처리하지 않습니다.");
            return;
        }

        this.OpenInventoryPopup();
    }

    public UIBase OpenUI(UIRootType uiRootType, UIType uiType, bool isInitialHide = false)
    {
        // 딱히 요청이 있진 않고 오픈만 하면 되는 UI에서 사용

        var openedUI = GetCreatedUI(uiRootType, uiType);

        bool isSetActiveOnOpen = (isInitialHide == false); // 열었을 때 기본적으로 숨겨서 열 것인지 체크
        if (_openedUIDic.Contains(uiType) == false)
        {
            openedUI.gameObject.SetActive(isSetActiveOnOpen);
            _openedUIDic.Add(uiType);
        }

        return openedUI;
    }

    public void CloseUI(UIRootType uiRootType, UIType uiType)
    {
        if (_openedUIDic.Contains(uiType))
        {
            var openedUi = _createdUIDic[uiType];
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
                root = Canvas_BgRoot.transform;
                break;
            case UIRootType.MainUI:
                root = Canvas_MainRoot.transform;
                break;
            case UIRootType.ContentUI:
                root = Canvas_ContentRoot.transform;
                break;
            case UIRootType.PopupUI:
                root = Canvas_PopupRoot.transform;
                break;
            case UIRootType.VeryFrontUI:
                root = Canvas_VeryFrontRoot.transform;
                break;
        }
        return root;
    }

    private void CreateUI(UIRootType uiRootType, UIType uiType)
    {
        if (_createdUIDic.ContainsKey(uiType) == false)
        {
            string path = this.GetUIPath(uiRootType, uiType);
            GameObject loadedObj = (GameObject)Resources.Load(path);
            Transform root = GetRootTransform(uiRootType);
            GameObject gObj = Instantiate(loadedObj, root);
            if (gObj != null)
            {
                var uiBase = gObj.GetComponent<UIBase>();
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
        return _createdUIDic[uiType];
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
