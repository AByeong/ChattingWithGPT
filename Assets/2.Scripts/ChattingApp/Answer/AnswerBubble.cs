using System;
using TMPro;
using UnityEngine;

public class AnswerBubble : MonoBehaviour
{
    public TextMeshProUGUI Dial_Text;

    public void Set(string dial)
    {
        if (string.IsNullOrEmpty(dial))
        {
            throw new Exception("name and dial are required");
        }
        
        Dial_Text.text = dial;
    }
}
