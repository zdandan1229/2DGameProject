using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapUI : UIBase
{
    [Serializable]
    private class MiniMapMoveButton
    {
        [SerializeField] private Button Button_Map;
        [SerializeField] private string _entryPointId;

        public Button MapButton => Button_Map;
        public string EntryPointId => _entryPointId;

        public void SetMapButton(Button mapButton)
        {
            Button_Map = mapButton;
        }
    }

    [Header("Map Buttons")]
    [SerializeField] private List<MiniMapMoveButton> _mapMoveButtonList = new List<MiniMapMoveButton>();

    private readonly Dictionary<Button, UnityEngine.Events.UnityAction> _buttonActionDic = new Dictionary<Button, UnityEngine.Events.UnityAction>();

    private void OnEnable()
    {
        RepairMissingMapButtonReferences();
        AddMapButtonListeners();
    }

    private void OnDisable()
    {
        RemoveMapButtonListeners();
    }

    private void AddMapButtonListeners()
    {
        if (_mapMoveButtonList == null)
        {
            Debug.LogWarning("MiniMapUI map move button list is missing.");
            return;
        }

        foreach (MiniMapMoveButton moveButton in _mapMoveButtonList)
        {
            AddMapButtonListener(moveButton);
        }
    }

    private void AddMapButtonListener(MiniMapMoveButton moveButton)
    {
        if (moveButton == null)
        {
            Debug.LogWarning("MiniMapUI map move button data is missing.");
            return;
        }

        Button mapButton = moveButton.MapButton;
        if (mapButton == null)
        {
            Debug.LogWarning("MiniMapUI map button reference is missing.");
            return;
        }

        if (string.IsNullOrEmpty(moveButton.EntryPointId))
        {
            Debug.LogWarning($"{mapButton.name} has no EntryPointId.");
            return;
        }

        string entryPointId = moveButton.EntryPointId;
        UnityEngine.Events.UnityAction action = () => RequestMoveToEntryPoint(entryPointId);
        mapButton.onClick.AddListener(action);
        _buttonActionDic[mapButton] = action;
    }

    private void RemoveMapButtonListeners()
    {
        foreach (KeyValuePair<Button, UnityEngine.Events.UnityAction> pair in _buttonActionDic)
        {
            if (pair.Key != null)
            {
                pair.Key.onClick.RemoveListener(pair.Value);
            }
        }

        _buttonActionDic.Clear();
    }

    private void RequestMoveToEntryPoint(string entryPointId)
    {
        if (string.IsNullOrEmpty(entryPointId))
        {
            Debug.LogWarning("EntryPointId is empty, so player cannot be moved.");
            return;
        }

        if (WorldTransitionManager.Instance == null)
        {
            Debug.LogWarning("WorldTransitionManager.Instance is missing, so player cannot be moved.");
            return;
        }

        WorldTransitionManager.Instance.MovePlayerToEntryPoint(entryPointId);
    }

    private void RepairMissingMapButtonReferences()
    {
        if (_mapMoveButtonList == null || _mapMoveButtonList.Count == 0)
        {
            return;
        }

        List<Button> childMapButtonList = FindChildButtonsByName("Button_Map");
        if (childMapButtonList.Count == 0)
        {
            childMapButtonList = FindChildRoomButtons();
        }

        int buttonIndex = 0;

        for (int i = 0; i < _mapMoveButtonList.Count; i++)
        {
            MiniMapMoveButton moveButton = _mapMoveButtonList[i];
            if (moveButton == null || moveButton.MapButton != null)
            {
                continue;
            }

            if (buttonIndex >= childMapButtonList.Count)
            {
                Debug.LogWarning("MiniMapUI does not have enough child map buttons to repair references.");
                return;
            }

            moveButton.SetMapButton(childMapButtonList[buttonIndex]);
            buttonIndex++;
        }
    }

    private List<Button> FindChildButtonsByName(string childName)
    {
        List<Button> buttonList = new List<Button>();
        CollectChildButtonsByName(transform, childName, buttonList);
        return buttonList;
    }

    private List<Button> FindChildRoomButtons()
    {
        List<Button> buttonList = new List<Button>();
        CollectChildRoomButtons(transform, buttonList);
        return buttonList;
    }

    private void CollectChildRoomButtons(Transform root, List<Button> buttonList)
    {
        if (root == null || buttonList == null)
        {
            return;
        }

        Button button = root.GetComponent<Button>();
        if (button != null)
        {
            buttonList.Add(button);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectChildRoomButtons(root.GetChild(i), buttonList);
        }
    }

    private void CollectChildButtonsByName(Transform root, string childName, List<Button> buttonList)
    {
        if (root == null || buttonList == null)
        {
            return;
        }

        if (root.name == childName)
        {
            Button button = root.GetComponent<Button>();
            if (button != null)
            {
                buttonList.Add(button);
            }
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectChildButtonsByName(root.GetChild(i), childName, buttonList);
        }
    }
}
