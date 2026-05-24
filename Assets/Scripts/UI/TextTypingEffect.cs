using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TextTypingEffect : MonoBehaviour
{
    [SerializeField] private Text Text_Target;
    [SerializeField] private float _typingInterval = 0.03f;

    private string _currentFullText = string.Empty;
    private Coroutine _typingCoroutine;

    public bool IsTyping
    {
        get
        {
            return _typingCoroutine != null;
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Initialize(Text textTarget)
    {
        Text_Target = textTarget;
    }

    public void Play(string text)
    {
        if (Text_Target == null)
        {
            Debug.LogWarning($"{nameof(TextTypingEffect)}의 Text_Target 참조가 누락되어 있습니다.");
            return;
        }

        Stop();

        _currentFullText = text ?? string.Empty;
        if (string.IsNullOrEmpty(_currentFullText) || _typingInterval <= 0f)
        {
            Text_Target.text = _currentFullText;
            return;
        }

        _typingCoroutine = StartCoroutine(PlayTyping(_currentFullText));
    }

    public void Skip()
    {
        Stop();

        if (Text_Target == null)
        {
            Debug.LogWarning($"{nameof(TextTypingEffect)}의 Text_Target 참조가 누락되어 있습니다.");
            return;
        }

        Text_Target.text = _currentFullText;
    }

    public void Stop()
    {
        if (_typingCoroutine == null)
        {
            return;
        }

        StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
    }

    public void Clear()
    {
        Stop();
        _currentFullText = string.Empty;

        if (Text_Target != null)
        {
            Text_Target.text = string.Empty;
        }
    }

    private IEnumerator PlayTyping(string text)
    {
        Text_Target.text = string.Empty;

        foreach (char letter in text)
        {
            Text_Target.text += letter;
            yield return new WaitForSecondsRealtime(_typingInterval);
        }

        _typingCoroutine = null;
    }
}
