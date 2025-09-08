// PlantInfo.cs
using UnityEngine;

[CreateAssetMenu(fileName = "PlantInfo", menuName = "ClashFarm/Plant")]
public class PlantInfo : ScriptableObject
{
    public int Id;
    public string DisplayName;
    public string Description;
    public int    GrowthTimeMinutes;
    public long   SellPrice;
    public int    UnlockLevel;
    public string IconSeed;
    public string IconPlant;
    public string IconGrown;
    public string IconFruit;

    public bool   IsActive;   // 👈 додали
}