public enum InventoryUiGroup
{
    Equipment = 0,
    Rings     = 1,
    Potions   = 2,
    Scrolls   = 3,
    Gifts     = 4,
    Curses    = 5,
    Event     = 6,
    Other     = 7
}

public class InventoryItemViewModel
{
    public InventoryItemClientDto Data;
    public InventoryUiGroup Group;

    public string SortKeyPrimary;
    public int    SortKeyLevel;
    public long   SortKeyId;
}
