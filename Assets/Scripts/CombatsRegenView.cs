using UnityEngine;
using TMPro;

public class CombatsRegenView : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI countLabel;   // "X/6"
    public TextMeshProUGUI timerLabel;   // "MM:SS"

    [Header("Smoothing")]
    [Tooltip("макс. дозволене збільшення відображеного часу за один апдейт (сек)")]
    public float maxUpCorrectionPerTick = 0.25f; // не дозволяємо «підскок» більше ніж на 0.25s
    [Tooltip("швидкість м'якої корекції до серверного значення (0..1)")]
    public float smoothFactor = 0.15f; // 0.1..0.2 — комфортно

    const int DefaultMax = 6;
    int currentCombats;
    int maxCombats = DefaultMax;

    // Локальний «візуальний» таймер у секундах — те, що ми показуємо на екрані
    float displayRemaining = 0f;
    bool ticking = false;

    void OnEnable()
    {
        RedrawCount();
        RedrawTime();
    }

    void Update()
    {
        if (!ticking)
        {
            // коли не тікає — тримаємо 00:00
            RedrawTime();
            return;
        }

        // Локально зменшуємо відлік (безстрибково)
        displayRemaining -= Time.unscaledDeltaTime;
        if (displayRemaining < 0f) displayRemaining = 0f;

        RedrawTime();

        // Коли локально дійшли до нуля — чекаємо наступний heartbeat, який вже інкрементить combats
        if (displayRemaining <= 0.0001f)
            ticking = false;
    }

    /// <summary>
    /// Викликати після combats/heartbeat: combats, max, remaining (сек до наступного тіку)
    /// </summary>
    public void OnCombatsHeartbeat(int combats, int max, int serverRemaining)
    {
        currentCombats = combats;
        maxCombats = (max > 0) ? max : DefaultMax;
        RedrawCount();

        if (combats >= maxCombats)
        {
            ticking = false;
            displayRemaining = 0f;
            RedrawTime();
            return;
        }

        // М'яка корекція:
        // 1) Розраховуємо «ціль» від сервера
        float target = Mathf.Max(0f, serverRemaining);

        if (!ticking)
        {
            // якщо таймер стояв — стартуємо з серверного значення (без стрибка)
            displayRemaining = target;
            ticking = true;
        }
        else
        {
            // якщо таймер уже тікає — коригуємо м'яко
            // базова EMA (exponential moving average)
            float blended = Mathf.Lerp(displayRemaining, target, smoothFactor);

            // не дозволяємо ЗРОСТАННЯ більше ніж на maxUpCorrectionPerTick за один апдейт
            if (blended > displayRemaining)
                blended = Mathf.Min(blended, displayRemaining + maxUpCorrectionPerTick);

            // й ніколи не дозволяємо стрибок вниз понад норму (це майже не видно і так, але хай буде плавно)
            // якщо хочеш ще плавніше — можеш також обмежити падіння (симетрично), але зазвичай падіння не дратує.
            displayRemaining = Mathf.Max(0f, blended);
        }

        ticking = true;
        RedrawTime();
    }

    void RedrawCount()
    {
        if (countLabel != null)
            countLabel.text = $"{currentCombats}/{maxCombats}";
    }

    void RedrawTime()
    {
        if (timerLabel == null) return;

        int rem = Mathf.CeilToInt(displayRemaining);
        int mm = rem / 60;
        int ss = rem % 60;
        timerLabel.text = $"{mm:00}:{ss:00}";
    }
}
