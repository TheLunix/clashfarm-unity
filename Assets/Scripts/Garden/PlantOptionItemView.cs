using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using ClashFarm.Localization;

public sealed class PlantOptionItemView : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text shortDesc;
    [SerializeField] private TMP_Text growTimeText;
    [SerializeField] private TMP_Text priceText;

    [Header("Action area (one place, toggled)")]
    [SerializeField] private Button plantButton;     // RowAction/PlantButton
    [SerializeField] private GameObject lockedRow;   // RowAction/LockedGroup
    [SerializeField] private TMP_Text lockedText;    // RowAction/LockedGroup/Text

    [Header("Optional")]
    [SerializeField] private Sprite placeholderIcon;

    [Header("Icon URL builder")]
    [SerializeField] private string iconBaseUrl = "https://api.clashfarm.com/plants/";
    [SerializeField] private string iconSuffix  = ".png";

    // Яку іконку показувати на картці
    public enum IconVariant { Seed, Plant, Grown, Fruit }
    [SerializeField] private IconVariant iconToShow = IconVariant.Grown;

    private PlantInfo _data;
    private Action<PlantInfo> _onPlant;

    /// <summary>Заповнює картку і показує потрібний стан дії.</summary>
    public void Bind(PlantInfo data, bool unlocked, Action<PlantInfo> onPlant)
    {
        _data = data;
        _onPlant = onPlant;

        // --- Тексти (ЛОКАЛІЗАЦІЯ) ---
        var loc = LocalizationService.Instance;
        string title = (loc != null) ? loc.Tr(data.DisplayName) : data.DisplayName;
        string desc  = (loc != null) ? loc.Tr(data.Description) : data.Description;

        nameText.text      = string.IsNullOrEmpty(title) ? "-" : title;
        shortDesc.text     = string.IsNullOrEmpty(desc)  ? ""  : desc;
        growTimeText.text  = $"{FormatDuration(data.GrowthTimeMinutes*60)}";
        priceText.text     = $"<sprite=0> {FormatMoney(data.SellPrice)}";

        // --- Стани дії (одне місце) ---
        plantButton.gameObject.SetActive(unlocked);
        lockedRow.SetActive(!unlocked);
        if (!unlocked && lockedText != null)
            lockedText.text = $"Доступно з {data.UnlockLevel} рівня";

        plantButton.interactable = unlocked;
        plantButton.onClick.RemoveAllListeners();
        if (unlocked)
            plantButton.onClick.AddListener(() => _onPlant?.Invoke(_data));

        // --- Іконка ---
        if (placeholderIcon && icon && icon.sprite == null)
            icon.sprite = placeholderIcon;

        // --- Іконка (тільки з URL) ---
        if (placeholderIcon && icon && icon.sprite == null)
            icon.sprite = placeholderIcon;

        string iconName = GetIconNameFromData(data, iconToShow); // напр. data.IconGrown
        string url = BuildIconUrl(iconName);                      // base + name + .png

        if (!string.IsNullOrEmpty(url))
        {
            ImageCache.Instance.GetSprite(url, s =>
            {
                if (s != null) icon.sprite = s;
                // else: лишаємо placeholder
            });
        }
    }

    // === Helpers ===
    private string GetIconNameFromData(PlantInfo p, IconVariant variant)
    {
        switch (variant)
        {
            case IconVariant.Seed:  return p.IconSeed;
            case IconVariant.Plant: return p.IconPlant;
            case IconVariant.Fruit: return p.IconFruit;
            default:                return p.IconGrown; // Grown за замовчуванням
        }
    }
    private string BuildIconUrl(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return null;

        // Якщо раптом у БД вже є розширення — не дублюємо
        bool hasExt = iconName.Contains(".");
        string suffix = iconSuffix;
        if (!string.IsNullOrEmpty(suffix) && !suffix.StartsWith("."))
            suffix = "." + suffix;

        string file = hasExt ? iconName.Trim() : (iconName.Trim() + suffix);
        string baseUrl = (iconBaseUrl ?? "").Trim().TrimEnd('/');

        return string.IsNullOrEmpty(baseUrl) ? file : $"{baseUrl}/{file}";
    }
    private string FormatDuration(int sec)
    {
        if (sec <= 0) return "миттєво";
        var t = TimeSpan.FromSeconds(sec);
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}год {t.Minutes}хв";
        if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}хв";
        return $"{t.Seconds}с";
    }

    private string FormatMoney(long v)
    {
        if (v >= 1_000_000) return (v / 1_000_000f).ToString("0.##") + "M";
        if (v >= 1000)      return v.ToString("#,0").Replace(',', ' ');
        return v.ToString();
    }
}
