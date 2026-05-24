using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InspectObjectUI : UIBase
{
    [Header("Inspect Object")]
    [SerializeField] private RectTransform Layout_ObjectRoot;
    [SerializeField] private GameObject Prefab_Object;

    [Header("Description")]
    [SerializeField] private Text Text_Description;
    [SerializeField] private TextTypingEffect TypingEffect_Description;

    [Header("Next")]
    [SerializeField] private Button Button_Next;

    [Header("Exit")]
    [SerializeField] private Button Button_Exit;
    [SerializeField] private string _completeInspectDescription = "살펴볼 수 있는 부분을 모두 확인했어.";

    private GameObject _createdObject;
    private Queue<string> _descriptionQueue = new Queue<string>();
    private int _inspectPointCount;
    private bool _isCompleteInspectDescriptionShown;
    private bool _isInspectTextShowing;
    private HashSet<InspectPoint> _inspectedPointSet = new HashSet<InspectPoint>();

    private void OnEnable()
    {
        if (Button_Next != null)
        {
            Button_Next.onClick.AddListener(RequestNextDescription);
        }
        else
        {
            Debug.LogWarning("InspectObjectUI의 Button_Next 참조가 누락되어 있습니다.");
        }

        if (Button_Exit != null)
        {
            Button_Exit.onClick.AddListener(CloseInspectObjectUI);
        }
        else
        {
            Debug.LogWarning("InspectObjectUI의 Button_Exit 참조가 누락되어 있습니다.");
        }

        RefreshNextButton(false);
        RefreshExitButton(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 존재하지 않아 조사 UI에서 게임 정지를 요청하지 못했습니다.");
        }

        ShowDefaultObject();
    }

    private void OnDisable()
    {
        ClearDescriptionTyping();

        if (Button_Next != null)
        {
            Button_Next.onClick.RemoveListener(RequestNextDescription);
        }

        if (Button_Exit != null)
        {
            Button_Exit.onClick.RemoveListener(CloseInspectObjectUI);
        }

        ClearObject();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 존재하지 않아 조사 UI에서 게임 재개를 요청하지 못했습니다.");
        }
    }

    public void StartInspectObject(GameObject objectPrefab, string description)
    {
        ShowObject(objectPrefab);
        StartDescription(description);
    }

    public void StartInspectObject(string inspectObjectDataId)
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 조사 오브젝트 데이터를 조회할 수 없습니다.");
            CloseInspectObjectUI();
            return;
        }

        InspectObjectData inspectObjectData = GameDataManager.Instance.GetInspectObjectData(inspectObjectDataId);
        if (inspectObjectData == null)
        {
            Debug.LogWarning($"조사 오브젝트 데이터가 존재하지 않습니다 : {inspectObjectDataId}");
            CloseInspectObjectUI();
            return;
        }

        if (string.IsNullOrEmpty(inspectObjectData.PrefabPath))
        {
            Debug.LogWarning($"조사 오브젝트 프리팹 경로가 비어 있습니다 : {inspectObjectDataId}");
            CloseInspectObjectUI();
            return;
        }

        GameObject objectPrefab = Resources.Load<GameObject>(inspectObjectData.PrefabPath);
        if (objectPrefab == null)
        {
            Debug.LogWarning($"조사 오브젝트 프리팹을 찾을 수 없습니다 : {inspectObjectData.PrefabPath}");
            CloseInspectObjectUI();
            return;
        }

        ShowObject(objectPrefab);
        ShowStartInspectText(inspectObjectData);
    }

    public void ShowInspectText(InspectPoint inspectPoint)
    {
        if (inspectPoint == null)
        {
            Debug.LogWarning("조사 포인트 참조가 비어 있습니다.");
            return;
        }

        bool isTextShown = ShowInspectText(inspectPoint.InspectTextId);
        if (isTextShown == false)
        {
            return;
        }

        RegisterInspectedPoint(inspectPoint);
    }

    public bool ShowInspectText(string inspectTextId)
    {
        if (string.IsNullOrEmpty(inspectTextId))
        {
            Debug.LogWarning("출력할 조사 텍스트 ID가 비어 있습니다.");
            return false;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance가 존재하지 않아 조사 텍스트 데이터를 조회할 수 없습니다.");
            return false;
        }

        InspectTextData inspectTextData = GameDataManager.Instance.GetInspectTextData(inspectTextId);
        if (inspectTextData == null)
        {
            Debug.LogWarning($"조사 텍스트 데이터가 존재하지 않습니다 : {inspectTextId}");
            return false;
        }

        StartDescription(inspectTextData.Description);
        return true;
    }

    public void SetDescription(string description)
    {
        if (Text_Description == null)
        {
            Debug.LogWarning("InspectObjectUI의 Text_Description 참조가 누락되어 있습니다.");
            return;
        }

        if (TrySetupDescriptionTypingEffect() == false)
        {
            Text_Description.text = description ?? string.Empty;
            return;
        }

        TypingEffect_Description.Play(description);
    }

    public void CloseInspectObjectUI()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance가 존재하지 않아 InspectObjectUI를 닫을 수 없습니다.");
            return;
        }

        UIManager.Instance.CloseContentUI(UIType.InspectObjectUI);
    }

    private void ShowDefaultObject()
    {
        if (Prefab_Object == null)
        {
            return;
        }

        ShowObject(Prefab_Object);
    }

    private void ShowStartInspectText(InspectObjectData inspectObjectData)
    {
        if (inspectObjectData == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(inspectObjectData.StartInspectTextId) == false)
        {
            InspectTextData inspectTextData = GameDataManager.Instance.GetInspectTextData(inspectObjectData.StartInspectTextId);
            if (inspectTextData != null)
            {
                StartDescription(inspectTextData.Description);
                return;
            }

            Debug.LogWarning($"시작 조사 텍스트 데이터가 존재하지 않습니다 : {inspectObjectData.StartInspectTextId}");
        }

        StartDescription(inspectObjectData.Description);
    }

    private void ShowObject(GameObject objectPrefab)
    {
        if (Layout_ObjectRoot == null)
        {
            Debug.LogWarning("InspectObjectUI의 Layout_ObjectRoot 참조가 누락되어 확대 오브젝트를 생성할 수 없습니다.");
            return;
        }

        if (objectPrefab == null)
        {
            Debug.LogWarning("생성할 확대 오브젝트 프리팹이 비어 있습니다.");
            return;
        }

        ClearObject();
        _createdObject = Instantiate(objectPrefab, Layout_ObjectRoot);
        PrepareInspectPointProgress(_createdObject);
    }

    private void ClearObject()
    {
        if (_createdObject == null)
        {
            return;
        }

        Destroy(_createdObject);
        _createdObject = null;
        ResetInspectPointProgress();
    }

    private void PrepareInspectPointProgress(GameObject createdObject)
    {
        ResetInspectPointProgress();

        if (createdObject == null)
        {
            Debug.LogWarning("생성된 조사 오브젝트가 없어 조사 포인트를 확인할 수 없습니다.");
            RefreshExitButton(true);
            return;
        }

        InspectPoint[] inspectPointArr = createdObject.GetComponentsInChildren<InspectPoint>(true);
        _inspectPointCount = inspectPointArr.Length;

        if (_inspectPointCount <= 0)
        {
            Debug.LogWarning($"{createdObject.name} 안에 InspectPoint가 없어 바로 나갈 수 있도록 처리합니다.");
            RefreshExitButton(true);
        }
    }

    private void ResetInspectPointProgress()
    {
        ClearDescriptionTyping();

        _descriptionQueue.Clear();
        _inspectPointCount = 0;
        _isCompleteInspectDescriptionShown = false;
        _isInspectTextShowing = false;
        _inspectedPointSet.Clear();
        RefreshNextButton(false);
        RefreshExitButton(false);
    }

    private void RegisterInspectedPoint(InspectPoint inspectPoint)
    {
        if (inspectPoint == null)
        {
            return;
        }

        _inspectedPointSet.Add(inspectPoint);
    }

    private void CheckCompleteInspect()
    {
        if (_isCompleteInspectDescriptionShown)
        {
            return;
        }

        if (_isInspectTextShowing)
        {
            return;
        }

        if (_inspectPointCount <= 0)
        {
            return;
        }

        if (_inspectedPointSet.Count < _inspectPointCount)
        {
            return;
        }

        _isCompleteInspectDescriptionShown = true;
        SetDescription(_completeInspectDescription);
        RefreshExitButton(true);
    }

    private void RefreshExitButton(bool isActive)
    {
        if (Button_Exit == null)
        {
            return;
        }

        Button_Exit.gameObject.SetActive(isActive);
    }

    private void StartDescription(string description)
    {
        PrepareDescriptionQueue(description);
        _isInspectTextShowing = true;
        ShowNextDescriptionPage();
        RefreshNextButton(true);
    }

    private void PrepareDescriptionQueue(string description)
    {
        _descriptionQueue.Clear();

        if (string.IsNullOrEmpty(description))
        {
            _descriptionQueue.Enqueue(string.Empty);
            return;
        }

        if (description.Contains("<np>"))
        {
            string[] descriptionArr = description.Split("<np>");
            for (int i = 0; i < descriptionArr.Length; i++)
            {
                _descriptionQueue.Enqueue(descriptionArr[i]);
            }
            return;
        }

        _descriptionQueue.Enqueue(description);
    }

    private bool ShowNextDescriptionPage()
    {
        if (_descriptionQueue.Count <= 0)
        {
            return false;
        }

        SetDescription(_descriptionQueue.Dequeue());
        return true;
    }

    private void RequestNextDescription()
    {
        if (_isInspectTextShowing == false)
        {
            return;
        }

        if (IsDescriptionTyping())
        {
            SkipDescriptionTyping();
            return;
        }

        bool isNextDescriptionExist = ShowNextDescriptionPage();
        if (isNextDescriptionExist)
        {
            return;
        }

        _isInspectTextShowing = false;
        SetDescription(string.Empty);
        RefreshNextButton(false);
        CheckCompleteInspect();
    }

    private void RefreshNextButton(bool isActive)
    {
        if (Button_Next == null)
        {
            return;
        }

        Button_Next.gameObject.SetActive(isActive);
    }

    private void SkipDescriptionTyping()
    {
        if (TypingEffect_Description == null)
        {
            return;
        }

        TypingEffect_Description.Skip();
    }

    private void ClearDescriptionTyping()
    {
        if (TypingEffect_Description != null)
        {
            TypingEffect_Description.Clear();
            return;
        }

        if (Text_Description != null)
        {
            Text_Description.text = string.Empty;
        }
    }

    private bool IsDescriptionTyping()
    {
        return TypingEffect_Description != null && TypingEffect_Description.IsTyping;
    }

    private bool TrySetupDescriptionTypingEffect()
    {
        if (Text_Description == null)
        {
            Debug.LogWarning("InspectObjectUI의 Text_Description 참조가 누락되어 있습니다.");
            return false;
        }

        if (TypingEffect_Description == null)
        {
            TypingEffect_Description = GetComponent<TextTypingEffect>();
            if (TypingEffect_Description == null)
            {
                TypingEffect_Description = gameObject.AddComponent<TextTypingEffect>();
            }
        }

        TypingEffect_Description.Initialize(Text_Description);
        return true;
    }
}
