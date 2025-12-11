using System;

[Serializable]
public class ShopResponseClient
{
    public string error;
    public ShopItemClientDto[] items;
}

[Serializable]
public class ShopItemClientDto
{
    // Те, що приходить з сервера (імена полів мають збігатись!)
    public string ItemId;
    public byte   Category;
    public byte   Rarity;
    public byte   EquipSlot;      // на сервері byte?, але JsonUtility сприйме як 0, якщо null

    public string IconKey;

    public string NameLocKey;
    public string DescLocKey;

    public int    MinPlayerLevel; // з MinPlayerLevel
    public int    BasePrice;      // з BasePrice

    public bool   IsLocked;       // true = "Відкриється на N рівні"

    // 🔥 Сюди сервер кладе JSON з колонки base_stats_json
    public string BaseStatsJson;

    // Клієнтські допоміжні прапорці (поки що сервер їх не надсилає)
    public bool IsOwned;    // поки завжди false
}

// Ось так ми очікуємо JSON зі статами в BaseStatsJson
[Serializable]
public class ShopItemStatsData
{
    public int PlayerPower;
    public int PlayerSkill;
    public int PlayerDexterity;
    public int PlayerProtection;
    public int PlayerSurvivability;
}
