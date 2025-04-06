// AutoVoiceToGemini.cs
// Converts speech to text using Meta Voice SDK and sends it to Gemini API using B button toggle
// Instantiates object from Meshy assets folder based on recognized object name
// Enables object scaling via right joystick when grabbed
// Also selects best matching material based on a material name list, applied only after prompt

using System.Collections;
using UnityEngine;
using Meta.WitAi.Dictation;
using Oculus.Voice.Dictation;
using TMPro;
using UnityEngine.Networking;
using System;
using System.Text;
using Meta.WitAi.Dictation.Data;

[System.Serializable]
public class UnityAndGeminiKey
{
    public string key;
}

[System.Serializable]
public class Response
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content;
}

[System.Serializable]
public class Content
{
    public string role;
    public Part[] parts;
}

[System.Serializable]
public class Part
{
    public string text;
    public InlineData inlineData;
}

[System.Serializable]
public class InlineData
{
    public string mimeType;
    public string data;
}

public class AutoVoiceToGemini : MonoBehaviour
{
    [Header("Meta Voice SDK")]
    public AppDictationExperience dictationExperience;

    [Header("Gemini JSON API")]
    public TextAsset jsonApi;

    [Header("Debug Prompt")]
    public string lastPromptText = "";

    [Header("Meshy Props")]
    public GameObject[] meshPrefabs; // Match names like "chair", "bookshelf", etc.

    [Header("Material Options")]
    public Material[] availableMaterials; // Assign your materials like "leather", "wood", etc.

    private string apiKey;
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private bool isRecording = false;
    private GameObject lastSpawnedObject;
    private GameObject grabbedObject;

    private void Start()
    {
        UnityAndGeminiKey parsed = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        apiKey = parsed.key;
        dictationExperience.DictationEvents.OnFullTranscription.AddListener(OnDictationResult);
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two)) // B button
        {
            if (!isRecording)
            {
                Debug.Log("[VoiceGemini] Starting voice recording...");
                dictationExperience.Activate();
                isRecording = true;
            }
            else
            {
                Debug.Log("[VoiceGemini] Stopping voice recording...");
                dictationExperience.Deactivate();
                isRecording = false;
            }
        }

        // Find currently grabbed object
        foreach (OVRGrabbable grab in FindObjectsOfType<OVRGrabbable>())
        {
            if (grab.isGrabbed)
            {
                grabbedObject = grab.gameObject;
                break;
            }
        }

        // Scale object using joystick only when grabbed
        if (grabbedObject != null)
        {
            float joystickY = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
            if (Mathf.Abs(joystickY) > 0.1f)
            {
                Vector3 currentScale = grabbedObject.transform.localScale;
                float scaleFactor = 1 + joystickY * Time.deltaTime * 2f;
                grabbedObject.transform.localScale = currentScale * scaleFactor;
            }
        }
    }

    private void OnDictationResult(string finalText)
    {
        Debug.Log("[VoiceGemini] Final transcription: " + finalText);
        lastPromptText = finalText;

        // Try matching a prefab name
        foreach (GameObject prefab in meshPrefabs)
        {
            if (finalText.ToLower().Contains(prefab.name.ToLower()))
            {
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
                Quaternion spawnRot = Quaternion.Euler(-90f, 0f, 0f);
                lastSpawnedObject = Instantiate(prefab, spawnPos, spawnRot);
                Debug.Log("[VoiceGemini] Spawned object: " + prefab.name);
                return; // Exit here; no texture assignment
            }
        }

        // If it's not an object name, treat it as a prompt for material
        if (lastSpawnedObject != null)
        {
            string prompt = finalText + " give me the texture feature you got from this sentence, and the subject in the prompt, and write down each in one word, and give me only the 2 words, separated with commas";
            StartCoroutine(SendPromptRequestToGemini(prompt));
        }
    }

    private IEnumerator SendPromptRequestToGemini(string promptText)
    {
        string url = $"{apiEndpoint}?key={apiKey}";
        string jsonData = "{\"contents\": [{\"parts\": [{\"text\": \"" + promptText + "\"}]}]}";

        byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[VoiceGemini] Error: " + www.error);
            }
            else
            {
                Debug.Log("[VoiceGemini] Gemini response received.");
                Response response = JsonUtility.FromJson<Response>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                {
                    string result = response.candidates[0].content.parts[0].text;
                    Debug.Log("[VoiceGemini] Gemini extracted: " + result);

                    string[] words = result.Split(',');
                    if (words.Length > 1)
                    {
                        string textureWord = words[0].Trim().ToLower();

                        foreach (Material mat in availableMaterials)
                        {
                            if (mat.name.ToLower().Contains(textureWord))
                            {
                                Renderer rend = lastSpawnedObject.GetComponent<Renderer>();
                                if (rend != null)
                                {
                                    rend.material = mat;
                                    Debug.Log("[VoiceGemini] Applied material: " + mat.name);
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("[VoiceGemini] Gemini returned no text.");
                }
            }
        }
    }
}
