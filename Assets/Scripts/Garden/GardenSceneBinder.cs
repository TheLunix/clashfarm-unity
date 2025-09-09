using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class GardenSceneBinder : MonoBehaviour
{
    [SerializeField] private bool uiSlotsAreOneBased = true;
    [SerializeField] private Transform plotsRoot;

    [Header("Optional UX")]
    [SerializeField] private CanvasGroup plotsGroupBlocker;
    [SerializeField] private GameObject loadingOverlay;

    void OnEnable()
    {
        // 1) Спершу показуємо плейсхолдери (перші 3 — відкриті)
        ApplyPlaceholder(unlocked: 3);

        // 2) ВІДРАЗУ вимикаємо перехоплення кліків бекдропом
        if (loadingOverlay != null)
        {
            var cg = loadingOverlay.GetComponent<CanvasGroup>();
            var img = loadingOverlay.GetComponent<UnityEngine.UI.Image>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
            if (img != null) img.raycastTarget = false;
        }

        // 3) Озброюємо «клік-ґейт» на 2 кадри — перший тап гарантовано не пропаде
        GardenClickGate.Arm(this, frames: 2);

        // 4) Переносимо бінд після повної активації підсцени
        StartCoroutine(Co_BindAfterActivated());
    }

    private IEnumerator Co_BindAfterActivated()
    {
        // даємо UI один кадр активуватись
        yield return null;

        var cache = GardenStateCache.I;
        if (cache == null) yield break;

        // (тут НІЧОГО не треба робити з overlay — ми вже вимкнули raycasts вище)

        if (cache.IsReady)
        {
            BindFromCache(cache);
            StartCoroutine(HideOverlayNextFrame()); // сховаємо сам GO кадром пізніше
        }
        else
        {
            cache.OnReady += HandleCacheReady;
        }
    }

    void OnDisable()
    {
        if (GardenStateCache.I != null)
            GardenStateCache.I.OnReady -= HandleCacheReady;
    }

    void HandleCacheReady()
    {
        var cache = GardenStateCache.I;
        if (cache == null) return;
        cache.OnReady -= HandleCacheReady;
        BindFromCache(cache);
        StartCoroutine(HideOverlayNextFrame());
    }

    private System.Collections.IEnumerator HideOverlayNextFrame()
    {
        yield return null;
        if (loadingOverlay) loadingOverlay.SetActive(false);
    }

    void BindFromCache(GardenStateCache cache)
    {
        Dictionary<int, PlantInfo> byId = cache.PlantCatalog.ToDictionary(p => p.Id);

        foreach (var plot in GetPlots())
        {
            int serverSlot = uiSlotsAreOneBased ? (plot.SlotIndexUi - 1) : plot.SlotIndexUi;
            var model = cache.GetBySlot(serverSlot);

            PlantInfo plant = null;
            if (model != null && model.plantTypeId.HasValue)
                byId.TryGetValue(model.plantTypeId.Value, out plant);

            plot.ApplyModel(model, plant);
        }
    }

    void ApplyPlaceholder(int unlocked)
    {
        foreach (var plot in GetPlots())
        {
            int serverSlot = uiSlotsAreOneBased ? (plot.SlotIndexUi - 1) : plot.SlotIndexUi;

            var model = new PlotModel
            {
                slotIndex = serverSlot,
                isLocked = serverSlot >= unlocked,
                stage = 0,
                plantTypeId = null
            };
            plot.ApplyModel(model, null);
        }
    }

    GardenPlot[] GetPlots()
    {
        if (plotsRoot != null)
            return plotsRoot.GetComponentsInChildren<GardenPlot>(true);

#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<GardenPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return FindObjectsOfType<GardenPlot>(true);
#endif
    }
    public void RefreshNow()
    {
        var cache = GardenStateCache.I;
        if (cache == null) return;

        if (!cache.IsReady)
        {
            ApplyPlaceholder(unlocked: 3);
            cache.OnReady -= HandleCacheReady;
            cache.OnReady += HandleCacheReady;
            return;
        }

        BindFromCache(cache);
    }

}
