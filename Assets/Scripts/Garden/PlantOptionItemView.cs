// ЛЕГКА ВЕРСІЯ без зовнішніх залежностей
// Якщо потім захочеш підключити локалізацію/кеш іконок — дивись TODO унизу.

namespace ClashFarm.Garden
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Settings;
    using ClashFarm.Garden;

    public sealed class PlantOptionItemView : MonoBehaviour
    {
        [Header("UI refs")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text shortDesc;
        [SerializeField] private TMP_Text growTimeText;
        [SerializeField] private TMP_Text priceText;

        [Header("Action area (one place, toggled)")]
        [SerializeField] private Button plantButton;     // активна кнопка посадки
        [SerializeField] private GameObject lockedRow;   // рядок "заблоковано"
        [SerializeField] private TMP_Text lockedText;

        [Header("Optional placeholder")]
        [SerializeField] private Sprite placeholderIcon;

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

            // --- Тексти (поки БЕЗ локалізації: показуємо ключі або сирі строки) ---
            nameText.text = string.IsNullOrEmpty(data.DisplayName) ? "-" : data.DisplayName;
            shortDesc.text = string.IsNullOrEmpty(data.Description) ? "" : data.Description;

            StartCoroutine(LocalizeText(nameText, "Plants", data.DisplayName));
            StartCoroutine(LocalizeText(shortDesc, "Plants", data.Description));
            growTimeText.text = FormatDuration(data.GrowthTimeMinutes * 60);
            priceText.text = "<sprite=0> " + data.SellPrice.ToString(); // TODO: намалюєш свій префікс/іконку валюти

            // --- Стани дії ---
            if (plantButton != null)
            {
                plantButton.gameObject.SetActive(unlocked);
                plantButton.interactable = unlocked;
                plantButton.onClick.RemoveAllListeners();
                if (unlocked) plantButton.onClick.AddListener(() => _onPlant?.Invoke(_data));
            }
            if (lockedRow != null)
            {
                lockedRow.SetActive(!unlocked);
                if (!unlocked && lockedText != null)
                {
                    // fallback на випадок, якщо в таблиці ще нема ключа:
                    lockedText.text = $"Доступно з {data.UnlockLevel} рівня";
                    StartCoroutine(LocalizeFormat(lockedText, "UI", "plant.locked_level", data.UnlockLevel));
                }
            }

            // --- Іконка ---
            if (icon != null)
            {
                // Плейсхолдер, щоб не було порожньо
                if (placeholderIcon != null) icon.sprite = placeholderIcon;
                // --- Іконка (спочатку пробуємо взяти миттєво з пам’яті) ---
                string iconName = GetIconNameFromData(data, iconToShow); // seed/plant/grown key
                if (!string.IsNullOrEmpty(iconName))
                {
                    // 1) якщо вже в пам’яті — ставимо миттєво, без корутин і очікувань
                    if (RemoteSpriteCache.TryGetInMemory(iconName, out var cached))
                    {
                        if (icon) icon.sprite = cached;
                    }
                    else
                    {
                        // 2) інакше — підвантажимо з диска/мережі з м’яким дроселінгом
                        StartCoroutine(LoadIconCoroutine(iconName));
                    }
                }
            }
        }

        // === Helpers ===
        private string GetIconNameFromData(PlantInfo p, IconVariant variant)
        {
            switch (variant)
            {
                case IconVariant.Seed: return p.IconSeed;
                case IconVariant.Plant: return p.IconPlant;
                case IconVariant.Fruit: return p.IconFruit;
                default: return p.IconGrown; // grown за замовчуванням
            }
        }

        private string FormatDuration(int sec)
        {
            if (sec <= 0) return "миттєво";
            var t = TimeSpan.FromSeconds(sec);
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}год {t.Minutes}хв";
            if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}хв";
            return $"{t.Seconds}с";
        }

        private System.Collections.IEnumerator LocalizeText(TMP_Text target, string table, string key)
        {
            if (target == null || string.IsNullOrEmpty(key)) yield break;
            var handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key);
            yield return handle;
            if (target != null && handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                target.text = handle.Result;
        }
        private System.Collections.IEnumerator LocalizeFormat(TMP_Text target, string table, string key, params object[] args)
        {
            if (target == null || string.IsNullOrEmpty(key)) yield break;
            var ls = new LocalizedString(table, key);
            if (args != null && args.Length > 0) ls.Arguments = args;

            var handle = ls.GetLocalizedStringAsync();
            yield return handle;
            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && target != null)
                target.text = handle.Result;
        }
        // Глобальний ліміт: не більш як N завантажень за кадр для всіх карток
        static int _lfFrame, _lfCount;
        const int _lfMaxPerFrame = 3;
        static System.Collections.IEnumerator ThrottlePerFrame()
        {
            while (true)
            {
                if (Time.frameCount != _lfFrame) { _lfFrame = Time.frameCount; _lfCount = 0; }
                if (_lfCount < _lfMaxPerFrame) { _lfCount++; yield break; }
                yield return null;
            }
        }

        System.Collections.IEnumerator LoadIconCoroutine(string key)
        {
            if (icon == null || string.IsNullOrEmpty(key)) yield break;

            // м’який дроселінг, щоб не робити десятки LoadImage в одному кадрі
            yield return ThrottlePerFrame();

            var task = RemoteSpriteCache.GetSpriteAsync(key);
            while (!task.IsCompleted) yield return null;

            var sp = task.Result;
            if (sp != null && icon != null) icon.sprite = sp;
        }
    }
}
