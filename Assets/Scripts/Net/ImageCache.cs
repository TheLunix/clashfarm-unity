using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageCache : MonoBehaviour
{
    [Header("Memory cache")]
    [SerializeField] int memoryMaxItems = 256;

    [Header("Disk cache (persistent)")]
    [SerializeField] bool useDiskCache = true;
    [SerializeField] int diskTtlDays = 30;

    static ImageCache _inst;
    public static ImageCache Instance {
        get {
            if (_inst == null) {
                var go = new GameObject("ImageCache");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<ImageCache>();
            }
            return _inst;
        }
    }

    readonly Dictionary<string, Sprite> _mem = new();
    readonly LinkedList<string> _lru = new();
    readonly Dictionary<string, List<Action<Sprite>>> _inflight = new();

    string CacheDir => Path.Combine(Application.persistentDataPath, "imgcache");

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        DontDestroyOnLoad(gameObject); // ✅ якщо покладено у сцену вручну — теж живемо вічно
        if (useDiskCache && !Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir);
    }

    public void GetSprite(string url, Action<Sprite> onReady, Action<string> onError = null)
    {
        url = (url ?? "").Trim();
        if (string.IsNullOrEmpty(url)) { onError?.Invoke("Empty URL"); return; }

        if (_mem.TryGetValue(url, out var sp) && sp != null) {
            Touch(url);
            onReady?.Invoke(sp);
            return;
        }

        if (useDiskCache) {
            var path = PathFor(url);
            if (File.Exists(path) && !Expired(path)) {
                try {
                    var bytes = File.ReadAllBytes(path);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(tex, bytes, false)) {
                        var sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height),
                                                   new Vector2(0.5f,0.5f), 100f);
                        PutMemory(url, sprite);
                        onReady?.Invoke(sprite);
                        return;
                    } else {
                        SafeDelete(path);
                    }
                } catch (Exception e) {
                    Debug.LogWarning("Disk cache read failed: " + e.Message);
                }
            }
        }

        if (_inflight.TryGetValue(url, out var waiters)) {
            waiters.Add(onReady);
            return;
        } else {
            _inflight[url] = new List<Action<Sprite>> { onReady };
            StartCoroutine(Download(url, onError));
        }
    }

    public void ClearMemory()
    {
        foreach (var kv in _mem) {
            if (kv.Value != null) Destroy(kv.Value.texture);
            Destroy(kv.Value);
        }
        _mem.Clear();
        _lru.Clear();
    }

    public void ClearDisk()
    {
        if (Directory.Exists(CacheDir)) {
            foreach (var f in Directory.GetFiles(CacheDir)) SafeDelete(f);
        }
    }

    IEnumerator Download(string url, Action<string> onError)
    {
        using var req = UnityWebRequestTexture.GetTexture(url, true);
        req.timeout = 15;
        yield return req.SendWebRequest();

        if (_inflight.TryGetValue(url, out var listeners)) _inflight.Remove(url);

        if (req.result != UnityWebRequest.Result.Success) {
            onError?.Invoke($"Image load error: {req.error}");
            if (listeners != null) foreach (var cb in listeners) cb?.Invoke(null);
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(req);
        var sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height),
                                   new Vector2(0.5f,0.5f), 100f);

        PutMemory(url, sprite);

        if (useDiskCache) {
            try {
                var bytes = req.downloadHandler.data;
                if (bytes != null && bytes.Length > 0) {
                    var path = PathFor(url);
                    File.WriteAllBytes(path, bytes);
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                }
            } catch (Exception e) {
                Debug.LogWarning("Disk cache write failed: " + e.Message);
            }
        }

        if (listeners != null) foreach (var cb in listeners) cb?.Invoke(sprite);
    }

    void PutMemory(string url, Sprite sp)
    {
        _mem[url] = sp;
        _lru.AddFirst(url);

        while (_mem.Count > memoryMaxItems && _lru.Last != null) {
            var lastUrl = _lru.Last.Value;
            _lru.RemoveLast();
            if (_mem.TryGetValue(lastUrl, out var oldSp)) {
                if (oldSp != null) {
                    if (oldSp.texture != null) Destroy(oldSp.texture);
                    Destroy(oldSp);
                }
                _mem.Remove(lastUrl);
            }
        }
    }

    void Touch(string url)
    {
        var node = _lru.Find(url);
        if (node != null) { _lru.Remove(node); _lru.AddFirst(node); }
    }

    bool Expired(string path)
    {
        var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
        return age.TotalDays > diskTtlDays;
    }

    string PathFor(string url)
    {
        var hash = Md5(url);
        var ext = ".bin";
        var lower = url.ToLowerInvariant();
        if (lower.Contains(".png")) ext = ".png";
        else if (lower.Contains(".webp")) ext = ".webp";
        else if (lower.Contains(".jpg") || lower.Contains(".jpeg")) ext = ".jpg";
        return Path.Combine(CacheDir, hash + ext);
    }

    static string Md5(string s)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(s);
        var hash = md5.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    static void SafeDelete(string path)
    {
        try { File.Delete(path); } catch { }
    }
}
