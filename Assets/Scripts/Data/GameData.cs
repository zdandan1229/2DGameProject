using System;
using System.Collections.Generic;

[System.Serializable]
public class GameDataBase
{
    public string Id;
}

// C# 때와 약간 달라진 점
    // Syste.Text.Json대신 유니티 내장 JsonUtility를 사용
    // 따라서 프로퍼티말고 그냥 일반 public 멤버변수로 변경함
    // [System.Serializable]가 없다면 JsonUtility는 데이터를 무시

[System.Serializable]
public class CharacterData : GameDataBase
{
    public string Name;
    public string PortraitPath;
    public string PrefabPath;
    public string Description;
    public string CharacterFileData1;
    public string CharacterFileData2;
    public string CharacterFileData3;
    public string CharacterFileData4;
    public string DialoguePortraitDefault;
    public string DialoguePortraitHateDefault;
    public string DialoguePortraitLikeDefault;
    public string DialoguePortraitSickDefault;
    public string DialoguePortraitMadnessDefault;
}

[System.Serializable]
public class SkillData : GameDataBase
{
    public string Name;
    public string Description;
}

[System.Serializable]
public class WeaponData : GameDataBase
{
    public string Name;
    public string Description;
}

[System.Serializable] 
public class CostumeData : GameDataBase
{
    public string Name;
    public string Description;
}

[System.Serializable]
public class ItemData : GameDataBase
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
public class DialogueGroupData : GameDataBase
{
    public List<string> DialogueIdList;
}

[System.Serializable]
public class DialogueData : GameDataBase
{
    public string CharacterDataId;
    public string Description;
    public string NextDialogueId;
    public string CompleteJournalDataId;
    public string BGM;
    public string OpenInspectObjectDataId;
    public string DialogueFlagId;
    public List<string> SelectionNameList;
    public List<string> SelectionDialogueIdList;
    public string CharacterPortraitPath;
    public int AddLikeAbility;
}

[System.Serializable]
public class InspectObjectData : GameDataBase
{
    public string Name;
    public string Description;
    public string PrefabPath;
    public string IconPath;
    public string StartInspectTextId;
    public string CompleteRewardType;
    public string CompleteJournalDataId;
    public string CompleteInspectDescription;
}

[System.Serializable]
public class InspectAreaData : GameDataBase
{
    public string Name;
    public string Description;
    public string PrefabPath;
}

[System.Serializable]
public class InspectTextData : GameDataBase
{
    public string Description;
}

[System.Serializable]
public class JournalData : GameDataBase
{
    public string Title;
    public string Description;
    public string InspectText;
    public string InspectTextId;
    public string InspectDescription;
}

[System.Serializable]
public class InteractionObjectData : GameDataBase
{
    public string Name;
    public List<string> InteractionOptionIdList;
    public string CharacterDataId;
    public string DialogueDataId;
    public string RepeatDialogueDataId;
    public string DialogueCompleteFlagId;
    public string InspectObjectDataId;
    public string InspectAreaDataId;
    public string JournalDataId;
    public string PopupTextDataId;
}

[System.Serializable]
public class InteractionOptionData : GameDataBase
{
    public string ButtonText;
    public string ActionType;
}

[System.Serializable]
public class PopupTextData : GameDataBase
{
    public string Description;
}

[System.Serializable]
public class SoundData : GameDataBase
{
    public string AudioPath;
    public float Volume;
    public float Pitch;
    public bool IsLoop;
}

[System.Serializable]
public class PlayerOptionByStageTypeData : GameDataBase
{
    public string StageType;
    public float PlayerScale;
    public float MoveSpeed;
}

[System.Serializable]
public class FieldObjectData : GameDataBase
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
public class MonsterData : GameDataBase
{
    public string Name;
    public string Description;
    public string IconPath;
    public string PrefabPath;
}
