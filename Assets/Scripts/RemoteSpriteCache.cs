/*using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class RemoteSpriteCache
{
    // ==== CONFIG ====
    public static string BaseUrl = "https://cdn.yourgame.com/images/"; // ПІДСТАВИ СВІЙ
    const int MaxRamEntries = 128;
    const int RequestTimeoutSec = 15;
    static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);

    // ==== RAM LRU ====
    static readonly LinkedList<string> _lru = new();
    static readonly Dictionary<string,(Sprite sp, LinkedListNode<string> node)> _ram = new();

    // ==== DISK / INDEX ====
    static string Root => Path.Combine(Application.persistentDataPath, "remote_sprites");
    static string IndexPath => Path.Combine(Root, "cache_index.json");

    [Serializable] class IndexEntry {
        public string file;
        public string etag;
        public string lastModified;
        public long bytes;
        public long updatedAtMs;
    }
    static Dictionary<string, IndexEntry> _idx;
    static bool _loaded;
    static readonly object _ioLock = new();

    // ==== DEDUP ====
    static readonly Dictionary<string, Task<Sprite>> _inflight = new();
    static readonly SemaphoreSlim _gate = new(1,1);

    // ==== PUBLIC ====
    public static async Task<Sprite> GetSpriteAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        // RAM
        if (_ram.TryGetValue(key, out var ram))
        {
            TouchLru(key, ram.node);
            return ram.sp;
        }

        await _gate.WaitAsync();
        try {
            if (_inflight.TryGetValue(key, out var running)) return await running;
            var t = LoadSpriteInternalAsync(key);
            _inflight[key] = t;
            return await t;
        }
        finally {
            _inflight.Remove(key);
            _gate.Release();
        }
    }

    public static IReadOnlyDictionary<string, IndexEntry> ListCached()
    {
        EnsureIndex();
        return _idx;
    }

    public static void ClearAll()
    {
        lock (_ioLock) {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
            Directory.CreateDirectory(Root);
            _idx = new Dictionary<string, IndexEntry>();
            _loaded = true;
            File.WriteAllText(IndexPath, "{}");
        }
        _ram.Clear(); _lru.Clear();
    }

    // ==== INTERNAL ====
    static async Task<Sprite> LoadSpriteInternalAsync(string key)
    {
        EnsureIndex();
        Directory.CreateDirectory(Root);

        // Disk hit?
        if (_idx.TryGetValue(key, out var e))
        {
            var file = Path.Combine(Root, e.file);
            if (File.Exists(file))
            {
                // TTL проста перевірка; за наявності etag/lastModified — зробимо HEAD
                var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(e.updatedAtMs);
                if (age < DefaultTtl)
                    return await LoadFromDiskToRam(key, file);
                // спробуємо умовне оновлення
                var sp = await TryConditionalRefresh(key, e, file);
                if (sp != null) return sp;
                // якщо мережа не вдалася — віддаємо старе
                return await LoadFromDiskToRam(key, file);
            }
        }

        // Download fresh
        var fresh = await DownloadToDisk(key, prev: null);
        if (fresh == null) return null;
        return await LoadFromDiskToRam(key, fresh.filePath);
    }

    static async Task<Sprite> TryConditionalRefresh(string key, IndexEntry e, string file)
    {
        using var req = UnityWebRequest.Head(BuildUrl(key));
        if (!string.IsNullOrEmpty(e.etag)) req.SetRequestHeader("If-None-Match", e.etag);
        if (!string.IsNullOrEmpty(e.lastModified)) req.SetRequestHeader("If-Modified-Since", e.lastModified);
        req.timeout = RequestTimeoutSec;
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result == UnityWebRequest.Result.Success && req.responseCode == 304)
            return await LoadFromDiskToRam(key, file); // не змінилось

        // Змінилось або HEAD не підтримується — качаємо GET
        var fresh = await DownloadToDisk(key, e);
        if (fresh == null) return null;
        return await LoadFromDiskToRam(key, fresh.filePath);
    }

    static async Task<(string filePath, IndexEntry entry)?> DownloadToDisk(string key, IndexEntry prev)
    {
        using var req = UnityWebRequestTexture.GetTexture(BuildUrl(key), false);
        req.timeout = RequestTimeoutSec;
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success) return null;

        var tex = DownloadHandlerTexture.GetContent(req);
        var png = ImageConversion.EncodeToPNG(tex);
        UnityEngine.Object.Destroy(tex);

        var fileName = $"{Sanitize(key)}.png";
        var path = Path.Combine(Root, fileName);
        File.WriteAllBytes(path, png);

        var e = prev ?? new IndexEntry();
        e.file = fileName;
        e.bytes = png.Length;
        e.updatedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        e.etag = req.GetResponseHeader("ETag") ?? e.etag;
        e.lastModified = req.GetResponseHeader("Last-Modified") ?? e.lastModified;

        lock (_ioLock) {
            _idx[key] = e;
            File.WriteAllText(IndexPath, JsonUtility.ToJson(new IndexWrapper{ dict = _idx }, prettyPrint:false));
        }
        return (path, e);
    }

    static async Task<Sprite> LoadFromDiskToRam(string key, string file)
    {
        // Texture2D → Sprite
        var bytes = File.ReadAllBytes(file);
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(bytes, markNonReadable:true);
        t.filterMode = FilterMode.Bilinear;
        var sp = Sprite.Create(t, new Rect(0,0, t.width, t.height), new Vector2(0.5f,0.5f), 100f);
        AddToRam(key, sp);
        return sp;
    }

    static void AddToRam(string key, Sprite sp)
    {
        if (_ram.TryGetValue(key, out var existing))
        {
            TouchLru(key, existing.node);
            _ram[key] = (sp, existing.node);
            return;
        }
        var node = _lru.AddFirst(key);
        _ram[key] = (sp, node);
        if (_ram.Count > MaxRamEntries)
        {
            var tail = _lru.Last;
            if (tail != null) {
                var k = tail.Value;
                _lru.RemoveLast();
                if (_ram.TryGetValue(k, out var val)) {
                    UnityEngine.Object.Destroy(val.sp.texture);
                    UnityEngine.Object.Destroy(val.sp);
                }
                _ram.Remove(k);
            }
        }
    }

    static void TouchLru(string key, LinkedListNode<string> node)
    {
        _lru.Remove(node);
        _lru.AddFirst(node);
    }

    static string BuildUrl(string key) => BaseUrl.EndsWith("/") ? BaseUrl + key : $"{BaseUrl}/{key}";
    static string Sanitize(string key)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) key = key.Replace(c, '_');
        return key;
    }

    [Serializable] class IndexWrapper { public Dictionary<string, IndexEntry> dict; }
    static void EnsureIndex()
    {
        if (_loaded) return;
        lock (_ioLock)
        {
            Directory.CreateDirectory(Root);
            if (File.Exists(IndexPath))
            {
                var json = File.ReadAllText(IndexPath);
                var wrap = JsonUtility.FromJson<IndexWrapper>(string.IsNullOrEmpty(json) ? "{\"dict\":{}}" : json);
                _idx = wrap?.dict ?? new Dictionary<string, IndexEntry>();
            }
            else
            {
                _idx = new Dictionary<string, IndexEntry>();
                File.WriteAllText(IndexPath, "{\"dict\":{}}");
            }
            _loaded = true;
        }
    }
}
*/