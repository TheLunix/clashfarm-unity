using System;
using UnityEngine;

namespace ClashFarm.Localization
{
    [Serializable]
    public class LocalizationEntry
    {
        public string Key;
        [TextArea(1, 3)] public string Value;
    }
}
