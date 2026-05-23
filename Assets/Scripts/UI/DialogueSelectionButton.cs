using UnityEngine;
using UnityEngine.UI;

public class DialogueSelectionButton : MonoBehaviour
{
    private DialogueUI _dialogueUI;
    private Button _button;
    private string _nextDialogueId;

    public void Initialize(DialogueUI dialogueUI, string nextDialogueId)
    {
        _dialogueUI = dialogueUI;
        _nextDialogueId = nextDialogueId;

        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogWarning($"{gameObject.name}에 Button 컴포넌트가 없어 선택지 클릭을 연결할 수 없습니다.");
            return;
        }

        _button.onClick.RemoveListener(OnClickSelectionButton);
        _button.onClick.AddListener(OnClickSelectionButton);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClickSelectionButton);
        }
    }

    private void OnClickSelectionButton()
    {
        if (_dialogueUI == null)
        {
            Debug.LogWarning($"{gameObject.name}에 연결된 DialogueUI가 비어 있습니다.");
            return;
        }

        _dialogueUI.OnClickSelection(_nextDialogueId);
    }
}
