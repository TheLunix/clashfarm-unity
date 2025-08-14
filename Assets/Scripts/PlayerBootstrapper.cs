using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class PlayerBootstrapper : MonoBehaviour
{
    [Header("Keys in PlayerPrefs")]
    public string NickKey = "Name";
    public string SerialKey = "SerialCode";

    [Header("Scenes")]
    public string MainSceneName = "Main";

    async void Start()
    {
        if (PlayerSession.I == null)
        {
            var go = new GameObject("PlayerSession");
            go.AddComponent<PlayerSession>();
        }

        var nickname = PlayerPrefs.GetString(NickKey, "").Trim();
        var serial   = PlayerPrefs.GetString(SerialKey, "").Trim();

        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(serial))
        {
            Debug.LogError("No NickName/SerialCode in PlayerPrefs.");
            return;
        }
        
        // завантажуємо акаунт
        var data = await ApiClient.GetAccountAsync(nickname, serial);
        if (data == null)
        {
            Debug.LogError("Failed to load account.");
            return;
        }

        PlayerSession.I.Apply(data);

        // УВІМКНЕМО POLLER ТІЛЬКИ ТЕПЕР
        var poller = Object.FindFirstObjectByType<PlayerPoller>(FindObjectsInactive.Include);
        if (poller != null) poller.enabled = true;

        SceneManager.LoadScene(MainSceneName);
    }
}
