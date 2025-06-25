using UnityEngine;
using System.IO;
using System.Collections;
using System.Threading.Tasks; // Task를 사용하기 위해 추가
using UnityEngine.Networking;
using Utilities.Async;

public class ImageDisplayManager : MonoBehaviour // 이름을 ImageGenerator 또는 ComfyImageGenerator 등으로 바꾸는 것을 고려해보세요.
{
    [Header("연결")]
    [SerializeField] private ComfyUIClient comfyUIClient; // ComfyUIClient 참조

    // targetImageUI 필드 제거

    private void Awake()
    {
        if (comfyUIClient == null)
        {
            comfyUIClient = GetComponent<ComfyUIClient>();
            if (comfyUIClient == null)
            {
                Debug.LogError("ComfyUIClient를 씬에서 찾을 수 없습니다. 인스펙터에 할당하거나 씬에 존재하는지 확인하세요.");
            }
        }
    }

    /// <summary>
    /// ComfyUI에 이미지 생성을 요청하고, 생성된 Texture2D를 Task로 반환합니다.
    /// 이 클래스는 UI에 이미지를 직접 표시하지 않습니다.
    /// </summary>
    /// <param name="prompt">생성할 이미지의 프롬프트</param>
    /// <returns>생성된 이미지의 Texture2D를 담은 Task. 실패 시 null Texture2D를 반환.</returns>
    public async Task<Texture2D> GenerateAndGetImageAsync(string prompt)
    {
        if (comfyUIClient == null)
        {
            Debug.LogError("ComfyUIClient가 설정되지 않았습니다. 이미지 생성을 시작할 수 없습니다.");
            return null; // 실패 시 null 반환
        }

        if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogWarning("이미지 프롬프트가 비어 있어 이미지 생성을 건너뜁니다.");
            return null; // 실패 시 null 반환
        }

        Debug.Log($"[ImageDisplayManager] 이미지 생성 요청 중: {prompt}");

        Texture2D generatedTexture = null;
        string imagePath = null;

        // ComfyUIClient.GenerateImageAndWait는 코루틴이므로,
        // 이를 async/await 패턴과 통합하기 위해 WaitUntil을 사용합니다.
        // 또는 ComfyUIClient 자체의 GenerateImageAndWait를 Task 기반으로 변경하는 것이 가장 깔끔합니다.
        // 여기서는 현재 ComfyUIClient의 코루틴 기반 GenerateImageAndWait를 사용합니다.
        bool generationComplete = false;
        StartCoroutine(comfyUIClient.GenerateImageAndWait(prompt, (path) => { // 이 'path'는 string 타입입니다.
            imagePath = path;
            generationComplete = true; // 콜백이 호출되면 완료 플래그 설정
        }));

        await new WaitUntil(() => generationComplete); // 코루틴이 콜백을 호출할 때까지 기다림


        if (!string.IsNullOrEmpty(imagePath))
        {
            Debug.Log($"[ImageDisplayManager] 이미지 생성 완료! 경로: {imagePath}");
            generatedTexture = await LoadTextureFromFileAsync(imagePath);

            // 더 이상 UI에 직접 할당하지 않음
            // if (targetImageUI != null && generatedTexture != null)
            // {
            //     targetImageUI.texture = generatedTexture;
            // }
        }
        else
        {
            Debug.LogError("[ImageDisplayManager] 이미지 생성에 실패했거나 경로를 찾을 수 없습니다.");
        }

        return generatedTexture;
    }

    /// <summary>
    /// 로컬 경로에서 이미지를 비동기적으로 로드하여 Texture2D를 반환합니다.
    /// </summary>
    /// <param name="path">이미지 파일의 전체 경로</param>
    /// <returns>로드된 Texture2D를 담은 Task. 실패 시 null Texture2D 반환.</returns>
    private async Task<Texture2D> LoadTextureFromFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"[ImageDisplayManager] 이미지 파일을 찾을 수 없습니다: {path}");
            return null;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + path))
        {
            // UnityWebRequest.SendWebRequest()는 AsyncOperation을 반환하며,
            // await를 직접 사용할 수 있도록 Task.Yield()를 활용합니다.
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield(); // 다음 프레임까지 대기
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    return texture;
                }
                else
                {
                    Debug.LogError("[ImageDisplayManager] 다운로드한 텍스처가 유효하지 않습니다.");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"[ImageDisplayManager] 이미지 로드 실패: {request.error}");
                return null;
            }
        }
    }
}