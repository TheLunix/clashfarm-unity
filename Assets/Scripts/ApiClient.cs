using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class ApiClient
{
    // === БАЗОВІ ШЛЯХИ (підкоригуй, якщо потрібен інший домен/порт) ===
    private const string ApiRoot    = "https://api.clashfarm.com";
    private const string PlayerBase = ApiRoot + "/api/player";
    private const string PlantsBase = ApiRoot + "/api/plants";
    private static string GardenBase => ApiRoot + "/api/garden"; // якщо у тебе інший маршрут — заміни тут

    // === DTO ===
    [Serializable] private class HbDto { public int playerhp; public int maxhp; }

    [Serializable] private struct CombatsHbDto
    {
        public int combats;    // поточна к-сть боїв
        public int max;        // максимум (6)
        public int remaining;  // сек до +1 бою
    }

    [Serializable] public class PlantListWrap { public List<PlantInfo> plants; }

    [Serializable] public class PlantInfo
    {
        public int id;
        public string name;
        public string description;
        public int requiredLevel;
        public int growthTimeMinutes;
        public int sellPrice;
        public int isActive; // 1/0
        // за бажанням: ключі іконок: seedIconKey / sproutIconKey / readyIconKey
    }

    [Serializable] public class GardenState
    {
        public int unlocked;          // скільки слотів розблоковано
        public List<PlotDto> plots;   // стан кожної грядки
    }

    [Serializable] public class PlotDto
    {
        public int slot;             // 0..11
        public int plantId;          // 0 якщо порожньо
        public byte stage;           // 0=empty,1=seed,2=sprout,3=grown
        public long timeToNextSec;   // сек до наступної стадії або готовності
        public bool needsWater;      // чи просить води
        public bool hasWeeds;        // чи є бур’ян
    }

    // === HELPERS ===
    private static WWWForm Form(params (string key, string value)[] kv)
    {
        var f = new WWWForm();
        foreach (var (k, v) in kv)
        {
            var safeKey = string.IsNullOrEmpty(k) ? "field" : k;
            var safeVal = v ?? string.Empty; // ніколи не пхай null у AddField
            f.AddField(safeKey, safeVal);
        }
        return f;
    }

    private static async Task<bool> Send(UnityWebRequest req)
    {
        req.timeout = 15;
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();
        return req.result == UnityWebRequest.Result.Success;
    }

    private static async Task<string> PostFormGetText(string url, params (string key, string val)[] kv)
    {
        var form = Form(kv); // НЕ using
        using var req = UnityWebRequest.Post(url, form);
        req.downloadHandler = new DownloadHandlerBuffer();
        var ok = await Send(req);
        return ok ? (req.downloadHandler?.text ?? "") : null;
    }

    private static async Task<bool> PostOk(string url, params (string key, string val)[] kv)
    {
        var form = Form(kv);
        using var req = UnityWebRequest.Post(url, form);
        var ok = await Send(req);
        if (!ok) return false;
        var body = req.downloadHandler?.text?.Trim();
        // підлаштовуємося під бекенд: "0" або "OK" або JSON
        return body == "0" || string.Equals(body, "OK", StringComparison.OrdinalIgnoreCase) || (body?.StartsWith("{") ?? false);
    }

    // === PLAYER ===

    public static async Task<PlayerInfo> GetAccountAsync(string nickname, string serialcode)
    {
        nickname   = (nickname   ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();

        if (nickname.Length == 0 || serialcode.Length == 0)
        {
            Debug.LogError("GetAccountAsync: nickname/serialcode empty.");
            return null;
        }

        using var req = UnityWebRequest.Post($"{PlayerBase}/account",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode)));

        var ok   = await Send(req);
        var body = req.downloadHandler?.text ?? string.Empty;

        if (!ok)
        {
            Debug.LogError($"GetAccountAsync HTTP {req.responseCode}: {req.error}");
            return null;
        }

        var trimmed = body.Trim();
        if (trimmed == "1" || trimmed.Length == 0 || trimmed[0] != '{')
        {
            Debug.LogWarning($"GetAccountAsync non-JSON body: '{trimmed}'");
            return null;
        }

        try { return JsonUtility.FromJson<PlayerInfo>(body); }
        catch (Exception e)
        {
            Debug.LogError($"GetAccountAsync JSON error: {e.Message}\n{body}");
            return null;
        }
    }

    public static async Task<string> GetCellAsync(string nickname, string serialcode, string cell)
    {
        nickname   = (nickname   ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();
        cell       = (cell       ?? "").Trim();
        if (nickname.Length == 0 || serialcode.Length == 0 || cell.Length == 0) return null;

        using var req = UnityWebRequest.Post($"{PlayerBase}/getcell",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode), ("cell", cell)));

        var ok = await Send(req);
        if (!ok) return null;

        var body = req.downloadHandler?.text?.Trim();
        return (body == "1" || body == "3") ? null : body;
    }

    public static async Task<bool> SetCellAsync(string nickname, string serialcode, string cell, string value)
    {
        nickname   = (nickname   ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();
        cell       = (cell       ?? "").Trim();
        value      = (value      ?? "").Trim();
        if (nickname.Length == 0 || serialcode.Length == 0 || cell.Length == 0) return false;

        using var req = UnityWebRequest.Post($"{PlayerBase}/setcell",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode), ("cell", cell), ("value", value)));

        var ok = await Send(req);
        if (!ok) return false;

        var body = req.downloadHandler?.text?.Trim();
        return body == "0";
    }

    public static async Task<(int hp, int max)?> HpHeartbeatAsync(string nickname, string serialcode)
    {
        nickname   = (nickname   ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();
        if (nickname.Length == 0 || serialcode.Length == 0) return null;

        using var req = UnityWebRequest.Post($"{PlayerBase}/hp/heartbeat",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode)));

        var ok   = await Send(req);
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
        nickname   = (nickname   ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();
        if (nickname.Length == 0 || serialcode.Length == 0) return null;

        using var req = UnityWebRequest.Post($"{PlayerBase}/combats/heartbeat",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode)));
        req.downloadHandler = new DownloadHandlerBuffer();

        await req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) return null;

        var json = req.downloadHandler?.text ?? "";
        try
        {
            var dto = JsonUtility.FromJson<CombatsHbDto>(json);
            return (dto.combats, dto.max, dto.remaining);
        }
        catch { return null; }
    }

    // === PLANTS ===

    public static async Task<List<PlantInfo>> GetPlantsAsync(bool onlyActive = true)
    {
        string url = PlantsBase + "/list" + (onlyActive ? "?onlyActive=1" : "");
        using var req = UnityWebRequest.Get(url);
        req.downloadHandler = new DownloadHandlerBuffer();

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GET /api/plants/list failed: {req.responseCode} {req.error}");
            return null;
        }
        try
        {
            var wrap = JsonUtility.FromJson<PlantListWrap>(req.downloadHandler.text);
            return wrap?.plants ?? new List<PlantInfo>();
        }
        catch (Exception e)
        {
            Debug.LogError("Plants parse error: " + e);
            return null;
        }
    }

    // === GARDEN ===
    public static async Task<GardenState> GetGardenStateAsync(string nickname, string serialcode)
    {
        var txt = await PostFormGetText($"{GardenBase}/state",
            ("PlayerName", nickname), ("PlayerSerialCode", serialcode));
        if (string.IsNullOrEmpty(txt) || txt == "1") return null;

        try { return JsonUtility.FromJson<GardenState>(txt); }
        catch (Exception e)
        {
            Debug.LogError("GardenState parse: " + e + "\n" + txt);
            return null;
        }
    }

    public static Task<bool> PlantAsync(string nickname, string serialcode, int slot, int plantId) =>
        PostOk($"{GardenBase}/plant",
            ("PlayerName", nickname), ("PlayerSerialCode", serialcode),
            ("slot", slot.ToString()), ("plantId", plantId.ToString()));

    public static Task<bool> WaterAsync(string nickname, string serialcode, int slot) =>
        PostOk($"{GardenBase}/water",
            ("PlayerName", nickname), ("PlayerSerialCode", serialcode),
            ("slot", slot.ToString()));

    public static Task<bool> HarvestAsync(string nickname, string serialcode, int slot) =>
        PostOk($"{GardenBase}/harvest",
            ("PlayerName", nickname), ("PlayerSerialCode", serialcode),
            ("slot", slot.ToString()));

    public static Task<bool> UnlockAsync(string nickname, string serialcode, int slot) =>
        PostOk($"{GardenBase}/unlock",
            ("PlayerName", nickname), ("PlayerSerialCode", serialcode),
            ("slot", slot.ToString()));
}
