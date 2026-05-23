#if false
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Base_DialogueUI : Base_UIBase
{
    [SerializeField] private GameObject Layout_CharacterName;
    [SerializeField] private Text Text_Character;
    [SerializeField] private Text Text_Description;
    [SerializeField] private Base_UIButton Button_Next;

    private string _currentDialogueId;
    private Queue<string> _descriptionQueue = new Queue<string>();

    private void OnEnable()
    {
        Button_Next.BindOnClickButtonEvent(OnClick_Next);
    }

    // 다이얼로그에서 Next 버튼이 눌러질때 호출된다
    public void OnClick_Next()
    {
        // 다음 대사가 있는지 체크한다
        bool isNextDescriptionExist = CheckAndSetDescription();

        if (isNextDescriptionExist)
        {
            return;
        }

        // 대사가 없다면, 다음으로 이어지는 다이얼로그가 있는지 체크한다
        bool isNextDialogueExist = CheckAndStartNextDialogue();
        if(isNextDialogueExist == false)
        {
            Base_UIManager.Instance.CloseContentUI(Base_UIType.DialogueUI);
        }
    }

    private bool CheckAndStartNextDialogue()
    {
        var dialogueData = Base_GameDataManager.Instance.GetDialogueData(_currentDialogueId);
        if (dialogueData == null)
        {
            Debug.LogWarning($"다이얼로그 데이터가 존재하지 않습니다 {dialogueData}");
            return false;
        }

        // 현재 데이터를 기준으로 다음 다이얼로그가 있는지 체크해보고, 있다면 다음 다이얼로그를 시작한다!
        string nextDialogueId = dialogueData.NextDialogueId;
        if (string.IsNullOrEmpty(nextDialogueId) == false)
        {
            StartDialogue(nextDialogueId);
            return true;
        }

        return false;
    }

    // 다이얼로그를 시작하는 메서드 (외부에서 UIManager를 통해 다이얼로그 시작을 요청할때도 쓴다!)
    public void StartDialogue(string dialogeId)
    {
        var dialogueData = Base_GameDataManager.Instance.GetDialogueData(dialogeId);
        if (dialogueData == null)
        {
            Debug.LogWarning($"다이얼로그 데이터가 존재하지 않습니다 {dialogueData}");
            return;
        }

        // 현재 진행중인 다이얼로그 Id는 다음 다이얼로그가 있는지 체크할 때 쓸 수 있도록 보관한다
        _currentDialogueId = dialogeId;

        // 혹시 현재 대사가 너무 길거나 다음 페이지 처리가 필요할 때 <np> 키워드로 잘라주자!
        if (dialogueData.Description.Contains("<np>"))
        {
            string[] dialogueDescriptionList = dialogueData.Description.Split("<np>");
            foreach(string desc in dialogueDescriptionList)
            {
                _descriptionQueue.Enqueue(desc);
            }
            CheckAndSetDescription();
        }
        else
        {
            // Np 태그가 없다면 바로 다이얼로그 UI를 세팅하자
            SetCurrentDialogueDescription(dialogueData.Description);
        }

        SetCharacterName(dialogueData.CharacterDataId);
    }

    private bool CheckAndSetDescription()
    {
        bool isNextDescriptionExsist = (_descriptionQueue.Count > 0);
        if (isNextDescriptionExsist)
        {
            string desc = _descriptionQueue.Dequeue();
            SetCurrentDialogueDescription(desc);
        }

        return isNextDescriptionExsist;
    }

    private void SetCharacterName(string characterDataId)
    {
        // 캐릭터 정보가 있다면 말하는 이의 추가 정보를 표기해줄 수 있도록 연동하는 부분
        bool isActive = (string.IsNullOrEmpty(characterDataId) == false);
        Layout_CharacterName.SetActive(isActive);

        if (isActive)
        {
            var characterData = Base_GameDataManager.Instance.GetCharacterData(characterDataId);
            if(characterData != null)
            {
                Text_Character.text = characterData.Name;
            }
        }
    }

    private void SetCurrentDialogueDescription(string description)
    {
        Text_Description.text = description;
    }
}
#endif
