// НОВИЙ файл
namespace ClashFarm.Garden
{
    using System;

    [Serializable]
    public class PlantInfo   // мінімум полів, яких вимагають панелі
    {
        public int    Id;
        public string DisplayName;          // ключ локалізації (або просто назва на час розробки)
        public string Description;          // ключ локалізації
        public int    UnlockLevel;
        public int    GrowthTimeMinutes;
        public int    SellPrice;
        public string IconSeed, IconPlant, IconGrown, IconFruit;
        public bool   IsActive = true;
    }

    [Serializable]
    public class PlotModel   // мінімум, потрібний для PlantActionPanel
    {
        public int  stage;        // 0..3
        public bool hasWeeds;
        public bool needsWater;
    }
}
