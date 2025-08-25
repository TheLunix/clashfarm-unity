using UnityEngine;

[CreateAssetMenu(fileName = "PlantInfo", menuName = "ClashFarm/Plant")]
public class PlantInfo : ScriptableObject
{
    public int id;
    public string displayName;
    public Sprite icon;
    public int unlockLevel; // рівень, з якого відкривається
}