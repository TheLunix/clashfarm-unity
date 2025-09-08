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
    // === Налаштування ===
    [Header("Memory cache")]
    [SerializeField] int memoryMaxItems = 256;                 // скільки спрайтів тримати в RAM
    [Header("Disk cache (persistent)")]
    [SerializeField] bool useDiskCache = true;
    [SerializeField] int diskTtlDays = 30;                     // час життя файлу на диску

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

    // Пам'ять: LRU через список використання
    readonly Dictionary<string, Sprite> _mem = new();
    readonly LinkedList<string> _lru = new();                  // останні звертання (head — найсвіжіші)
    readonly Dictionary<string, List<Action<Sprite>>> _inflight = new(); // хто чекає на одне й те саме завантаження

    string CacheDir => Path.Combine(Application.persistentDataPath, "imgcache");

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        if (useDiskCache && !Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir);
    }

    // === Публічний API ===
    public void GetSprite(string url, Action<Sprite> onReady, Action<string> onError = null)
    {
        url = (url ?? "").Trim();
        if (string.IsNullOrEmpty(url)) { onError?.Invoke("Empty URL"); return; }

        // 1) пам'ять
        if (_mem.TryGetValue(url, out var sp) && sp != null) {
            Touch(url);
            onReady?.Invoke(sp);
            return;
        }

        // 2) диск
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
                        // зіпсований файл — видаляємо
                        SafeDelete(path);
                    }
                } catch (Exception e) {
                    Debug.LogWarning("Disk cache read failed: " + e.Message);
                }
            }
        }

        // 3) завантаження з мережі (де-дуплікація)
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

    // === Внутрішнє ===
    IEnumerator Download(string url, Action<string> onError)
    {
        using var req = UnityWebRequestTexture.GetTexture(url, true); // nonReadable=true зменшує RAM
        req.timeout = 15;
        yield return req.SendWebRequest();

        List<Action<Sprite>> listeners = null;
        if (_inflight.TryGetValue(url, out listeners)) _inflight.Remove(url);

        if (req.result != UnityWebRequest.Result.Success) {
            onError?.Invoke($"Image load error: {req.error}");
            if (listeners != null) foreach (var cb in listeners) cb?.Invoke(null);
            yield break;
        }

        // Спрайт
        var tex = DownloadHandlerTexture.GetContent(req); // Texture2D
        var sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height),
                                   new Vector2(0.5f,0.5f), 100f);

        PutMemory(url, sprite);

        // Зберегти байти на диск (оригінальні, без перекодування)
        if (useDiskCache) {
            try {
                var bytes = req.downloadHandler.data; // сирі байти відповіді
                if (bytes != null && bytes.Length > 0) {
                    File.WriteAllBytes(PathFor(url), bytes);
                    // торкнемо час доступу для TTL
                    File.SetLastWriteTimeUtc(PathFor(url), DateTime.UtcNow);
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
        // видалити надлишок (LRU)
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
        // перенести url в голову списку
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
        // спробуємо вгадати розширення з URL (png/webp/jpg)
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
        try { File.Delete(path); } catch { /* ignore */ }
    }
}
