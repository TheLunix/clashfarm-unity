using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlantDatabase", menuName = "ClashFarm/PlantDatabase")]
public class PlantDatabase : ScriptableObject
{
    public List<PlantInfo> plants = new();
}
