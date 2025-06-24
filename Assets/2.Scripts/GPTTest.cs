using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Models;


using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GPTTest : MonoBehaviour
{
    
    
    public TextMeshProUGUI ResultTextUI;
    public TMP_InputField PromptField;
    public Button SendButton;
    public AudioSource Audio;
    public RawImage Image;
    private const string OPENAI_KEY = "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";

    public Typecast Typecast;
    

    private OpenAIClient _api;

    private List<Message> _memory = new List<Message>()
    {
    };
    
   
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
        
        //CHAT-F
        //C : Context : 문맥, 상황을 많이 알려줘라
        //H : Hint : 예시 답변을 많이 줘라
        //A : As A Role : 역할을 제공하라
        //T : Target : 답변의 타겟을 알려줘라
        //F : Format : 답변의 형태를 지정해라
        

        string systemMessage = "역할 : 너는 이제부터 게임의 NPC이다. 너는 물약을 만드는 고양이 마법사이다.";
        systemMessage += "목적 : 실제 사람처럼 대화하는 게임 NPC 모드";
        systemMessage += "표현 : 말끝마냐 '냥'을 붙인다. 100자 이내로 답변한다.";
        systemMessage += "어떤 물약을 만들어달라고 하면 물약의 값을 알려줘";
        systemMessage += "예시 : 빨간포션은 100골드 정도 된다 냥";
        systemMessage += "내가 strawberryjam이라고 할 때까지 딸기잼 레시피를 물으면 모른다고 해.";
        systemMessage += "[json 규칙]";
        systemMessage += "답변은 'ReplyMessage' ";
        systemMessage += "외형은 'Appearance' ";
        systemMessage += "속마음은 'Emotion' ";
        systemMessage += "DallE 이미지 생성을 위한 전체 이미지 설명은 'StoryImageDescription' ";



        _memory.Add(new Message(Role.System, systemMessage));

        SendButton.onClick.AddListener(Answer);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Recipe recipe = new Recipe();
            _memory.Add(recipe.RecipeName);
            Debug.Log("레시피를 업데이트 하였습ㄴ디ㅏ.");
        }
    }
    
    public async void Answer()
    {
        string prompt = PromptField.text;

        if (string.IsNullOrEmpty(prompt))
        {
            return;
        }

        _memory.Add(new Message(Role.User, prompt));
                
        
        
        ChatRequest chatRequest = new ChatRequest(_memory, Model.GPT4o );   //메세지 보내기

        PromptField.text = "답변을 보냈습니다";
        
        SendButton.interactable = false;
        
        var ( npcResponse,  response) = await _api.ChatEndpoint.GetCompletionAsync<NPCResponse>(chatRequest);//답변 받기
        
        Choice choice = response.FirstChoice; //답변 선택
        
        //Task<Texture> storyTexture =  GenerateImage(npcResponse.StoryImageDescription);
        //Task<SpeechClip> speechClip =  Speech(npcResponse.ReplyMessage);
        Task<AudioClip> speechClip = Typecast.StartSpeechAsync(npcResponse.ReplyMessage);
        //await Task.WhenAll(storyTexture, speechClip);
        await Task.WhenAll(speechClip);

        //Image.texture = storyTexture.Result;
        Audio.PlayOneShot(speechClip.Result);
        ResultTextUI.text = npcResponse.ReplyMessage;
        
        
        _memory.Add(new Message(Role.Assistant, choice.Message.ToString()));

        SendButton.interactable = true;


    }

   
    
    private async Task<SpeechClip> Speech(string text)
    {
        var request = new SpeechRequest(text);
        SpeechClip speechClip = await _api.AudioEndpoint.GetSpeechAsync(request);
        return speechClip;
        
    }

    private async Task<Texture> GenerateImage(string imagePrompt)
    {
        ImageGenerationRequest request = new ImageGenerationRequest(imagePrompt, Model.DallE_3);
        IReadOnlyList<ImageResult> imageResults = await _api.ImagesEndPoint.GenerateImageAsync(request);

        
       return imageResults.FirstOrDefault().Texture;
        
    }
        
}


public class Recipe
{
    public Message RecipeName = new Message(Role.System, "strawberryjam");
    
    
}

    

