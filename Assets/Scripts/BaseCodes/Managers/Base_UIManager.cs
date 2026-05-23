#if false
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Base_UIManager : MonoBehaviour
{
    [SerializeField] Canvas Canvas_BgRoot;
    [SerializeField] Canvas Canvas_MainRoot;
    [SerializeField] Canvas Canvas_ContentRoot;
    [SerializeField] Canvas Canvas_PopupRoot;
    [SerializeField] Canvas Canvas_VeryFrontRoot;

    public static Base_UIManager Instance { get; set; }

    // 얘는 생성과 제거에 관한 부분 -> Instancing과 가비지컬렉터와 연관이 있는 애
    private Dictionary<Base_UIType, Base_UIBase> _createdUIDic = new Dictionary<Base_UIType, Base_UIBase>();
    // 얘는 활성과 비활성에 관한 부분 -> SetActive
    private HashSet<Base_UIType> _openedUIDic = new HashSet<Base_UIType>();


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 매니저들이 탄생한 후에 UI매니저가 처음으로 게임이 실행될 때 필요한 UI들을 오픈해준다!
        this.ShowStartupUIOnGameStart();
    }

    public Base_UIBase OpenUI(Base_UIRootType uiRootType, Base_UIType uiType, bool isInitialHide = false)
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

    public void CloseUI(Base_UIRootType uiRootType, Base_UIType uiType)
    {
        if (_openedUIDic.Contains(uiType))
        {
            var openedUi = _createdUIDic[uiType];
            openedUi.gameObject.SetActive(false);
            _openedUIDic.Remove(uiType);
        }
    }

    private Transform GetRootTransform(Base_UIRootType uiRootType)
    {
        Transform root = null;
        switch (uiRootType)
        {
            case Base_UIRootType.BackgroundUI:
                root = Canvas_BgRoot.transform;
                break;
            case Base_UIRootType.MainUI:
                root = Canvas_MainRoot.transform;
                break;
            case Base_UIRootType.ContentUI:
                root = Canvas_ContentRoot.transform;
                break;
            case Base_UIRootType.PopupUI:
                root = Canvas_PopupRoot.transform;
                break;
            case Base_UIRootType.VeryFrontUI:
                root = Canvas_VeryFrontRoot.transform;
                break;
        }
        return root;
    }

    private void CreateUI(Base_UIRootType uiRootType, Base_UIType uiType)
    {
        if (_createdUIDic.ContainsKey(uiType) == false)
        {
            string path = this.GetUIPath(uiRootType, uiType);
            GameObject loadedObj = (GameObject)Resources.Load(path);
            Transform root = GetRootTransform(uiRootType);
            GameObject gObj = Instantiate(loadedObj, root);
            if (gObj != null)
            {
                var uiBase = gObj.GetComponent<Base_UIBase>();
                _createdUIDic.Add(uiType, uiBase);
            }
        }
    }

    private Base_UIBase GetCreatedUI(Base_UIRootType uiRootType, Base_UIType uiType)
    {
        if (_createdUIDic.ContainsKey(uiType) == false)
        {
            CreateUI(uiRootType, uiType);
        }
        return _createdUIDic[uiType];
    }


    public Base_UIBase OpenContentUI(Base_UIType uiType)
    {
        return OpenUI(Base_UIRootType.ContentUI, uiType);
    }

    public Base_UIBase OpenPopupUI(Base_UIType uiType)
    {
        return OpenUI(Base_UIRootType.PopupUI, uiType);
    }

    public void CloseContentUI(Base_UIType uiType)
    {
        CloseUI(Base_UIRootType.ContentUI, uiType);
    }

    public void ClosePopupUI(Base_UIType uiType)
    {
        CloseUI(Base_UIRootType.PopupUI, uiType);
    }

}
#endif
