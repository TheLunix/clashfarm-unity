using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class PlayerConnect : MonoBehaviour
{
    private string NickName, SerialCode;
    private const string NickKey = "Name";
    private const string CodeKey = "SerialCode";
    private const string ApiUrl = "https://api.clashfarm.com/api/player/check";

    void Start()
    {
        NickName = PlayerPrefs.GetString(NickKey, "");
        SerialCode = PlayerPrefs.GetString(CodeKey, "");
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (!string.IsNullOrEmpty(NickName) && !string.IsNullOrEmpty(SerialCode))
        {
            Debug.Log($"Знайдено гравця у PlayerPrefs: {NickName}, Serial: {SerialCode}");
            //StartCoroutine(ValidateAccount());
        }
        else
        {
            Debug.LogWarning("Гравець не знайдений у PlayerPrefs");
            SceneManager.LoadScene("first_auth");
        }
    }

    private IEnumerator ValidateAccount()
    {
        WWWForm form = new WWWForm();
        form.AddField("PlayerName", NickName);
        form.AddField("PlayerSerialCode", SerialCode);

        using UnityWebRequest www = UnityWebRequest.Post(ApiUrl, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Помилка підключення: " + www.error);
        }
        else
        {
            Debug.Log("Відповідь сервера: " + www.downloadHandler.text);

            switch (www.downloadHandler.text.Trim())
            {
                case "0":
                    Debug.Log("Гравець підтверджений. Завантаження сцени...");
                    SceneManager.LoadScene(1); // або твоя назва сцени
                    break;
                case "1":
                    Debug.LogWarning("Гравець не знайдений у базі.");
                    break;
                default:
                    Debug.LogWarning("Невідома відповідь від сервера.");
                    break;
            }
        }
    }
}
