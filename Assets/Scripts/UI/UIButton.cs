using System;
using UnityEngine;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    [SerializeField] private Button Button_Base;
    [SerializeField] private Text Text_Base;
    [SerializeField] private Image Image_Base;
    [SerializeField] private Image Image_Select;

    private void Awake()
    {
        // 1-2) 이 오브젝트가 생성될 때, 한번 컴포넌트를 찾아서 캐싱하자
        InitUIButton();
        SetDefaultUI();
    }

    private void OnEnable()
    {
        BindOnClickButtonEvent(OnClickSetSelectUI);
    }

    private void OnDisable()
    {
        Button_Base.onClick.RemoveAllListeners();
    }


    private void SetDefaultUI()
    {
        if(Image_Select != null)
        {
            Image_Select.gameObject.SetActive(false);
        }
    }

    private void InitUIButton()
    {
        if(Button_Base != null)
        {
            return;
        }

        // 1-1) 외부에서도 등록할 수 있고,
            // 누군가 누락했다면 등록안해도 알아서 찾아주도록 로직을 넣어 놨다
        var button = this.gameObject.GetComponentInChildren<Button>();
        if(button != null)
        {
            this.Button_Base = button;
        }
    }

    public void BindOnClickButtonEvent(Action onClickCallback)
    {
        if(Button_Base == null) return;

        Button_Base.onClick.AddListener(new UnityEngine.Events.UnityAction(onClickCallback));

    }

    public void UnBindOnClickButtonEvent(Action onClickCallback)
    {
        if (Button_Base == null) return;

        Button_Base.onClick.RemoveListener(new UnityEngine.Events.UnityAction(onClickCallback));
    }

    public void ChangeButtonText(string buttonStr)
    {
        // 혹시 이버튼을 동적으로, 코드에서 텍스트를 수정해야할 때 사용
        if (Text_Base == null) return;

        Text_Base.text = buttonStr;
    }

    private void OnClickSetSelectUI()
    {
        if(Image_Select != null)
        {
            bool currentActive = Image_Select.gameObject.activeSelf;
            Image_Select.gameObject.SetActive(!currentActive);
        }
    }
}
