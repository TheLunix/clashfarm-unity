using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LoadingController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusLabel; // підпишіть у Canvas
    [SerializeField] private Slider progressBar;          // не обов’язково; можна лишити пустим

    [Header("Scene names")]
    [SerializeField] private string firstAuthScene = "FirstAuth";
    [SerializeField] private string mainScene     = "Main";

    // Ключі у PlayerPrefs — беремо ті самі, що використовували раніше
    const string NickKey   = "Name";
    const string SerialKey = "SerialCode";

    async void Start()
    {
        // 0) Гарантуємо сінглтони (PlayerSession, ImageCache) живуть між сценами
        EnsureSingleton<PlayerSession>("PlayerSession");
        EnsureSingletonIfExistsType("ImageCache"); // якщо клас є в проекті — створить його

        SetStatus("Готуємося…", 0.05f);

        // 1) Читаємо креденшли
        var nickname = PlayerPrefs.GetString(NickKey, string.Empty).Trim();
        var serial   = PlayerPrefs.GetString(SerialKey, string.Empty).Trim();

        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(serial))
        {
            // Немає даних — ідемо на першу авторизацію
            SetStatus("Потрібна реєстрація…", 0.1f);
            SceneManager.LoadScene(firstAuthScene);
            return;
        }

        // 2) Тягнемо акаунт з бекенду
        SetStatus("Перевіряємо обліковий запис…", 0.25f);
        var data = await ApiClient.GetAccountAsync(nickname, serial);
        if (data == null)
        {
            // Акаунт не знайшовся/помилка — повертаємо на FirstAuth
            SetStatus("Не вдалося завантажити акаунт. Потрібна реєстрація.", 0.3f);
            await Task.Delay(500);
            SceneManager.LoadScene(firstAuthScene);
            return;
        }

        // 3) Кладемо у сесію
        SetStatus("Ініціалізуємо профіль…", 0.4f);
        PlayerSession.I.Apply(data);

        // 4) (Місце для майбутніх кроків)
        //    Наприклад: прелоад рослин, грядок, кеш іконок тощо.
        //    Зараз пропускаємо, щоб зберегти простоту.

        // 5) На головну
        SetStatus("Все готово! Переходимо…", 1f);
        SceneManager.LoadScene(mainScene);
    }

    void SetStatus(string msg, float progress01)
    {
        if (statusLabel != null) statusLabel.text = msg;
        if (progressBar != null) progressBar.value = Mathf.Clamp01(progress01);
    }

    // Створює сінглтон компонент, якщо ще не існує
    static T EnsureSingleton<T>(string goName) where T : Component
    {
        var existing = Object.FindAnyObjectByType<T>();
        if (existing != null) return existing;

        var go = new GameObject(goName);
        var comp = go.AddComponent<T>();
        Object.DontDestroyOnLoad(go);
        return comp;
    }

    // М’яка спроба створити компонент за ім’ям типу (щоб не падати, якщо класу нема в проекті)
    static void EnsureSingletonIfExistsType(string typeName)
    {
        var t = System.Type.GetType(typeName);
        if (t == null) return; // у проекті немає класу — пропускаємо

        var existing = Object.FindFirstObjectByType(t);
        if (existing != null) return;

        var go = new GameObject(typeName);
        go.AddComponent(t);
        Object.DontDestroyOnLoad(go);
    }
}
