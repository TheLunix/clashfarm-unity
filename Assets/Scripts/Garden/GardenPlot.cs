using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class GardenPlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    [SerializeField] private string plotId = "plot_01";
    [SerializeField] private bool isEmpty = true;

    [Header("Visual (UI)")]
    [SerializeField] private Image background;
    [SerializeField] private Color emptyTint = Color.white;
    [SerializeField] private Color plantedTint = Color.white;

    [Header("Crop layer (child Image)")]
    [SerializeField] private Image cropImage;
    [SerializeField] private bool autoFindCropImage = true;
    [SerializeField] private Sprite clearPlaceholder;
    [SerializeField] private bool useFruitAtStage3 = false;
    [Header("Info overlay (child 'info')")]
    [SerializeField] private GameObject infoRoot;   // GO з таймером на грядці
    [SerializeField] private TMP_Text infoTimer;   // текст таймера на грядці

    [Header("Decor")]
    [SerializeField] private GameObject weed;

    private GardenController _controller;
    private bool _isLocked;

    public string PlotId => plotId;
    public bool IsEmpty => isEmpty;

    // Live model & корутина тікання
    private PlotModel _model;
    private Coroutine _timerCo;

    void Awake()
    {
#if UNITY_2022_2_OR_NEWER
        _controller = FindFirstObjectByType<GardenController>();
#else
        _controller = FindObjectOfType<GardenController>();
#endif
        if (background == null) background = GetComponent<Image>();

        if (weed != null)
        {
            var wi = weed.GetComponent<Image>();
            if (wi != null) wi.raycastTarget = false;
        }

        if (cropImage == null && autoFindCropImage)
            cropImage = FindCropImageCandidate();

        if (cropImage != null) cropImage.raycastTarget = false;

        if (clearPlaceholder == null)
            clearPlaceholder = MakeRuntimeClearSprite();

        ApplyVisual();
        SetCropSprite(clearPlaceholder);
    }

    private Image FindCropImageCandidate()
    {
        var imgs = GetComponentsInChildren<Image>(true);
        Image weedImg = weed ? weed.GetComponent<Image>() : null;

        foreach (var img in imgs)
        {
            if (img == background || img == weedImg) continue;
            if (img.transform == this.transform) continue;
            if (string.Equals(img.name, "Crop", System.StringComparison.OrdinalIgnoreCase))
                return img;
        }
        foreach (var img in imgs)
        {
            if (img == background || img == weedImg) continue;
            if (img.transform == this.transform) continue;
            var n = img.name.ToLowerInvariant();
            if (n.Contains("crop") || n.Contains("plant") || n.Contains("icon"))
                return img;
        }
        foreach (var img in imgs)
        {
            if (img == background || img == weedImg) continue;
            if (img.transform == this.transform) continue;
            return img;
        }
        return null;
    }

    private Sprite MakeRuntimeClearSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, new Color(1, 1, 1, 0));
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        sp.name = "clear_runtime";
        return sp;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // відсікаємо перший клік під час активації підсцени
        if (!GardenClickGate.Ready) return;

        if (_controller == null) { Debug.LogError("[GardenPlotUI] GardenController not found"); return; }
        _controller.OnPlotClicked(this);
    }

    public void SetPlanted(PlantInfo plant)
    {
        isEmpty = false;
        ApplyVisual();
    }

    public void SetEmpty()
    {
        isEmpty = true;
        ApplyVisual();

        if (_timerCo != null) { StopCoroutine(_timerCo); _timerCo = null; }
        if (infoRoot) infoRoot.SetActive(false);
        if (infoTimer) infoTimer.text = "";

        SetCropSprite(clearPlaceholder);
    }

    public void SetWeedActive(bool on)
    {
        if (weed != null) weed.SetActive(on);
    }

    private void ApplyVisual()
    {
        if (background != null) background.color = isEmpty ? emptyTint : plantedTint;
    }

#if UNITY_EDITOR
    void Reset()
    {
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0);
        }
        img.raycastTarget = true;
    }
#endif

    [SerializeField] private int slotIndexUi = 1;
    public int SlotIndexUi => slotIndexUi;

    public void ApplyModel(
        PlotModel model,
        PlantInfo plant,
        string baseUrl = "https://api.clashfarm.com/plants",
        string suffix = ".png")
    {
        bool lockedNow = (model == null) || model.isLocked;
        SetLocked(lockedNow);          // оновлює isLocked і (за потреби) lockOverlay
        SetWeedActive(lockedNow);      // декоративний бур’ян на заблокованих

        if (lockedNow)
        {
            SetEmpty();
            return;
        }

        if (model.plantTypeId == null || model.stage == 0)
        {
            SetEmpty();
            SetWeedActive(false);
            return;
        }

        isEmpty = false;
        ApplyVisual();
        SetWeedActive(false);

        string iconName = null;
        if (plant != null)
        {
            switch (model.stage)
            {
                case 1: iconName = plant.IconSeed; break;
                case 2: iconName = plant.IconPlant; break;
                case 3: iconName = useFruitAtStage3 ? plant.IconFruit : plant.IconGrown; break;
            }
        }

        if (string.IsNullOrEmpty(iconName))
        {
            SetCropSprite(clearPlaceholder);
            return;
        }

        string url = BuildUrl(baseUrl, iconName, suffix);
        if (cropImage == null) return;

        var cache = ImageCache.Instance;
        if (cache == null)
        {
            StartCoroutine(RetryLoadNextFrame(url));
            return;
        }

        cache.GetSprite(url, s =>
        {
            if (this == null || cropImage == null) return;
            cropImage.sprite = s != null ? s : clearPlaceholder;
        });

        // Оновлюємо таймер на грядці
        SetInfoTimer(model);
    }

    private IEnumerator RetryLoadNextFrame(string url)
    {
        yield return null;
        var cache = ImageCache.Instance;
        if (cache != null && cropImage != null)
        {
            cache.GetSprite(url, s =>
            {
                if (this == null || cropImage == null) return;
                cropImage.sprite = s != null ? s : clearPlaceholder;
            });
        }
    }

    private string BuildUrl(string baseUrl, string name, string suffix)
    {
        string b = (baseUrl ?? "").Trim().TrimEnd('/');
        string n = (name ?? "").Trim();
        if (string.IsNullOrEmpty(n)) return null;

        string sfx = (suffix ?? "").Trim();
        if (!n.Contains("."))
        {
            if (!string.IsNullOrEmpty(sfx) && !sfx.StartsWith(".")) sfx = "." + sfx;
            n += sfx;
        }
        return string.IsNullOrEmpty(b) ? n : (b + "/" + n);
    }

    private void SetCropSprite(Sprite s)
    {
        if (cropImage != null) cropImage.sprite = s;
    }

    private void SetInfoTimer(PlotModel model)
    {
        _model = model;

        // зупини попередній тікер
        if (_timerCo != null) { StopCoroutine(_timerCo); _timerCo = null; }

        if (infoRoot) infoRoot.SetActive(model != null);

        if (model == null)
        {
            if (infoTimer) infoTimer.text = "";
            return;
        }

        // показуємо час лише поки росте
        if (infoTimer)
        {
            if (model.stage >= 3 || model.timeToNextSec <= 0)
            {
                infoTimer.text = "—";
            }
            else
            {
                infoTimer.text = Format((int)model.timeToNextSec);
                _timerCo = StartCoroutine(TickTimer());
            }
        }
    }

    private System.Collections.IEnumerator TickTimer()
    {
        while (_model != null && _model.stage < 3 && _model.timeToNextSec > 0)
        {
            _model.timeToNextSec = Math.Max(0, _model.timeToNextSec - 1);
            if (infoTimer) infoTimer.text = Format((int)_model.timeToNextSec);
            yield return new WaitForSeconds(1f);
        }

        // добігли до нуля
        if (_model != null)
        {
            if (infoTimer) infoTimer.text = (_model.stage >= 3) ? "—" : "0с";
        }
        _timerCo = null;
    }

    private string Format(int sec)
    {
        var t = TimeSpan.FromSeconds(sec);
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}год {t.Minutes}хв";
        if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}хв {t.Seconds}с";
        return $"{t.Seconds}с";
    }
    [SerializeField] private bool isLocked = false;        // якщо тримаєш локальний стан
    [SerializeField] private GameObject lockOverlay;       // (опційно) кастомний бейдж "замкнено"
    [SerializeField] private GameObject weedRoot;          // твій об’єкт бур’яну (якщо такий є)

    public bool IsLocked => isLocked;

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            if (lockOverlay) lockOverlay.SetActive(locked);
            // якщо на заблокованих грядках показувався "бур’ян як антураж", можеш теж вмикати/вимикати:
            if (weedRoot) weedRoot.SetActive(locked);
        }


}
