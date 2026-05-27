using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; set; }

    private void Awake()
    {
        Instance = this;

        // +++ C# 콘솔때와 다르게 이제 Main()함수가 아닌
        // 모노의 메서드에서 호출될 수 있으므로, 데이터 매니저가 활성화되면 바로 모든 데이터를 한번 받아오자
        // 이처리는 원하는 시점이 있다면 이전해도 된다
        GameUtil.LoadFullData();
    }

    // --- JsonUtility의 한계를 극복하기 위한 Wrapper 클래스 ---
    [Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> items; // JSON 파일의 루트 키 이름이 "items"여야 함
    }
    // ---------------------------------------------------

    public Dictionary<string, CharacterData> CharacterDataList { get; private set; } = new Dictionary<string, CharacterData>();
    public Dictionary<string, SkillData> SkillDataList { get; private set; } = new Dictionary<string, SkillData>();
    public Dictionary<string, WeaponData> WeaponDataList { get; private set; } = new Dictionary<string, WeaponData>();
    public Dictionary<string, CostumeData> CostumeDataList { get; private set; } = new Dictionary<string, CostumeData>();
    public Dictionary<string, ItemData> ItemDataList { get; private set; } = new Dictionary<string, ItemData>();
    public Dictionary<string, DialogueGroupData> DialogueGroupDataList { get; private set; } = new Dictionary<string, DialogueGroupData>();
    public Dictionary<string, DialogueData> DialogueDataList { get; private set; } = new Dictionary<string, DialogueData>();
    public Dictionary<string, InspectObjectData> InspectObjectDataList { get; private set; } = new Dictionary<string, InspectObjectData>();
    public Dictionary<string, InspectTextData> InspectTextDataList { get; private set; } = new Dictionary<string, InspectTextData>();
    public Dictionary<string, InteractionObjectData> InteractionObjectDataList { get; private set; } = new Dictionary<string, InteractionObjectData>();
    public Dictionary<string, InteractionOptionData> InteractionOptionDataList { get; private set; } = new Dictionary<string, InteractionOptionData>();
    public Dictionary<string, FieldObjectData> FieldObjectDataList { get; private set; } = new Dictionary<string, FieldObjectData>();
    public Dictionary<string, MonsterData> MonsterDataList { get; private set; } = new Dictionary<string, MonsterData>();

    private bool HasJsonTable(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            Debug.LogWarning("확인할 테이블 이름이 비어 있습니다.");
            return false;
        }

        string resourcePath = $"JsonOutput/{tableName}";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        return textAsset != null;
    }

    private Dictionary<string, T> LoadData<T>(string tableName) where T : GameDataBase
    {
        // 1. 경로 설정 (확장자 .json 제외!)
        // Resources/JsonOutput 폴더
        string resourcePath = $"JsonOutput/{tableName}";

        // 2. 리소스 로드
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

        // 3. 파일 존재 여부 체크
        if (textAsset == null)
        {
            Debug.LogWarning($"로드할 데이터를 찾지 못해 건너뜁니다: Resources/{resourcePath}");
            return new Dictionary<string, T>();
        }

        try
        {
            string jsonString = textAsset.text;

            // 4. JsonUtility용 Wrapper 트릭 적용
            string wrappedJson = "{\"items\":" + jsonString + "}";
            SerializationWrapper<T> wrapper = JsonUtility.FromJson<SerializationWrapper<T>>(wrappedJson);

            if (wrapper != null && wrapper.items != null)
            {
                Debug.Log($"{typeof(T).Name} 데이터를 {wrapper.items.Count}개 로드했습니다.");
                Dictionary<string, T> dataDic = new Dictionary<string, T>();
                for (int i = 0; i < wrapper.items.Count; i++)
                {
                    T item = wrapper.items[i];
                    if (item == null)
                    {
                        Debug.LogWarning($"{typeof(T).Name} 데이터 중 비어 있는 항목이 있어 건너뜁니다. index : {i}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(item.Id))
                    {
                        Debug.LogWarning($"{typeof(T).Name} 데이터의 Id가 비어 있어 건너뜁니다. index : {i}");
                        continue;
                    }

                    if (dataDic.ContainsKey(item.Id))
                    {
                        Debug.LogWarning($"{typeof(T).Name} 데이터 Id가 중복되어 덮어씁니다 : {item.Id}");
                    }

                    dataDic[item.Id] = item;
                }

                return dataDic;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{typeof(T).Name} JSON 로드 오류] {ex.Message}");
        }

        return new Dictionary<string, T>();
    }

    public void LoadSkillData(string jsonPath)
    {
        SkillDataList = LoadData<SkillData>(jsonPath);
    }

    public void LoadCharacterData(string jsonPath)
    {
        CharacterDataList = LoadData<CharacterData>(jsonPath);
    }

    public void LoadWeaponData(string jsonPath)
    {
        WeaponDataList = LoadData<WeaponData>(jsonPath);
    }

    public void LoadCostumeData(string jsonPath)
    {
        CostumeDataList = LoadData<CostumeData>(jsonPath);
    }

    public void LoadItemData(string jsonPath)
    {
        ItemDataList = LoadData<ItemData>(jsonPath);
    }

    public void LoadDialogueData()
    {
        if (HasJsonTable("DialogueGroup"))
        {
            DialogueGroupDataList = LoadData<DialogueGroupData>("DialogueGroup");
        }
        else
        {
            DialogueGroupDataList = new Dictionary<string, DialogueGroupData>();
        }

        DialogueDataList = LoadData<DialogueData>("Dialogue");
    }

    public void LoadInspectData()
    {
        if (HasJsonTable("InspectObject"))
        {
            InspectObjectDataList = LoadData<InspectObjectData>("InspectObject");
        }
        else
        {
            InspectObjectDataList = new Dictionary<string, InspectObjectData>();
        }

        if (HasJsonTable("InspectText"))
        {
            InspectTextDataList = LoadData<InspectTextData>("InspectText");
        }
        else
        {
            InspectTextDataList = new Dictionary<string, InspectTextData>();
        }
    }

    public void LoadInteractionData()
    {
        if (HasJsonTable("InteractionObject"))
        {
            InteractionObjectDataList = LoadData<InteractionObjectData>("InteractionObject");
        }
        else
        {
            InteractionObjectDataList = new Dictionary<string, InteractionObjectData>();
        }

        if (HasJsonTable("InteractionOption"))
        {
            InteractionOptionDataList = LoadData<InteractionOptionData>("InteractionOption");
        }
        else
        {
            InteractionOptionDataList = new Dictionary<string, InteractionOptionData>();
        }
    }

    public void LoadAll()
    {
        FieldObjectDataList = LoadData<FieldObjectData>("FieldObject");
        MonsterDataList = LoadData<MonsterData>("Monster");
    }


    // [아래는 사용을 위한 부분들을 메서드 정의] =========================================================================================
    // Get과 Find이름을 꼭 구별 하자!

    public CharacterData GetCharacterData(string id)
    {
        if (CharacterDataList == null || string.IsNullOrEmpty(id)) return null;

        return CharacterDataList.TryGetValue(id, out var item) ? item : null;
    }

    public SkillData GetSkill(string id)
    {
        if (SkillDataList == null || string.IsNullOrEmpty(id)) return null;

        return SkillDataList.TryGetValue(id, out var item) ? item : null;
    }

    public WeaponData GetWeaponData(string id)
    {
        if (WeaponDataList == null || string.IsNullOrEmpty(id)) return null;

        return WeaponDataList.TryGetValue(id, out var data) ? data : null;
    }

    public CostumeData GetCostumeData(string id)
    {
        if (CostumeDataList == null || string.IsNullOrEmpty(id)) return null;

        return CostumeDataList.TryGetValue(id, out var data) ? data : null;
    }

    public ItemData GetItemData(string id)
    {
        if (ItemDataList == null || string.IsNullOrEmpty(id)) return null;

        return ItemDataList.TryGetValue(id, out var data) ? data : null;
    }

    public DialogueGroupData GetDialogueGroupData(string dataId)
    {
        if (DialogueGroupDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return DialogueGroupDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public DialogueData GetDialogueData(string dataId)
    {
        if (DialogueDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return DialogueDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public InspectObjectData GetInspectObjectData(string dataId)
    {
        if (InspectObjectDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return InspectObjectDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public InspectTextData GetInspectTextData(string dataId)
    {
        if (InspectTextDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return InspectTextDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public InteractionObjectData GetInteractionObjectData(string dataId)
    {
        if (InteractionObjectDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return InteractionObjectDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public InteractionOptionData GetInteractionOptionData(string dataId)
    {
        if (InteractionOptionDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return InteractionOptionDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public MonsterData GetMonsterData(string dataId)
    {
        if (MonsterDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return MonsterDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public FieldObjectData GetFieldObjectData(string dataId)
    {
        if (FieldObjectDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return FieldObjectDataList.TryGetValue(dataId, out var data) ? data : null;
    }
}
