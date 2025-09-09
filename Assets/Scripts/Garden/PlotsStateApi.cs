using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public static class PlotsStateApi
{
    [Serializable]
    public class PlotDto
    {
        public int slot;
        public int? plantId;
        public int stage;              // 0..3, 255=locked
        public long timeToNextSec;     // для stage 1..2
        public bool needsWater;
        public bool hasWeeds;
    }

    [Serializable]
    class StateResp
    {
        public int unlocked;
        public PlotDto[] plots;
    }

    // --- СТАН ---
    public static IEnumerator LoadState(
        string apiBase, string playerName, string serialCode,
        Action<int, List<PlotModel>> onOk, Action<string> onError)
    {
        string url = apiBase.TrimEnd('/') + "/api/garden/state";
        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);

        using (var req = UnityWebRequest.Post(url, form))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }
            var txt = req.downloadHandler.text;
            if (string.IsNullOrEmpty(txt)) { onError?.Invoke("Empty response"); yield break; }

            StateResp resp = null;
            try { resp = JsonUtility.FromJson<StateResp>(txt); }
            catch (Exception e) { onError?.Invoke("Parse error: " + e.Message); yield break; }

            if (resp == null || resp.plots == null) { onError?.Invoke("Invalid payload"); yield break; }

            var list = new List<PlotModel>(resp.plots.Length);
            foreach (var p in resp.plots)
            {
                var m = new PlotModel
                {
                    slotIndex = p.slot,
                    isLocked = (p.stage == 255),
                    plantTypeId = p.plantId,
                    stage = (p.stage == 255 ? 0 : p.stage),
                    timeToNextSec = p.timeToNextSec,
                    needsWater = p.needsWater,
                    hasWeeds = p.hasWeeds
                };
                list.Add(m);
            }
            onOk?.Invoke(resp.unlocked, list);
        }
    }

    // --- ПОСАДИТИ ---
    // codes: "0" ok, "1" no player, "2" busy, "3" no plant, "4" level low, "5" slot locked
    public static IEnumerator Plant(
        string apiBase, string playerName, string serialCode, int slotIndex, int plantId,
        Action onOk, Action<string> onError)
    {
        string url = apiBase.TrimEnd('/') + "/api/garden/plant";
        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);
        form.AddField("slotIndex", slotIndex);
        form.AddField("plantId", plantId);

        using (var req = UnityWebRequest.Post(url, form))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }

            var code = req.downloadHandler.text?.Trim('"');
            if (code == "0") onOk?.Invoke();
            else onError?.Invoke(code ?? "ERR");
        }
    }

    // --- ПОЛИТИ ---
    // codes: "0" ok, "1" no player, "2" empty, "3" plant missing, "4" already ripe
    public static IEnumerator Water(
        string apiBase, string playerName, string serialCode, int slotIndex,
        Action onOk, Action<string> onError)
    {
        string url = apiBase.TrimEnd('/') + "/api/garden/water";
        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);
        form.AddField("slotIndex", slotIndex);

        using (var req = UnityWebRequest.Post(url, form))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }

            var code = req.downloadHandler.text?.Trim('"');
            if (code == "0") onOk?.Invoke();
            else onError?.Invoke(code ?? "ERR");
        }
    }

    // --- ЗІБРАТИ ---
    // codes: "0" ok, "1" no player, "2" empty, "3" plant missing, "4" not ripe
    public static IEnumerator Harvest(
        string apiBase, string playerName, string serialCode, int slotIndex,
        Action onOk, Action<string> onError)
    {
        string url = apiBase.TrimEnd('/') + "/api/garden/harvest";
        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);
        form.AddField("slotIndex", slotIndex);

        using (var req = UnityWebRequest.Post(url, form))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }

            var code = req.downloadHandler.text?.Trim('"');
            if (code == "0") onOk?.Invoke();
            else onError?.Invoke(code ?? "ERR");
        }
    }

    // --- ЗАГОТОВКА ПІД БУР'ЯН/ШКІДНИКІВ ---
    // public static IEnumerator CleanWeeds(...) { /* TODO: бекенд /api/garden/cleanweeds */ }
    // --- ПРИБРАТИ БУР'ЯН ---
    /*
       Очікує бекенд POST /api/garden/cleanweeds
       form: PlayerName, PlayerSerialCode, slotIndex
       codes: "0" ok, "1" no player, "2" empty, "3" not allowed / no weeds
    */
    public static IEnumerator CleanWeeds(
        string apiBase, string playerName, string serialCode, int slotIndex,
        Action onOk, Action<string> onError)
    {
        string url = apiBase.TrimEnd('/') + "/api/garden/cleanweeds";
        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);
        form.AddField("slotIndex", slotIndex);

        using (var req = UnityWebRequest.Post(url, form))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }

            var code = req.downloadHandler.text?.Trim('"');
            if (code == "0") onOk?.Invoke();
            else onError?.Invoke(code ?? "ERR");
        }
    }

    [Serializable]
    private class UnlockResp
    {
        public bool ok;
        public int unlocked;
        public int slot;     // 0-баз. індекс наступної розблокованої
        public int cost;
        public string error;  // "NO_PLAYER", "NO_GOLD", "MAX_REACHED" ...
        public int need;      // скільки треба золота (якщо NO_GOLD)
        public int have;      // скільки є
    }

    public static IEnumerator Unlock(
        string apiBase,
        string playerName,
        string serialCode,
        Action<int, int, int> onOk,    // (unlocked, slot, cost)
        Action<string> onError)
    {
        var url = $"{apiBase.TrimEnd('/')}/api/garden/unlock";
        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);

        using (var req = UnityWebRequest.Post(url, form))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"HTTP {(long)req.responseCode}: {req.error}");
                yield break;
            }

            var txt = req.downloadHandler.text ?? "";
            UnlockResp resp = null;
            try { resp = JsonUtility.FromJson<UnlockResp>(txt); } catch { /* ignore */ }

            if (resp != null)
            {
                if (resp.ok)
                {
                    onOk?.Invoke(resp.unlocked, resp.slot, resp.cost);
                    yield break;
                }
                else
                {
                    var msg = !string.IsNullOrEmpty(resp.error) ? resp.error : "UNKNOWN_ERROR";
                    onError?.Invoke(msg);
                    yield break;
                }
            }

            // На випадок, якщо бекенд поверне короткий текст помилки
            onError?.Invoke(string.IsNullOrWhiteSpace(txt) ? "EMPTY_RESPONSE" : txt);
        }
    }

}
