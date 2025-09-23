// Assets/Scripts/Garden/GardenApi.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ClashFarm.Garden
{
    public static class GardenApi
    {
        // TODO: підстав свій базовий URL бекенду
        public static string BaseUrl = "https://api.clashfarm.com/api/garden";

        // Якщо твій /state — POST, вистави true. Якщо GET — false.
        const bool USE_POST_STATE = true;

        public static async Task<GardenStateResponse> GetStateAsync(string playerName, string playerSerial)
        {
            var form = new WWWForm();
            form.AddField("PlayerName", playerName);
            form.AddField("PlayerSerialCode", playerSerial);
            using var req = UnityWebRequest.Post($"{BaseUrl}/state", form);
            req.downloadHandler = new DownloadHandlerBuffer();
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(req.error);

            var resp = JsonUtility.FromJson<GardenStateResponse>(req.downloadHandler.text);
            if (resp == null) throw new Exception("Failed to parse GardenStateResponse");
            return resp;
        }

        public static Task<PlotDto> PlantAsync(string playerName, string playerSerial, int slot, int plantId)
        {
            var f = new WWWForm();
            f.AddField("PlayerName", playerName);
            f.AddField("PlayerSerialCode", playerSerial);
            f.AddField("slotIndex", slot);
            f.AddField("plantId", plantId);
            return Post<PlotDto>("plant", f);
        }

        public static Task<ApiResultOrDto<PlotDto>> WaterAsync(string playerName, string playerSerial, int slot)
        {
            var f = new WWWForm();
            f.AddField("PlayerName", playerName);
            f.AddField("PlayerSerialCode", playerSerial);
            f.AddField("slotIndex", slot);
            return PostResultOrDto<PlotDto>("water", f);
        }

        public static Task<ApiResultOrDto<PlotDto>> WeedAsync(string playerName, string playerSerial, int slot)
        {
            var f = new WWWForm();
            f.AddField("PlayerName", playerName);
            f.AddField("PlayerSerialCode", playerSerial);
            f.AddField("slotIndex", slot);
            return PostResultOrDto<PlotDto>("weed", f);
        }

        public static async Task<ApiResultOrDto<HarvestResponse>> HarvestAsync(string playerName, string playerSerial, int slot)
        {
            var f = new WWWForm();
            f.AddField("PlayerName", playerName);
            f.AddField("PlayerSerialCode", playerSerial);
            f.AddField("slotIndex", slot);

            using var req = UnityWebRequest.Post($"{BaseUrl}/harvest", f);
            req.downloadHandler = new DownloadHandlerBuffer();
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                return new ApiResultOrDto<HarvestResponse> { ok = false, error = req.error };

            var text = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(text))
                return new ApiResultOrDto<HarvestResponse> { ok = false, error = "empty_response" };

            var trimmed = text.Trim();

            // 0) Якщо бек повернув { "error": "..." }
            if (trimmed.StartsWith("{") && text.Contains("\"error\""))
            {
                try
                {
                    var e = JsonUtility.FromJson<ApiError>(trimmed);
                    if (!string.IsNullOrEmpty(e.error))
                        return new ApiResultOrDto<HarvestResponse> { ok = false, error = e.error };
                }
                catch { /* ідемо парсити нижче */ }
            }

            // 1) Спроба: повна відповідь { greenAdded, newPlot }
            if (trimmed.StartsWith("{"))
            {
                // спроба А: повний об'єкт
                try
                {
                    var resp = JsonUtility.FromJson<HarvestResponse>(trimmed);
                    if (resp != null && (resp.newPlot != null || resp.greenAdded != 0))
                        return new ApiResultOrDto<HarvestResponse> { ok = true, dto = resp };
                }
                catch { }

                // спроба Б: сервер вертає тільки newPlot ({ slot, stage, ... })
                try
                {
                    var plot = JsonUtility.FromJson<PlotDto>(trimmed);
                    if (plot != null && plot.slot >= 0)
                        return new ApiResultOrDto<HarvestResponse> { ok = true, dto = new HarvestResponse { greenAdded = 0, newPlot = plot } };
                }
                catch { }

                return new ApiResultOrDto<HarvestResponse> { ok = false, error = "bad_harvest_object" };
            }

            // 2) Спроба: просто число (greenAdded)
            if (int.TryParse(trimmed, out var green))
                return new ApiResultOrDto<HarvestResponse> { ok = true, dto = new HarvestResponse { greenAdded = green, newPlot = null } };

            // 3) Інакше — невідомий формат
            Debug.LogError($"/harvest returned unexpected payload: {trimmed}");
            return new ApiResultOrDto<HarvestResponse> { ok = false, error = "unexpected_payload" };
        }

        public static Task<ApiResultOrDto<UnlockResponse>> UnlockAsync(string playerName, string playerSerial)
        {
            var f = new WWWForm();
            f.AddField("PlayerName", playerName);
            f.AddField("PlayerSerialCode", playerSerial);
            return PostResultOrDto<UnlockResponse>("unlock", f);
        }

        // ====== Helpers ======

        static async Task<T> Post<T>(string path, WWWForm form)
        {
            using var req = UnityWebRequest.Post($"{BaseUrl}/{path}", form);
            req.downloadHandler = new DownloadHandlerBuffer();
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(req.error);

            var json = req.downloadHandler.text;
            var obj = JsonUtility.FromJson<T>(json);
            if (obj == null) throw new Exception($"Failed to parse {typeof(T).Name}");
            return obj;
        }

        /// <summary>
        /// Бекенд може іноді вертати { error = "..." } замість DTO.
        /// </summary>
        static async Task<ApiResultOrDto<T>> PostResultOrDto<T>(string path, WWWForm form)
        {
            using var req = UnityWebRequest.Post($"{BaseUrl}/{path}", form);
            req.downloadHandler = new DownloadHandlerBuffer();
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                return new ApiResultOrDto<T> { error = req.error, ok = false };

            var json = req.downloadHandler.text;

            // Простенький хак: якщо відповідь починається з '{"error":'
            if (!string.IsNullOrEmpty(json) && json.Contains("\"error\""))
            {
                var e = JsonUtility.FromJson<ApiError>(json);
                return new ApiResultOrDto<T> { error = e.error, ok = false };
            }
            var dto = JsonUtility.FromJson<T>(json);
            return new ApiResultOrDto<T> { ok = true, dto = dto };
        }

        [Serializable] public class ApiError { public string error; }
        [Serializable] public class ApiResultOrDto<T> { public bool ok; public string error; public T dto; }

        // ====== DTO ======

        [Serializable]
        public class GardenStateResponse
        {
            public int maxSlots;
            public int unlockedSlots;
            public long serverTimeMs;
            public List<PlotDto> plots;
        }

        [Serializable]
        public class PlotDto
        {
            public int slot;
            public int plantedId;     // 0 якщо пусто
            public int stage;         // 0..3
            public bool needsWater;
            public bool hasWeeds;
            public long onPlantedTime;
            public long nextStageTime;
            public long timeEndGrowth;
            public int sellPrice;
        }

        [Serializable]
        public class HarvestResponse
        {
            public int greenAdded;
            public PlotDto newPlot;
        }

        [Serializable]
        public class UnlockResponse
        {
            public int unlockedSlots;
            public int slot;
            public int cost;
            public PlotDto newPlot;
        }

        [Serializable]
        public class PlantCatalogItem
        {
            public int id;
            public string displayName;   // ключ локалізації
            public string description;   // ключ локалізації
            public int unlockLevel;
            public int growthTimeMinutes;
            public int sellPrice;
            public string iconSeed, iconPlant, iconGrown, iconFruit; // ключі файлів на сервері
            public int isActive;
        }
        // ДОДАЙ ці server-DTO поруч з іншими [Serializable] класами:
        [Serializable] class PlantListWrapperServer
        {
            public List<PlantCatalogItemServer> plants; // фактичний ключ на беку
            public List<PlantCatalogItemServer> items;  // на випадок іншого формату
        }
        [Serializable]
        class PlantCatalogItemServer
        {
            public int id;
            public string name;            // <-- сервер
            public string description;
            public int requiredLevel;      // <-- сервер
            public int growthTimeMinutes;
            public int sellPrice;
            public string iconSeed, iconPlant, iconGrown, iconFruit;
            public int isActive;
        }


        public static async Task<List<PlantCatalogItem>> GetPlantsAsync()
        {
            using var req = UnityWebRequest.Get($"{BaseUrl.Replace("/garden", "")}/plants/list");
            req.downloadHandler = new DownloadHandlerBuffer();
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(req.error);

            var text = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("Empty plants response");

            var trimmed = text.Trim();

            // Якщо раптом прийде чистий масив, загорнемо в об’єкт під ключем "plants"
            if (trimmed.StartsWith("["))
                trimmed = "{\"plants\":" + trimmed + "}";

            PlantListWrapperServer wrap = null;
            try
            {
                wrap = JsonUtility.FromJson<PlantListWrapperServer>(trimmed);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Plants] Parse failed: {e}\nRAW: {trimmed}");
                throw;
            }

            var src = wrap?.plants ?? wrap?.items;
            if (src == null)
                throw new Exception("Bad plants payload: no 'plants' or 'items'");

            // Мапимо серверну модель у нашу клієнтську
            var list = new List<PlantCatalogItem>(src.Count);
            foreach (var s in src)
            {
                var c = new PlantCatalogItem
                {
                    id = s.id,
                    displayName = s.name,                 // name -> displayName
                    description = s.description,
                    unlockLevel = s.requiredLevel,        // requiredLevel -> unlockLevel
                    growthTimeMinutes = s.growthTimeMinutes,
                    sellPrice = s.sellPrice,
                    iconSeed = s.iconSeed,
                    iconPlant = s.iconPlant,
                    iconGrown = s.iconGrown,
                    iconFruit = s.iconFruit,
                    isActive = s.isActive
                };
                list.Add(c);
            }

            return list;
        }

        [Serializable] class PlantListWrapper { public List<PlantCatalogItem> items; }


    }
}
