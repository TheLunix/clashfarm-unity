using System.Collections.Generic;
using UnityEngine;

namespace ClashFarm.Localization
{
    public sealed class LocalizationService : MonoBehaviour
    {
        public static LocalizationService Instance { get; private set; }

        [Header("Tables to load")]
        public List<LocalizationTable> Tables = new(); // додаси тут uk/en і т.д.
        [Header("Default / Current")]
        public string DefaultLocale = "en";
        public string CurrentLocale = "en";

        private Dictionary<string, string> _active = new();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RebuildActive();
        }

        public void SetLocale(string locale)
        {
            if (string.IsNullOrEmpty(locale)) return;
            CurrentLocale = locale;
            RebuildActive();
        }

        private void RebuildActive()
        {
            _active.Clear();

            LocalizationTable def = Tables.Find(t => t != null && t.Locale == DefaultLocale);
            LocalizationTable cur = Tables.Find(t => t != null && t.Locale == CurrentLocale);

            // 1) Спочатку дефолтна (як базовий шар)
            if (def != null)
            {
                def.BuildMap();
                foreach (var e in def.Entries)
                    if (!string.IsNullOrEmpty(e?.Key)) _active[e.Key] = e.Value ?? "";
            }

            // 2) Потім накриваємо поточною локаллю (перекриває дефолт)
            if (cur != null && cur != def)
            {
                cur.BuildMap();
                foreach (var e in cur.Entries)
                    if (!string.IsNullOrEmpty(e?.Key)) _active[e.Key] = e.Value ?? "";
            }
        }

        /// <summary>Отримати переклад. Якщо ключа нема — повертає сам ключ (щоб було видно відсутність).</summary>
        public string Tr(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            return _active.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : key;
        }

        /// <summary>Опційно: змерджити переклади, отримані з сервера (key→value).</summary>
        public void MergeFromServer(Dictionary<string, string> remote)
        {
            if (remote == null) return;
            foreach (var kv in remote)
                _active[kv.Key] = kv.Value ?? "";
        }
    }
}
