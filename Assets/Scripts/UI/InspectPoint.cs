using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InspectPoint : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string _inspectTextId;
    [SerializeField] private string _inspectObjectDataId;
    [SerializeField] private string _completeFlagId;
    [SerializeField] private string _completeJournalDataId;
    [SerializeField] private float _alphaHitTestMinimumThreshold = 0.1f;

    public string InspectTextId
    {
        get { return _inspectTextId; }
    }

    public bool HasInspectTarget()
    {
        return string.IsNullOrEmpty(_inspectTextId) == false ||
               string.IsNullOrEmpty(_inspectObjectDataId) == false;
    }

    public string CompleteFlagId
    {
        get { return _completeFlagId; }
    }

    public string CompleteJournalDataId
    {
        get { return _completeJournalDataId; }
    }

    private void Awake()
    {
        SetAlphaHitTest();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        RequestShowInspectText();
    }

    private void RequestShowInspectText()
    {
        if (HasInspectTarget() == false)
        {
            Debug.LogWarning($"{gameObject.name}의 조사 텍스트 ID와 조사 오브젝트 ID가 모두 비어 있습니다.");
            return;
        }

        InspectObjectUI inspectObjectUI = GetComponentInParent<InspectObjectUI>();
        if (inspectObjectUI == null)
        {
            Debug.LogWarning($"{gameObject.name}의 부모에서 InspectObjectUI를 찾지 못했습니다.");
            return;
        }

        if (string.IsNullOrEmpty(_inspectObjectDataId) == false)
        {
            inspectObjectUI.OpenInspectObjectFromArea(_inspectObjectDataId, this);
            return;
        }

        inspectObjectUI.ShowInspectText(this);
    }

    private void SetAlphaHitTest()
    {
        Image image = GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"{gameObject.name}에 Image 컴포넌트가 없어 알파 클릭 판정을 설정할 수 없습니다.");
            return;
        }

        if (image.sprite == null)
        {
            Debug.LogWarning($"{gameObject.name}의 Image Sprite가 비어 있어 알파 클릭 판정을 설정할 수 없습니다.");
            return;
        }

        image.alphaHitTestMinimumThreshold = _alphaHitTestMinimumThreshold;
    }
}
