#if false
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Base_GameDataManager : MonoBehaviour
{
    public static Base_GameDataManager Instance { get; set; }

    private void Awake()
    {
        Instance = this;

        // +++ C# 콘솔때와 다르게 이제 Main()함수가 아닌
        // 모노의 메서드에서 호출될 수 있으므로, 데이터 매니저가 활성화되면 바로 모든 데이터를 한번 받아오자
        // 이처리는 원하는 시점이 있다면 이전해도 된다
        Base_GameUtil.LoadFullData();
    }

    // --- JsonUtility의 한계를 극복하기 위한 Wrapper 클래스 ---
    [Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> items; // JSON 파일의 루트 키 이름이 "items"여야 함
    }
    // ---------------------------------------------------

    public Dictionary<string, Base_CharacterData> CharacterDataList { get; private set; } = new Dictionary<string, Base_CharacterData>();
    public Dictionary<string, Base_SkillData> SkillDataList { get; private set; } = new Dictionary<string, Base_SkillData>();
    public Dictionary<string, Base_WeaponData> WeaponDataList { get; private set; } = new Dictionary<string, Base_WeaponData>();
    public Dictionary<string, Base_CostumeData> CostumeDataList { get; private set; } = new Dictionary<string, Base_CostumeData>();
    public Dictionary<string, Base_ItemData> ItemDataList { get; private set; } = new Dictionary<string, Base_ItemData>();
    public Dictionary<string, Base_DialogueGroupData> DialogueGroupDataList { get; private set; } = new Dictionary<string, Base_DialogueGroupData>();
    public Dictionary<string, Base_DialogueData> DialogueDataList { get; private set; } = new Dictionary<string, Base_DialogueData>();
    public Dictionary<string, Base_FieldObjectData> FieldObjectDataList { get; private set; } = new Dictionary<string, Base_FieldObjectData>();
    public Dictionary<string, Base_MonsterData> MonsterDataList { get; private set; } = new Dictionary<string, Base_MonsterData>();

    private Dictionary<string, T> LoadData<T>(string tableName) where T : Base_GameDataBase
    {
        // 1. 경로 설정 (확장자 .json 제외!)
        // Resources/JsonOutput 폴더
        string resourcePath = $"JsonOutput/{tableName}";

        // 2. 리소스 로드
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

        // 3. 파일 존재 여부 체크
        if (textAsset == null)
        {
            Debug.LogError($"[Error] 리소스를 찾을 수 없습니다: Resources/{resourcePath}");
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
                // ToDictionary를 사용하려면 각 클래스(T)에 Id 필드가 있어야 합니다.
                return wrapper.items.ToDictionary(item => item.Id.ToString());
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
        SkillDataList = LoadData<Base_SkillData>(jsonPath);
    }

    public void LoadCharacterData(string jsonPath)
    {
        CharacterDataList = LoadData<Base_CharacterData>(jsonPath);
    }

    public void LoadWeaponData(string jsonPath)
    {
        WeaponDataList = LoadData<Base_WeaponData>(jsonPath);
    }

    public void LoadCostumeData(string jsonPath)
    {
        CostumeDataList = LoadData<Base_CostumeData>(jsonPath);
    }

    public void LoadItemData(string jsonPath)
    {
        ItemDataList = LoadData<Base_ItemData>(jsonPath);
    }

    public void LoadDialogueData()
    {
        DialogueGroupDataList = LoadData<Base_DialogueGroupData>("DialogueGroup");
        DialogueDataList = LoadData<Base_DialogueData>("Dialogue");
    }

    public void LoadAll()
    {
        FieldObjectDataList = LoadData<Base_FieldObjectData>("FieldObject");
        MonsterDataList = LoadData<Base_MonsterData>("Monster");
    }


    // [아래는 사용을 위한 부분들을 메서드 정의] =========================================================================================
    // Get과 Find이름을 꼭 구별 하자!

    public Base_CharacterData GetCharacterData(string id)
    {
        if (CharacterDataList == null || string.IsNullOrEmpty(id)) return null;

        return CharacterDataList.TryGetValue(id, out var item) ? item : null;
    }

    public Base_SkillData GetSkill(string id)
    {
        if (SkillDataList == null || string.IsNullOrEmpty(id)) return null;

        return SkillDataList.TryGetValue(id, out var item) ? item : null;
    }

    public Base_WeaponData GetWeaponData(string id)
    {
        if (WeaponDataList == null || string.IsNullOrEmpty(id)) return null;

        return WeaponDataList.TryGetValue(id, out var data) ? data : null;
    }

    public Base_CostumeData GetCostumeData(string id)
    {
        if (CostumeDataList == null || string.IsNullOrEmpty(id)) return null;

        return CostumeDataList.TryGetValue(id, out var data) ? data : null;
    }

    public Base_ItemData GetItemData(string id)
    {
        if (ItemDataList == null || string.IsNullOrEmpty(id)) return null;

        return ItemDataList.TryGetValue(id, out var data) ? data : null;
    }

    public Base_DialogueGroupData GetDialogueGroupData(string dataId)
    {
        if (DialogueGroupDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return DialogueGroupDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public Base_DialogueData GetDialogueData(string dataId)
    {
        if (DialogueDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return DialogueDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public Base_MonsterData GetMonsterData(string dataId)
    {
        if (MonsterDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return MonsterDataList.TryGetValue(dataId, out var data) ? data : null;
    }

    public Base_FieldObjectData GetFieldObjectData(string dataId)
    {
        if (FieldObjectDataList == null || string.IsNullOrEmpty(dataId)) return null;

        return FieldObjectDataList.TryGetValue(dataId, out var data) ? data : null;
    }
}
#endif
