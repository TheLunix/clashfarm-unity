using System.Collections.Generic;
using UnityEngine;

namespace ClashFarm.Localization
{
    [CreateAssetMenu(fileName = "LocTable", menuName = "ClashFarm/Localization Table")]
    public class LocalizationTable : ScriptableObject
    {
        public string Locale = "en";                    // "uk", "en", "ru", "ar", "zh"...
        public List<LocalizationEntry> Entries = new(); // редагуєш в інспекторі

        // Runtime-словник (будується при активації сервісу)
        private Dictionary<string, string> _map;

        public void BuildMap()
        {
            _map = new Dictionary<string, string>();
            foreach (var e in Entries)
            {
                if (!string.IsNullOrEmpty(e?.Key))
                    _map[e.Key] = e.Value ?? "";
            }
        }

        public bool TryGet(string key, out string value)
        {
            if (_map == null) BuildMap();
            return _map.TryGetValue(key, out value);
        }
    }
}
