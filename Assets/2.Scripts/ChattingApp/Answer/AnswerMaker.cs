using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using OpenAI.Responses;
using UnityEngine;
using UnityEngine.UI;
using Message = OpenAI.Chat.Message;

public class AnswerMaker : MonoBehaviour
{

    [Header("기본 세팅")]
    private OpenAIClient _api;
    private List<Message> _memory = new List<Message>();

    [Header("인풋")]
    [SerializeField] private AudioSource _audio;
    public RawImage CharacterImage;
    [SerializeField] private Typecast _typecast;
    [SerializeField] private ImageDisplayManager _imageDisplayManager; // ImageDisplayManager 참조

    [Header("아웃풋")]
    public Output OutputAnswer = new Output(new AnswerJson("", "", "", "", ""));

    [Header("데이터")]
    public WorldInformationSO WorldInformationSO;

    public event Action OnPromptSend;
    public event Action OnAnswerGet;

    public PlayerStatManager StatManager;

    private void Awake()
    {
        _api = new OpenAIClient(EnvironmentInformation.GPT_API_KEY);
        if (_audio == null)
        {
            _audio = GetComponent<AudioSource>();
        }

        if (_typecast == null)
        {
            _typecast = GetComponent<Typecast>();
        }
    }

    private void Start()
    {
        if (WorldInformationSO != null)
        {
            _memory.AddRange(WorldInformationSO.WorldDescriptionMessages());
        }

        _memory.Add(new Message(Role.System, "ReplyMessage에는 대답을 넣어줘."));
        _memory.Add(new Message(Role.System, "ActingMessage에는 지금하고 있는 행동을 넣어줘."));
        _memory.Add(new Message(Role.System, "ActingMessage에는 특별히 중요한 지시문이 아니라면 그냥 비워줬으면 좋겠어."));
        _memory.Add(new Message(Role.System, "ActingMessage는 냥으로 끝나지 않았으면 좋겠어."));
        _memory.Add(new Message(Role.System, "ActingMessage는 뭐뭐하고 있다라는 문장 마무리로 끝났으면 좋겠어."));
        _memory.Add(new Message(Role.System, "Price에는 지금 플레이어에게 팔려는 포션의 가격이었으면 좋겠어"));

        _memory.Add(new Message(Role.System, $"만약 플레이어가 구매를 시도할 때, 플레이어의 재화의 양이 제시된 금액보다 낮으면 Buy에 '실패'이라고 해줘"));
        _memory.Add(new Message(Role.System, $"만약 플레이어가 구매를 시도할 때, 플레이어의 재화의 양이 제시된 금액보다 높거나 같으면 Buy에 '성공'이라고 해줘"));
        _memory.Add(new Message(Role.System, $"만약 플레이어가 구매를 시도하지 않는다면 Buy는 빈 문자열을 넣어줘"));
        _memory.Add(new Message(Role.System, $"Buy에는 내가 말해준 저 세개의 경우 빼고는 어떤 경우도 없었으면 좋겠어"));
        _memory.Add(new Message(Role.System, "Price 값에는 오직 숫자만 입력해줘. '골드', '원', '돈' 같은 단어 없이 숫자만 넣어줘."));

        _memory.Add(new Message(Role.System, "만약 ActingMessage가 비어있지 않다면, ImageDescription에는 현재 캐릭터의 행동을 매우 상세하고 구체적으로 묘사하는 영어 프롬프트를 넣어줘."));
        _memory.Add(new Message(Role.System, "ImageDescription은 ComfyUI를 위한 프롬프트이므로, 'a cute cat girl is smiling'과 같이 이미지 생성에 적합한 구체적인 영어 단어와 구문으로 구성되어야 해. 배경 묘사는 포함하지 말아줘."));
        _memory.Add(new Message(Role.System, "ActingMessage가 비어있다면 ImageDescription도 빈 문자열로 남겨둬."));
    }

    public async Task MakeOutput(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            return;
        }

        OnPromptSend?.Invoke();
        _memory.Add(new Message(Role.User, prompt));

        _memory.Add(new Message(Role.System, $"현재 플레이어가 가진 재화의 양은 {StatManager.MyStat.Gold}야."));


        ChatRequest chatRequest = new ChatRequest(_memory, Model.GPT4o);

        var (answerJson, response) = await _api.ChatEndpoint.GetCompletionAsync<AnswerJson>(chatRequest);
        Choice choice = response.FirstChoice;

        Task<AudioClip> speechClipTask = _typecast.StartSpeechAsync(answerJson.ReplyMessage);

        Task<Texture2D> imageTextureTask = null;
        if (_imageDisplayManager != null && !string.IsNullOrEmpty(answerJson.ActingMessage))
        {
            // ActingMessage가 있을 때만 ImageDescription을 ComfyUI 프롬프트로 사용
            Debug.Log($"[AnswerMaker] ActingMessage 존재. 이미지 생성 요청 ({answerJson.ImageDescripton})");
            imageTextureTask = _imageDisplayManager.GenerateAndGetImageAsync(answerJson.ImageDescripton);
        }
        else if (string.IsNullOrEmpty(answerJson.ActingMessage))
        {
            Debug.Log("[AnswerMaker] ActingMessage가 비어있으므로 이미지 생성을 건너뜁니다.");
            OutputAnswer.CharacterTextureImage = null; // 이미지를 생성하지 않을 때는 기존 이미지를 비워둘 수 있음
        }


        List<Task> allTasks = new List<Task> { speechClipTask };
        if (imageTextureTask != null)
        {
            allTasks.Add(imageTextureTask);
        }
        await Task.WhenAll(allTasks);

        OutputAnswer.CharacterAnswerJson = answerJson;
        OutputAnswer.CharacterAudioclip = speechClipTask.Result;
        if (imageTextureTask != null)
        {
            OutputAnswer.CharacterTextureImage = imageTextureTask.Result;
        }

        _memory.Add(new Message(Role.Assistant, choice.Message.ToString()));

        Debug.Log(answerJson.ToString());

        if (answerJson.Buy == "성공")
        {
            StatManager.Purchase(int.Parse(answerJson.Price));
        }

        OnAnswerGet?.Invoke();
    }
}

public class Output
{
    public AnswerJson CharacterAnswerJson { get; set; }
    public AudioClip CharacterAudioclip { get; set; }
    public Texture CharacterTextureImage { get; set; }

    public Output(AnswerJson characterAnswerJson, AudioClip characterAudioclip = null, Texture characterTextureImage = null)
    {
        CharacterAnswerJson = characterAnswerJson;
        CharacterAudioclip = characterAudioclip;
        CharacterTextureImage = characterTextureImage;
    }
}

public class AnswerJson
{
    [JsonProperty("ReplyMessage")]
    public string ReplyMessage { get; set; }

    [JsonProperty("ActingMessage")]
    public string ActingMessage { get; set; }

    [JsonProperty("ImageDescripton")]
    public string ImageDescripton { get; set; }


    [JsonProperty("Price")]
    public string Price { get; set; }

    [JsonProperty("Buy")]
    public string Buy { get; set; }

    public AnswerJson(string replyMessage, string actingMessage, string imageDescripton, string price, string buy)
    {
        ReplyMessage = replyMessage;
        ActingMessage = actingMessage;
        ImageDescripton = imageDescripton;
        Price = price;
        Buy = buy;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented); 
    }

}