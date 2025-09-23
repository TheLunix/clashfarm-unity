using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using ClashFarm.Garden;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LoadingController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusLabel; // підпишіть у Canvas
    [SerializeField] private Slider progressBar;          // не обов’язково; можна лишити пустим

    [Header("Scene names")]
    [SerializeField] private string firstAuthScene = "FirstAuth";
    [SerializeField] private string mainScene = "Main";

    // Ключі у PlayerPrefs — беремо ті самі, що використовували раніше
    const string NickKey = "Name";
    const string SerialKey = "SerialCode";
    const string LocaleKey = "locale.code"; // напр. "uk", "en", "pl"


    async void Start()
    {
        // 0) Гарантуємо сінглтони (PlayerSession, ImageCache) живуть між сценами
        EnsureSingleton<PlayerSession>("PlayerSession");
        EnsureSingleton<GardenSession>("GardenSession");
        ClashFarm.Garden.RemoteSpriteCache.BaseUrl = "https://api.clashfarm.com/plants/";
        EnsureSingletonIfExistsType("ImageCache"); // якщо клас є в проекті — створить його

        SetStatus("Готуємося…", 0.05f);
        SetStatus("Завантажуємо локалізацію…", 0.08f);
        await InitLocalizationAsync();

        // 1) Читаємо креденшли
        var nickname = PlayerPrefs.GetString(NickKey, string.Empty).Trim();
        var serial = PlayerPrefs.GetString(SerialKey, string.Empty).Trim();

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
        SetStatus("Дістаємо лопати й граблі…", 0.45f);
        var gs = EnsureSingleton<GardenSession>("GardenSession");
        gs.PlayerName = nickname;              // з PlayerPrefs / PlayerSession
        gs.PlayerSerialCode = serial;
        gs.Init();                             // ← ОБОВ’ЯЗКОВО

        // Дочекаємось готовності GardenSession (маємо Stage/PlantedID)
        while (GardenSession.I == null || !GardenSession.I.IsReady) await System.Threading.Tasks.Task.Yield();

        // Прогріваємо каталог і дисковий кеш іконок (видимі грядки + трохи для пікера)
        // + опційно — всі іконки на першому вході
        await PrewarmGardenAsync(GardenSession.I);

        // (далі вже завантажуй mainScene як і було)

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

        var existing = Object.FindFirstObjectByType(t); // або FindObjectOfType(t) на старих Unity
        if (existing != null) return;

        var go = new GameObject(typeName);
        go.AddComponent(t);
        Object.DontDestroyOnLoad(go);
    }
    // оновлення статусу (безпечне до null)
    void SetStatus(string text, float? progress = null)
    {
        if (statusLabel) statusLabel.text = text;
        if (progressBar && progress.HasValue) progressBar.value = Mathf.Clamp01(progress.Value);
    }

    // м’який прогрів іконок з обмеженням паралельності і таймаутом
    async Task WarmIconsAsync(IEnumerable<string> keys, int maxParallel = 4, int softTimeoutMs = 3000)
    {
        var list = new List<string>(keys);
        if (list.Count == 0) return;

        var sem = new SemaphoreSlim(maxParallel);
        var tasks = new List<Task>(list.Count);
        foreach (var key in list)
        {
            await sem.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try { await RemoteSpriteCache.GetSpriteAsync(key); }
                catch { /* ок */ }
                finally { sem.Release(); }
            }));
        }

        // м’який таймаут: чекаємо або завершення всього, або softTimeoutMs
        await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(softTimeoutMs));
    }
    async System.Threading.Tasks.Task PrewarmGardenAsync(ClashFarm.Garden.GardenSession gs)
    {
        // 1) Каталог
        List<GardenApi.PlantCatalogItem> plants = null;
        try
        {
            plants = await GardenApi.GetPlantsAsync();
            plants.RemoveAll(p => p.isActive == 0);
            PlantCatalogCache.SetAll(plants);
            Debug.Log($"[Prewarm] Plants: {plants.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Prewarm] GetPlants failed: {e.Message}");
            return; // не блокуємо вхід
        }

        // 2) Ключі іконок для видимих грядок (тільки Stage>0)
        var iconKeys = new HashSet<string>();
        for (int i = 0; i < gs.Plots.Length; i++)
        {
            var p = gs.Plots[i];
            if (p == null || !p.Unlocked || p.Stage <= 0) continue;
            if (!PlantCatalogCache.TryGet(p.PlantedID, out var plant)) continue;
            string key = p.Stage == 1 ? plant.iconSeed : (p.Stage == 2 ? plant.iconPlant : plant.iconGrown);
            if (!string.IsNullOrEmpty(key)) iconKeys.Add(key);
        }

        // 3) (опційно) кілька іконок для пікера: наступна заблокована + 3 доступні
        try
        {
            var d = PlayerSession.I?.Data;
            int lvl = d != null ? d.playerlvl : 1;

            GardenApi.PlantCatalogItem nextLocked = null;
            int minAbove = int.MaxValue;
            foreach (var p in plants)
                if (p.unlockLevel > lvl && p.unlockLevel < minAbove) { minAbove = p.unlockLevel; nextLocked = p; }
            if (nextLocked != null && !string.IsNullOrEmpty(nextLocked.iconGrown)) iconKeys.Add(nextLocked.iconGrown);

            int added = 0;
            foreach (var p in plants)
            {
                if (p.unlockLevel <= lvl)
                {
                    if (!string.IsNullOrEmpty(p.iconGrown)) iconKeys.Add(p.iconGrown);
                    if (++added >= 3) break;
                }
            }
        }
        catch { /* не критично */ }

        // 4) Prefetch на диск без RAM-піків
        try
        {
            Debug.Log($"[Prewarm] Icon keys to warm: {iconKeys.Count}");
            await RemoteSpriteCache.PrefetchToDiskOnly(iconKeys, maxParallel: 3, softTimeoutMs: 3000);

            // (опційно) перший запуск — витягнути ВЕСЬ каталог на диск
            if (PlayerPrefs.GetInt("icons.prefetched", 0) == 0)
            {
                var all = new HashSet<string>();
                foreach (var p in plants)
                {
                    if (!string.IsNullOrEmpty(p.iconSeed)) all.Add(p.iconSeed);
                    if (!string.IsNullOrEmpty(p.iconPlant)) all.Add(p.iconPlant);
                    if (!string.IsNullOrEmpty(p.iconGrown)) all.Add(p.iconGrown);
                }
                Debug.Log($"[Prewarm] First run: prefetch ALL icons: {all.Count}");
                await RemoteSpriteCache.PrefetchToDiskOnly(all, maxParallel: 3, softTimeoutMs: 5000);
                PlayerPrefs.SetInt("icons.prefetched", 1);
                PlayerPrefs.Save();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Prewarm] Prefetch error: {e.Message}");
        }
    }
    private async System.Threading.Tasks.Task InitLocalizationAsync()
    {
        // 1) Дочекатися ініціалізації LocalizationSettings
        var init = LocalizationSettings.InitializationOperation;
        if (!init.IsDone) await init.Task;

        // 2) Вибрати мову з PlayerPrefs (якщо є) або лишити дефолт
        var code = PlayerPrefs.GetString(LocaleKey, string.Empty);
        if (!string.IsNullOrEmpty(code))
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale != null && locale != LocalizationSettings.SelectedLocale)
                LocalizationSettings.SelectedLocale = locale;
        }
        // 3) Прелоад потрібних таблиць (щоб GetLocalizedStringAsync спрацьовував миттєво)
        try
        {
            var plantsHandle = LocalizationSettings.StringDatabase.GetTableAsync("Plants");
            var uiHandle = LocalizationSettings.StringDatabase.GetTableAsync("UI");
            await System.Threading.Tasks.Task.WhenAll(plantsHandle.Task, uiHandle.Task);
        }
        catch { /* не критично — продовжуємо */ }
    }
}
