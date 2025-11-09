using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class AutoRegisterResponse
{
    public int id;
    public string nickname;
    // ВАЖЛИВО: імена полів 1-в-1 як у JSON від бекенду
    public string serialcode;
    public int playerfraction;
}

public class PlayerRegister : MonoBehaviour
{
    private const string AutoRegisterUrl = "https://api.clashfarm.com/api/player/autoregister";
    private static bool _busy;

    // Виклич цю функцію на кнопці "Забрати приз / Продовжити"
    public void OnFinishTraining()
    {
        if (_busy) return; // анти-даблклік
        StartCoroutine(AutoRegister());
    }

    private IEnumerator AutoRegister()
    {
        _busy = true;

        using UnityWebRequest www = UnityWebRequest.Post(AutoRegisterUrl, new WWWForm());
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("AutoRegister error: " + www.error + "\nBODY: " + (www.downloadHandler?.text ?? ""));
            _busy = false;
            yield break;
        }

        var json = www.downloadHandler.text;
        Debug.Log("AutoRegister response: " + json);

        AutoRegisterResponse data = null;
        try
        {
            data = JsonUtility.FromJson<AutoRegisterResponse>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Bad JSON from server: " + e.Message + "\n" + json);
            _busy = false;
            yield break;
        }

        // перевіряємо саме ті ключі, які реально приходять
        if (data == null || string.IsNullOrEmpty(data.serialcode) || string.IsNullOrEmpty(data.nickname))
        {
            Debug.LogError("Помилка реєстрації: відсутні обов'язкові поля у відповіді.");
            _busy = false;
            yield break;
        }

        PlayerPrefs.SetString("Name", data.nickname);
        PlayerPrefs.SetString("SerialCode", data.serialcode);
        PlayerPrefs.SetInt("Fraction", data.playerfraction);
        PlayerPrefs.SetInt("ID", data.id);
        PlayerPrefs.Save();

        // Перехід у головне меню/сцену
        SceneManager.LoadScene("loading"); // за потреби заміни на свою назву
    }
}
