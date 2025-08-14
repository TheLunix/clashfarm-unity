using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class ApiClient
{
    // TODO: якщо немає nginx/https — тимчасово став http://<IP>:5062/api/player
    const string BaseUrl = "https://api.clashfarm.com/api/player";

    [System.Serializable]
    private class HbDto { public int playerhp; public int maxhp; } // ← винесено з методу

    // ==== NULL-SAFE ====
    static WWWForm Form(params (string key, string value)[] kv)
    {
        var f = new WWWForm();
        foreach (var (k, v) in kv)
        {
            var safeKey = string.IsNullOrEmpty(k) ? "field" : k;
            var safeVal = v ?? string.Empty; // ніколи не даємо null у AddField
            f.AddField(safeKey, safeVal);
        }
        return f;
    }

    static async Task<bool> Send(UnityWebRequest req)
    {
        // опційно: таймаут
        req.timeout = 15;
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();
        return req.result == UnityWebRequest.Result.Success;
    }

    public static async Task<PlayerInfo> GetAccountAsync(string nickname, string serialcode)
    {
        nickname = (nickname ?? string.Empty).Trim();
        serialcode = (serialcode ?? string.Empty).Trim();

        if (nickname.Length == 0 || serialcode.Length == 0)
        {
            Debug.LogError("GetAccountAsync: nickname/serialcode empty.");
            return null;
        }

        using var req = UnityWebRequest.Post($"{BaseUrl}/account",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode)));

        var ok = await Send(req);
        var body = req.downloadHandler?.text ?? string.Empty;
        if (!ok)
        {
            Debug.LogError($"GetAccountAsync HTTP {req.responseCode}: {req.error}");
            return null;
        }

        // Явно обробляємо не-JSON відповіді бекенда типу "1"
        var trimmed = body.Trim();
        if (trimmed == "1" || trimmed.Length == 0 || trimmed[0] != '{')
        {
            Debug.LogWarning($"GetAccountAsync non-JSON body: '{trimmed}'");
            return null;
        }

        try { return JsonUtility.FromJson<PlayerInfo>(body); }
        catch (System.Exception e)
        {
            Debug.LogError($"GetAccountAsync JSON error: {e.Message}\n{body}");
            return null;
        }
    }

    public static async Task<string> GetCellAsync(string nickname, string serialcode, string cell)
    {
        nickname = (nickname ?? string.Empty).Trim();
        serialcode = (serialcode ?? string.Empty).Trim();
        cell = (cell ?? string.Empty).Trim();
        if (nickname.Length == 0 || serialcode.Length == 0 || cell.Length == 0) return null;

        using var req = UnityWebRequest.Post($"{BaseUrl}/getcell",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode), ("cell", cell)));

        var ok = await Send(req);
        if (!ok) return null;

        var body = req.downloadHandler.text?.Trim();
        return (body == "1" || body == "3") ? null : body;
    }

    public static async Task<bool> SetCellAsync(string nickname, string serialcode, string cell, string value)
    {
        nickname = (nickname ?? string.Empty).Trim();
        serialcode = (serialcode ?? string.Empty).Trim();
        cell = (cell ?? string.Empty).Trim();
        value = (value ?? string.Empty).Trim();
        if (nickname.Length == 0 || serialcode.Length == 0 || cell.Length == 0) return false;

        using var req = UnityWebRequest.Post($"{BaseUrl}/setcell",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode), ("cell", cell), ("value", value)));

        var ok = await Send(req);
        if (!ok) return false;

        var body = req.downloadHandler.text?.Trim();
        return body == "0";
    }

    public static async Task<(int hp, int max)?> HpHeartbeatAsync(string nickname, string serialcode)
    {
        nickname = (nickname ?? string.Empty).Trim();
        serialcode = (serialcode ?? string.Empty).Trim();
        if (nickname.Length == 0 || serialcode.Length == 0) return null;

        using var req = UnityWebRequest.Post($"{BaseUrl}/hp/heartbeat",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode)));

        var ok = await Send(req);
        var json = req.downloadHandler?.text ?? string.Empty;
        if (!ok)
        {
            Debug.LogError($"Heartbeat HTTP {req.responseCode}: {req.error}");
            return null;
        }

        var trimmed = json.Trim();
        if (trimmed.Length == 0 || trimmed[0] != '{')
        {
            Debug.LogWarning($"Heartbeat non-JSON body: '{trimmed}'");
            return null;
        }

        try
        {
            var obj = JsonUtility.FromJson<HbDto>(json);
            return (obj.playerhp, obj.maxhp);
        }
        catch
        {
            Debug.LogError($"Heartbeat JSON parse failed: '{json}'");
            return null;
        }
    }
    
    public static async Task<(int combats, int max, int remaining)?> CombatsHeartbeatAsync(string nickname, string serialcode)
    {
        if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(serialcode))
            return null;

        var form = new WWWForm();
        form.AddField("PlayerName", nickname);
        form.AddField("PlayerSerialCode", serialcode);

        using var req = UnityEngine.Networking.UnityWebRequest.Post($"{BaseUrl}/combats/heartbeat", form);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();

        await req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            return null;

        var json = req.downloadHandler.text;
        var dto = JsonUtility.FromJson<CombatsHbDto>(json);
        return (dto.combats, dto.max, dto.remaining);
    }

    [Serializable]
    private struct CombatsHbDto { public int combats; public int max; public int remaining; }

}
