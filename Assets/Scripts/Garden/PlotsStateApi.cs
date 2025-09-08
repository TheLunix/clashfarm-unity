using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public static class PlotsStateApi
{
    [Serializable] private class PlotDto
    {
        public int slot;               // 0..11
        public int plantId;            // JsonUtility не вміє в int?, тому 0 === null
        public int stage;             // 0..3, 255 = locked
        public long timeToNextSec;
        public bool needsWater;
        public bool hasWeeds;
    }

    [Serializable] private class Resp
    {
        public int unlocked;
        public PlotDto[] plots;
    }

    public static IEnumerator LoadState(
        string apiBase,
        string playerName,
        string serialCode,
        Action<int, List<PlotModel>> onDone,
        Action<string> onError)
    {
        var url = $"{apiBase.TrimEnd('/')}/api/garden/state";
        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);

        using (var req = UnityWebRequest.Post(url, form))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"HTTP {(long)req.responseCode}: {req.error}");
                yield break;
            }

            Resp resp = null;
            try { resp = JsonUtility.FromJson<Resp>(req.downloadHandler.text); }
            catch (Exception e) { onError?.Invoke("JSON parse error: " + e.Message); yield break; }

            if (resp?.plots == null) { onError?.Invoke("No plots in payload"); yield break; }

            var list = new List<PlotModel>(resp.plots.Length);
            foreach (var d in resp.plots)
            {
                var m = new PlotModel
                {
                    slotIndex     = d.slot,              // 0..11
                    isLocked      = (d.stage == 255),    // 255 із бекенду = "заблоковано"
                    stage         = (d.stage == 255) ? -1 : d.stage,
                    plantTypeId   = (d.plantId == 0 ? (int?)null : d.plantId),
                    timeToNextSec = d.timeToNextSec,
                    needsWater    = d.needsWater,
                    hasWeeds      = d.hasWeeds
                };
                list.Add(m);
            }

            onDone?.Invoke(resp.unlocked, list);
        }
    }
}
