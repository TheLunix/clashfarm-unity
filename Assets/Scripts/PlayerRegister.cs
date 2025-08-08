using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class AutoRegisterResponse
{
    public int id;
    public string nickname;
    public string serialCode;
    public int playerFraction;
}

public class PlayerRegister : MonoBehaviour
{
    private const string AutoRegisterUrl = "https://api.clashfarm.com/api/player/autoregister";

    // Виклич цю функцію на кнопці "Забрати приз / Продовжити"
    public void OnFinishTraining()
    {
        StartCoroutine(AutoRegister());
    }

    private IEnumerator AutoRegister()
    {
        using UnityWebRequest www = UnityWebRequest.Post(AutoRegisterUrl, new WWWForm());
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("AutoRegister error: " + www.error);
            yield break;
        }

        var json = www.downloadHandler.text;
        Debug.Log("AutoRegister response: " + json);

        AutoRegisterResponse data = null;
        try
        {
            data = JsonUtility.FromJson<AutoRegisterResponse>(json);
        }
        catch
        {
            Debug.LogError("Bad JSON from server: " + json);
        }

        if (data == null || string.IsNullOrEmpty(data.serialCode) || string.IsNullOrEmpty(data.nickname))
        {
            Debug.Log("Помилка реєстрації. Спробуй ще раз.");
            yield break;
        }

        PlayerPrefs.SetString("Name", data.nickname);
        PlayerPrefs.SetString("SerialCode", data.serialCode);
        PlayerPrefs.SetInt("Fraction", data.playerFraction);
        PlayerPrefs.SetInt("ID", data.id);
        PlayerPrefs.Save();

        // Перехід у головне меню/сцену
        SceneManager.LoadScene("main"); // ← заміни на свою назву
    }

}
