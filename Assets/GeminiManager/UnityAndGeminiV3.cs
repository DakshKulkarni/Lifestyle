using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using TMPro;
using System.IO; 
using System;

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


public class UnityAndGeminiV3: MonoBehaviour
{
    [Header("JSON API Configuration")]
    public TextAsset jsonApi;

    
    private string apiKey = ""; 
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent"; // Edit it and choose your prefer model
   



    [Header("Prompt Function")]
    public string prompt = "" ;


  


    void Start()
    {
        UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        apiKey = jsonApiKey.key;   
       
        if (prompt != ""){StartCoroutine( SendPromptRequestToGemini(prompt+ "give me the texture feature you got from this sentence, and the subject in the prompt, and write down each in one word, and give me and give me only the 2 word, seperate with commas"));};
        
    }

    private IEnumerator SendPromptRequestToGemini(string promptText)
    {
        string url = $"{apiEndpoint}?key={apiKey}";
     
        string jsonData = "{\"contents\": [{\"parts\": [{\"text\": \"{" + promptText + "}\"}]}]}";

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Create a UnityWebRequest with the JSON data
        using (UnityWebRequest www = new UnityWebRequest(url, "POST")){
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                Debug.Log("Request complete!");
                Response response = JsonUtility.FromJson<Response>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                    {
                        //This is the response to your request
                        string text = response.candidates[0].content.parts[0].text;
                        Debug.Log(text);
                    }
                else
                {
                    Debug.Log("No text found.");
                }
            }
        }
    }

    

    



}


