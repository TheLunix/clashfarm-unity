using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private const string ApiRoot    = "https://api.clashfarm.com";
    private const string PlayerBase = ApiRoot + "/api/player";
    private const string PlantsBase = ApiRoot + "/api/plants";
    private static string GardenBase => ApiRoot + "/api/garden";

    // === DTO (додано) ===
    [Serializable] private class GoogleLoginReq { public string idToken; public GoogleLoginReq(string t){ idToken=t; } }

    // === DTO (існуючі) ===
    [Serializable] private class HbDto { public int playerhp; public int maxhp; }

    [Serializable] public class CombatsDto
    {
        public int combats;
        public int combatsMax;
        public int remainingToNextSec;
        public int remainingToFullSec;
        public string error; // optional
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

    // === НОВЕ: прив’язка Google до поточного гравця ===
    public IEnumerator LinkGoogleAccount(string idToken)
    {
        var url  = $"{ApiRoot}/api/auth/link-google";
        var json = JsonUtility.ToJson(new GoogleLoginReq(idToken));

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        AttachJwt(req); // додаємо Authorization: Bearer

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Google linked successfully: " + req.downloadHandler.text);
            // TODO: popup "Успішно прив’язано"
        }
        else
        {
            Debug.LogError("Link failed: " + req.responseCode + " " + req.downloadHandler.text);
            // TODO: popup помилки
        }
    }

    // === HELPERS ===
    private static WWWForm Form(params (string key, string value)[] kv)
    {
        var f = new WWWForm();
        foreach (var (k, v) in kv)
        {
            var safeKey = string.IsNullOrEmpty(k) ? "field" : k;
            var safeVal = v ?? string.Empty;
            f.AddField(safeKey, safeVal);
        }
        return f;
    }

    /// <summary> Додає JWT із PlayerPrefs (ключ "auth_token") </summary>
    private static void AttachJwt(UnityWebRequest req)
    {
        var jwt = PlayerPrefs.GetString("auth_token", "");
        if (!string.IsNullOrEmpty(jwt))
            req.SetRequestHeader("Authorization", "Bearer " + jwt);
    }

    /// <summary>
    /// Відправляє запит. Повертає true для 2xx. У разі помилки логить код, помилку і BODY.
    /// </summary>
    private static async Task<bool> Send(UnityWebRequest req)
    {
        req.timeout = 15;
        if (req.downloadHandler == null)
            req.downloadHandler = new DownloadHandlerBuffer();

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

#if UNITY_2020_3_OR_NEWER
        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
#else
        bool ok = !req.isNetworkError && !req.isHttpError && req.responseCode >= 200 && req.responseCode < 300;
#endif
        if (!ok)
        {
            string body = req.downloadHandler?.text ?? "";
            Debug.LogError($"{req.method} {req.url} -> HTTP {req.responseCode}: {req.error}\nBODY: {body}");
        }
        return ok;
    }

    private static async Task<string> PostFormGetText(string url, params (string key, string val)[] kv)
    {
        var form = Form(kv);
        using var req = UnityWebRequest.Post(url, form);
        req.downloadHandler = new DownloadHandlerBuffer();
        var ok = await Send(req);
        return ok ? (req.downloadHandler?.text ?? "") : null;
    }

    private static async Task<bool> PostOk(string url, params (string key, string val)[] kv)
    {
        var form = Form(kv);
        using var req = UnityWebRequest.Post(url, form);
        req.downloadHandler = new DownloadHandlerBuffer();
        var ok = await Send(req);
        if (!ok) return false;

        var body = req.downloadHandler?.text?.Trim();
        return body == "0" || string.Equals(body, "OK", StringComparison.OrdinalIgnoreCase) || (body?.StartsWith("{") ?? false);
    }

    // === PLAYER ===

    // Тип PlayerInfo – твій існуючий клас.
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
            Debug.LogError($"GetAccountAsync HTTP {req.responseCode}: {req.error}\nBODY: {body}");
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

    public static async Task<(int hp, int max)?> HpHeartbeatAsync(string nickname, string serialcode)
    {
        nickname   = (nickname ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();
        if (nickname.Length == 0 || serialcode.Length == 0) return null;

        using var req = UnityWebRequest.Post($"{PlayerBase}/hp/heartbeat",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode)));
        req.downloadHandler = new DownloadHandlerBuffer();

        var ok   = await Send(req);
        var json = req.downloadHandler?.text ?? string.Empty;
        if (!ok) return null;

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

    public static async Task<CombatsDto> CombatsHeartbeatAsync(string nickname, string serialcode)
    {
        nickname = (nickname ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();
        if (nickname.Length == 0 || serialcode.Length == 0) return null;

        using var req = UnityWebRequest.Post($"{PlayerBase}/combats/heartbeat",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode)));
        req.downloadHandler = new DownloadHandlerBuffer();

        var ok = await Send(req);
        if (!ok) return null;

        var txt = req.downloadHandler?.text ?? "";
        if (string.IsNullOrEmpty(txt) || txt[0] != '{') return null;

        try { return JsonUtility.FromJson<CombatsDto>(txt); }
        catch { return null; }
    }

    public static async Task<CombatsDto> CombatsUseAsync(string nickname, string serialcode)
    {
        nickname = (nickname ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();
        if (nickname.Length == 0 || serialcode.Length == 0) return null;

        using var req = UnityWebRequest.Post($"{PlayerBase}/combats/use",
            Form(("PlayerName", nickname), ("PlayerSerialCode", serialcode)));
        req.downloadHandler = new DownloadHandlerBuffer();

        var ok = await Send(req);
        if (!ok) return null;

        var txt = req.downloadHandler?.text ?? "";
        if (string.IsNullOrEmpty(txt) || txt[0] != '{') return null;

        try { return JsonUtility.FromJson<CombatsDto>(txt); }
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
            Debug.LogError($"GET /api/plants/list failed: {req.responseCode} {req.error}\nBODY: {req.downloadHandler?.text}");
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
            ("slotIndex", slot.ToString()), ("plantId", plantId.ToString()));

    public static Task<bool> WaterAsync(string nickname, string serialcode, int slot) =>
        PostOk($"{GardenBase}/water",
            ("PlayerName", nickname), ("PlayerSerialCode", serialcode),
            ("slotIndex", slot.ToString()));

    public static Task<bool> HarvestAsync(string nickname, string serialcode, int slot) =>
        PostOk($"{GardenBase}/harvest",
            ("PlayerName", nickname), ("PlayerSerialCode", serialcode),
            ("slotIndex", slot.ToString()));

    public static Task<bool> UnlockAsync(string nickname, string serialcode) =>
        PostOk($"{GardenBase}/unlock",
            ("PlayerName", nickname), ("PlayerSerialCode", serialcode));

    // === PLAYER UPGRADE (JSON) ===

    [Serializable] private class UpgradeReq
    {
        public string nickname;
        public string serialcode;
        public string stat;       // "power" | "skill" | "survivability" | "protection" | "dexterity"
        public int fromLevel;
    }

    public static async Task<PlayerInfo> PostUpgradeAsync(string nickname, string serialcode, string statWireKey, int fromLevel)
    {
        nickname   = (nickname   ?? "").Trim();
        serialcode = (serialcode ?? "").Trim();
        statWireKey = (statWireKey ?? "").Trim().ToLowerInvariant();
        if (nickname.Length == 0 || serialcode.Length == 0 || statWireKey.Length == 0) return null;

        var payload = new UpgradeReq
        {
            nickname   = nickname,
            serialcode = serialcode,
            stat       = statWireKey,
            fromLevel  = fromLevel
        };

        var url  = $"{PlayerBase}/upgrade";
        var json = JsonUtility.ToJson(payload);

        using var req = new UnityWebRequest(url, "POST");
        var body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        var ok = await Send(req);
        if (!ok || req.responseCode < 200 || req.responseCode >= 300)
        {
            Debug.LogError($"PostUpgradeAsync HTTP {req.responseCode}: {req.error}\n{req.downloadHandler?.text}");
            return null;
        }

        var txt = req.downloadHandler?.text ?? "";
        var trimmed = txt.Trim();
        if (trimmed.Length == 0 || trimmed[0] != '{')
        {
            Debug.LogWarning($"PostUpgradeAsync non-JSON body: '{trimmed}'");
            return null;
        }

        try { return JsonUtility.FromJson<PlayerInfo>(txt); }
        catch (Exception e)
        {
            Debug.LogError($"PostUpgradeAsync JSON parse error: {e.Message}\n{txt}");
            return null;
        }
    }
}
