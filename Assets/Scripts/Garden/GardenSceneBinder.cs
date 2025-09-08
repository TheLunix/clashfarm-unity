using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public sealed class GardenSceneBinder : MonoBehaviour
{
    [SerializeField] private bool uiSlotsAreOneBased = true;
    [SerializeField] private Transform plotsRoot;

    [Header("Optional UX")]
    [SerializeField] private CanvasGroup plotsGroupBlocker; // опціонально: щоб блокувати кліки
    [SerializeField] private GameObject  loadingOverlay;    // опціонально: затемнення/спінер

    void OnEnable()
    {
        var cache = GardenStateCache.I;
        if (cache == null) return;

        if (cache.IsReady)
        {
            BindFromCache(cache);
            return;
        }

        // було: BlockInteractions(true); ShowLoading(true);
        // ЗАЛИШАЄМО тільки плейсхолдери — щоб клік працював зразу:
        ApplyPlaceholder(unlocked: 3);

        // якщо показуєш оверлей — зроби його НЕ клікабельним:
        // (CanvasGroup на overlay) interactable = false; blocksRaycasts = false;

        cache.OnReady += HandleCacheReady;
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

        // якщо є loadingOverlay — лиши blocksRaycasts = false;
        // і сховай overlay з невеличкою паузою, щоб не "з’їсти" клік:
        StartCoroutine(HideOverlayNextFrame());
    }

    private System.Collections.IEnumerator HideOverlayNextFrame()
    {
        yield return null; // 1 кадр
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
                slotIndex  = serverSlot,
                isLocked   = serverSlot >= unlocked, // оце головне
                stage      = 0,
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

    void BlockInteractions(bool on)
    {
        if (plotsGroupBlocker != null)
        {
            plotsGroupBlocker.interactable   = !on;
            plotsGroupBlocker.blocksRaycasts = on;
        }
    }

    void ShowLoading(bool on)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(on);
    }
}
