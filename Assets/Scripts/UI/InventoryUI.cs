using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : UIBase
{
    [Header("Grid")]
    [SerializeField] private RectTransform Layout_GridInventory;

    [Header("Viewer")]
    [SerializeField] private RectTransform Layout_ObjectViewer;
    [SerializeField] private Text Text_ObjectName;
    [SerializeField] private Vector2 _previewObjectSize = new Vector2(720f, 540f);

    [Header("Description")]
    [SerializeField] private RectTransform Layout_Description;
    [SerializeField] private Text Text_InventoryDescription;

    [Header("Inspect")]
    [SerializeField] private RectTransform Layout_InspectButton;
    [SerializeField] private Button Button_InspectObject;

    private string _selectedInspectObjectDataId;
    private GameObject _createdPreviewObject;

    private void OnEnable()
    {
        if (Button_InspectObject != null)
        {
            Button_InspectObject.onClick.AddListener(RequestOpenSelectedInspectObject);
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

        ClearPreviewObject();
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
        SetObjectName(inspectObjectData.Name);
        SetObjectDescription(inspectObjectData.Description);
        RefreshSelectedObjectContent(true);
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
        FitPreviewObjectToViewer(_createdPreviewObject);
        PreparePreviewObject(_createdPreviewObject);
        BringObjectNameToFront();
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
            Debug.LogWarning("InventoryUI preview object is null, so preview transform cannot be normalized.");
            return;
        }

        RectTransform previewRectTransform = previewObject.transform as RectTransform;
        if (previewRectTransform == null)
        {
            Debug.LogWarning($"{previewObject.name} has no RectTransform, so preview transform cannot be normalized.");
            return;
        }

        previewRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        previewRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        previewRectTransform.pivot = new Vector2(0.5f, 0.5f);
        previewRectTransform.anchoredPosition = Vector2.zero;
        previewRectTransform.localScale = Vector3.one;
    }

    private void FitPreviewObjectToViewer(GameObject previewObject)
    {
        if (previewObject == null)
        {
            Debug.LogWarning("InventoryUI preview object is null, so preview fit cannot be applied.");
            return;
        }

        if (Layout_ObjectViewer == null)
        {
            Debug.LogWarning("InventoryUI Layout_ObjectViewer reference is missing, so preview fit cannot be applied.");
            return;
        }

        RectTransform previewRectTransform = previewObject.transform as RectTransform;
        if (previewRectTransform == null)
        {
            Debug.LogWarning($"{previewObject.name} has no RectTransform, so preview fit cannot be applied.");
            return;
        }

        Vector2 viewerSize = Layout_ObjectViewer.rect.size;
        if (viewerSize.x <= 0f || viewerSize.y <= 0f)
        {
            Debug.LogWarning($"InventoryUI Layout_ObjectViewer size is invalid: {viewerSize}");
            return;
        }

        Vector2 previewSize = previewRectTransform.rect.size;
        if ((previewSize.x <= 0f || previewSize.y <= 0f) && _previewObjectSize.x > 0f && _previewObjectSize.y > 0f)
        {
            previewSize = _previewObjectSize;
        }

        if (previewSize.x <= 0f || previewSize.y <= 0f)
        {
            Debug.LogWarning($"InventoryUI preview object size is invalid: {previewSize}");
            return;
        }

        float widthScale = viewerSize.x / previewSize.x;
        float heightScale = viewerSize.y / previewSize.y;
        float fitScale = Mathf.Min(1f, widthScale, heightScale);

        previewRectTransform.localScale = Vector3.one * fitScale;
    }

    private void SetObjectDescription(string description)
    {
        if (Text_InventoryDescription == null)
        {
            Debug.LogWarning("InventoryUI Text_InventoryDescription reference is missing.");
            return;
        }

        Text_InventoryDescription.text = description ?? string.Empty;
    }

    private void SetObjectName(string objectName)
    {
        if (Text_ObjectName == null)
        {
            Debug.LogWarning("InventoryUI Text_ObjectName reference is missing.");
            return;
        }

        Text_ObjectName.text = objectName ?? string.Empty;
        BringObjectNameToFront();
    }

    private void BringObjectNameToFront()
    {
        if (Text_ObjectName == null)
        {
            return;
        }

        Text_ObjectName.transform.SetAsLastSibling();
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
        UIManager.Instance.ClosePlayerMenuUI();
        UIManager.Instance.OpenInspectObjectUIFromInventory(_selectedInspectObjectDataId);
    }

    private void ClearSelectedObject()
    {
        _selectedInspectObjectDataId = string.Empty;

        SetObjectName(string.Empty);
        SetObjectDescription(string.Empty);
        RefreshSelectedObjectContent(false);
        RefreshInspectObjectButton();

        ClearPreviewObject();
    }

    private void RefreshSelectedObjectContent(bool isVisible)
    {
        if (Layout_ObjectViewer != null)
        {
            Layout_ObjectViewer.gameObject.SetActive(isVisible);
        }
        else
        {
            Debug.LogWarning("InventoryUI Layout_ObjectViewer reference is missing.");
        }

        if (Layout_Description != null)
        {
            Layout_Description.gameObject.SetActive(isVisible);
        }
        else
        {
            Debug.LogWarning("InventoryUI Layout_Description reference is missing.");
        }

        if (Layout_InspectButton != null)
        {
            Layout_InspectButton.gameObject.SetActive(isVisible);
        }
        else
        {
            Debug.LogWarning("InventoryUI Layout_InspectButton reference is missing.");
        }
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
