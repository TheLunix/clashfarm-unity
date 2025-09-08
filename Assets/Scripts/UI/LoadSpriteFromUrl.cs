using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LoadSpriteFromUrl : MonoBehaviour
{
    [Header("Base URL and suffix")]
    [SerializeField] private string coreUrl = "https://api.clashfarm.com/plants"; // без кінцевого '/'
    [SerializeField] private string suffix  = ".png"; // можна .webp/.jpg

    [Header("Image name from DB (or full URL)")]
    [SerializeField] public string plant = "clear"; // напр.: "mushroom_fullgrowth" або повний URL

    [Header("Target UI")]
    [SerializeField] private Image target;
    [SerializeField] private Sprite placeholder;

    // версія запиту — щоб не перетирати новішу картинку старою відповіддю
    private int _ver;

    private void Reset()
    {
        target = GetComponent<Image>();
    }

    private void Awake()
    {
        if (!target) target = GetComponent<Image>();
    }

    private void Start()
    {
        if (placeholder && target) target.sprite = placeholder;
        if (!string.IsNullOrWhiteSpace(plant))
            LoadFor(plant);
    }

    /// <summary>
    /// Викликай, коли підтягуєш ім'я з БД динамічно.
    /// Приймає або коротке ім'я (без розширення), або повний URL.
    /// </summary>
    public void LoadFor(string plantName)
    {
        plant = plantName?.Trim();

        // інвалідовуємо старі колбеки
        int my = ++_ver;

        if (placeholder && target) target.sprite = placeholder;

        string url = BuildUrl(plant);

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("LoadSpriteFromUrl: порожній URL/ім'я зображення");
            return;
        }

        ImageCache.Instance.GetSprite(
            url,
            sprite =>
            {
                if (my != _ver) return;             // старий запит — ігноруємо
                if (sprite && target) target.sprite = sprite;
            },
            err =>
            {
                if (my != _ver) return;
                Debug.LogError($"Image load failed: {err} | {url}");
            }
        );
    }

    private string BuildUrl(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        // Якщо вже передали повний URL — повертаємо як є
        if (name.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return name;
        }

        // Інакше: coreUrl + name (+ suffix, якщо без розширення)
        string baseUrl = (coreUrl ?? string.Empty).Trim().TrimEnd('/');
        string clean   = name.Trim().Trim('/');
        string ext     = (suffix ?? string.Empty).Trim();

        bool hasExt = clean.Contains(".");
        if (!hasExt)
        {
            if (!string.IsNullOrEmpty(ext) && !ext.StartsWith(".")) ext = "." + ext;
            clean += ext;
        }

        return $"{baseUrl}/{clean}";
    }
}
