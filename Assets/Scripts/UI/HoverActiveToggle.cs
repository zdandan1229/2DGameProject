using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverActiveToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject _targetObject;
    [SerializeField] private bool _hideOnEnable = true;

    private void Awake()
    {
        DisableTargetRaycast();
    }

    private void OnEnable()
    {
        DisableTargetRaycast();

        if (_hideOnEnable)
        {
            SetTargetActive(false);
        }
    }

    private void OnDisable()
    {
        SetTargetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetTargetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetTargetActive(false);
    }

    public void SetTargetObject(GameObject targetObject)
    {
        _targetObject = targetObject;
        DisableTargetRaycast();
    }

    private void SetTargetActive(bool isActive)
    {
        if (_targetObject == null)
        {
            Debug.LogWarning($"{gameObject.name}의 HoverActiveToggle에 _targetObject 참조가 비어 있습니다.");
            return;
        }

        if (_targetObject.activeSelf == isActive)
        {
            return;
        }

        _targetObject.SetActive(isActive);
    }

    private void DisableTargetRaycast()
    {
        if (_targetObject == null)
        {
            return;
        }

        Graphic[] targetGraphicArr = _targetObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < targetGraphicArr.Length; i++)
        {
            if (targetGraphicArr[i] != null)
            {
                targetGraphicArr[i].raycastTarget = false;
            }
        }
    }
}
