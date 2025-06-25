using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.Networking; // IEnumerator를 위해 필요

public class ImageDisplayManager : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private ComfyUIClient comfyUIClient; // ComfyUIClient 참조
    [SerializeField] private RawImage targetImageUI;       // 이미지를 표시할 RawImage UI

    [Header("테스트 설정")]
    [SerializeField] private string testPrompt = "a cat, cute, detailed, high quality";
    [SerializeField] private Button generateButton; // 버튼으로 이미지 생성 트리거

    private void Awake()
    {
        // Inspector에서 연결하지 않았을 경우 자동으로 찾기 시도 (권장되지는 않음)
        if (comfyUIClient == null)
        {
            comfyUIClient = FindObjectOfType<ComfyUIClient>();
            if (comfyUIClient == null)
            {
                Debug.LogError("ComfyUIClient를 씬에서 찾을 수 없습니다. 인스펙터에 할당하거나 씬에 존재하는지 확인하세요.");
            }
        }

        // 버튼이 있다면 클릭 이벤트에 연결
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(OnGenerateButtonClick);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 리스너 제거 (메모리 누수 방지)
        if (generateButton != null)
        {
            generateButton.onClick.RemoveListener(OnGenerateButtonClick);
        }
    }

    private void OnGenerateButtonClick()
    {
        // 버튼 클릭 시 이미지 생성 시작
        StartImageGeneration(testPrompt);
    }

    /// <summary>
    /// 이미지 생성을 시작하고 UI에 표시하는 메서드
    /// </summary>
    /// <param name="prompt">생성할 이미지의 프롬프트</param>
    public void StartImageGeneration(string prompt)
    {
        if (comfyUIClient == null)
        {
            Debug.LogError("ComfyUIClient가 설정되지 않았습니다. 이미지 생성을 시작할 수 없습니다.");
            return;
        }

        Debug.Log($"이미지 생성 요청 중: {prompt}");

        // ComfyUIClient의 코루틴 시작
        StartCoroutine(comfyUIClient.GenerateImageAndWait(prompt, OnImageGenerationComplete));
    }

    /// <summary>
    /// 이미지 생성 완료 시 호출될 콜백 메서드
    /// </summary>
    /// <param name="imagePath">생성된 이미지의 로컬 파일 경로 (실패 시 null)</param>
    private void OnImageGenerationComplete(string imagePath)
    {
        if (!string.IsNullOrEmpty(imagePath))
        {
            Debug.Log($"이미지 생성 완료! 경로: {imagePath}");
            // 생성된 이미지를 UI에 로드하여 표시
            LoadAndDisplayImage(imagePath);
        }
        else
        {
            Debug.LogError("이미지 생성에 실패했거나 경로를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 로컬 경로에서 이미지를 로드하여 RawImage에 표시
    /// </summary>
    /// <param name="path">이미지 파일의 전체 경로</param>
    private void LoadAndDisplayImage(string path)
    {
        if (targetImageUI == null)
        {
            Debug.LogWarning("Target RawImage UI가 할당되지 않았습니다.");
            return;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"이미지 파일을 찾을 수 없습니다: {path}");
            return;
        }

        // 파일에서 이미지를 비동기적으로 로드 (코루틴 사용)
        StartCoroutine(LoadTextureFromFile(path));
    }

    private IEnumerator LoadTextureFromFile(string path)
    {
        // UnityWebRequestTexture를 사용하여 파일에서 텍스처를 로드
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + path))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    targetImageUI.texture = texture;
                    // RawImage의 크기를 텍스처에 맞추고 싶다면 추가
                    // targetImageUI.SetNativeSize();
                }
                else
                {
                    Debug.LogError("다운로드한 텍스처가 유효하지 않습니다.");
                }
            }
            else
            {
                Debug.LogError($"이미지 로드 실패: {request.error}");
            }
        }
    }
}