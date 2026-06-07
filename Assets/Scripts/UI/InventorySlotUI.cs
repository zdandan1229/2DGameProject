using UnityEngine;
using UnityEngine.UI;
using System;

public class InventorySlotUI : MonoBehaviour
{
    private const string DefaultIconPath = "Icons/Icon_Test";

    [SerializeField] private Image Image_Icon;
    [SerializeField] private Button Button_Slot;

    private string _inspectObjectDataId;
    private bool _hasItem;
    private Action<string> _onClickSlot;

    private void Awake()
    {
        TrySetupIconImage();
        TrySetupButton();
        ClearSlot();
    }

    private void OnDestroy()
    {
        if (Button_Slot != null)
        {
            Button_Slot.onClick.RemoveListener(RequestSelectSlot);
        }
    }

    public void SetInspectObjectData(InspectObjectData inspectObjectData, Action<string> onClickSlot)
    {
        if (inspectObjectData == null)
        {
            Debug.LogWarning("InventorySlotUI cannot set item because InspectObjectData is null.");
            ClearSlot();
            return;
        }

        if (string.IsNullOrEmpty(inspectObjectData.Id))
        {
            Debug.LogWarning("InventorySlotUI cannot set item because InspectObjectData.Id is empty.");
            ClearSlot();
            return;
        }

        if (TrySetupIconImage() == false)
        {
            return;
        }

        Sprite iconSprite = LoadIconSprite(inspectObjectData.IconPath);
        if (iconSprite == null)
        {
            Debug.LogError($"Inventory default icon was not found: Resources/{DefaultIconPath}");
            ClearSlot();
            return;
        }

        _inspectObjectDataId = inspectObjectData.Id;
        _hasItem = true;
        _onClickSlot = onClickSlot;

        Image_Icon.sprite = iconSprite;
        Image_Icon.gameObject.SetActive(true);
        RefreshButtonInteractable();
    }

    public void ClearSlot()
    {
        _inspectObjectDataId = string.Empty;
        _hasItem = false;
        _onClickSlot = null;

        if (TrySetupIconImage() == false)
        {
            return;
        }

        Image_Icon.sprite = null;
        Image_Icon.gameObject.SetActive(false);
        RefreshButtonInteractable();
    }

    public string GetInspectObjectDataId()
    {
        return _inspectObjectDataId;
    }

    public bool HasItem()
    {
        return _hasItem;
    }

    private Sprite LoadIconSprite(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath) == false)
        {
            Sprite dataIconSprite = Resources.Load<Sprite>(iconPath);
            if (dataIconSprite != null)
            {
                return dataIconSprite;
            }

            Debug.LogWarning($"Inventory icon was not found, so default icon will be used: Resources/{iconPath}");
        }

        return Resources.Load<Sprite>(DefaultIconPath);
    }

    private bool TrySetupIconImage()
    {
        if (Image_Icon != null)
        {
            return true;
        }

        Transform iconTransform = transform.Find("Image_Icon");
        if (iconTransform != null)
        {
            Image_Icon = iconTransform.GetComponent<Image>();
        }

        if (Image_Icon == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing Image_Icon reference.");
            return false;
        }

        return true;
    }

    private bool TrySetupButton()
    {
        if (Button_Slot != null)
        {
            return true;
        }

        Button_Slot = GetComponent<Button>();
        if (Button_Slot == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing Button component.");
            return false;
        }

        Button_Slot.onClick.RemoveListener(RequestSelectSlot);
        Button_Slot.onClick.AddListener(RequestSelectSlot);
        RefreshButtonInteractable();
        return true;
    }

    private void RefreshButtonInteractable()
    {
        if (Button_Slot == null)
        {
            return;
        }

        Button_Slot.interactable = _hasItem;
    }

    private void RequestSelectSlot()
    {
        if (_hasItem == false)
        {
            return;
        }

        if (string.IsNullOrEmpty(_inspectObjectDataId))
        {
            Debug.LogWarning($"{gameObject.name} has no inspect object data id.");
            return;
        }

        _onClickSlot?.Invoke(_inspectObjectDataId);
    }
}
