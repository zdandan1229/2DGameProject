using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : UIBase
{
    [Header("Grid")]
    [SerializeField] private RectTransform Layout_GridInventory;

    [Header("Viewer")]
    [SerializeField] private RectTransform Layout_ObjectViewer;
    [SerializeField] private Vector2 _previewObjectSize = new Vector2(720f, 540f);

    [Header("Description")]
    [SerializeField] private Text Text_Description;

    [Header("Inspect")]
    [SerializeField] private Button Button_InspectObject;

    [Header("Close")]
    [SerializeField] private Button Button_CloseInventory;

    private string _selectedInspectObjectDataId;
    private GameObject _createdPreviewObject;
    private bool _didPauseGame;

    private void OnEnable()
    {
        RequestPauseGame();

        if (Button_InspectObject != null)
        {
            Button_InspectObject.onClick.AddListener(RequestOpenSelectedInspectObject);
        }

        if (Button_CloseInventory != null)
        {
            Button_CloseInventory.onClick.AddListener(CloseInventoryUI);
        }
        else
        {
            Debug.LogWarning("InventoryUI Button_CloseInventory reference is missing.");
        }

        ClearSelectedObject();
        Debug.Log("InventoryUI opened.");
    }

    private void OnDisable()
    {
        if (Button_InspectObject != null)
        {
            Button_InspectObject.onClick.RemoveListener(RequestOpenSelectedInspectObject);
        }

        if (Button_CloseInventory != null)
        {
            Button_CloseInventory.onClick.RemoveListener(CloseInventoryUI);
        }

        ClearPreviewObject();
        RequestResumeGame();
    }

    private void RequestPauseGame()
    {
        _didPauseGame = false;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is missing, so InventoryUI cannot pause the game.");
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
            Debug.LogWarning("GameManager.Instance is missing, so InventoryUI cannot resume the game.");
            _didPauseGame = false;
            return;
        }

        GameManager.Instance.ResumeGame();
        _didPauseGame = false;
    }

    public void RefreshInventory(List<string> inspectObjectDataIdList)
    {
        if (Layout_GridInventory == null)
        {
            Debug.LogWarning("InventoryUI Layout_GridInventory reference is missing.");
            return;
        }

        if (inspectObjectDataIdList == null)
        {
            Debug.LogWarning("Inventory inspect object list is null.");
            return;
        }

        ClearInventorySlots();

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so inventory slot data cannot be loaded.");
            return;
        }

        int slotCount = Layout_GridInventory.childCount;
        int displayCount = Mathf.Min(inspectObjectDataIdList.Count, slotCount);

        for (int i = 0; i < displayCount; i++)
        {
            InventorySlotUI inventorySlotUI = GetInventorySlotUI(i);
            if (inventorySlotUI == null)
            {
                continue;
            }

            string inspectObjectDataId = inspectObjectDataIdList[i];
            InspectObjectData inspectObjectData = GameDataManager.Instance.GetInspectObjectData(inspectObjectDataId);
            if (inspectObjectData == null)
            {
                Debug.LogWarning($"Inventory inspect object data was not found: {inspectObjectDataId}");
                inventorySlotUI.ClearSlot();
                continue;
            }

            inventorySlotUI.SetInspectObjectData(inspectObjectData, SelectInspectObject);
        }

        Debug.Log($"Inventory refresh requested. Count: {inspectObjectDataIdList.Count}");
    }

    private void ClearInventorySlots()
    {
        if (Layout_GridInventory == null)
        {
            return;
        }

        for (int i = 0; i < Layout_GridInventory.childCount; i++)
        {
            InventorySlotUI inventorySlotUI = GetInventorySlotUI(i);
            if (inventorySlotUI != null)
            {
                inventorySlotUI.ClearSlot();
            }
        }
    }

    private InventorySlotUI GetInventorySlotUI(int slotIndex)
    {
        if (Layout_GridInventory == null)
        {
            Debug.LogWarning("InventoryUI Layout_GridInventory reference is missing.");
            return null;
        }

        if (slotIndex < 0 || slotIndex >= Layout_GridInventory.childCount)
        {
            Debug.LogWarning($"Inventory slot index is out of range: {slotIndex}");
            return null;
        }

        Transform slotTransform = Layout_GridInventory.GetChild(slotIndex);
        InventorySlotUI inventorySlotUI = slotTransform.GetComponent<InventorySlotUI>();
        if (inventorySlotUI == null)
        {
            inventorySlotUI = slotTransform.gameObject.AddComponent<InventorySlotUI>();
        }

        return inventorySlotUI;
    }

    public void SelectInspectObject(string inspectObjectDataId)
    {
        if (string.IsNullOrEmpty(inspectObjectDataId))
        {
            Debug.LogWarning("Selected inspect object data id is empty.");
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so inspect object data cannot be loaded.");
            return;
        }

        InspectObjectData inspectObjectData = GameDataManager.Instance.GetInspectObjectData(inspectObjectDataId);
        if (inspectObjectData == null)
        {
            Debug.LogWarning($"Inspect object data was not found: {inspectObjectDataId}");
            return;
        }

        _selectedInspectObjectDataId = inspectObjectDataId;
        ShowObjectPreview(inspectObjectData);
        SetObjectDescription(inspectObjectData.Description);
        RefreshInspectObjectButton();

        Debug.Log($"Inventory inspect object selected: {inspectObjectDataId}");
    }

    private void ShowObjectPreview(InspectObjectData inspectObjectData)
    {
        ClearPreviewObject();

        if (Layout_ObjectViewer == null)
        {
            Debug.LogWarning("InventoryUI Layout_ObjectViewer reference is missing.");
            return;
        }

        if (inspectObjectData == null)
        {
            Debug.LogWarning("Inspect object data for preview is null.");
            return;
        }

        if (string.IsNullOrEmpty(inspectObjectData.PrefabPath))
        {
            Debug.LogWarning("Inspect object preview prefab path is empty.");
            return;
        }

        GameObject objectPrefab = Resources.Load<GameObject>(inspectObjectData.PrefabPath);
        if (objectPrefab == null)
        {
            Debug.LogWarning($"Inspect object preview prefab was not found: {inspectObjectData.PrefabPath}");
            return;
        }

        EnsureObjectViewerMask();
        _createdPreviewObject = Instantiate(objectPrefab, Layout_ObjectViewer);
        NormalizePreviewRectTransforms(_createdPreviewObject);
        PreparePreviewObject(_createdPreviewObject);
    }

    private void EnsureObjectViewerMask()
    {
        if (Layout_ObjectViewer == null)
        {
            return;
        }

        RectMask2D rectMask = Layout_ObjectViewer.GetComponent<RectMask2D>();
        if (rectMask == null)
        {
            Layout_ObjectViewer.gameObject.AddComponent<RectMask2D>();
        }
    }

    private void PreparePreviewObject(GameObject previewObject)
    {
        if (previewObject == null)
        {
            return;
        }

        Button[] buttonArr = previewObject.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttonArr.Length; i++)
        {
            buttonArr[i].onClick.RemoveAllListeners();
        }

        Graphic[] graphicArr = previewObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphicArr.Length; i++)
        {
            graphicArr[i].raycastTarget = false;
        }

        InspectPoint[] inspectPointArr = previewObject.GetComponentsInChildren<InspectPoint>(true);
        for (int i = 0; i < inspectPointArr.Length; i++)
        {
            inspectPointArr[i].enabled = false;
        }
    }

    private void NormalizePreviewRectTransforms(GameObject previewObject)
    {
        if (previewObject == null)
        {
            return;
        }

        RectTransform[] rectTransformArr = previewObject.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransformArr.Length; i++)
        {
            RectTransform rectTransform = rectTransformArr[i];
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = _previewObjectSize;
            rectTransform.localScale = Vector3.one;
        }
    }

    private void SetObjectDescription(string description)
    {
        if (Text_Description == null)
        {
            Debug.LogWarning("InventoryUI Text_Description reference is missing.");
            return;
        }

        Text_Description.text = description ?? string.Empty;
    }

    private void RequestOpenSelectedInspectObject()
    {
        if (string.IsNullOrEmpty(_selectedInspectObjectDataId))
        {
            Debug.LogWarning("No inspect object is selected, so InspectObjectUI cannot be opened.");
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing, so selected inspect object cannot be opened.");
            return;
        }

        if (GameDataManager.Instance == null || GameDataManager.Instance.GetInspectObjectData(_selectedInspectObjectDataId) == null)
        {
            Debug.LogWarning($"Selected inspect object data was not found: {_selectedInspectObjectDataId}");
            return;
        }

        Debug.Log($"InspectObjectUI open requested: {_selectedInspectObjectDataId}");
        UIManager.Instance.CloseContentUI(UIType.Inventory);
        UIManager.Instance.OpenInspectObjectUIFromInventory(_selectedInspectObjectDataId);
    }

    private void CloseInventoryUI()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing, so InventoryUI cannot be closed.");
            return;
        }

        UIManager.Instance.CloseContentUI(UIType.Inventory);
    }

    private void ClearSelectedObject()
    {
        _selectedInspectObjectDataId = string.Empty;

        SetObjectDescription(string.Empty);
        RefreshInspectObjectButton();

        ClearPreviewObject();
    }

    private void RefreshInspectObjectButton()
    {
        if (Button_InspectObject == null)
        {
            return;
        }

        Button_InspectObject.interactable = string.IsNullOrEmpty(_selectedInspectObjectDataId) == false;
    }

    private void ClearPreviewObject()
    {
        if (_createdPreviewObject == null)
        {
            return;
        }

        Destroy(_createdPreviewObject);
        _createdPreviewObject = null;
    }
}
