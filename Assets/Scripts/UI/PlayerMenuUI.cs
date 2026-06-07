using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerMenuTabType
{
    Inventory,
    Journal,
    MiniMap
}

public class PlayerMenuUI : UIBase
{
    [Header("Panels")]
    [SerializeField] private GameObject Panel_Inventory;
    [SerializeField] private GameObject Panel_Journal;
    [SerializeField] private GameObject Panel_Map;

    [Header("Tab Buttons")]
    [SerializeField] private Button Button_Inventory;
    [SerializeField] private Button Button_Journal;
    [SerializeField] private Button Button_MiniMap;

    [Header("Close")]
    [SerializeField] private Button Button_Exit;

    private InventoryUI _inventoryUI;
    private JournalUI _journalUI;
    private MiniMapUI _miniMapUI;
    private bool _didPauseGame;

    public PlayerMenuTabType CurrentTabType { get; private set; }

    private void Awake()
    {
        BindMissingReferences();
    }

    private void OnEnable()
    {
        RequestPauseGame();
        AddButtonListeners();
        RefreshTabButtonInteractable();
        OpenFirstAvailableTab();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
        RequestResumeGame();
    }

    private void Update()
    {
        RefreshTabButtonInteractable();

        if (InputManager.GetPlayerMenuNextTabDown())
        {
            OpenNextTab();
        }
    }

    public void OpenInventoryTab()
    {
        if (CanOpenInventoryTab() == false)
        {
            Debug.LogWarning("인벤토리 탭이 잠겨 있어 열 수 없습니다.");
            return;
        }

        ShowTab(PlayerMenuTabType.Inventory, string.Empty);
    }

    public void OpenInventoryTab(string selectedInspectObjectDataId)
    {
        if (CanOpenInventoryTab() == false)
        {
            Debug.LogWarning("인벤토리 탭이 잠겨 있어 열 수 없습니다.");
            return;
        }

        ShowTab(PlayerMenuTabType.Inventory, selectedInspectObjectDataId);
    }

    public void OpenJournalTab()
    {
        if (CanOpenJournalTab() == false)
        {
            Debug.LogWarning("일지 탭이 잠겨 있어 열 수 없습니다.");
            return;
        }

        ShowTab(PlayerMenuTabType.Journal, string.Empty);
    }

    public void OpenJournalTab(string selectedJournalDataId, string noticeMessage)
    {
        if (CanOpenJournalTab() == false)
        {
            Debug.LogWarning("일지 탭이 잠겨 있어 열 수 없습니다.");
            return;
        }

        ShowTab(PlayerMenuTabType.Journal, string.Empty);
        RefreshJournalPanel(selectedJournalDataId, noticeMessage);
    }

    public void OpenMiniMapTab()
    {
        if (CanOpenMiniMapTab() == false)
        {
            Debug.LogWarning("미니맵 탭이 잠겨 있어 열 수 없습니다.");
            return;
        }

        ShowTab(PlayerMenuTabType.MiniMap, string.Empty);
    }

    public void OpenNextTab()
    {
        switch (CurrentTabType)
        {
            case PlayerMenuTabType.Inventory:
                OpenNextAvailableTab(PlayerMenuTabType.Journal);
                break;
            case PlayerMenuTabType.Journal:
                OpenNextAvailableTab(PlayerMenuTabType.MiniMap);
                break;
            case PlayerMenuTabType.MiniMap:
                OpenNextAvailableTab(PlayerMenuTabType.Inventory);
                break;
            default:
                Debug.LogWarning($"알 수 없는 PlayerMenu 탭 상태입니다: {CurrentTabType}");
                OpenNextAvailableTab(PlayerMenuTabType.Inventory);
                break;
        }
    }

    private void OpenFirstAvailableTab()
    {
        if (CanOpenInventoryTab())
        {
            ShowTab(PlayerMenuTabType.Inventory, string.Empty);
            return;
        }

        if (CanOpenJournalTab())
        {
            ShowTab(PlayerMenuTabType.Journal, string.Empty);
            return;
        }

        if (CanOpenMiniMapTab())
        {
            ShowTab(PlayerMenuTabType.MiniMap, string.Empty);
            return;
        }

        SetPanelActive(Panel_Inventory, false);
        SetPanelActive(Panel_Journal, false);
        SetPanelActive(Panel_Map, false);
        Debug.LogWarning("열 수 있는 PlayerMenu 탭이 없어 모든 탭 패널을 닫습니다.");
    }

    private void OpenNextAvailableTab(PlayerMenuTabType firstTabType)
    {
        PlayerMenuTabType nextTabType = firstTabType;
        for (int i = 0; i < 3; i++)
        {
            if (CanOpenTab(nextTabType))
            {
                OpenTab(nextTabType);
                return;
            }

            nextTabType = GetNextTabType(nextTabType);
        }

        Debug.LogWarning("열 수 있는 PlayerMenu 탭이 없습니다.");
    }

    private void OpenTab(PlayerMenuTabType tabType)
    {
        switch (tabType)
        {
            case PlayerMenuTabType.Inventory:
                OpenInventoryTab();
                break;
            case PlayerMenuTabType.Journal:
                OpenJournalTab();
                break;
            case PlayerMenuTabType.MiniMap:
                OpenMiniMapTab();
                break;
            default:
                Debug.LogWarning($"알 수 없는 PlayerMenu 탭 상태입니다: {tabType}");
                break;
        }
    }

    private PlayerMenuTabType GetNextTabType(PlayerMenuTabType tabType)
    {
        switch (tabType)
        {
            case PlayerMenuTabType.Inventory:
                return PlayerMenuTabType.Journal;
            case PlayerMenuTabType.Journal:
                return PlayerMenuTabType.MiniMap;
            case PlayerMenuTabType.MiniMap:
                return PlayerMenuTabType.Inventory;
            default:
                Debug.LogWarning($"알 수 없는 PlayerMenu 탭 상태입니다: {tabType}");
                return PlayerMenuTabType.Inventory;
        }
    }

    private bool CanOpenTab(PlayerMenuTabType tabType)
    {
        switch (tabType)
        {
            case PlayerMenuTabType.Inventory:
                return CanOpenInventoryTab();
            case PlayerMenuTabType.Journal:
                return CanOpenJournalTab();
            case PlayerMenuTabType.MiniMap:
                return CanOpenMiniMapTab();
            default:
                Debug.LogWarning($"알 수 없는 PlayerMenu 탭 상태입니다: {tabType}");
                return false;
        }
    }

    private bool CanOpenInventoryTab()
    {
        return GameManager.Instance == null || GameManager.Instance.CanUseInventoryHotkey();
    }

    private bool CanOpenJournalTab()
    {
        return GameManager.Instance == null || GameManager.Instance.CanUseJournalHotkey();
    }

    private bool CanOpenMiniMapTab()
    {
        return GameManager.Instance == null || GameManager.Instance.CanUseMiniMapHotkey();
    }

    private void RefreshTabButtonInteractable()
    {
        SetButtonInteractable(Button_Inventory, CanOpenInventoryTab());
        SetButtonInteractable(Button_Journal, CanOpenJournalTab());
        SetButtonInteractable(Button_MiniMap, CanOpenMiniMapTab());
    }

    private void SetButtonInteractable(Button button, bool isInteractable)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = isInteractable;
    }

    private void ShowTab(PlayerMenuTabType tabType, string selectedInspectObjectDataId)
    {
        CurrentTabType = tabType;

        SetPanelActive(Panel_Inventory, tabType == PlayerMenuTabType.Inventory);
        SetPanelActive(Panel_Journal, tabType == PlayerMenuTabType.Journal);
        SetPanelActive(Panel_Map, tabType == PlayerMenuTabType.MiniMap);

        if (tabType == PlayerMenuTabType.Inventory)
        {
            RefreshInventoryPanel(selectedInspectObjectDataId);
        }

        if (tabType == PlayerMenuTabType.Journal)
        {
            RefreshJournalPanel(string.Empty, string.Empty);
        }
    }

    private void RefreshInventoryPanel(string selectedInspectObjectDataId)
    {
        if (_inventoryUI == null)
        {
            Debug.LogWarning("PlayerMenuUI의 InventoryUI 참조가 없어 인벤토리 패널을 갱신할 수 없습니다.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager.Instance가 없어 빈 인벤토리 목록으로 갱신합니다.");
            _inventoryUI.RefreshInventory(new List<string>());
        }
        else
        {
            _inventoryUI.RefreshInventory(InventoryManager.Instance.GetInspectObjectDataIdList());
        }

        if (string.IsNullOrEmpty(selectedInspectObjectDataId) == false)
        {
            _inventoryUI.SelectInspectObject(selectedInspectObjectDataId);
        }
    }

    private void RefreshJournalPanel(string selectedJournalDataId, string noticeMessage)
    {
        if (_journalUI == null)
        {
            Debug.LogWarning("PlayerMenuUI에 JournalUI 참조가 없어 일지 패널을 갱신할 수 없습니다.");
            return;
        }

        _journalUI.RefreshJournalList();

        if (string.IsNullOrEmpty(selectedJournalDataId) == false)
        {
            _journalUI.SelectJournal(selectedJournalDataId, noticeMessage);
        }
    }

    private void ClosePlayerMenu()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 PlayerMenuUI를 닫을 수 없습니다.");
            return;
        }

        UIManager.Instance.ClosePlayerMenuUI();
    }

    private void AddButtonListeners()
    {
        AddButtonListener(Button_Inventory, OnClickInventoryTab, "Button_Inventory");
        AddButtonListener(Button_Journal, OnClickJournalTab, "Button_Journal");
        AddButtonListener(Button_MiniMap, OnClickMiniMapTab, "Button_MiniMap");
        AddButtonListener(Button_Exit, ClosePlayerMenu, "Button_Exit");
    }

    private void RemoveButtonListeners()
    {
        if (Button_Inventory != null)
        {
            Button_Inventory.onClick.RemoveListener(OnClickInventoryTab);
        }

        if (Button_Journal != null)
        {
            Button_Journal.onClick.RemoveListener(OnClickJournalTab);
        }

        if (Button_MiniMap != null)
        {
            Button_MiniMap.onClick.RemoveListener(OnClickMiniMapTab);
        }

        if (Button_Exit != null)
        {
            Button_Exit.onClick.RemoveListener(ClosePlayerMenu);
        }
    }

    private void AddButtonListener(Button button, UnityEngine.Events.UnityAction action, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"PlayerMenuUI의 {buttonName} 참조가 비어 있습니다.");
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void OnClickInventoryTab()
    {
        OpenInventoryTab();
    }

    private void OnClickJournalTab()
    {
        OpenJournalTab();
    }

    private void OnClickMiniMapTab()
    {
        OpenMiniMapTab();
    }

    private void RequestPauseGame()
    {
        _didPauseGame = false;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance가 없어 PlayerMenuUI에서 게임 정지를 요청할 수 없습니다.");
            return;
        }

        if (GameManager.Instance.IsGamePaused)
        {
            return;
        }

        GameManager.Instance.PauseGame();
        _didPauseGame = true;
    }

    private void RequestResumeGame()
    {
        if (_didPauseGame == false)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance가 없어 PlayerMenuUI에서 게임 재개를 요청할 수 없습니다.");
            _didPauseGame = false;
            return;
        }

        GameManager.Instance.ResumeGame();
        _didPauseGame = false;
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel == null)
        {
            Debug.LogWarning("PlayerMenuUI의 패널 참조가 비어 있어 탭을 전환할 수 없습니다.");
            return;
        }

        panel.SetActive(isActive);
    }

    private void BindMissingReferences()
    {
        Panel_Inventory = FindChildGameObjectIfNull(Panel_Inventory, "Panel_Inventory");
        Panel_Journal = FindChildGameObjectIfNull(Panel_Journal, "Panel_Journal");
        Panel_Map = FindChildGameObjectIfNull(Panel_Map, "Panel_Map");

        Button_Inventory = FindChildButtonIfNull(Button_Inventory, "Button_Inventory");
        Button_Journal = FindChildButtonIfNull(Button_Journal, "Button_Journal");
        Button_MiniMap = FindChildButtonIfNull(Button_MiniMap, "Button_MiniMap");
        if (Button_MiniMap == null)
        {
            Button_MiniMap = FindChildButtonIfNull(Button_MiniMap, "Button_Map");
        }

        if (Button_MiniMap == null)
        {
            Button_MiniMap = FindChildButtonIfNull(Button_MiniMap, "Button_MapTab");
        }

        Button_Exit = FindChildButtonIfNull(Button_Exit, "Button_Exit");

        if (Panel_Inventory != null)
        {
            _inventoryUI = Panel_Inventory.GetComponent<InventoryUI>();
        }

        if (Panel_Journal != null)
        {
            _journalUI = Panel_Journal.GetComponent<JournalUI>();
        }

        if (Panel_Map != null)
        {
            _miniMapUI = Panel_Map.GetComponent<MiniMapUI>();
        }

        if (_journalUI == null)
        {
            Debug.LogWarning("PlayerMenuUI의 Panel_Journal에 JournalUI가 없습니다.");
        }

        if (_miniMapUI == null)
        {
            Debug.LogWarning("PlayerMenuUI의 Panel_Map에 MiniMapUI가 없습니다.");
        }
    }

    private GameObject FindChildGameObjectIfNull(GameObject currentValue, string childName)
    {
        if (currentValue != null)
        {
            return currentValue;
        }

        Transform childTransform = FindChildByName(transform, childName);
        if (childTransform == null)
        {
            Debug.LogWarning($"PlayerMenuUI에서 {childName} 오브젝트를 찾지 못했습니다.");
            return null;
        }

        return childTransform.gameObject;
    }

    private Button FindChildButtonIfNull(Button currentValue, string childName)
    {
        if (currentValue != null)
        {
            return currentValue;
        }

        Transform childTransform = FindChildByName(transform, childName);
        if (childTransform == null)
        {
            return null;
        }

        Button button = childTransform.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"{childName} 오브젝트에 Button 컴포넌트가 없습니다.");
        }

        return button;
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform foundChild = FindChildByName(root.GetChild(i), childName);
            if (foundChild != null)
            {
                return foundChild;
            }
        }

        return null;
    }
}
