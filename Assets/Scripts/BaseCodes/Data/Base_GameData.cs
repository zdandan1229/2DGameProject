#if false
using System;
using System.Collections.Generic;

[System.Serializable]
public class Base_GameDataBase
{
    public string Id;
}

// C# 때와 약간 달라진 점
    // Syste.Text.Json대신 유니티 내장 JsonUtility를 사용
    // 따라서 프로퍼티말고 그냥 일반 public 멤버변수로 변경함
    // [System.Serializable]가 없다면 JsonUtility는 데이터를 무시

[System.Serializable]
public class Base_CharacterData : Base_GameDataBase
{
    public string Name;
    public string SkillList;
    public string UseWeaponId;
    public string BasicCostumeId;
}

[System.Serializable]
public class Base_SkillData : Base_GameDataBase
{
    public string Name;
    public string Description;
}

[System.Serializable]
public class Base_WeaponData : Base_GameDataBase
{
    public string Name;
    public string Description;
}

[System.Serializable] 
public class Base_CostumeData : Base_GameDataBase
{
    public string Name;
    public string Description;
}

[System.Serializable]
public class Base_ItemData : Base_GameDataBase
{
    public string Name;
    public string Description;
    public string ItemType;
    public string Grade;
    public string MaxStackCount;
    public string SellingPrice;
    public string IconPath;
}

[System.Serializable]
public class Base_DialogueGroupData : Base_GameDataBase
{
    public List<string> DialogueIdList;
}

[System.Serializable]
public class Base_DialogueData : Base_GameDataBase
{
    public string CharacterDataId;
    public string Description;
    public string NextDialogueId;
    public List<string> SelectionNameList;
    public List<string> SelectionDialogueIdList;
    public string TexturePath;
    public string VoicePath;
}

[System.Serializable]
public class Base_FieldObjectData : Base_GameDataBase
{
    public string Name;
    public string Description;
    public string FieldObjectType;
    public List<int> DropCountRange;
    public string DropItemDataId;
    public string IconPath;
    public string PrefabPath;
}

[System.Serializable]
public class Base_MonsterData : Base_GameDataBase
{
    public string Name;
    public string Description;
    public string IconPath;
    public string PrefabPath;
}
#endif
