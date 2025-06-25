// AnswerBubble.cs

using System;
using UnityEngine;

public class AnswerBubble : MonoBehaviour
{
    [Tooltip("타자 효과를 사용할지 여부")]
    public bool UseTypewriterEffect = true;
    
    [Tooltip("타자 효과의 속도 (1초당 글자 수)")]
    public float CharactersPerSecond = 20f;

    // UI를 갱신하라는 신호를 모든 데이터와 함께 보냅니다.
    public event Action<string, bool, float> OnSet;

    /// <summary>
    /// 새로운 메시지를 설정하고 UI 갱신 이벤트를 발생시킵니다.
    /// </summary>
    public void Set(string message)
    {
        // UI_AnswerBubble에게 메시지, 타자 효과 사용 여부, 속도 정보를 전달합니다.
        OnSet?.Invoke(message, UseTypewriterEffect, CharactersPerSecond);
    }
}