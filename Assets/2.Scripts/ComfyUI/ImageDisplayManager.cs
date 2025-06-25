using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.Networking; // IEnumerator�� ���� �ʿ�

public class ImageDisplayManager : MonoBehaviour
{
    [Header("����")]
    [SerializeField] private ComfyUIClient comfyUIClient; // ComfyUIClient ����
    [SerializeField] private RawImage targetImageUI;       // �̹����� ǥ���� RawImage UI

    [Header("�׽�Ʈ ����")]
    [SerializeField] private string testPrompt = "a cat, cute, detailed, high quality";
    [SerializeField] private Button generateButton; // ��ư���� �̹��� ���� Ʈ����

    private void Awake()
    {
        // Inspector���� �������� �ʾ��� ��� �ڵ����� ã�� �õ� (��������� ����)
        if (comfyUIClient == null)
        {
            comfyUIClient = FindObjectOfType<ComfyUIClient>();
            if (comfyUIClient == null)
            {
                Debug.LogError("ComfyUIClient�� ������ ã�� �� �����ϴ�. �ν����Ϳ� �Ҵ��ϰų� ���� �����ϴ��� Ȯ���ϼ���.");
            }
        }

        // ��ư�� �ִٸ� Ŭ�� �̺�Ʈ�� ����
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(OnGenerateButtonClick);
        }
    }

    private void OnDestroy()
    {
        // �̺�Ʈ ������ ���� (�޸� ���� ����)
        if (generateButton != null)
        {
            generateButton.onClick.RemoveListener(OnGenerateButtonClick);
        }
    }

    private void OnGenerateButtonClick()
    {
        // ��ư Ŭ�� �� �̹��� ���� ����
        StartImageGeneration(testPrompt);
    }

    /// <summary>
    /// �̹��� ������ �����ϰ� UI�� ǥ���ϴ� �޼���
    /// </summary>
    /// <param name="prompt">������ �̹����� ������Ʈ</param>
    public void StartImageGeneration(string prompt)
    {
        if (comfyUIClient == null)
        {
            Debug.LogError("ComfyUIClient�� �������� �ʾҽ��ϴ�. �̹��� ������ ������ �� �����ϴ�.");
            return;
        }

        Debug.Log($"�̹��� ���� ��û ��: {prompt}");

        // ComfyUIClient�� �ڷ�ƾ ����
        StartCoroutine(comfyUIClient.GenerateImageAndWait(prompt, OnImageGenerationComplete));
    }

    /// <summary>
    /// �̹��� ���� �Ϸ� �� ȣ��� �ݹ� �޼���
    /// </summary>
    /// <param name="imagePath">������ �̹����� ���� ���� ��� (���� �� null)</param>
    private void OnImageGenerationComplete(string imagePath)
    {
        if (!string.IsNullOrEmpty(imagePath))
        {
            Debug.Log($"�̹��� ���� �Ϸ�! ���: {imagePath}");
            // ������ �̹����� UI�� �ε��Ͽ� ǥ��
            LoadAndDisplayImage(imagePath);
        }
        else
        {
            Debug.LogError("�̹��� ������ �����߰ų� ��θ� ã�� �� �����ϴ�.");
        }
    }

    /// <summary>
    /// ���� ��ο��� �̹����� �ε��Ͽ� RawImage�� ǥ��
    /// </summary>
    /// <param name="path">�̹��� ������ ��ü ���</param>
    private void LoadAndDisplayImage(string path)
    {
        if (targetImageUI == null)
        {
            Debug.LogWarning("Target RawImage UI�� �Ҵ���� �ʾҽ��ϴ�.");
            return;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"�̹��� ������ ã�� �� �����ϴ�: {path}");
            return;
        }

        // ���Ͽ��� �̹����� �񵿱������� �ε� (�ڷ�ƾ ���)
        StartCoroutine(LoadTextureFromFile(path));
    }

    private IEnumerator LoadTextureFromFile(string path)
    {
        // UnityWebRequestTexture�� ����Ͽ� ���Ͽ��� �ؽ�ó�� �ε�
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + path))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    targetImageUI.texture = texture;
                    // RawImage�� ũ�⸦ �ؽ�ó�� ���߰� �ʹٸ� �߰�
                    // targetImageUI.SetNativeSize();
                }
                else
                {
                    Debug.LogError("�ٿ�ε��� �ؽ�ó�� ��ȿ���� �ʽ��ϴ�.");
                }
            }
            else
            {
                Debug.LogError($"�̹��� �ε� ����: {request.error}");
            }
        }
    }
}