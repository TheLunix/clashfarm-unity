using System;

[Serializable]
public class InventoryResponseClient
{
    public string error;
    public int usedslots;
    public int maxslots;
    public InventoryItemClientDto[] items;
}

[Serializable]
public class InventoryItemClientDto
{
    public long   Id;
    public int    PlayerId;
    public string ItemId;
    public byte   ItemLevel;
    public int    StackCount;
    public bool   IsEquipped;
    public byte   EquippedSlot;
    public bool   IsLocked;

    public byte   Category;
    public byte   EquipSlot;
    public byte   Rarity;
    public string IconKey;
    public bool   CanSell;
    public bool   CanUse;
    public bool   CountsForCapacity;
    public bool   VisibleInMainInventory;
    public bool   VisibleInPetInventory;
    public string NameLocKey;
    public string DescLocKey;
    public int    BasePrice;
}
