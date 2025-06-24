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
    private const string OPENAI_KEY = "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";
    private List<Message> _memory = new List<Message>();

    [Header("인풋")]
    [SerializeField] private AudioSource Audio;
    public RawImage CharacterImage;
    [SerializeField] private Typecast Typecast;
    
    [Header("아웃풋")]
    public Output OutputAnswer = new Output(new AnswerJson("","",""));
    
    [Header("데이터")]
    public WorldInformationSO WorldInformationSO;

    private void Awake()
    {
        _api = new OpenAIClient(OPENAI_KEY);                                   //API 클라이언트 초기화 -> GPT에 접속
        if (Audio == null)
        {
            Audio = GetComponent<AudioSource>();
        }

        if (Typecast == null)
        {
            Typecast = GetComponent<Typecast>();
        }
    }

    private void Start()
    {
        //세계관에 대한 내용을 추가한다.
        if (WorldInformationSO != null)
        {
            _memory.AddRange(WorldInformationSO.WorldDescriptionMessages());
        }
    }

    
    //Output을 만든다
    public async Task MakeOutput(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            return;
        }
        
        //내가 한 말을 기억한다.
        _memory.Add(new Message(Role.User, prompt));

        ChatRequest chatRequest = new ChatRequest(_memory, Model.GPT4o );   //메세지 보내기

        var ( answerJson,  response) = await _api.ChatEndpoint.GetCompletionAsync<AnswerJson>(chatRequest);//답변 받기
        Choice choice = response.FirstChoice; //답변 선택

        
        //타입캐스트를 활용하여 오디오를 만든다
         Task<AudioClip> speechClip = Typecast.StartSpeechAsync(answerJson.ReplyMessage);
         await Task.WhenAll(speechClip);

         
         //출력
        OutputAnswer.CharacterAnswerJson = answerJson;
        OutputAnswer.CharacterAudioclip = speechClip.Result;
         
         _memory.Add(new Message(Role.Assistant, choice.Message.ToString()));
    }
    
    
    
    
}

public class Output
{
    public AnswerJson CharacterAnswerJson { get; set; }
    public AudioClip CharacterAudioclip { get; set; }
    public Texture CharacterTextureImage { get; set; }

    public Output(AnswerJson characterAnswerJson, AudioClip characterAudioclip = null, Texture characterTextureImage = null)
    {
        characterAnswerJson = characterAnswerJson;
        CharacterAudioclip = characterAudioclip;
        CharacterTextureImage = characterTextureImage;
    }
}

public class AnswerJson
{
    [JsonProperty("ReplyMessage")]
    public string ReplyMessage{get; set;}
    
    [JsonProperty("ActingMessage")]
    public string ActingMessage{get; set;}
    
    [JsonProperty("ImageDescripton")]
    public string ImageDescripton{get; set;}

    public AnswerJson(string replyMessage, string actingMessage, string imageDescripton)
    {
        ReplyMessage = replyMessage;
        ActingMessage = actingMessage;
        ImageDescripton = imageDescripton;
    }
    
}
