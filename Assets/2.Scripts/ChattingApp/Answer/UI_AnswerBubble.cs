using TMPro;
using UnityEngine;

public class UI_AnswerBubble : MonoBehaviour
{
    public AnswerBubble AnswerBubble;
    [SerializeField] private float maxWidth = 300f;  // 최대 너비 제한
    public TextMeshProUGUI messageText;  // 동적 변경할 텍스트
    public RectTransform background;     // 배경 (Panel)
    
    private Vector2 padding = new Vector2(20f, 40f); // 좌우 여백 (X: 좌우, Y: 상하)
    private void Start()
    {
        AnswerBubble.OnAppear += AppearAnimation;
    
    
   

    

        maxWidth = messageText.gameObject.GetComponent<RectTransform>().sizeDelta.x;
        // 텍스트 크기 가져오기
        Vector2 textSize = messageText.GetPreferredValues();

        // 배경 크기 조정 (텍스트 크기 + 패딩 적용)
        float width = Mathf.Min(textSize.x, maxWidth) + 50f;
        background.sizeDelta = new Vector2(width, textSize.y + padding.y);
    }
    

    private void AppearAnimation()
    {
        
    }
}
