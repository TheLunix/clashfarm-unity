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
    using System.Globalization;

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
        private string _deferredIconKey;
        private int _deferredDurationSec;       // для локалізації часу після активації
        private bool _needsLocalizeDuration;    // прапор для запуску FormatDuration в OnEnable

        /// <summary>Заповнює картку і показує потрібний стан дії.</summary>
        public void Bind(PlantInfo data, bool unlocked, Action<PlantInfo> onPlant)
        {
            // Якщо елемент (або його батько) зараз неактивний, корутини не можна стартувати.
            // Робимо швидкий бінд без корутин і відкладаємо завантаження іконки до OnEnable.
            if (!gameObject.activeInHierarchy)
            {
                LazyBindNoCoroutines(data, unlocked, onPlant);
                return;
            }
            _data = data;
            _onPlant = onPlant;

            // --- Тексти (поки БЕЗ локалізації: показуємо ключі або сирі строки) ---
            nameText.text = string.IsNullOrEmpty(data.DisplayName) ? "-" : data.DisplayName;
            shortDesc.text = string.IsNullOrEmpty(data.Description) ? "" : data.Description;

            StartCoroutine(LocalizeText(nameText, "Plants", data.DisplayName));
            StartCoroutine(LocalizeText(shortDesc, "Plants", data.Description));
            FormatDuration(data.GrowthTimeMinutes * 60);
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
        private void LazyBindNoCoroutines(PlantInfo data, bool unlocked, Action<PlantInfo> onPlant)
        {
            _data = data;
            _onPlant = onPlant;

            // --- Тексти: беремо синхронно (через вашу локалізаційну обгортку або ключі як є)
            var loc = ClashFarm.Localization.LocalizationService.Instance;
            string title = (loc != null) ? loc.Tr(data.DisplayName) : data.DisplayName;
            string desc = (loc != null) ? loc.Tr(data.Description) : data.Description;

            if (nameText) nameText.text = string.IsNullOrEmpty(title) ? "-" : title;
            if (shortDesc) shortDesc.text = string.IsNullOrEmpty(desc) ? "" : desc;
            if (growTimeText)
            {
                _deferredDurationSec = data.GrowthTimeMinutes * 60;
                growTimeText.text = FormatShortFallback(_deferredDurationSec); // синхронний фолбек
                _needsLocalizeDuration = true; // локалізуємо в OnEnable
            }
            if (priceText) priceText.text = $"<sprite=0> {FormatMoney(data.SellPrice)}";

            // --- Стани дії
            if (plantButton) plantButton.gameObject.SetActive(unlocked);
            if (lockedRow) lockedRow.SetActive(!unlocked);
            if (!unlocked && lockedText) lockedText.text = $"Доступно з {data.UnlockLevel} рівня";
            if (plantButton)
            {
                plantButton.interactable = unlocked;
                plantButton.onClick.RemoveAllListeners();
                if (unlocked) plantButton.onClick.AddListener(() => _onPlant?.Invoke(_data));
            }

            // --- Іконка: ставимо плейсхолдер, а URL запам’ятовуємо для докачки
            if (placeholderIcon && icon && icon.sprite == null) icon.sprite = placeholderIcon;
            string iconName = GetIconNameFromData(data, iconToShow);
            _deferredIconKey = iconName;  // ключ зі стору/СDN, докачаємо в OnEnable
        }
        void OnEnable()
        {
            // 1) Докачка іконки після активації
            if (!string.IsNullOrEmpty(_deferredIconKey))
            {
                var key = _deferredIconKey;
                _deferredIconKey = null;
                StartCoroutine(LoadIconCoroutine(key));
            }

            // 2) Локалізація тривалості після активації (корутини тепер можна)
            if (_needsLocalizeDuration)
            {
                _needsLocalizeDuration = false;
                FormatDuration(_deferredDurationSec);
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

        private void FormatDuration(int sec)
        {
            if (growTimeText == null) return;

            if (sec <= 0)
            {
                // фолбек, якщо ключа "миттєво" нема — можна зробити окремий key, якщо захочеш
                StartCoroutine(LocalizeFormat(growTimeText, "UI", "time.instant"));
                return;
            }

            var t = TimeSpan.FromSeconds(sec);

            if (t.TotalHours >= 24)
            {
                int days = (int)Math.Floor(t.TotalHours / 24);
                int hours = (int)Math.Ceiling(t.TotalHours - (days * 24));
                StartCoroutine(LocalizeFormat(growTimeText, "UI", "time.dh", days, hours));
                return;
            }
            if (t.TotalHours >= 1)
            {
                int hours = (int)Math.Floor(t.TotalHours);
                StartCoroutine(LocalizeFormat(growTimeText, "UI", "time.hm", hours, t.Minutes));
                return;
            }
            if (t.TotalMinutes >= 1)
            {
                StartCoroutine(LocalizeFormat(growTimeText, "UI", "time.ms", t.Minutes, t.Seconds));
                return;
            }

            StartCoroutine(LocalizeFormat(growTimeText, "UI", "time.s", t.Seconds));
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
        private string FormatMoney(int amount)
        {
            // 12 300 → "12,300" за invariant; якщо хочеш пробіли — заміни на CultureInfo("uk-UA")
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }
        private string FormatShortFallback(int totalSec)
        {
            if (totalSec < 60) return totalSec + "с";
            int min = totalSec / 60;
            if (min < 60) return min + "хв";
            int hrs = min / 60;
            min = min % 60;
            if (hrs >= 24)
            {
                int d = hrs / 24; hrs = hrs % 24;
                return d + "д " + hrs + "г";
            }
            return hrs + "г " + (min > 0 ? (min + "хв") : "");
        }
    }
}
