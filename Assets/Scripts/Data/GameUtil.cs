using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public static class GameUtil
{
    private const char SpritePathSeparator = '|';

    // 마지막으로 할당된 ID를 전역적으로 기록 (스레드 안전)
    private static long _lastId = 0;

    public static void LoadFullData()
    {
        // 현재 프로젝트의 테스트 맵 단계에서는 대화 기능에 필요한 데이터만 우선 로드한다.
        // 사용하지 않는 시스템 데이터는 이후 실제로 붙일 때 다시 추가하자.
        GameDataManager.Instance.LoadCharacterData("Character");
        GameDataManager.Instance.LoadDialogueData();
        GameDataManager.Instance.LoadInspectData();
        GameDataManager.Instance.LoadJournalData();
        GameDataManager.Instance.LoadInteractionData();
        GameDataManager.Instance.LoadSoundData();
        GameDataManager.Instance.LoadPlayerOptionByStageTypeData();
    }

    public static int CalcCharacterFinalDamage(int curCharacterLevel, int levelPerDamage, bool isCritical)
    {
        int damagePerLevel = (curCharacterLevel + levelPerDamage);
        int finalDamage = isCritical ? (damagePerLevel * 2) : damagePerLevel;
        return finalDamage;
    }

    public static bool TryParseBoolText(string text, out bool value)
    {
        value = false;

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("bool 값으로 해석할 문자열이 비어 있습니다.");
            return false;
        }

        string normalizedText = text.Trim().ToUpperInvariant();
        if (normalizedText == "O" || normalizedText == "TRUE" || normalizedText == "1")
        {
            value = true;
            return true;
        }

        if (normalizedText == "X" || normalizedText == "FALSE" || normalizedText == "0")
        {
            value = false;
            return true;
        }

        Debug.LogWarning($"bool 값으로 해석할 수 없습니다: {text}");
        return false;
    }

    public static bool ParseBoolText(string text)
    {
        if (TryParseBoolText(text, out bool value))
        {
            return value;
        }

        return false;
    }

    public static bool TryParseEnumText<TEnum>(string text, out TEnum value) where TEnum : struct
    {
        value = default;

        if (typeof(TEnum).IsEnum == false)
        {
            Debug.LogError($"{typeof(TEnum).Name}은 enum 타입이 아닙니다.");
            return false;
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"{typeof(TEnum).Name} 값으로 해석할 문자열이 비어 있습니다.");
            return false;
        }

        string normalizedText = text.Trim();
        if (Enum.TryParse(normalizedText, true, out value) == false)
        {
            Debug.LogWarning($"{typeof(TEnum).Name} 값으로 해석할 수 없습니다: {text}");
            return false;
        }

        if (Enum.IsDefined(typeof(TEnum), value) == false)
        {
            Debug.LogWarning($"{typeof(TEnum).Name}에 정의되지 않은 값입니다: {text}");
            return false;
        }

        return true;
    }

    public static bool IsSameEnumText<TEnum>(string text, TEnum enumValue) where TEnum : struct
    {
        if (TryParseEnumText(text, out TEnum parsedValue) == false)
        {
            return false;
        }

        return EqualityComparer<TEnum>.Default.Equals(parsedValue, enumValue);
    }

    public static Sprite LoadSpriteCanBeNull(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            Debug.LogWarning("스프라이트 경로가 비어 있어 에셋을 로드할 수 없습니다.");
            return null;
        }

        if (spriteName.Contains(SpritePathSeparator))
        {
            return LoadInnerSpriteCanBeNull(spriteName);
        }

        // 1. Resources/ 경로에서 이름으로 스프라이트 로드
        // 예: spriteName이 "Sword"라면 Assets/Resources/2D/Sword.png를 찾음
        // 이 2D같은 경로는 나중에 Sprite, Texture 등등 다양하게 바꿔도 무관합니다!
        Sprite loadedSprite = Resources.Load<Sprite>($"{spriteName}");

        if (loadedSprite != null)
        {
            return loadedSprite;
        }

        Debug.LogError($"에셋을 찾을 수 없습니다: {spriteName}");
        return null;
    }

    private static Sprite LoadInnerSpriteCanBeNull(string spriteName)
    {
        int separatorIndex = spriteName.IndexOf(SpritePathSeparator);
        string spriteResourcePath = spriteName.Substring(0, separatorIndex).Trim();
        string innerSpriteName = spriteName.Substring(separatorIndex + 1).Trim();

        if (string.IsNullOrEmpty(spriteResourcePath) || string.IsNullOrEmpty(innerSpriteName))
        {
            Debug.LogWarning($"내부 스프라이트 경로 형식이 올바르지 않습니다. 형식 : Resources경로|스프라이트이름, 입력값 : {spriteName}");
            return null;
        }

        Sprite[] loadedSpriteList = Resources.LoadAll<Sprite>(spriteResourcePath);
        if (loadedSpriteList == null || loadedSpriteList.Length <= 0)
        {
            Debug.LogError($"내부 스프라이트를 찾을 Resources 에셋을 로드할 수 없습니다: {spriteResourcePath}");
            return null;
        }

        for (int i = 0; i < loadedSpriteList.Length; i++)
        {
            Sprite loadedSprite = loadedSpriteList[i];
            if (loadedSprite == null)
            {
                continue;
            }

            if (loadedSprite.name == innerSpriteName)
            {
                return loadedSprite;
            }
        }

        Debug.LogError($"내부 스프라이트를 찾을 수 없습니다. Resources 경로 : {spriteResourcePath}, 내부 이름 : {innerSpriteName}");
        return null;
    }

    public static async UniTask<Sprite> LoadAndSetSpriteImage(Image targetImage, string spritePath)
    {
        Sprite sprite = await ResourceManager.Inst.LoadSprite(spritePath);
        if (sprite != null)
        {
            targetImage.sprite = sprite;
        }
        return sprite;
    }

    public static async UniTaskVoid LoadAndPlayAudioClip(AudioSource audioSource, string audioPath, bool isLoop = false)
    {
        AudioClip clip = await ResourceManager.Inst.LoadAsset<AudioClip>(audioPath);
        if (clip == null)
        {
            Debug.LogError($"{audioPath}를 찾을 수 없습니다! 어드레서블 설정이 되어 있는지 확인해주세요.");
            return;
        }

        if (isLoop == true)
        {
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public static async UniTaskVoid LoadAndSetTexture(RawImage targetRawImage, string texturePath)
    {
        // 비동기로 로드하기 전까지는 해당 오브젝트를 잠깐 비활성화 해준다
        targetRawImage.gameObject.SetActive(false);
        Texture texture = await ResourceManager.Inst.LoadAsset<Texture>(texturePath);
        if (texture != null)
        {
            targetRawImage.texture = texture;
        }
        targetRawImage.gameObject.SetActive(true);
    }

    public static List<string> GetDialogueIdList(string dialogueGroupId)
    {
        var list = new List<string>();

        // "dialogue_group_mainstream_1_1"
        var data = GameDataManager.Instance.GetDialogueGroupData(dialogueGroupId);
        if (data != null)
        {
            var idArr = data.DialogueIdList;
            foreach (var id in idArr)
            {
                list.Add(id);
            }
        }

        return list;
    }

    // 그냥 유니크 키가 발급되어야 할 때 사용하려고 만든 것 (의미가 있는 건 아니므로 사용만 하세요)
    public static long GenerateUniqueId()
    {
        long newId = DateTime.UtcNow.Ticks;

        // 원자적 연산으로 안전하게 ID 갱신
        while (true)
        {
            long lastId = Volatile.Read(ref _lastId);

            // 만약 현재 시간이 이전 ID보다 작거나 같다면 (루프가 너무 빠른 경우 포함)
            // 이전 ID + 1로 강제 설정하여 중복 방지
            long idToAssign = (newId <= lastId) ? lastId + 1 : newId;

            // _lastId가 내가 읽은 시점과 같다면 idToAssign으로 교체 (성공 시 루프 탈출)
            if (Interlocked.CompareExchange(ref _lastId, idToAssign, lastId) == lastId)
            {
                return idToAssign;
            }
            // 그 사이 다른 스레드가 값을 바꿨다면 다시 시도
        }
    }
}
