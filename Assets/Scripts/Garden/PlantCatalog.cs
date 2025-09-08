using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public static class PlantCatalog
{
    [Serializable]
    private class PlantDto
    {
        public int    id;                   // 👈 число, не string
        public string name;
        public string description;
        public int    requiredLevel;
        public int    growthTimeMinutes;
        public int    sellPrice;
        public string iconSeed;
        public string iconPlant;
        public string iconGrown;
        public string iconFruit;
        public byte   isActive;
    }

    [Serializable]
    private class Resp { public PlantDto[] plants; }   // 👈 масив у полі "plants"

    public static IEnumerator Load(string apiBase, Action<List<PlantInfo>> onDone, Action<string> onError)
    {
        string url = $"{apiBase.TrimEnd('/')}/api/plants/list";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"HTTP {(long)req.responseCode}: {req.error}");
                yield break;
            }

            var txt = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(txt)) { onError?.Invoke("Empty response"); yield break; }

            // діагностика форми
            // Debug.Log($"[PlantCatalog] raw: {txt.Substring(0, Mathf.Min(400, txt.Length))}");

            Resp resp = null;
            try { resp = JsonUtility.FromJson<Resp>(txt); }
            catch (Exception e) { onError?.Invoke("JSON parse error: " + e.Message); yield break; }

            if (resp?.plants == null) { onError?.Invoke("Payload has no 'plants'"); yield break; }

            var list = new List<PlantInfo>(resp.plants.Length);
            foreach (var d in resp.plants)
            {
                if (d.isActive == 0) continue; // фільтр неактивних

                var so = ScriptableObject.CreateInstance<PlantInfo>();
                so.Id                = d.id;     // 👈 конвертуємо
                so.DisplayName       = d.name;              // (у тебе це ключ локалізації)
                so.Description       = d.description;       // (і це, йде в LocalizationService)
                so.UnlockLevel       = d.requiredLevel;
                so.GrowthTimeMinutes = d.growthTimeMinutes;
                so.SellPrice         = d.sellPrice;         // int → long у PlantInfo ок
                so.IconSeed          = d.iconSeed;
                so.IconPlant         = d.iconPlant;
                so.IconGrown         = d.iconGrown;
                so.IconFruit         = d.iconFruit;
                so.IsActive          = d.isActive != 0; 

                list.Add(so);
            }

            onDone?.Invoke(list);
        }
    }
}
