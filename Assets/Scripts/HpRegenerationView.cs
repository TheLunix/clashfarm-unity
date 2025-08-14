using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HpRegenerationView : MonoBehaviour
{
    [Header("UI")]
    public Slider hpSlider; // нормалізовано: 0..1

    [Header("Animation")]
    [Range(0.1f, 2f)]
    public float smoothDuration = 0.5f;

    Coroutine smoothRoutine;

    void Awake()
    {
        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;      // важливо: працюємо у частках
            hpSlider.wholeNumbers = false;
            hpSlider.value = 0f;         // старт без «стрибу»
        }
    }

    void OnEnable()
    {
        // НЕ викликаємо миттєве виставлення тут, хай перший heartbeat все підтягне плавно
        if (PlayerSession.I != null) PlayerSession.I.OnChanged += OnSessionChanged;
    }

    void OnDisable()
    {
        if (PlayerSession.I != null) PlayerSession.I.OnChanged -= OnSessionChanged;
        if (smoothRoutine != null) StopCoroutine(smoothRoutine);
    }

    void OnSessionChanged()
    {
        var s = PlayerSession.I;
        if (s == null || s.Data == null || hpSlider == null) return;
        OnHpUpdated(s.Data.playerhp, s.Data.maxhp); // плавно, а не миттєво
    }

    public void OnHpUpdated(float newHp, int maxHp)
    {
        if (hpSlider == null) return;

        float currentFrac = Mathf.Clamp01(hpSlider.value);
        float targetFrac  = (maxHp > 0) ? Mathf.Clamp01(newHp / maxHp) : 0f;

        if (smoothRoutine != null) StopCoroutine(smoothRoutine);
        smoothRoutine = StartCoroutine(SmoothTo(currentFrac, targetFrac, smoothDuration));
    }

    IEnumerator SmoothTo(float from, float to, float duration)
    {
        if (duration <= 0f) { hpSlider.value = to; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            hpSlider.value = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        hpSlider.value = to;
    }
}
