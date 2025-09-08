using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public static class PlotsUnlockApi
{
    [Serializable] private class Resp
    {
        public bool ok;
        public string error;  // "NO_PLAYER", "NO_GOLD", "MAX_REACHED", "INVALID_AUTH"
        public int unlocked;  // скільки стало
        public int slot;      // який слот розблоковано (0..11)
        public int cost;      // скільки списали
    }

    public static IEnumerator UnlockNext(
        string apiBase,
        string playerName,
        string serialCode,
        Action<int/*unlocked*/,int/*slot*/,int/*cost*/> onOk,
        Action<string> onError)
    {
        var url = $"{apiBase.TrimEnd('/')}/api/garden/unlock";
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

            if (resp == null) { onError?.Invoke("Empty response"); yield break; }
            if (!string.IsNullOrEmpty(resp.error))
            {
                onError?.Invoke(resp.error);
                yield break;
            }

            if (!resp.ok) { onError?.Invoke("Unknown error"); yield break; }

            onOk?.Invoke(resp.unlocked, resp.slot, resp.cost);
        }
    }
}
