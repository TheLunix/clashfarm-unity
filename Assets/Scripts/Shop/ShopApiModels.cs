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
    public string ItemId;

    public byte   Category;
    public byte?  EquipSlot;
    public byte   Rarity;

    public string IconKey;

    public int    BasePrice;          // ціна в зелені
    public int    MinPlayerLevel;     // рівень відкриття

    public bool   IsLocked;           // показуємо "Відкриється на ..."
    public bool   IsOwned;            // чи є хоч 1
    public int    OwnedCount;         // скільки всього (для стакових)

    public bool   IsStackable;        // MaxStack > 1
    public int    MaxStack;           // для UI/інвентаря

    public bool   CountsForCapacity;  // чи займає слоти інвентаря

    public bool   IsRing;             // важливо: кільця не продаємо в магазині
    public bool   IsPetCollar;        // важливо: нашийники не продаємо в магазині

    public string BaseStatsJson;      // стати з JSON
}