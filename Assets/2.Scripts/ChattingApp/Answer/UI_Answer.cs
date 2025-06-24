using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UI_Answer : MonoBehaviour
{
    
    [Header("채팅창")]
   public Transform BubbleParent;
   public GameObject PlayerBubble;
   public GameObject CharacterBubble;
   public TMP_InputField InputText;
   public Button sendButton;
   
   
   [Header("데이터")]
   public AnswerMaker AnswerMaker;
   private AnswerJson _answerJson;
   [FormerlySerializedAs("WorldInformation")] public WorldInformationSO worldInformationSo;
    
   
   [Header("오디오")]
   public AudioSource AudioSource;
   
   public void MakeOutput()
   {
       GetOutputAnswer(InputText.text);
   }
   
   private async void GetOutputAnswer(string prompt)
   {
       await AnswerMaker.MakeOutput(prompt);
       MakePlayerBubble(prompt);
       MakeCharacterBubble(AnswerMaker.OutputAnswer.CharacterAnswerJson);
       SpeakDial(AnswerMaker.OutputAnswer.CharacterAudioclip);
   }
   
   public void MakePlayerBubble(string myDial)
   {
      GameObject playerBubble = Instantiate(PlayerBubble);
      playerBubble.transform.SetParent(BubbleParent);
      playerBubble.GetComponent<AnswerBubble>().Set(myDial);
   }

   public void MakeCharacterBubble(AnswerJson answerJson)
   {
       if (answerJson == null)
       {
           throw new Exception("answerJson is null");
       }
       
       GameObject characterBubble = Instantiate(CharacterBubble);
       characterBubble.transform.SetParent(BubbleParent);
       characterBubble.GetComponent<AnswerBubble>().Set(answerJson.ReplyMessage);
   }

   public void ChangeImage()
   {
      
   }

   public void SpeakDial(AudioClip audioClip)
   {
      AudioSource.PlayOneShot(audioClip);
   }
}
