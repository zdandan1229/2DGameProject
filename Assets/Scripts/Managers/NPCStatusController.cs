using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCStatusController : MonoBehaviour
{
    [Serializable]
    private class NPCStatusState
    {
        public string CharacterDataId;
        public int AffinityPoint;
        public bool IsSick;
        public bool IsMadness;
    }

    public static NPCStatusController Instance { get; private set; }

    [SerializeField] private int _likeAffinityThreshold = 10;
    [SerializeField] private int _hateAffinityThreshold = -10;
    [SerializeField] private List<NPCStatusState> _defaultStatusList = new List<NPCStatusState>();

    private Dictionary<string, NPCStatusState> _statusByCharacterDataId = new Dictionary<string, NPCStatusState>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NPCStatusController Instance가 이미 존재합니다. 중복 생성된 NPCStatusController를 비활성화합니다.");
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
        InitializeStatusDictionary();
    }

    public int GetAffinity(string characterDataId)
    {
        NPCStatusState statusState = GetOrCreateStatusState(characterDataId);
        if (statusState == null)
        {
            return 0;
        }

        return statusState.AffinityPoint;
    }

    public bool AddAffinity(string characterDataId, int amount)
    {
        NPCStatusState statusState = GetOrCreateStatusState(characterDataId);
        if (statusState == null)
        {
            return false;
        }

        statusState.AffinityPoint += amount;
        return true;
    }

    public bool SetAffinity(string characterDataId, int affinityPoint)
    {
        NPCStatusState statusState = GetOrCreateStatusState(characterDataId);
        if (statusState == null)
        {
            return false;
        }

        statusState.AffinityPoint = affinityPoint;
        return true;
    }

    public bool IsLike(string characterDataId)
    {
        return GetAffinity(characterDataId) >= _likeAffinityThreshold;
    }

    public bool IsHate(string characterDataId)
    {
        return GetAffinity(characterDataId) <= _hateAffinityThreshold;
    }

    public bool IsSick(string characterDataId)
    {
        NPCStatusState statusState = GetOrCreateStatusState(characterDataId);
        return statusState != null && statusState.IsSick;
    }

    public bool IsMadness(string characterDataId)
    {
        NPCStatusState statusState = GetOrCreateStatusState(characterDataId);
        return statusState != null && statusState.IsMadness;
    }

    public bool SetSick(string characterDataId, bool isSick)
    {
        NPCStatusState statusState = GetOrCreateStatusState(characterDataId);
        if (statusState == null)
        {
            return false;
        }

        statusState.IsSick = isSick;
        return true;
    }

    public bool SetMadness(string characterDataId, bool isMadness)
    {
        NPCStatusState statusState = GetOrCreateStatusState(characterDataId);
        if (statusState == null)
        {
            return false;
        }

        statusState.IsMadness = isMadness;
        return true;
    }

    public void ResetAllStatus()
    {
        InitializeStatusDictionary();
    }

    private void InitializeStatusDictionary()
    {
        _statusByCharacterDataId.Clear();

        for (int i = 0; i < _defaultStatusList.Count; i++)
        {
            NPCStatusState defaultStatus = _defaultStatusList[i];
            if (defaultStatus == null || string.IsNullOrEmpty(defaultStatus.CharacterDataId))
            {
                Debug.LogWarning($"NPCStatusController 기본 상태 목록에 비어 있는 캐릭터 ID가 있습니다. index: {i}");
                continue;
            }

            _statusByCharacterDataId[defaultStatus.CharacterDataId] = CopyStatusState(defaultStatus);
        }
    }

    private NPCStatusState GetOrCreateStatusState(string characterDataId)
    {
        if (IsValidCharacterDataId(characterDataId) == false)
        {
            return null;
        }

        if (_statusByCharacterDataId.TryGetValue(characterDataId, out NPCStatusState statusState))
        {
            return statusState;
        }

        statusState = new NPCStatusState
        {
            CharacterDataId = characterDataId,
            AffinityPoint = 0,
            IsSick = false,
            IsMadness = false
        };
        _statusByCharacterDataId.Add(characterDataId, statusState);
        return statusState;
    }

    private NPCStatusState CopyStatusState(NPCStatusState source)
    {
        return new NPCStatusState
        {
            CharacterDataId = source.CharacterDataId,
            AffinityPoint = source.AffinityPoint,
            IsSick = source.IsSick,
            IsMadness = source.IsMadness
        };
    }

    private bool IsValidCharacterDataId(string characterDataId)
    {
        if (string.IsNullOrEmpty(characterDataId))
        {
            Debug.LogWarning("NPC 상태를 확인할 캐릭터 ID가 비어 있습니다.");
            return false;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 캐릭터 데이터를 확인할 수 없습니다.");
            return false;
        }

        CharacterData characterData = GameDataManager.Instance.GetCharacterData(characterDataId);
        if (characterData == null)
        {
            Debug.LogWarning($"캐릭터 데이터가 존재하지 않아 NPC 상태를 사용할 수 없습니다 : {characterDataId}");
            return false;
        }

        return true;
    }
}
