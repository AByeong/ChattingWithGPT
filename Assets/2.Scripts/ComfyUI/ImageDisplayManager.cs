using UnityEngine;
using System.IO;
using System.Collections;
using System.Threading.Tasks; // Task�� ����ϱ� ���� �߰�
using UnityEngine.Networking;
using Utilities.Async;

public class ImageDisplayManager : MonoBehaviour // �̸��� ImageGenerator �Ǵ� ComfyImageGenerator ������ �ٲٴ� ���� ����غ�����.
{
    [Header("����")]
    [SerializeField] private ComfyUIClient comfyUIClient; // ComfyUIClient ����

    // targetImageUI �ʵ� ����

    private void Awake()
    {
        if (comfyUIClient == null)
        {
            comfyUIClient = GetComponent<ComfyUIClient>();
            if (comfyUIClient == null)
            {
                Debug.LogError("ComfyUIClient�� ������ ã�� �� �����ϴ�. �ν����Ϳ� �Ҵ��ϰų� ���� �����ϴ��� Ȯ���ϼ���.");
            }
        }
    }

    /// <summary>
    /// ComfyUI�� �̹��� ������ ��û�ϰ�, ������ Texture2D�� Task�� ��ȯ�մϴ�.
    /// �� Ŭ������ UI�� �̹����� ���� ǥ������ �ʽ��ϴ�.
    /// </summary>
    /// <param name="prompt">������ �̹����� ������Ʈ</param>
    /// <returns>������ �̹����� Texture2D�� ���� Task. ���� �� null Texture2D�� ��ȯ.</returns>
    public async Task<Texture2D> GenerateAndGetImageAsync(string prompt)
    {
        if (comfyUIClient == null)
        {
            Debug.LogError("ComfyUIClient�� �������� �ʾҽ��ϴ�. �̹��� ������ ������ �� �����ϴ�.");
            return null; // ���� �� null ��ȯ
        }

        if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogWarning("�̹��� ������Ʈ�� ��� �־� �̹��� ������ �ǳʶݴϴ�.");
            return null; // ���� �� null ��ȯ
        }

        Debug.Log($"[ImageDisplayManager] �̹��� ���� ��û ��: {prompt}");

        Texture2D generatedTexture = null;
        string imagePath = null;

        // ComfyUIClient.GenerateImageAndWait�� �ڷ�ƾ�̹Ƿ�,
        // �̸� async/await ���ϰ� �����ϱ� ���� WaitUntil�� ����մϴ�.
        // �Ǵ� ComfyUIClient ��ü�� GenerateImageAndWait�� Task ������� �����ϴ� ���� ���� ����մϴ�.
        // ���⼭�� ���� ComfyUIClient�� �ڷ�ƾ ��� GenerateImageAndWait�� ����մϴ�.
        bool generationComplete = false;
        StartCoroutine(comfyUIClient.GenerateImageAndWait(prompt, (path) => { // �� 'path'�� string Ÿ���Դϴ�.
            imagePath = path;
            generationComplete = true; // �ݹ��� ȣ��Ǹ� �Ϸ� �÷��� ����
        }));

        await new WaitUntil(() => generationComplete); // �ڷ�ƾ�� �ݹ��� ȣ���� ������ ��ٸ�


        if (!string.IsNullOrEmpty(imagePath))
        {
            Debug.Log($"[ImageDisplayManager] �̹��� ���� �Ϸ�! ���: {imagePath}");
            generatedTexture = await LoadTextureFromFileAsync(imagePath);

            // �� �̻� UI�� ���� �Ҵ����� ����
            // if (targetImageUI != null && generatedTexture != null)
            // {
            //     targetImageUI.texture = generatedTexture;
            // }
        }
        else
        {
            Debug.LogError("[ImageDisplayManager] �̹��� ������ �����߰ų� ��θ� ã�� �� �����ϴ�.");
        }

        return generatedTexture;
    }

    /// <summary>
    /// ���� ��ο��� �̹����� �񵿱������� �ε��Ͽ� Texture2D�� ��ȯ�մϴ�.
    /// </summary>
    /// <param name="path">�̹��� ������ ��ü ���</param>
    /// <returns>�ε�� Texture2D�� ���� Task. ���� �� null Texture2D ��ȯ.</returns>
    private async Task<Texture2D> LoadTextureFromFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"[ImageDisplayManager] �̹��� ������ ã�� �� �����ϴ�: {path}");
            return null;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + path))
        {
            // UnityWebRequest.SendWebRequest()�� AsyncOperation�� ��ȯ�ϸ�,
            // await�� ���� ����� �� �ֵ��� Task.Yield()�� Ȱ���մϴ�.
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield(); // ���� �����ӱ��� ���
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
                    Debug.LogError("[ImageDisplayManager] �ٿ�ε��� �ؽ�ó�� ��ȿ���� �ʽ��ϴ�.");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"[ImageDisplayManager] �̹��� �ε� ����: {request.error}");
                return null;
            }
        }
    }
}