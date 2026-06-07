using UnityEngine;
using UnityEngine.UI;

public class NPCStatusUI : UIBase
{
    private const string LockedInfoText = "해금되지 않은 정보입니다. 호감도를 올리거나 메인 스토리를 진행하여 해금하세요.";

    [SerializeField] private Image Image_Portrait;
    [SerializeField] private Text Text_NPCName;
    [SerializeField] private Text Text_Description;
    [SerializeField] private Button Button_Data1;
    [SerializeField] private Button Button_Data2;
    [SerializeField] private Button Button_Data3;
    [SerializeField] private Button Button_Data4;
    [SerializeField] private Button Button_Exit;

    private CharacterData _currentCharacterData;

    private void Awake()
    {
        FindReferencesIfNull();
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void OpenCharacterStatus(string characterDataId)
    {
        if (string.IsNullOrEmpty(characterDataId))
        {
            Debug.LogWarning("NPCStatusUI에 전달된 캐릭터 데이터 ID가 비어 있습니다.");
            ClearUI();
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 NPC 상태 UI 데이터를 불러올 수 없습니다.");
            ClearUI();
            return;
        }

        CharacterData characterData = GameDataManager.Instance.GetCharacterData(characterDataId);
        if (characterData == null)
        {
            Debug.LogWarning($"NPCStatusUI에서 캐릭터 데이터를 찾을 수 없습니다: {characterDataId}");
            ClearUI();
            return;
        }

        _currentCharacterData = characterData;
        RefreshProfile(characterData);
        ShowCharacterFileData(1);
    }

    private void RefreshProfile(CharacterData characterData)
    {
        if (Text_NPCName == null)
        {
            Debug.LogWarning("NPCStatusUI의 Text_NPCName 참조가 비어 있어 캐릭터 이름을 표시할 수 없습니다.");
        }
        else
        {
            Text_NPCName.text = characterData.Name ?? string.Empty;
        }

        RefreshPortrait(characterData.PortraitPath);
    }

    private void RefreshPortrait(string portraitPath)
    {
        if (Image_Portrait == null)
        {
            Debug.LogWarning("NPCStatusUI의 Image_Portrait 참조가 비어 있어 프로필 이미지를 표시할 수 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(portraitPath))
        {
            Debug.LogWarning("캐릭터 PortraitPath가 비어 있어 프로필 이미지를 표시하지 않습니다.");
            Image_Portrait.sprite = null;
            Image_Portrait.gameObject.SetActive(false);
            return;
        }

        Sprite portraitSprite = GameUtil.LoadSpriteCanBeNull(portraitPath);
        if (portraitSprite == null)
        {
            Image_Portrait.sprite = null;
            Image_Portrait.gameObject.SetActive(false);
            return;
        }

        Image_Portrait.gameObject.SetActive(true);
        Image_Portrait.sprite = portraitSprite;
        Image_Portrait.preserveAspect = true;
    }

    private void ShowCharacterFileData(int dataIndex)
    {
        if (Text_Description == null)
        {
            Debug.LogWarning("NPCStatusUI의 Text_Description 참조가 비어 있어 캐릭터 파일 내용을 표시할 수 없습니다.");
            return;
        }

        if (_currentCharacterData == null)
        {
            Debug.LogWarning("NPCStatusUI에 현재 캐릭터 데이터가 없어 캐릭터 파일 내용을 표시할 수 없습니다.");
            Text_Description.text = LockedInfoText;
            return;
        }

        string fileText = GetCharacterFileData(dataIndex);
        Text_Description.text = string.IsNullOrEmpty(fileText) ? LockedInfoText : fileText;
    }

    private string GetCharacterFileData(int dataIndex)
    {
        switch (dataIndex)
        {
            case 1:
                return _currentCharacterData.CharacterFileData1;
            case 2:
                return _currentCharacterData.CharacterFileData2;
            case 3:
                return _currentCharacterData.CharacterFileData3;
            case 4:
                return _currentCharacterData.CharacterFileData4;
            default:
                Debug.LogWarning($"지원하지 않는 캐릭터 파일 번호입니다: {dataIndex}");
                return string.Empty;
        }
    }

    private void ClearUI()
    {
        _currentCharacterData = null;

        if (Text_NPCName != null)
        {
            Text_NPCName.text = string.Empty;
        }

        if (Text_Description != null)
        {
            Text_Description.text = LockedInfoText;
        }

        if (Image_Portrait != null)
        {
            Image_Portrait.sprite = null;
            Image_Portrait.gameObject.SetActive(false);
        }
    }

    private void BindButtons()
    {
        BindButton(Button_Data1, OnClickData1, "Button_Data1");
        BindButton(Button_Data2, OnClickData2, "Button_Data2");
        BindButton(Button_Data3, OnClickData3, "Button_Data3");
        BindButton(Button_Data4, OnClickData4, "Button_Data4");
        BindButton(Button_Exit, OnClickExit, "Button_Exit");
    }

    private void UnbindButtons()
    {
        UnbindButton(Button_Data1, OnClickData1);
        UnbindButton(Button_Data2, OnClickData2);
        UnbindButton(Button_Data3, OnClickData3);
        UnbindButton(Button_Data4, OnClickData4);
        UnbindButton(Button_Exit, OnClickExit);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction onClickAction, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"NPCStatusUI의 {buttonName} 참조가 비어 있어 버튼 이벤트를 연결할 수 없습니다.");
            return;
        }

        button.onClick.RemoveListener(onClickAction);
        button.onClick.AddListener(onClickAction);
    }

    private void UnbindButton(Button button, UnityEngine.Events.UnityAction onClickAction)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(onClickAction);
    }

    private void OnClickData1()
    {
        ShowCharacterFileData(1);
    }

    private void OnClickData2()
    {
        ShowCharacterFileData(2);
    }

    private void OnClickData3()
    {
        ShowCharacterFileData(3);
    }

    private void OnClickData4()
    {
        ShowCharacterFileData(4);
    }

    private void OnClickExit()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 없어 NPCStatusUI를 닫을 수 없습니다.");
            return;
        }

        UIManager.Instance.CloseNPCStatusUI();
    }

    private void FindReferencesIfNull()
    {
        if (Image_Portrait == null)
        {
            Image_Portrait = FindChildComponentByName<Image>("Image_Portrait");
        }

        if (Text_NPCName == null)
        {
            Text_NPCName = FindChildComponentByName<Text>("Text_NPCName");
        }

        if (Text_Description == null)
        {
            Text_Description = FindChildComponentByName<Text>("Text_Description");
        }

        if (Button_Data1 == null)
        {
            Button_Data1 = FindChildComponentByName<Button>("Button_Data1");
        }

        if (Button_Data2 == null)
        {
            Button_Data2 = FindChildComponentByName<Button>("Button_Data2");
        }

        if (Button_Data3 == null)
        {
            Button_Data3 = FindChildComponentByName<Button>("Button_Data3");
        }

        if (Button_Data4 == null)
        {
            Button_Data4 = FindChildComponentByName<Button>("Button_Data4");
        }

        if (Button_Exit == null)
        {
            Button_Exit = FindChildComponentByName<Button>("Button_Exit");
        }
    }

    private T FindChildComponentByName<T>(string childName) where T : Component
    {
        Transform childTransform = FindChildByName(transform, childName);
        if (childTransform == null)
        {
            Debug.LogWarning($"NPCStatusUI에서 {childName} 오브젝트를 찾을 수 없습니다.");
            return null;
        }

        T component = childTransform.GetComponent<T>();
        if (component == null)
        {
            Debug.LogWarning($"NPCStatusUI의 {childName} 오브젝트에 {typeof(T).Name} 컴포넌트가 없습니다.");
        }

        return component;
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
