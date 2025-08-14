using System.Collections;
using UnityEngine;

public class PlayerPoller : MonoBehaviour
{
    [Header("Polling")]
    public float intervalSeconds = 2f;
    [Tooltip("Кожний скільки-тий тік робити повний /account")]
    public int fullRefreshEvery = 5; // наприклад, раз на 10 секунд при intervalSeconds=2

    [Header("UI refs (опційно)")]
    public HpRegenerationView hpView; // якщо є плавне оновлення ХП
    public CombatsRegenView combatsView; // підкинь у інспекторі

    int tick;

    void Awake()
    {
        if (hpView == null) hpView = FindAnyObjectByType<HpRegenerationView>();
        if (combatsView == null) combatsView = FindAnyObjectByType<CombatsRegenView>();

    }

    void OnEnable()  { StartCoroutine(Loop()); }
    void OnDisable() { StopAllCoroutines(); }

    bool HasCreds()
    {
        var s = PlayerSession.I;
        if (s == null || s.Data == null) return false;
        return !string.IsNullOrWhiteSpace(s.Data.nickname)
            && !string.IsNullOrWhiteSpace(s.Data.serialcode);
    }

    IEnumerator Loop()
    {
        // чекаємо валідні креденшли
        while (!HasCreds()) yield return null;

        var wait = new WaitForSeconds(intervalSeconds);

        tick = 0;
        while (true)
        {
            yield return wait;
            if (!HasCreds()) continue;

            var d = PlayerSession.I.Data;
            tick++;

            // --- Раз на N тіків: повний акаунт (оновлює ВСЕ) ---
            if (tick % Mathf.Max(1, fullRefreshEvery) == 0)
            {
                var accTask = ApiClient.GetAccountAsync(d.nickname, d.serialcode);
                while (!accTask.IsCompleted) yield return null;

                if (accTask.Exception == null && accTask.Result != null)
                {
                    var fresh = accTask.Result;

                    // плавне оновлення hp (якщо хочеш)
                    if (hpView != null) hpView.OnHpUpdated(fresh.playerhp, fresh.maxhp);

                    // оновлюємо сесію ПОВНІСТЮ
                    PlayerSession.I.Apply(fresh);
                }
                continue;
            }

            // окремо — бої
            var hbCombTask = ApiClient.CombatsHeartbeatAsync(d.nickname, d.serialcode);
            while (!hbCombTask.IsCompleted) yield return null;

            if (hbCombTask.Exception == null && hbCombTask.Result.HasValue)
            {
                var comb = hbCombTask.Result.Value;

                // оновлюємо модель
                PlayerSession.I.Patch(pd => { pd.combats = comb.combats; });

                // оновлюємо візуал з м'якою корекцією
                if (combatsView != null)
                    combatsView.OnCombatsHeartbeat(comb.combats, comb.max, comb.remaining);
}

            // --- Інші тікі: легкий heartbeat (оновлює HP і зберігає його в БД) ---
            var hbTask = ApiClient.HpHeartbeatAsync(d.nickname, d.serialcode);
            while (!hbTask.IsCompleted) yield return null;

            if (hbTask.Exception == null && hbTask.Result.HasValue)
            {
                var (hp, max) = hbTask.Result.Value;

                // плавне оновлення hp (UI)
                if (hpView != null) hpView.OnHpUpdated(hp, max);

                // оновлюємо тільки hp/max у сесії
                PlayerSession.I.Patch(pd => { pd.playerhp = hp; pd.maxhp = max; });
            }
        }
    }
}
