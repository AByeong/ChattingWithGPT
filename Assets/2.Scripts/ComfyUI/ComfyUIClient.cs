using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;

public class ComfyUIClient : MonoBehaviour
{
    [Header("����")]
    public string comfyUIUrl = "http://localhost:8188";
    private string workflowFileName = "WorkflowForUnity.json";
    private string outputFolderPath = @"C:\ComfyUI\ComfyUI_windows_portable_nvidia\ComfyUI_windows_portable\ComfyUI\output";

    [Header("�����")]
    public bool enableDebugLogs = true;

    /// <summary>
    /// �̹��� �����ϰ� �Ϸ���� ��ٸ��� ���� �޼���
    /// </summary>
    public IEnumerator GenerateImageAndWait(string prompt, System.Action<string> onComplete)
    {
        if (enableDebugLogs) Debug.Log($"?? �̹��� ���� ����: {prompt}");

        string promptId = null;
        // 1. �̹��� ���� ��û
        yield return GenerateImageRequest(prompt, (id) => {
            promptId = id;
        });

        if (string.IsNullOrEmpty(promptId))
        {
            Debug.LogError("? �̹��� ���� ��û ����");
            onComplete?.Invoke(null);
            yield break;
        }

        // 2. �Ϸ���� ��ٸ���
        yield return WaitForCompletion(promptId, onComplete);
    }

    /// <summary>
    /// ComfyUI�� �̹��� ���� ��û
    /// </summary>
    private IEnumerator GenerateImageRequest(string prompt, System.Action<string> onComplete)
    {
        // ��ũ�÷ο� ���� �ε� �� ����
        string workflowJson = null;

        try
        {
            workflowJson = PrepareWorkflow(prompt);
        }
        catch (Exception e)
        {
            Debug.LogError($"? ��ũ�÷ο� �غ� ����: {e.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        // ComfyUI�� ��û ����
        using (UnityWebRequest request = new UnityWebRequest(comfyUIUrl + "/prompt", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(workflowJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    JObject response = JObject.Parse(request.downloadHandler.text);
                    string promptId = response["prompt_id"]?.ToString();

                    if (enableDebugLogs) Debug.Log($"? ��û ���� - ID: {promptId}");
                    onComplete?.Invoke(promptId);
                }
                catch (Exception e)
                {
                    Debug.LogError($"? ���� �Ľ� ����: {e.Message}");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"? ��û ����: {request.error}\n����: {request.downloadHandler.text}");
                onComplete?.Invoke(null);
            }
        }
    }

    /// <summary>
    /// ��ũ�÷ο� JSON �غ� (������Ʈ�� ��ũ�÷ο��)
    /// </summary>
    private string PrepareWorkflow(string prompt)
    {
        string workflowPath = Path.Combine(Application.streamingAssetsPath, workflowFileName);

        if (!File.Exists(workflowPath))
        {
            throw new FileNotFoundException($"��ũ�÷ο� ������ �����ϴ�: {workflowPath}");
        }

        // JSON �ε� �� ����
        string rawJson = File.ReadAllText(workflowPath);
        JObject workflow = JObject.Parse(rawJson);

        // ������Ʈ ������Ʈ (��� 4 - ���� ������Ʈ)
        UpdatePromptInWorkflow(workflow, prompt);

        // ���� �õ� ���� (��� 3 - KSampler)
        UpdateSeedInWorkflow(workflow);

        // ��ũ�÷ο� ��ȿ�� �˻�
        ValidateWorkflow(workflow);

        // API ��û �������� ����
        JObject apiRequest = new JObject
        {
            ["prompt"] = workflow
        };

        string jsonResult = apiRequest.ToString();

        // ����׿� - ��ü JSON ���Ϸ� ����
        if (enableDebugLogs)
        {
            string debugPath = Path.Combine(Application.persistentDataPath, "debug_workflow.json");
            File.WriteAllText(debugPath, jsonResult);
            Debug.Log($"?? ����׿� ��ũ�÷ο� �����: {debugPath}");
        }

        return jsonResult;
    }

    /// <summary>
    /// ��ũ�÷ο쿡�� ������Ʈ ��� ������Ʈ (��� 4 - ���� ������Ʈ)
    /// </summary>
    private void UpdatePromptInWorkflow(JObject workflow, string prompt)
    {
        try
        {
            // ��� 4 - ���� ������Ʈ ������Ʈ
            JToken positiveNode = workflow["4"];
            if (positiveNode != null && positiveNode["inputs"] != null)
            {
                string currentText = positiveNode["inputs"]["text"]?.ToString() ?? "";

                // ���� ������Ʈ���� ����� �Է� ������Ʈ�� �տ� �߰�
                if (!string.IsNullOrEmpty(currentText))
                {
                    positiveNode["inputs"]["text"] = currentText + ", " + prompt;
                }
                else
                {
                    positiveNode["inputs"]["text"] = prompt;
                }

                if (enableDebugLogs)
                    Debug.Log($"?? ���� ������Ʈ ������Ʈ: {positiveNode["inputs"]["text"]}");
            }
            else
            {
                Debug.LogWarning("?? ���� ������Ʈ ���(4)�� ã�� �� �����ϴ�.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"? ������Ʈ ������Ʈ ����: {e.Message}");
        }
    }

    /// <summary>
    /// ���� �õ� ���� (��� 3 - KSampler)
    /// </summary>
    private void UpdateSeedInWorkflow(JObject workflow)
    {
        try
        {
            JToken samplerNode = workflow["3"];
            if (samplerNode != null && samplerNode["inputs"] != null)
            {
                // ���ο� ���� �õ� ����
                long newSeed = UnityEngine.Random.Range(1, 999999999);
                samplerNode["inputs"]["seed"] = newSeed;

                if (enableDebugLogs)
                    Debug.Log($"?? ���ο� �õ� ����: {newSeed}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"? �õ� ������Ʈ ����: {e.Message}");
        }
    }

    /// <summary>
    /// ��ũ�÷ο� ��ȿ�� �˻�
    /// </summary>
    private void ValidateWorkflow(JObject workflow)
    {
        if (!enableDebugLogs) return;

        Debug.Log("?? ��ũ�÷ο� ��ȿ�� �˻�...");

        // �ʼ� ���� Ȯ�� (��� 8 �߰�)
        string[] requiredNodes = { "1", "2", "3", "4", "5", "6", "7", "8" };

        foreach (string nodeId in requiredNodes)
        {
            if (workflow[nodeId] == null)
            {
                Debug.LogWarning($"?? �ʼ� ��� {nodeId}�� �����ϴ�");
            }
            else
            {
                string nodeType = workflow[nodeId]["class_type"]?.ToString() ?? "Unknown";
                Debug.Log($"? ��� {nodeId}: {nodeType}");
            }
        }

        // ���� ���� ���� üũ
        CheckBasicConnections(workflow);
    }

    /// <summary>
    /// �⺻ ���� ���� Ȯ��
    /// </summary>
    private void CheckBasicConnections(JObject workflow)
    {
        try
        {
            // KSampler(3) ���� Ȯ��
            JToken samplerNode = workflow["3"];
            if (samplerNode?["inputs"] != null)
            {
                var inputs = samplerNode["inputs"];

                // �� ���� Ȯ��
                if (inputs["model"] != null)
                    Debug.Log($"KSampler �� ����: {inputs["model"]}");

                // ������Ʈ ���� Ȯ��
                if (inputs["positive"] != null)
                    Debug.Log($"KSampler ���� ������Ʈ ����: {inputs["positive"]}");
                if (inputs["negative"] != null)
                    Debug.Log($"KSampler ���� ������Ʈ ����: {inputs["negative"]}");

                // ���� �̹��� ���� Ȯ�� (��� 8)
                if (inputs["latent_image"] != null)
                    Debug.Log($"KSampler ���� �̹��� ����: {inputs["latent_image"]}");
            }

            // VAEDecode(6) ���� Ȯ��
            JToken vaeNode = workflow["6"];
            if (vaeNode?["inputs"] != null)
            {
                var inputs = vaeNode["inputs"];
                if (inputs["samples"] != null)
                    Debug.Log($"AEDecode ���� ����: {inputs["samples"]}");
                if (inputs["vae"] != null)
                    Debug.Log($"VAEDecode VAE ����: {inputs["vae"]}");
            }

            // EmptyLatentImage(8) ���� Ȯ��
            JToken latentNode = workflow["8"];
            if (latentNode?["inputs"] != null)
            {
                var inputs = latentNode["inputs"];
                Debug.Log($"EmptyLatentImage ũ��: {inputs["width"]}x{inputs["height"]}, ��ġ: {inputs["batch_size"]}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"�� ���� Ȯ�� �� ����: {e.Message}");
        }
    }

    /// <summary>
    /// �̹��� ���� �Ϸ���� ���
    /// </summary>
    private IEnumerator WaitForCompletion(string promptId, System.Action<string> onComplete)
    {
        int maxWaitTime = 120; // �ִ� 2�� ���
        int checkInterval = 2;  // 2�ʸ��� üũ
        int elapsedTime = 0;

        while (elapsedTime < maxWaitTime)
        {
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;

            // �Ϸ� ���� Ȯ��
            bool isComplete = false;
            string imagePath = null;

            yield return CheckIfComplete(promptId, (complete, path) => {
                isComplete = complete;
                imagePath = path;
            });

            if (isComplete)
            {
                if (enableDebugLogs) Debug.Log($"?? �̹��� ���� �Ϸ�: {imagePath}");
                onComplete?.Invoke(imagePath);
                yield break;
            }

            if (enableDebugLogs) Debug.Log($"? ��� ��... ({elapsedTime}/{maxWaitTime}��)");
        }

        // Ÿ�Ӿƿ� - �ֽ� ���� �õ�
        string latestImage = GetLatestImageFile();
        Debug.LogWarning($"? Ÿ�Ӿƿ� - �ֽ� ���� ��ȯ: {latestImage}");
        onComplete?.Invoke(latestImage);
    }

    /// <summary>
    /// ���� �Ϸ� ���� Ȯ��
    /// </summary>
    private IEnumerator CheckIfComplete(string promptId, System.Action<bool, string> onComplete)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{comfyUIUrl}/history/{promptId}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    JObject history = JObject.Parse(request.downloadHandler.text);

                    // �����丮�� �ش� ID�� ������ �Ϸ�
                    if (history.ContainsKey(promptId))
                    {
                        string imagePath = ExtractImagePath(history[promptId], promptId);
                        onComplete?.Invoke(true, imagePath);
                    }
                    else
                    {
                        onComplete?.Invoke(false, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"�����丮 �Ľ� ����: {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                onComplete?.Invoke(false, null);
            }
        }
    }

    /// <summary>
    /// �����丮���� �̹��� ��� ����
    /// </summary>
    private string ExtractImagePath(JToken historyEntry, string promptId)
    {
        try
        {
            // outputs ���ǿ��� �̹��� ã��
            JToken outputs = historyEntry["outputs"];
            if (outputs != null)
            {
                foreach (JProperty outputNode in outputs)
                {
                    JToken images = outputNode.Value["images"];
                    if (images != null && images.HasValues)
                    {
                        foreach (JToken image in images)
                        {
                            string fileName = image["filename"]?.ToString();
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                string fullPath = Path.Combine(outputFolderPath, fileName);
                                if (File.Exists(fullPath))
                                {
                                    return fullPath;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"�̹��� ��� ���� ����: {e.Message}");
        }

        // ���� ���� �� �ֽ� ���� ��ȯ
        return GetLatestImageFile();
    }

    /// <summary>
    /// ��� �������� ���� �ֽ� �̹��� ���� ã��
    /// </summary>
    private string GetLatestImageFile()
    {
        try
        {
            if (!Directory.Exists(outputFolderPath))
            {
                Debug.LogError($"��� ������ �������� �ʽ��ϴ�: {outputFolderPath}");
                return null;
            }

            string[] imageFiles = Directory.GetFiles(outputFolderPath, "*.png");

            if (imageFiles.Length == 0)
            {
                Debug.LogWarning("��� ������ �̹��� ������ �����ϴ�.");
                return null;
            }

            string latestFile = imageFiles[0];
            DateTime latestTime = File.GetLastWriteTime(latestFile);

            foreach (string file in imageFiles)
            {
                DateTime fileTime = File.GetLastWriteTime(file);
                if (fileTime > latestTime)
                {
                    latestFile = file;
                    latestTime = fileTime;
                }
            }

            return latestFile;
        }
        catch (Exception e)
        {
            Debug.LogError($"�ֽ� �̹��� ���� ã�� ����: {e.Message}");
            return null;
        }
    }
}