using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Answer : MonoBehaviour
{
    [Header("채팅창")]
    public Transform BubbleParent;
    public GameObject PlayerBubble;
    public GameObject CharacterBubble;
    public TMP_InputField InputText;
    public Button sendButton;

    [Header("입력창 설정")]
    public RectTransform InputFieldRect;  // TMP_InputField의 부모 RectTransform
    public float NormalHeight = 100f;     // 기본 높이
    public float FocusedHeight = 250f;    // 입력 중일 때 높이
    public float ResizeDuration = 0.25f;  // 높이 애니메이션 시간
    public float DownOffsetY = -150f;     // 입력 시 아래로 이동 거리
    public float DownDuration = 0.3f;
    public float UpYPosition = 50f;       // 답변 후 위치 복원값

    [Header("데이터")]
    public AnswerMaker AnswerMaker;
    private AnswerJson _answerJson;

    [Header("오디오")]
    public AudioSource AudioSource;

    private float currentHeight = -1f;
    [SerializeField]private bool isFocused = false;

    private void Start()
    {
        InputText.onSelect.AddListener(OnInputFieldFocused);
        InputText.onDeselect.AddListener(OnInputFieldUnfocused);
        InputText.onSubmit.AddListener((value) => 
        {
            Debug.Log("onSubmit 이벤트로 제출됨");
            MakeOutput();
        });

        // 이벤트 등록 (외부에서 호출)
        AnswerMaker.OnPromptSend += InputBoxDown;
        AnswerMaker.OnAnswerGet += InputBoxUp;
    }

    private void OnInputFieldFocused(string _)
    {
        isFocused = true;
        AnimateInputFieldHeight(FocusedHeight);
    }

    private void OnInputFieldUnfocused(string _)
    {
        isFocused = false;
        AnimateInputFieldHeight(NormalHeight);
    }

    private void AnimateInputFieldHeight(float targetHeight)
    {
        if (InputFieldRect == null) return;

        if (Mathf.Approximately(currentHeight, targetHeight)) return;
        currentHeight = targetHeight;

        InputFieldRect
            .DOSizeDelta(new Vector2(InputFieldRect.sizeDelta.x, targetHeight), ResizeDuration)
            .SetEase(Ease.OutCubic);
    }

    private void InputBoxDown()
    {
        if (InputFieldRect == null) return;

        
        OnInputFieldUnfocused("");

        InputFieldRect.DOAnchorPosY(DownOffsetY, DownDuration)
            .SetRelative(true)
            .SetEase(Ease.OutBack);
        InputText.text = "";
    }

    private void InputBoxUp()
    {
        if (InputFieldRect == null) return;

        InputFieldRect.DOAnchorPosY(UpYPosition, 0.25f)
            .SetRelative(false)
            .SetEase(Ease.OutCubic);
    }

    public void MakeOutput()
    {
        GetOutputAnswer(InputText.text);
    }

    // UI_Answer.cs

    private async void GetOutputAnswer(string prompt)
    {
        // 1. 입력이 시작되면 버튼과 입력창을 모두 비활성화
        sendButton.interactable = false;
        InputText.interactable = false;

        try
        {
            MakePlayerBubble(prompt);
            await AnswerMaker.MakeOutput(prompt); // 답변이 올 때까지 대기
            MakeCharacterBubble(AnswerMaker.OutputAnswer.CharacterAnswerJson);
            SpeakDial(AnswerMaker.OutputAnswer.CharacterAudioclip);
        }
        catch (Exception e)
        {
            Debug.LogError($"답변 처리 중 오류 발생: {e.Message}");
            // (선택) 여기에 오류 발생 시 보여줄 말풍선 생성 로직 추가
        }
        finally
        {
            // 2. 작업이 성공하든, 실패(오류)하든 반드시 실행되는 부분
            // 여기서 버튼과 입력창을 다시 활성화합니다.
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

        GameObject characterBubble = Instantiate(CharacterBubble, BubbleParent);
        characterBubble.GetComponent<AnswerBubble>().Set(answerJson.ReplyMessage);
    }

    public void SpeakDial(AudioClip audioClip)
    {
        if (audioClip == null) return;
        AudioSource.PlayOneShot(audioClip);
    }
}
