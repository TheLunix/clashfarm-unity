using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    private string NickName, SerialCode;
    private const string NickKey = "Name";
    private const string CodeKey = "SerialCode";
    private const string ApiUrl = "https://api.clashfarm.com/api/player/check";

    void Start()
    {
        NickName = PlayerPrefs.GetString(NickKey, "");
        SerialCode = PlayerPrefs.GetString(CodeKey, "");

        if (!string.IsNullOrEmpty(NickName) && !string.IsNullOrEmpty(SerialCode))
        {
            Debug.Log("Починається перевірка акаунта...");
            StartCoroutine(CheckAccountAndLoad());
        }
        else
        {
            Debug.Log("Інформація про гравця відсутня. Перенаправлення на реєстрацію...");
            SceneManager.LoadScene("first_auth"); // або зміни на свою сцену реєстрації
        }
    }

    private IEnumerator CheckAccountAndLoad()
    {
        WWWForm form = new WWWForm();
        form.AddField("PlayerName", NickName);
        form.AddField("PlayerSerialCode", SerialCode);

        using UnityWebRequest www = UnityWebRequest.Post(ApiUrl, form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Помилка з'єднання: " + www.error);
            yield break;
        }

        string response = www.downloadHandler.text.Trim();
        Debug.Log("Відповідь сервера: " + response);

        switch (response)
        {
            case "0":
                Debug.Log("Гравець знайдений. Завантаження...");
                SceneManager.LoadScene("main"); // зміни на свою назву
                break;
            case "1":
                Debug.LogWarning("Гравець не знайдений. Перенаправлення на реєстрацію.");
                SceneManager.LoadScene("first_auth"); // або твоя сцена
                break;
            default:
                Debug.LogError("Невідома відповідь від сервера.");
                break;
        }
    }
}
