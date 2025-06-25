using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(AnswerBubble))]
[RequireComponent(typeof(CanvasGroup))]
public class UI_AnswerBubble : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private RectTransform background;
    [SerializeField] private float _defaultWidth = 400f; // 기본 너비
    [SerializeField] private float _maxWidth = 2000f;     // 텍스트가 줄바꿈될 최대 너비

    private AnswerBubble answerBubble;
    private CanvasGroup canvasGroup;
    [SerializeField] private Vector2 padding = new Vector2(60f, 60f);

    private Coroutine _typewriterCoroutine;

    private void Awake()
    {
        answerBubble = GetComponent<AnswerBubble>();
        canvasGroup = GetComponent<CanvasGroup>();
        answerBubble.OnSet += HandleSet;
    }

    private void OnDestroy()
    {
        if (answerBubble != null)
        {
            answerBubble.OnSet -= HandleSet;
        }
    }

    private void HandleSet(string message, bool useTypewriter, float speed)
    {
        messageText.text = ""; // 초기화
        UpdateBackgroundSize(); // 초기 크기 설정
        PlayAppearAnimation();
        PlayTextAnimation(message, useTypewriter, speed);
    }

    /// <summary>
    /// 텍스트의 현재 내용에 따라 배경 크기 갱신
    /// </summary>
    private void UpdateBackgroundSize()
    {
        float targetWidth;
        float finalHeight;

        // Step 1: Calculate preferred size assuming infinite width
        // 이 preferredSize.x는 텍스트가 줄바꿈 없이 한 줄로 쭉 이어진다고 가정했을 때의 너비입니다.
        Vector2 preferredSizeUnbounded = messageText.GetPreferredValues(messageText.text, float.PositiveInfinity, float.PositiveInfinity);

        // Step 2: Determine the target width for the messageText
        // 텍스트의 선호 너비가 _defaultWidth보다 작으면 _defaultWidth 사용
        if (preferredSizeUnbounded.x <= _defaultWidth)
        {
            targetWidth = _defaultWidth;
        }
        // 텍스트의 선호 너비가 _defaultWidth보다 크고 _maxWidth보다 작거나 같으면 그 선호 너비 사용
        else if (preferredSizeUnbounded.x <= _maxWidth)
        {
            targetWidth = preferredSizeUnbounded.x;
        }
        // 텍스트의 선호 너비가 _maxWidth보다 크면 _maxWidth로 고정하여 줄바꿈 발생
        else
        {
            targetWidth = _maxWidth;
        }

        // Step 3: Calculate the final height based on the determined targetWidth
        // 최종 결정된 targetWidth를 기준으로 실제 텍스트가 차지할 높이를 계산합니다.
        // 이 때 줄바꿈이 필요한 경우 TextMeshPro가 알아서 처리해줍니다.
        finalHeight = messageText.GetPreferredValues(messageText.text, targetWidth, float.PositiveInfinity).y;

        // Step 4: Apply the calculated size to the background RectTransform
        background.sizeDelta = new Vector2(targetWidth + padding.x, finalHeight + padding.y);
    }

    private void PlayAppearAnimation()
    {
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.8f;

        DOTween.Sequence()
            .Join(canvasGroup.DOFade(1f, 0.3f))
            .Join(transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
    }

    private void PlayTextAnimation(string message, bool useTypewriter, float speed)
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
        }

        if (useTypewriter)
        {
            float duration = message.Length / speed;
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(message, duration));
        }
        else
        {
            messageText.text = message;
            UpdateBackgroundSize(); // 정적 출력 시에도 크기 갱신
        }
    }

    /// <summary>
    /// Rich Text 태그를 인식하여 처리하는 타자 효과 코루틴
    /// </summary>
    private IEnumerator TypewriterCoroutine(string message, float duration)
    {
        yield return null;

        // 타이핑 속도 계산
        int visibleCharCount = 0;
        bool inTag = false;
        foreach (char c in message)
        {
            if (c == '<') inTag = true;
            if (!inTag) visibleCharCount++;
            if (c == '>') inTag = false;
        }

        if (visibleCharCount == 0)
        {
            messageText.text = message;
            UpdateBackgroundSize();
            yield break;
        }

        float timePerCharacter = duration / visibleCharCount;

        messageText.text = "";
        int i = 0;
        while (i < message.Length)
        {
            if (message[i] == '<')
            {
                string tag = "";
                int tagEndIndex = i;
                while (tagEndIndex < message.Length && message[tagEndIndex] != '>')
                {
                    tag += message[tagEndIndex];
                    tagEndIndex++;
                }
                tag += '>';
                tagEndIndex++;

                messageText.text += tag;
                i = tagEndIndex;
            }
            else
            {
                messageText.text += message[i];
                i++;

                UpdateBackgroundSize(); //타이핑 중 동적 크기 갱신
                yield return new WaitForSeconds(timePerCharacter);
            }
        }

        UpdateBackgroundSize(); // 타이핑 완료 후 최종 보정
        _typewriterCoroutine = null;
    }
}