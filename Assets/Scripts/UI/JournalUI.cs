using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    [SerializeField] private RectTransform Content_JournalList;
    [SerializeField] private RectTransform Layout_JournalDescriptionViewer;
    [SerializeField] private Text Text_JournalDescription;
    [SerializeField] private Button Button_JournalTitle;

    private List<GameObject> _createdButtonObjectList = new List<GameObject>();

    private void Awake()
    {
        BindMissingReferences();
        SetupJournalListLayout();
        ClearSelectedJournalContent();
    }

    private void OnDisable()
    {
        ClearSelectedJournalContent();
    }

    public void RefreshJournalList()
    {
        ClearCreatedButtons();
        ClearSelectedJournalContent();

        if (JournalManager.Instance == null)
        {
            Debug.LogWarning("JournalManager.Instance가 없어 일지 목록을 갱신할 수 없습니다. ManagerContainer에 JournalManager가 배치되어 있는지 확인하세요.");
            return;
        }

        List<string> journalDataIdList = JournalManager.Instance.GetJournalDataIdList();
        for (int i = 0; i < journalDataIdList.Count; i++)
        {
            CreateJournalButton(journalDataIdList[i]);
        }
    }

    public void SelectJournal(string journalDataId)
    {
        SelectJournal(journalDataId, string.Empty);
    }

    public void SelectJournal(string journalDataId, string noticeMessage)
    {
        if (string.IsNullOrEmpty(journalDataId))
        {
            ClearSelectedJournalContent();
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 일지 내용을 조회할 수 없습니다.");
            return;
        }

        JournalData journalData = GameDataManager.Instance.GetJournalData(journalDataId);
        if (journalData == null)
        {
            Debug.LogWarning($"일지 데이터가 존재하지 않습니다 : {journalDataId}");
            return;
        }

        if (string.IsNullOrEmpty(noticeMessage))
        {
            RefreshSelectedJournalContent(true);
            SetDescription(journalData.Description);
            return;
        }

        RefreshSelectedJournalContent(true);
        SetDescription($"{noticeMessage}\n\n{journalData.Description}");
    }

    private void CreateJournalButton(string journalDataId)
    {
        if (Content_JournalList == null)
        {
            Debug.LogWarning("JournalUI의 Content_JournalList 참조가 비어 있습니다.");
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 없어 일지 버튼을 만들 수 없습니다.");
            return;
        }

        JournalData journalData = GameDataManager.Instance.GetJournalData(journalDataId);
        if (journalData == null)
        {
            Debug.LogWarning($"일지 데이터가 존재하지 않아 버튼을 만들 수 없습니다 : {journalDataId}");
            return;
        }

        JournalTitleButtonUI journalButton = CreateButtonInstance();
        if (journalButton == null)
        {
            return;
        }

        journalButton.gameObject.SetActive(true);
        journalButton.name = $"Button_JournalTitle_{journalDataId}";
        SetupJournalButtonLayout(journalButton);
        journalButton.Initialize(this, journalDataId, journalData.Title);
        _createdButtonObjectList.Add(journalButton.gameObject);
    }

    private JournalTitleButtonUI CreateButtonInstance()
    {
        if (Button_JournalTitle == null)
        {
            Button_JournalTitle = Resources.Load<Button>("Prefabs/UI/ContentUI/Button_JournalTitle");
        }

        if (Button_JournalTitle == null)
        {
            Debug.LogWarning("Resources/Prefabs/UI/ContentUI/Button_JournalTitle 프리팹을 찾을 수 없어 일지 버튼을 생성할 수 없습니다.");
            return null;
        }

        Button createdButton = Instantiate(Button_JournalTitle, Content_JournalList);
        if (createdButton == null)
        {
            Debug.LogWarning("Button_JournalTitle 인스턴스 생성에 실패했습니다.");
            return null;
        }

        JournalTitleButtonUI journalTitleButtonUI = createdButton.GetComponent<JournalTitleButtonUI>();
        if (journalTitleButtonUI == null)
        {
            journalTitleButtonUI = createdButton.gameObject.AddComponent<JournalTitleButtonUI>();
        }

        return journalTitleButtonUI;
    }

    private void ClearCreatedButtons()
    {
        for (int i = 0; i < _createdButtonObjectList.Count; i++)
        {
            if (_createdButtonObjectList[i] == null)
            {
                continue;
            }

            Destroy(_createdButtonObjectList[i]);
        }

        _createdButtonObjectList.Clear();
    }

    private void SetDescription(string description)
    {
        if (Text_JournalDescription == null)
        {
            Debug.LogWarning("JournalUI의 Text_JournalDescription 참조가 비어 있습니다.");
            return;
        }

        Text_JournalDescription.text = description ?? string.Empty;
        RefreshDescriptionTextLayout();
    }

    private void RefreshDescriptionTextLayout()
    {
        if (Text_JournalDescription == null)
        {
            return;
        }

        RectTransform descriptionRectTransform = Text_JournalDescription.transform as RectTransform;
        if (descriptionRectTransform == null)
        {
            Debug.LogWarning("JournalUI의 Text_JournalDescription에 RectTransform이 없어 스크롤 높이를 갱신할 수 없습니다.");
            return;
        }

        Text_JournalDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
        Text_JournalDescription.verticalOverflow = VerticalWrapMode.Overflow;
        Text_JournalDescription.alignment = TextAnchor.UpperLeft;

        descriptionRectTransform.anchorMin = new Vector2(0f, 1f);
        descriptionRectTransform.anchorMax = new Vector2(1f, 1f);
        descriptionRectTransform.pivot = new Vector2(0f, 1f);
        descriptionRectTransform.anchoredPosition = Vector2.zero;
        descriptionRectTransform.sizeDelta = new Vector2(0f, Text_JournalDescription.preferredHeight);

        ScrollRect descriptionScrollRect = GetDescriptionScrollRect();
        if (descriptionScrollRect != null)
        {
            descriptionScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ClearSelectedJournalContent()
    {
        SetDescription(string.Empty);
        RefreshSelectedJournalContent(false);
    }

    private void RefreshSelectedJournalContent(bool isVisible)
    {
        if (Layout_JournalDescriptionViewer == null)
        {
            Debug.LogWarning("JournalUI의 Layout_JournalDescriptionViewer 참조가 비어 있습니다.");
            return;
        }

        Layout_JournalDescriptionViewer.gameObject.SetActive(isVisible);
    }

    private ScrollRect GetDescriptionScrollRect()
    {
        if (Layout_JournalDescriptionViewer == null)
        {
            return null;
        }

        return Layout_JournalDescriptionViewer.GetComponent<ScrollRect>();
    }

    private void SetupJournalListLayout()
    {
        if (Content_JournalList == null)
        {
            Debug.LogWarning("JournalUI의 Content_JournalList 참조가 비어 있어 목록 레이아웃을 설정할 수 없습니다.");
            return;
        }

        Content_JournalList.anchorMin = new Vector2(0f, 1f);
        Content_JournalList.anchorMax = new Vector2(1f, 1f);
        Content_JournalList.pivot = new Vector2(0.5f, 1f);
        Content_JournalList.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup verticalLayoutGroup = Content_JournalList.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup == null)
        {
            verticalLayoutGroup = Content_JournalList.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        verticalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        verticalLayoutGroup.spacing = 0f;
        verticalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        verticalLayoutGroup.childControlWidth = true;
        verticalLayoutGroup.childControlHeight = true;
        verticalLayoutGroup.childForceExpandWidth = true;
        verticalLayoutGroup.childForceExpandHeight = false;

        ContentSizeFitter contentSizeFitter = Content_JournalList.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = Content_JournalList.gameObject.AddComponent<ContentSizeFitter>();
        }

        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void SetupJournalButtonLayout(JournalTitleButtonUI journalButton)
    {
        if (journalButton == null)
        {
            return;
        }

        RectTransform buttonRectTransform = journalButton.transform as RectTransform;
        if (buttonRectTransform != null)
        {
            buttonRectTransform.anchorMin = new Vector2(0f, 1f);
            buttonRectTransform.anchorMax = new Vector2(1f, 1f);
            buttonRectTransform.pivot = new Vector2(0.5f, 1f);
            buttonRectTransform.anchoredPosition = Vector2.zero;
        }

        LayoutElement layoutElement = journalButton.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = journalButton.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = 58f;
        layoutElement.preferredHeight = 58f;
        layoutElement.flexibleHeight = 0f;
    }

    private void BindMissingReferences()
    {
        if (Content_JournalList == null)
        {
            Transform contentTransform = FindChildByName(transform, "Content_JournalList");
            if (contentTransform == null)
            {
                contentTransform = FindChildByName(transform, "Content");
            }

            if (contentTransform != null)
            {
                Content_JournalList = contentTransform as RectTransform;
            }
        }

        if (Text_JournalDescription == null)
        {
            Transform descriptionTransform = FindChildByName(transform, "Text_JournalDescription");
            if (descriptionTransform == null)
            {
                descriptionTransform = FindChildByName(transform, "Text_Description");
            }

            if (descriptionTransform != null)
            {
                Text_JournalDescription = descriptionTransform.GetComponent<Text>();
            }
        }

        if (Layout_JournalDescriptionViewer == null)
        {
            Transform descriptionViewerTransform = FindChildByName(transform, "Scroll View_DescriptionViewer");
            if (descriptionViewerTransform == null)
            {
                descriptionViewerTransform = FindChildByName(transform, "ScrollView_DescriptionViewer");
            }

            if (descriptionViewerTransform != null)
            {
                Layout_JournalDescriptionViewer = descriptionViewerTransform as RectTransform;
            }
        }

        if (Button_JournalTitle == null)
        {
            Button_JournalTitle = Resources.Load<Button>("Prefabs/UI/ContentUI/Button_JournalTitle");
            if (Button_JournalTitle == null)
            {
                Debug.LogWarning("Resources/Prefabs/UI/ContentUI/Button_JournalTitle 프리팹을 찾지 못했습니다.");
            }
        }
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
