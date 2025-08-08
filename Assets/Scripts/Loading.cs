using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // <— для Slider

public class Loading : MonoBehaviour
{
    private string NickName, SerialCode;
    private const string NickKey = "Name";
    private const string CodeKey = "SerialCode";
    private const string ApiUrl = "https://api.clashfarm.com/api/player/check";

    [Header("UI")]
    public Slider ProgressBar;          // прив’яжи твій Slider тут
    public TMPro.TextMeshProUGUI PercentText; // опційно, якщо хочеш % (можна лишити порожнім)

    [Header("Delay")]
    public float DelaySeconds = 2f;     // скільки триває “завантаження”

    void Start()
    {
        // Скидаємо прогресбар на старті
        if (ProgressBar) ProgressBar.value = 0f;
        if (PercentText) PercentText.text = "0%";

        NickName  = PlayerPrefs.GetString(NickKey, "");
        SerialCode = PlayerPrefs.GetString(CodeKey, "");

        if (!string.IsNullOrEmpty(NickName) && !string.IsNullOrEmpty(SerialCode))
        {
            Debug.Log("Починається перевірка акаунта...");
            StartCoroutine(CheckAccountAndLoad());
        }
        else
        {
            Debug.Log("Інформація про гравця відсутня. Перенаправлення на реєстрацію...");
            StartCoroutine(LoadWithBarAndDelay(sceneIndex: 2, seconds: DelaySeconds)); // до сцени реєстрації
        }
    }

    private IEnumerator CheckAccountAndLoad()
    {
        // Можеш показати маленький стартовий рух прогресу до 20%, щоб не стояло “на місці”
        yield return StartCoroutine(FakeProgressTo(0.2f, DelaySeconds * 0.25f));

        WWWForm form = new WWWForm();
        form.AddField("PlayerName", NickName);
        form.AddField("PlayerSerialCode", SerialCode);

        using UnityWebRequest www = UnityWebRequest.Post(ApiUrl, form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Помилка з'єднання: " + www.error);
            // На помилці теж зробимо плавне завершення і підемо на реєстрацію
            yield return StartCoroutine(LoadWithBarAndDelay(sceneIndex: 2, seconds: DelaySeconds));
            yield break;
        }

        string response = www.downloadHandler.text.Trim();
        Debug.Log("Відповідь сервера: " + response);

        if (response == "0")
        {
            Debug.Log("Гравець знайдений. Завантаження...");
            yield return StartCoroutine(LoadWithBarAndDelay(sceneIndex: 1, seconds: DelaySeconds));
        }
        else if (response == "1")
        {
            Debug.LogWarning("Гравець не знайдений. Перенаправлення на реєстрацію.");
            yield return StartCoroutine(LoadWithBarAndDelay(sceneIndex: 2, seconds: DelaySeconds));
        }
        else
        {
            Debug.LogError("Невідома відповідь від сервера. Перенаправлення на реєстрацію.");
            yield return StartCoroutine(LoadWithBarAndDelay(sceneIndex: 2, seconds: DelaySeconds));
        }
    }

    // Плавне "завантаження" з ProgressBar за заданий час, потім перехід на сцену
    private IEnumerator LoadWithBarAndDelay(int sceneIndex, float seconds)
    {
        // Доводимо прогрес до 100% рівномірно за 'seconds'
        yield return StartCoroutine(FakeProgressTo(1f, seconds));
        SceneManager.LoadScene(sceneIndex);
    }

    // Допоміжний метод: плавно рухаємо повзунок до target за duration
    private IEnumerator FakeProgressTo(float target, float duration)
    {
        if (ProgressBar == null && PercentText == null)
        {
            // Якщо UI не підв’язаний — просто чекаємо потрібний час
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float start = ProgressBar ? ProgressBar.value : 0f;
        float t = 0f;

        // Гарантуємо адекватні межі
        target = Mathf.Clamp01(target);
        start  = Mathf.Clamp01(start);
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Lerp(start, target, t / duration);

            if (ProgressBar) ProgressBar.value = p;
            if (PercentText) PercentText.text = Mathf.RoundToInt(p * 100f) + "%";

            yield return null;
        }

        // Зафіксуємо точно в таргет, щоб не було похибок
        if (ProgressBar) ProgressBar.value = target;
        if (PercentText) PercentText.text = Mathf.RoundToInt(target * 100f) + "%";
    }
}
