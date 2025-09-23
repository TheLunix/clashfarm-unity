// Простий кеш іконок з CDN: пам'ять + диск + префетч на диск без RAM піків.
namespace ClashFarm.Garden
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.IO;
    using System.Text;

    public static class RemoteSpriteCache
    {
        public static string BaseUrl = "https://cdn.clashfarm.com/icons/"; // !!! підстав свій, закінчується на '/'

        static readonly Dictionary<string, Sprite> _mem = new();
        // LRU для обмеження in-memory спрайтів
        static readonly LinkedList<string> _lru = new();
        static readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new();
        // Верхня межа кешу в пам'яті (кількість спрайтів)
        public static int MaxInMemory = 96;
        static string Dir => Path.Combine(Application.persistentDataPath, "icons");

        // ---------- ПУБЛІЧНЕ АПІ ----------
        public static bool TryGetInMemory(string key, out Sprite sp)
        {
            if (string.IsNullOrEmpty(key)) { sp = null; return false; }
            if (_mem.TryGetValue(key, out sp) && sp != null)
            {
                Touch(key);
                return true;
            }
            return false;
        }
        static void Touch(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_lruNodes.TryGetValue(key, out var node))
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
            }
            else
            {
                var n = new LinkedListNode<string>(key);
                _lru.AddFirst(n);
                _lruNodes[key] = n;
            }
        }

        static void DestroyKey(string key)
        {
            if (_mem.TryGetValue(key, out var sp) && sp)
            {
                var tex = sp.texture;
                if (tex) Object.Destroy(tex);
                Object.Destroy(sp);
            }
            _mem.Remove(key);
            if (_lruNodes.TryGetValue(key, out var node))
            {
                _lru.Remove(node);
                _lruNodes.Remove(key);
            }
        }

        static void MaybeEvict()
        {
            while (_mem.Count > MaxInMemory && _lru.Count > 0)
            {
                var victim = _lru.Last.Value;
                DestroyKey(victim);
            }
        }

        static void PutInMemory(string key, Sprite sp)
        {
            if (string.IsNullOrEmpty(key) || sp == null) return;
            _mem[key] = sp;
            Touch(key);
            MaybeEvict();
        }

        public static void SetMaxInMemory(int max)
        {
            MaxInMemory = Mathf.Max(8, max);
            MaybeEvict();
        }

        // зручно для дебаг-панелі
        public static string GetDiskDir() => Dir;
        public static int InMemoryCount() => _mem.Count;
        public static async Task<Sprite> GetSpriteAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_mem.TryGetValue(key, out var sp) && sp != null) return sp;

            string path = Path.Combine(Dir, key + ".png");

            // 1) спроба з диска
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
#if UNITY_2021_2_OR_NEWER
                if (t.LoadImage(bytes, true)) // markNonReadable=true → не тримаємо CPU-копію
#else
                if (t.LoadImage(bytes))
#endif
                {
#if !UNITY_2021_2_OR_NEWER
                    t.Apply(false, true); // старі юніті: руками робимо nonReadable
#endif
                    sp = Sprite.Create(t, new Rect(0,0,t.width,t.height), new Vector2(0.5f,0.5f), 100f);
                    PutInMemory(key, sp);
                    return sp;
                }
            }

            // 2) мережа → одразу пишемо на диск, щоб не плодити RAM
            Directory.CreateDirectory(Dir);
            var url = BaseUrl + key + ".png";
            byte[] data = null;
            using (var req = UnityWebRequest.Get(url))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result != UnityWebRequest.Result.Success)
                    return null;
                data = req.downloadHandler.data;
            }

            if (data == null || data.Length == 0) return null;
            try { File.WriteAllBytes(path, data); } catch { /* ок */ }

            // тепер завантажимо з диска як non-readable (див. пункт 1)
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
#if UNITY_2021_2_OR_NEWER
                if (!t.LoadImage(data, true)) return null;
#else
                if (!t.LoadImage(data)) return null;
                t.Apply(false, true);
#endif
                sp = Sprite.Create(t, new Rect(0,0,t.width,t.height), new Vector2(0.5f,0.5f), 100f);
                PutInMemory(key, sp);
                return sp;
            }
        }

        // Prefetch всіх ключів на ДИСК (без створення текстур/спрайтів → мінімум RAM)
        public static async Task PrefetchToDiskOnly(IEnumerable<string> keys, int maxParallel = 3, int softTimeoutMs = 5000)
        {
            if (keys == null) return;
            Directory.CreateDirectory(Dir);
            var list = new List<string>(keys);
            if (list.Count == 0) return;

            using var sem = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task>(list.Count);

            foreach (var key in list)
            {
                string path = Path.Combine(Dir, key + ".png");
                if (File.Exists(path)) continue; // вже є

                await sem.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var req = UnityWebRequest.Get(BaseUrl + key + ".png");
                        // зберігаємо напряму на диск
                        string tmp = path + ".tmp";
                        req.downloadHandler = new DownloadHandlerFile(tmp) { removeFileOnAbort = true };
                        var op = req.SendWebRequest();
                        while (!op.isDone) await Task.Yield();
                        if (req.result == UnityWebRequest.Result.Success)
                        {
                            try
                            {
                                if (File.Exists(path)) File.Delete(path);
                                File.Move(tmp, path);
                            }
                            catch { /* ок */ }
                        }
                        else { try { if (File.Exists(tmp)) File.Delete(tmp); } catch {} }
                    }
                    catch { /* ок */ }
                    finally { sem.Release(); }
                }));
            }

            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(softTimeoutMs));
        }

        // Діагностика
        public static string DebugReport()
        {
            long bytes = 0; int n = 0;
            foreach (var kv in _mem)
            {
                var sp = kv.Value; var tex = sp ? sp.texture : null;
                if (!tex) continue;
                n++;
                bytes += (long)tex.width * tex.height * 4;
            }
            float mb = bytes / (1024f * 1024f);
            var sb = new StringBuilder();
            sb.Append("Mem sprites: ").Append(n).Append("/").Append(MaxInMemory)
            .Append(" (~").Append(mb.ToString("0.0")).Append(" MB)")
            .Append("\nLRU keys: ").Append(_lru.Count)
            .Append("\nDisk: ").Append(Dir);
            return sb.ToString();
        }


        public static void ClearMemory()
        {
            foreach (var kv in _mem)
            {
                var sp = kv.Value;
                if (sp != null)
                {
                    var tex = sp.texture;
                    if (tex != null) Object.Destroy(tex);
                    Object.Destroy(sp);
                }
            }
            _mem.Clear();
            _lru.Clear();
            _lruNodes.Clear();
        }
    }
}
