using System.Collections.Generic;
using OpenAI;
using OpenAI.Chat;
using UnityEngine;

[CreateAssetMenu(fileName = "World Data", menuName = "Scriptable Object/World Data", order = int.MaxValue)]
public class WorldInformationSO : ScriptableObject
{
   public List<string> Description;

   public List<Message> WorldDescriptionMessages()
   {
      List<Message> WorldDescriptionMessages = new List<Message>();

      foreach (string description in Description)
      {
         WorldDescriptionMessages.Add(new Message(Role.System, description));
      }
      
      return WorldDescriptionMessages;
   }
}
