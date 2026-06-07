using UnityEngine;
using UnityEngine.UI;

public class JournalTitleButtonUI : MonoBehaviour
{
    [SerializeField] private Text Text_JournalTitle;
    [SerializeField] private Button Button_JournalTitle;

    private JournalUI _ownerJournalUI;
    private string _journalDataId;

    private void Awake()
    {
        BindMissingReferences();
    }

    private void OnDisable()
    {
        if (Button_JournalTitle != null)
        {
            Button_JournalTitle.onClick.RemoveListener(OnClickJournalTitle);
        }
    }

    public void Initialize(JournalUI ownerJournalUI, string journalDataId, string title)
    {
        _ownerJournalUI = ownerJournalUI;
        _journalDataId = journalDataId;

        BindMissingReferences();
        SetTitle(title);
        BindButton();
    }

    private void OnClickJournalTitle()
    {
        if (_ownerJournalUI == null)
        {
            Debug.LogWarning("JournalTitleButtonUI에 JournalUI 참조가 없어 일지를 선택할 수 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(_journalDataId))
        {
            Debug.LogWarning("JournalTitleButtonUI에 JournalData Id가 없어 일지를 선택할 수 없습니다.");
            return;
        }

        _ownerJournalUI.SelectJournal(_journalDataId);
    }

    private void SetTitle(string title)
    {
        if (Text_JournalTitle == null)
        {
            Debug.LogWarning("JournalTitleButtonUI의 Text_JournalTitle 참조가 비어 있어 제목을 출력할 수 없습니다.");
            return;
        }

        Text_JournalTitle.raycastTarget = false;
        Text_JournalTitle.text = title ?? string.Empty;
    }

    private void BindButton()
    {
        if (Button_JournalTitle == null)
        {
            Debug.LogWarning("JournalTitleButtonUI의 Button_JournalTitle 참조가 비어 있어 클릭 이벤트를 연결할 수 없습니다.");
            return;
        }

        if (Button_JournalTitle.targetGraphic != null)
        {
            Button_JournalTitle.targetGraphic.raycastTarget = true;
        }

        Button_JournalTitle.onClick.RemoveListener(OnClickJournalTitle);
        Button_JournalTitle.onClick.AddListener(OnClickJournalTitle);
    }

    private void BindMissingReferences()
    {
        if (Button_JournalTitle == null)
        {
            Button_JournalTitle = GetComponent<Button>();
        }

        if (Text_JournalTitle == null)
        {
            Text_JournalTitle = GetComponentInChildren<Text>(true);
        }
    }
}
