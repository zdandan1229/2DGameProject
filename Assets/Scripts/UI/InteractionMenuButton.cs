using System;
using UnityEngine;
using UnityEngine.UI;

public class InteractionMenuButton : MonoBehaviour
{
    [SerializeField] private Button Button_Base;
    [SerializeField] private Text Text_Base;

    private InteractionOption _interactionOption;
    private Action<InteractionOption> _onClickCallback;

    public void Initialize(InteractionOption interactionOption, Action<InteractionOption> onClickCallback)
    {
        _interactionOption = interactionOption;
        _onClickCallback = onClickCallback;

        TrySetupButton();
        TrySetupText();

        if (Text_Base != null)
        {
            Text_Base.text = interactionOption != null ? interactionOption.ButtonText : string.Empty;
        }

        if (Button_Base == null)
        {
            Debug.LogWarning($"{gameObject.name}에 Button 컴포넌트가 없어 상호작용 버튼을 초기화할 수 없습니다.");
            return;
        }

        Button_Base.onClick.RemoveListener(OnClickButton);
        Button_Base.onClick.AddListener(OnClickButton);
    }

    private void OnDestroy()
    {
        if (Button_Base != null)
        {
            Button_Base.onClick.RemoveListener(OnClickButton);
        }
    }

    private void TrySetupButton()
    {
        if (Button_Base != null)
        {
            return;
        }

        Button_Base = GetComponent<Button>();
        if (Button_Base == null)
        {
            Button_Base = GetComponentInChildren<Button>(true);
        }
    }

    private void TrySetupText()
    {
        if (Text_Base != null)
        {
            return;
        }

        Text_Base = GetComponentInChildren<Text>(true);
    }

    private void OnClickButton()
    {
        if (_interactionOption == null)
        {
            Debug.LogWarning($"{gameObject.name}의 상호작용 옵션이 비어 있습니다.");
            return;
        }

        if (_interactionOption.CanShowInMenu() == false)
        {
            Debug.LogWarning($"{gameObject.name}의 상호작용 옵션이 올바르지 않습니다.");
            return;
        }

        if (_onClickCallback == null)
        {
            Debug.LogWarning($"{gameObject.name}의 상호작용 버튼 클릭 콜백이 비어 있습니다.");
            return;
        }

        _onClickCallback.Invoke(_interactionOption);
    }
}
