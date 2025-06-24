using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimator : MonoBehaviour
{
    [Header("UI Components")]
    public Image characterImage;
    public CanvasGroup textBoxGroup;
    public TMP_Text dialogueText;

    [Header("Animation Settings")]
    public float characterEnterDuration = 0.6f;
    public float textboxFadeDuration = 0.4f;
    public float typingSpeed = 0.03f;

    private Vector2 characterOffscreenPos = new Vector2(-500f, 0);

    private void Awake()
    {
        // 초기 상태 설정
        if (characterImage != null)
        {
            characterImage.color = new Color(1, 1, 1, 0);
            characterImage.rectTransform.anchoredPosition = characterOffscreenPos;
        }

        if (textBoxGroup != null)
        {
            textBoxGroup.alpha = 0;
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }

    

    private IEnumerator CharacterEntrance()
    {
        if (characterImage == null) yield break;

        Sequence seq = DOTween.Sequence();
        seq.Append(characterImage.rectTransform.DOAnchorPosX(0, characterEnterDuration).SetEase(Ease.OutBack));
        seq.Join(characterImage.DOFade(1f, characterEnterDuration));

        yield return seq.WaitForCompletion();
    }

    private IEnumerator ShowTextBox()
    {
        if (textBoxGroup == null) yield break;

        yield return textBoxGroup.DOFade(1f, textboxFadeDuration).WaitForCompletion();
    }

    private IEnumerator TypeDialogue(string text)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
