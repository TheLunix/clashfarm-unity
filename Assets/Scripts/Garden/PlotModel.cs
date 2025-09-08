using System;

[Serializable]
public class PlotModel
{
    public int slotIndex;         // 0..11 (як на бекенді)
    public int? plantTypeId;       // null -> порожньо
    public int stage;             // -1=locked, 0=empty, 1=seed, 2=sprout, 3=grown, 255=locked
    public long timeToNextSec;     // сек. до наступної стадії
    public bool needsWater;
    public bool hasWeeds;
    public bool isLocked;
}
