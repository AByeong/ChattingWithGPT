using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;

public class ComfyUIClient : MonoBehaviour
{
    [Header("설정")]
    public string comfyUIUrl = "http://localhost:8188";
    private string workflowFileName = "WorkflowForUnity.json";
    private string outputFolderPath = @"C:\ComfyUI\ComfyUI_windows_portable_nvidia\ComfyUI_windows_portable\ComfyUI\output";

    [Header("디버그")]
    public bool enableDebugLogs = true;

    /// <summary>
    /// 이미지 생성하고 완료까지 기다리는 메인 메서드
    /// </summary>
    public IEnumerator GenerateImageAndWait(string prompt, System.Action<string> onComplete)
    {
        if (enableDebugLogs) Debug.Log($"?? 이미지 생성 시작: {prompt}");

        string promptId = null;
        // 1. 이미지 생성 요청
        yield return GenerateImageRequest(prompt, (id) => {
            promptId = id;
        });

        if (string.IsNullOrEmpty(promptId))
        {
            Debug.LogError("? 이미지 생성 요청 실패");
            onComplete?.Invoke(null);
            yield break;
        }

        // 2. 완료까지 기다리기
        yield return WaitForCompletion(promptId, onComplete);
    }

    /// <summary>
    /// ComfyUI에 이미지 생성 요청
    /// </summary>
    private IEnumerator GenerateImageRequest(string prompt, System.Action<string> onComplete)
    {
        // 워크플로우 파일 로드 및 수정
        string workflowJson = null;

        try
        {
            workflowJson = PrepareWorkflow(prompt);
        }
        catch (Exception e)
        {
            Debug.LogError($"? 워크플로우 준비 실패: {e.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        // ComfyUI로 요청 전송
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

                    if (enableDebugLogs) Debug.Log($"? 요청 성공 - ID: {promptId}");
                    onComplete?.Invoke(promptId);
                }
                catch (Exception e)
                {
                    Debug.LogError($"? 응답 파싱 실패: {e.Message}");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"? 요청 실패: {request.error}\n응답: {request.downloadHandler.text}");
                onComplete?.Invoke(null);
            }
        }
    }

    /// <summary>
    /// 워크플로우 JSON 준비 (업데이트된 워크플로우용)
    /// </summary>
    private string PrepareWorkflow(string prompt)
    {
        string workflowPath = Path.Combine(Application.streamingAssetsPath, workflowFileName);

        if (!File.Exists(workflowPath))
        {
            throw new FileNotFoundException($"워크플로우 파일이 없습니다: {workflowPath}");
        }

        // JSON 로드 및 수정
        string rawJson = File.ReadAllText(workflowPath);
        JObject workflow = JObject.Parse(rawJson);

        // 프롬프트 업데이트 (노드 4 - 긍정 프롬프트)
        UpdatePromptInWorkflow(workflow, prompt);

        // 랜덤 시드 설정 (노드 3 - KSampler)
        UpdateSeedInWorkflow(workflow);

        // 워크플로우 유효성 검사
        ValidateWorkflow(workflow);

        // API 요청 형식으로 래핑
        JObject apiRequest = new JObject
        {
            ["prompt"] = workflow
        };

        string jsonResult = apiRequest.ToString();

        // 디버그용 - 전체 JSON 파일로 저장
        if (enableDebugLogs)
        {
            string debugPath = Path.Combine(Application.persistentDataPath, "debug_workflow.json");
            File.WriteAllText(debugPath, jsonResult);
            Debug.Log($"?? 디버그용 워크플로우 저장됨: {debugPath}");
        }

        return jsonResult;
    }

    /// <summary>
    /// 워크플로우에서 프롬프트 노드 업데이트 (노드 4 - 긍정 프롬프트)
    /// </summary>
    private void UpdatePromptInWorkflow(JObject workflow, string prompt)
    {
        try
        {
            // 노드 4 - 긍정 프롬프트 업데이트
            JToken positiveNode = workflow["4"];
            if (positiveNode != null && positiveNode["inputs"] != null)
            {
                string currentText = positiveNode["inputs"]["text"]?.ToString() ?? "";

                // 기존 프롬프트에서 사용자 입력 프롬프트를 앞에 추가
                if (!string.IsNullOrEmpty(currentText))
                {
                    positiveNode["inputs"]["text"] = currentText + ", " + prompt;
                }
                else
                {
                    positiveNode["inputs"]["text"] = prompt;
                }

                if (enableDebugLogs)
                    Debug.Log($"?? 긍정 프롬프트 업데이트: {positiveNode["inputs"]["text"]}");
            }
            else
            {
                Debug.LogWarning("?? 긍정 프롬프트 노드(4)를 찾을 수 없습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"? 프롬프트 업데이트 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 랜덤 시드 설정 (노드 3 - KSampler)
    /// </summary>
    private void UpdateSeedInWorkflow(JObject workflow)
    {
        try
        {
            JToken samplerNode = workflow["3"];
            if (samplerNode != null && samplerNode["inputs"] != null)
            {
                // 새로운 랜덤 시드 생성
                long newSeed = UnityEngine.Random.Range(1, 999999999);
                samplerNode["inputs"]["seed"] = newSeed;

                if (enableDebugLogs)
                    Debug.Log($"?? 새로운 시드 설정: {newSeed}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"? 시드 업데이트 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 워크플로우 유효성 검사
    /// </summary>
    private void ValidateWorkflow(JObject workflow)
    {
        if (!enableDebugLogs) return;

        Debug.Log("?? 워크플로우 유효성 검사...");

        // 필수 노드들 확인 (노드 8 추가)
        string[] requiredNodes = { "1", "2", "3", "4", "5", "6", "7", "8" };

        foreach (string nodeId in requiredNodes)
        {
            if (workflow[nodeId] == null)
            {
                Debug.LogWarning($"?? 필수 노드 {nodeId}가 없습니다");
            }
            else
            {
                string nodeType = workflow[nodeId]["class_type"]?.ToString() ?? "Unknown";
                Debug.Log($"? 노드 {nodeId}: {nodeType}");
            }
        }

        // 연결 상태 간단 체크
        CheckBasicConnections(workflow);
    }

    /// <summary>
    /// 기본 연결 상태 확인
    /// </summary>
    private void CheckBasicConnections(JObject workflow)
    {
        try
        {
            // KSampler(3) 연결 확인
            JToken samplerNode = workflow["3"];
            if (samplerNode?["inputs"] != null)
            {
                var inputs = samplerNode["inputs"];

                // 모델 연결 확인
                if (inputs["model"] != null)
                    Debug.Log($"KSampler 모델 연결: {inputs["model"]}");

                // 프롬프트 연결 확인
                if (inputs["positive"] != null)
                    Debug.Log($"KSampler 긍정 프롬프트 연결: {inputs["positive"]}");
                if (inputs["negative"] != null)
                    Debug.Log($"KSampler 부정 프롬프트 연결: {inputs["negative"]}");

                // 잠재 이미지 연결 확인 (노드 8)
                if (inputs["latent_image"] != null)
                    Debug.Log($"KSampler 잠재 이미지 연결: {inputs["latent_image"]}");
            }

            // VAEDecode(6) 연결 확인
            JToken vaeNode = workflow["6"];
            if (vaeNode?["inputs"] != null)
            {
                var inputs = vaeNode["inputs"];
                if (inputs["samples"] != null)
                    Debug.Log($"AEDecode 샘플 연결: {inputs["samples"]}");
                if (inputs["vae"] != null)
                    Debug.Log($"VAEDecode VAE 연결: {inputs["vae"]}");
            }

            // EmptyLatentImage(8) 설정 확인
            JToken latentNode = workflow["8"];
            if (latentNode?["inputs"] != null)
            {
                var inputs = latentNode["inputs"];
                Debug.Log($"EmptyLatentImage 크기: {inputs["width"]}x{inputs["height"]}, 배치: {inputs["batch_size"]}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"결 상태 확인 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 이미지 생성 완료까지 대기
    /// </summary>
    private IEnumerator WaitForCompletion(string promptId, System.Action<string> onComplete)
    {
        int maxWaitTime = 120; // 최대 2분 대기
        int checkInterval = 2;  // 2초마다 체크
        int elapsedTime = 0;

        while (elapsedTime < maxWaitTime)
        {
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;

            // 완료 상태 확인
            bool isComplete = false;
            string imagePath = null;

            yield return CheckIfComplete(promptId, (complete, path) => {
                isComplete = complete;
                imagePath = path;
            });

            if (isComplete)
            {
                if (enableDebugLogs) Debug.Log($"?? 이미지 생성 완료: {imagePath}");
                onComplete?.Invoke(imagePath);
                yield break;
            }

            if (enableDebugLogs) Debug.Log($"? 대기 중... ({elapsedTime}/{maxWaitTime}초)");
        }

        // 타임아웃 - 최신 파일 시도
        string latestImage = GetLatestImageFile();
        Debug.LogWarning($"? 타임아웃 - 최신 파일 반환: {latestImage}");
        onComplete?.Invoke(latestImage);
    }

    /// <summary>
    /// 생성 완료 여부 확인
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

                    // 히스토리에 해당 ID가 있으면 완료
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
                    Debug.LogError($"히스토리 파싱 오류: {e.Message}");
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
    /// 히스토리에서 이미지 경로 추출
    /// </summary>
    private string ExtractImagePath(JToken historyEntry, string promptId)
    {
        try
        {
            // outputs 섹션에서 이미지 찾기
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
            Debug.LogError($"이미지 경로 추출 실패: {e.Message}");
        }

        // 추출 실패 시 최신 파일 반환
        return GetLatestImageFile();
    }

    /// <summary>
    /// 출력 폴더에서 가장 최신 이미지 파일 찾기
    /// </summary>
    private string GetLatestImageFile()
    {
        try
        {
            if (!Directory.Exists(outputFolderPath))
            {
                Debug.LogError($"출력 폴더가 존재하지 않습니다: {outputFolderPath}");
                return null;
            }

            string[] imageFiles = Directory.GetFiles(outputFolderPath, "*.png");

            if (imageFiles.Length == 0)
            {
                Debug.LogWarning("출력 폴더에 이미지 파일이 없습니다.");
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
            Debug.LogError($"최신 이미지 파일 찾기 실패: {e.Message}");
            return null;
        }
    }
}