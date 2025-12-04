using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ItemIconProvider : MonoBehaviour
{
    public static ItemIconProvider Instance { get; private set; }

    [SerializeField] private string itemsBaseUrl = "https://api.clashfarm.com/items/"; 
    [SerializeField] private Sprite fallbackSprite;

    private readonly Dictionary<string, Sprite> _cache = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    public IEnumerator LoadIcon(string iconKey, System.Action<Sprite> onDone)
    {
        if (string.IsNullOrWhiteSpace(iconKey))
        {
            onDone?.Invoke(fallbackSprite);
            yield break;
        }

        if (_cache.TryGetValue(iconKey, out var cached))
        {
            onDone?.Invoke(cached);
            yield break;
        }

        var url = itemsBaseUrl + iconKey + ".png";  // weapon_lvl_1.png, armor_lvl_3.png і т.д.
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ItemIconProvider] Failed to load {url}: {req.error}");
            onDone?.Invoke(fallbackSprite);
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(req);

        // 🔥 Ось тут ми ГАРАНТОВАНО беремо ВСЮ картинку
        var rect = new Rect(0, 0, tex.width, tex.height);
        var pivot = new Vector2(0.5f, 0.5f);

        // pixelsPerUnit — будь-яке твоє значення. Для UI часто 100.
        float pixelsPerUnit = 100f;

        var sprite = Sprite.Create(tex, rect, pivot, pixelsPerUnit);
        _cache[iconKey] = sprite;

        onDone?.Invoke(sprite);
    }
}
