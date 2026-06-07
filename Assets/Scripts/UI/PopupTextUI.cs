using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PopupTextUI : UIBase
{
    [SerializeField] private Text Text_PopupText;
    [SerializeField] private CanvasGroup CanvasGroup_PopupText;
    [SerializeField] private float _visibleDuration = 5f;
    [SerializeField] private float _fadeOutDuration = 2f;

    private Coroutine _showCoroutine;

    public void ShowPopupText(string popupText)
    {
        if (TrySetupReferences() == false)
        {
            return;
        }

        StopShowCoroutine();
        _showCoroutine = StartCoroutine(ShowPopupTextRoutine(popupText));
    }

    private void OnDisable()
    {
        StopShowCoroutine();

        if (CanvasGroup_PopupText != null)
        {
            CanvasGroup_PopupText.alpha = 1f;
        }
    }

    private void InitializeReferences()
    {
        if (Text_PopupText == null)
        {
            Text_PopupText = GetComponentInChildren<Text>(true);
            if (Text_PopupText == null)
            {
                Debug.LogWarning("PopupTextUI Text_PopupText reference is missing, so popup text cannot be shown.");
            }
        }

        if (CanvasGroup_PopupText == null)
        {
            CanvasGroup_PopupText = GetComponent<CanvasGroup>();
            if (CanvasGroup_PopupText == null)
            {
                CanvasGroup_PopupText = gameObject.AddComponent<CanvasGroup>();
                Debug.LogWarning("PopupTextUI CanvasGroup reference was missing, so a new CanvasGroup was added at runtime.");
            }
        }
    }

    private bool TrySetupReferences()
    {
        InitializeReferences();
        if (Text_PopupText == null)
        {
            return false;
        }

        if (CanvasGroup_PopupText == null)
        {
            Debug.LogWarning("PopupTextUI CanvasGroup reference is missing, so fade out cannot run.");
            return false;
        }

        return true;
    }

    private IEnumerator ShowPopupTextRoutine(string popupText)
    {
        Text_PopupText.text = popupText;
        CanvasGroup_PopupText.alpha = 1f;

        float visibleDuration = Mathf.Max(0f, _visibleDuration);
        if (visibleDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(visibleDuration);
        }

        float fadeOutDuration = Mathf.Max(0f, _fadeOutDuration);
        if (fadeOutDuration > 0f)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                CanvasGroup_PopupText.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                yield return null;
            }
        }

        CanvasGroup_PopupText.alpha = 0f;
        _showCoroutine = null;
        ClosePopupTextUI();
    }

    private void StopShowCoroutine()
    {
        if (_showCoroutine == null)
        {
            return;
        }

        StopCoroutine(_showCoroutine);
        _showCoroutine = null;
    }

    private void ClosePopupTextUI()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is missing, so PopupTextUI cannot be closed through UIManager.");
            gameObject.SetActive(false);
            return;
        }

        UIManager.Instance.ClosePopupUI(UIType.PopupTextUI);
    }
}
