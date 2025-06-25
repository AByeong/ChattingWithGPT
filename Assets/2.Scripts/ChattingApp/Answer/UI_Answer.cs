// UI_Answer.cs

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class UI_Answer : MonoBehaviour
{
    [Header("채팅창")]
    [SerializeField] private Transform BubbleParent;
    [SerializeField] private GameObject PlayerBubble;
    [SerializeField] private GameObject CharacterBubble;
    [SerializeField] private TMP_InputField InputText;
    [SerializeField] private Button sendButton;

    [Header("스크롤")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("입력창 설정")]
    [SerializeField] private RectTransform InputFieldRect;
    [SerializeField] private float NormalHeight = 100f;
    [SerializeField] private float FocusedHeight = 250f;
    [SerializeField] private float ResizeDuration = 0.25f;
    [SerializeField] private float DownOffsetY = -150f;
    [SerializeField] private float DownDuration = 0.3f;
    [SerializeField] private float UpYPosition = 50f;


    [Header("이미지")]
    [SerializeField] private RawImage Image;

    [Header("로딩창")]
    [SerializeField] private GameObject _loadingUI;
    
    [Header("데이터")]
    [SerializeField] private AnswerMaker AnswerMaker;

    [Header("오디오")]
    [SerializeField] private AudioSource AudioSource;

    private float currentHeight = -1f;

    private void Start()
    {
        InputText.onSelect.AddListener(OnInputFieldFocused);
        InputText.onDeselect.AddListener(OnInputFieldUnfocused);
        InputText.onSubmit.AddListener((value) => MakeOutput());
        sendButton.onClick.AddListener(MakeOutput);

        AnswerMaker.OnPromptSend += InputBoxDown;
        AnswerMaker.OnPromptSend += () => _loadingUI.SetActive(true);

        AnswerMaker.OnAnswerGet += InputBoxUp;
        AnswerMaker.OnAnswerGet +=() => _loadingUI.SetActive(false);
        AnswerMaker.OnAnswerGet += SetTexture;
 
    }

    private void OnDestroy()
    {
        if (InputText != null)
        {
            InputText.onSelect.RemoveAllListeners();
            InputText.onDeselect.RemoveAllListeners();
            InputText.onSubmit.RemoveAllListeners();
        }
        if (sendButton != null) sendButton.onClick.RemoveAllListeners();
        if (AnswerMaker != null)
        {
            AnswerMaker.OnPromptSend -= InputBoxDown;
            AnswerMaker.OnAnswerGet -= InputBoxUp;
        }
    }

    private void OnInputFieldFocused(string _) => AnimateInputFieldHeight(FocusedHeight);
    private void OnInputFieldUnfocused(string _) => AnimateInputFieldHeight(NormalHeight);

    private void AnimateInputFieldHeight(float targetHeight)
    {
        if (InputFieldRect == null || Mathf.Approximately(currentHeight, targetHeight)) return;
        currentHeight = targetHeight;
        InputFieldRect.DOSizeDelta(new Vector2(InputFieldRect.sizeDelta.x, targetHeight), ResizeDuration).SetEase(Ease.OutCubic);
    }

    private void InputBoxDown()
    {
        if (InputFieldRect == null) return;
        currentHeight = NormalHeight;
        InputText.text = "";
        DOTween.Sequence()
            .Join(InputFieldRect.DOSizeDelta(new Vector2(InputFieldRect.sizeDelta.x, NormalHeight), ResizeDuration).SetEase(Ease.OutCubic))
            .Join(InputFieldRect.DOAnchorPosY(DownOffsetY, DownDuration).SetRelative(true).SetEase(Ease.OutBack));
    }

    private void InputBoxUp()
    {
        if (InputFieldRect == null) return;
        InputFieldRect.DOAnchorPosY(UpYPosition, 0.25f).SetRelative(false).SetEase(Ease.OutCubic);
    }

    public void MakeOutput()
    {
        if (string.IsNullOrWhiteSpace(InputText.text)) return;
        GetOutputAnswer(InputText.text);
    }


    private async void GetOutputAnswer(string prompt)
    {
        sendButton.interactable = false;
        InputText.interactable = false;

        try
        {
            MakePlayerBubble(prompt);
            ScrollToBottom();

            await AnswerMaker.MakeOutput(prompt);
            MakeCharacterBubble(AnswerMaker.OutputAnswer.CharacterAnswerJson);
            SpeakDial(AnswerMaker.OutputAnswer.CharacterAudioclip);

            ScrollToBottom();
        }
        catch (Exception e)
        {
            Debug.LogError($"답변 처리 중 오류 발생: {e.Message}");
        }
        finally
        {
            sendButton.interactable = true;
            InputText.interactable = true;
        }
    }

    public void MakePlayerBubble(string myDial)
    {
        GameObject playerBubble = Instantiate(PlayerBubble, BubbleParent);
        playerBubble.GetComponent<AnswerBubble>().Set(myDial);
    }

    public void MakeCharacterBubble(AnswerJson answerJson)
    {
        if (answerJson == null)
        {
            Debug.LogError("answerJson is null.");
            return;
        }
        GameObject characterBubble = Instantiate(CharacterBubble, BubbleParent);

        if (string.IsNullOrEmpty(answerJson.ActingMessage))
        {
            characterBubble.GetComponent<AnswerBubble>().Set($"{answerJson.ReplyMessage}");

        }
        else
        {
            characterBubble.GetComponent<AnswerBubble>().Set($"<color=grey>{answerJson.ActingMessage}</color>\n{answerJson.ReplyMessage}");

        }
    }

    public void SpeakDial(AudioClip audioClip)
    {
        if (audioClip == null) return;
        AudioSource.PlayOneShot(audioClip);
    }

    public void ScrollToBottom()
    {
        StartCoroutine(ScrollToBottomCoroutine());
    }

    private IEnumerator ScrollToBottomCoroutine()
    {
        if (scrollRect == null) yield break;
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }


    public void SetTexture()
    {
        Texture texture = AnswerMaker.OutputAnswer.CharacterTextureImage;

        if(texture == null)
        {
            return;
        }
        Image.texture = texture;
    }

}