namespace ClashFarm.Garden
{
    using System;
    using System.Collections.Generic;

    public static class PlantCatalogCache
    {
        static readonly Dictionary<int, GardenApi.PlantCatalogItem> _byId = new();

        // ← нове: прапорець готовності та подія
        public static bool IsReady { get; private set; }
        public static event Action OnReady;

        public static void SetAll(List<GardenApi.PlantCatalogItem> items)
        {
            _byId.Clear();
            IsReady = false;

            if (items != null)
                for (int i = 0; i < items.Count; i++)
                    _byId[items[i].id] = items[i];

            IsReady = _byId.Count > 0;
            if (IsReady) OnReady?.Invoke();
        }

        public static bool TryGet(int id, out GardenApi.PlantCatalogItem item)
            => _byId.TryGetValue(id, out item);

        // ← опціонально: якщо десь треба “скинути” кеш
        public static void Clear()
        {
            _byId.Clear();
            IsReady = false;
        }
    }
}
