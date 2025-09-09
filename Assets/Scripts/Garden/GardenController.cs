using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class GardenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private PlantSelectionPanel plantPanel;
    [SerializeField] private PlantSelectionPanel plantPanelPrefab;
    [SerializeField] private PlantActionPanel   actionPanel;      // 👈 нова панель дій
    [SerializeField] private Transform          uiParent;
    [SerializeField] private PlotUnlockPanel unlockPanel;          // панель у сцені (опційно)
    [SerializeField] private PlotUnlockPanel unlockPanelPrefab;    // або префаб, якщо тримаєш поза сценою

    [Header("Player")]
    [SerializeField] private int playerLevel = 1;

    [Header("Catalog source")]
    [SerializeField] private bool loadFromServer = true;
    [SerializeField] private string apiBase = "https://api.clashfarm.com";
    [SerializeField] private List<PlantInfo> localCatalog = new();

    [Header("Slots indexing")]
    [SerializeField] private bool uiSlotsAreOneBased = true;

    private List<PlantInfo> _catalog;
    private bool _loading;

    void Awake()
    {
    #if UNITY_2022_2_OR_NEWER
        if (plantPanel == null) plantPanel = FindFirstObjectByType<PlantSelectionPanel>(FindObjectsInactive.Include);
        if (actionPanel == null) actionPanel = FindFirstObjectByType<PlantActionPanel>(FindObjectsInactive.Include);
    #else
        if (plantPanel == null) plantPanel = FindObjectOfType<PlantSelectionPanel>();
        if (actionPanel == null) actionPanel = FindObjectOfType<PlantActionPanel>();
    #endif
    }

    void Start()
    {
        var cache = GardenStateCache.I;
        if (cache != null && cache.CatalogReady) _catalog = cache.PlantCatalog;

        if (_catalog == null || _catalog.Count == 0)
        {
            if (loadFromServer) StartCoroutine(LoadCatalog());
        }
        else
        {
            if (plantPanel != null)
                plantPanel.Prewarm(_catalog, playerLevel, _ => { });
        }
    }

    private IEnumerator LoadCatalog()
    {
        if (_loading) yield break;
        _loading = true;

        yield return PlantCatalog.Load(apiBase,
            list => {
                _catalog = list; _loading = false;
                if (plantPanel != null)
                    plantPanel.Prewarm(_catalog, playerLevel, _ => { });
            },
            err  => { _catalog = new List<PlantInfo>(); _loading = false; Debug.LogError("[Garden] Load fail: " + err); }
        );
    }

    public void OnPlotClicked(GardenPlot plot)
    {
        if (plot == null) return;

        var cache = GardenStateCache.I;
        int unlocked = (cache != null && cache.IsReady) ? cache.UnlockedSlots : 3;

        int uiSlot = plot.SlotIndexUi;
        int serverSlot = uiSlotsAreOneBased ? (uiSlot - 1) : uiSlot;

        bool lockedByCache = serverSlot >= unlocked;
        bool isLockedNow   = plot.IsLocked || lockedByCache;

        // 1) Заблоковано → покупка
        if (isLockedNow) // ВАЖЛИВО: використовуємо isLockedNow (а не лише plot.IsLocked)
        {
            if (!EnsureUnlockPanel())
            {
                Debug.LogError("[Garden] PlotUnlockPanel is missing (assign in Inspector or set prefab).");
                return;
            }

            // тут не оголошуємо заново cache/unlocked — використовуємо ті, що вище
            int nextUi = unlocked + 1;                         // показати номер наступної (1-баз.)
            int costGold = 300 * Mathf.Max(1, unlocked - 2);    // 4-та = 300, 5-та = 600, ...

            StartCoroutine(CoOpenUnlockDeferred(unlocked + 1, costGold));

            return;
        }

        // 2) Якщо порожня → посадка
        if (plot.IsEmpty)
        {
            if (_catalog == null || _catalog.Count == 0)
            {
                Debug.LogWarning("[Garden] Catalog is empty. Loading...");
                if (loadFromServer) StartCoroutine(LoadCatalog());
                return;
            }

            if (!EnsurePlantPanel())
            {
                Debug.LogError("[Garden] PlantSelectionPanel is missing.");
                return;
            }

            plantPanel.SetData(_catalog, playerLevel,
                plant => StartCoroutine(CoPlant(serverSlot, plant)));
            plantPanel.Show();
            return;
        }

        // 3) Засаджена → панель дій (полив/збір)
        StartCoroutine(OpenActionsFor(serverSlot));
    }

    IEnumerator OpenActionsFor(int serverSlot)
    {
        var cache = GardenStateCache.I;

        PlotModel model = cache?.GetBySlot(serverSlot);
        if (model == null || _catalog == null || _catalog.Count == 0)
        {
            // підстрахуємось — підтягнемо стан
            yield return RefreshAllPlotsFromServer();
            model = GardenStateCache.I?.GetBySlot(serverSlot);
        }

        if (model == null) yield break;

        PlantInfo plant = null;
        if (model.plantTypeId.HasValue)
            plant = _catalog.FirstOrDefault(p => p.Id == model.plantTypeId.Value);

        if (actionPanel == null)
        {
            Debug.LogError("[Garden] PlantActionPanel not assigned");
            yield break;
        }

        actionPanel.Open(
            model,
            plant,
            // onWater
            () => StartCoroutine(CoWater(serverSlot)),
            // onWeed
            () => StartCoroutine(CoWeed(serverSlot)),   // 👈 додано
            // onHarvest
            () => StartCoroutine(CoHarvest(serverSlot)),
            // onClose
            () => { /* no-op */ }
        );
    }

    IEnumerator CoWeed(int serverSlot)
    {
        var name = PlayerSession.I?.Data?.nickname ?? "";
        var serial = PlayerSession.I?.Data?.serialcode ?? "";

        // Якщо бекенд ще не готовий — тут можна просто локально сховати бейдж/стан або показати тост.
        yield return PlotsStateApi.CleanWeeds(apiBase, name, serial, serverSlot,
            onOk: () => StartCoroutine(RefreshAllPlotsFromServer()),
            onError: code => Debug.LogWarning("[Garden] CleanWeeds failed: " + code)
        );
    }

    IEnumerator CoPlant(int serverSlot, PlantInfo plant)
    {
        var name   = PlayerSession.I?.Data?.nickname   ?? "";
        var serial = PlayerSession.I?.Data?.serialcode ?? "";

        yield return PlotsStateApi.Plant(apiBase, name, serial, serverSlot, plant.Id,
            onOk: () =>
            {
                plantPanel.Close();
                StartCoroutine(RefreshAllPlotsFromServer());
            },
            onError: code => {
                Debug.LogWarning("[Garden] Plant failed: " + code);
            }
        );
    }

    IEnumerator CoWater(int serverSlot)
    {
        var name   = PlayerSession.I?.Data?.nickname   ?? "";
        var serial = PlayerSession.I?.Data?.serialcode ?? "";

        yield return PlotsStateApi.Water(apiBase, name, serial, serverSlot,
            onOk: () => StartCoroutine(RefreshAllPlotsFromServer()),
            onError: code => Debug.LogWarning("[Garden] Water failed: " + code)
        );
    }

    IEnumerator CoHarvest(int serverSlot)
    {
        var name   = PlayerSession.I?.Data?.nickname   ?? "";
        var serial = PlayerSession.I?.Data?.serialcode ?? "";

        yield return PlotsStateApi.Harvest(apiBase, name, serial, serverSlot,
            onOk: () => StartCoroutine(RefreshAllPlotsFromServer()),
            onError: code => Debug.LogWarning("[Garden] Harvest failed: " + code)
        );
    }

    IEnumerator RefreshAllPlotsFromServer()
    {
        var name   = PlayerSession.I?.Data?.nickname   ?? "";
        var serial = PlayerSession.I?.Data?.serialcode ?? "";

        bool done = false;
        int unlocked = 3;
        List<PlotModel> list = null;

        yield return PlotsStateApi.LoadState(apiBase, name, serial,
            (u, l) => { unlocked = u; list = l; done = true; },
            err => { Debug.LogError("[Garden] state refresh fail: " + err); done = true; }
        );

        if (!done || list == null) yield break;

        // Перемалюємо UI напряму (без зміни кешу)
        var plots = GetPlotsInScene();
        var byId = (_catalog ?? new List<PlantInfo>()).ToDictionary(p => p.Id, p => p);

        foreach (var plot in plots)
        {
            int serverSlot = uiSlotsAreOneBased ? (plot.SlotIndexUi - 1) : plot.SlotIndexUi;
            var m = list.FirstOrDefault(x => x.slotIndex == serverSlot);
            PlantInfo plant = null;
            if (m != null && m.plantTypeId.HasValue) byId.TryGetValue(m.plantTypeId.Value, out plant);
            plot.ApplyModel(m, plant);
        }

        // (опційно) онови кеш, якщо в тебе є методи на GardenStateCache
        // GardenStateCache.I?.ApplyState(unlocked, list);
    }

    private bool EnsurePlantPanel()
    {
        if (plantPanel != null) return true;

        // шукаємо існуючу панель у сцені (включно з вимкненими GO)
    #if UNITY_2022_2_OR_NEWER
        plantPanel = FindFirstObjectByType<PlantSelectionPanel>(FindObjectsInactive.Include);
    #else
        plantPanel = FindObjectOfType<PlantSelectionPanel>();
    #endif
        if (plantPanel != null) return true;

        // якщо не знайшли — інстанціюємо з префабу
        if (plantPanelPrefab != null)
        {
            var parent = uiParent != null ? uiParent : FindFirstObjectByType<Canvas>()?.transform;
            plantPanel = Instantiate(plantPanelPrefab, parent, false);
            return plantPanel != null;
        }
        return false;
    }

    private bool EnsureUnlockPanel()
    {
        if (unlockPanel != null) return true;

#if UNITY_2023_1_OR_NEWER
    unlockPanel = FindFirstObjectByType<PlotUnlockPanel>(FindObjectsInactive.Include);
#else
        unlockPanel = FindObjectOfType<PlotUnlockPanel>(true);
#endif
        if (unlockPanel != null) return true;

        if (unlockPanelPrefab != null)
        {
            Transform parent = uiParent;
            if (parent == null)
            {
#if UNITY_2023_1_OR_NEWER
        var canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
#else
                var canvas = FindObjectOfType<Canvas>();
#endif
                parent = canvas ? canvas.transform : null;
            }

            unlockPanel = Instantiate(unlockPanelPrefab, parent, false);
            return unlockPanel != null;
        }
        return false;
    }
    private IEnumerator CoOpenUnlockDeferred(int uiSlotToShow, int price)
    {
        // Дочекаємось завершення поточного циклу обробки кліку
        yield return null;

        // Якщо панель ще не створена з якихось причин — підстрахуємось
        if (!EnsureUnlockPanel()) yield break;

        unlockPanel.Open(uiSlotToShow, price, onConfirm: () =>
        {
            StartCoroutine(CoUnlockNextSlot());
        });
    }
    private IEnumerator CoUnlockNextSlot()
    {
        // НЕ закриваємо панель наперед — блокуємо кнопки від дабл-кліку
        if (unlockPanel != null) unlockPanel.SetInteractable(false);

        var sess = PlayerSession.I;
        if (sess == null || sess.Data == null)
        {
            Debug.LogError("[Garden] No PlayerSession");
            if (unlockPanel != null) unlockPanel.SetInteractable(true);
            yield break;
        }

        bool done = false;
        string err = null;
        int newUnlocked = -1, unlockedSlot = -1, spent = 0;

        yield return PlotsStateApi.Unlock(
            apiBase,
            sess.Data.nickname,
            sess.Data.serialcode,
            onOk: (unlocked, slot, cost) =>
            {
                newUnlocked = unlocked;
                unlockedSlot = slot;
                spent = cost;
                done = true;
            },
            onError: e => { err = e; done = true; }
        );

        if (!done)
        {
            if (unlockPanel != null) unlockPanel.SetInteractable(true);
            yield break;
        }

        if (err != null)
        {
            // Показуємо інлайнову помилку на панелі, не закриваючи її
            if (unlockPanel != null)
            {
                var msg = err.Contains("NO_GOLD", StringComparison.OrdinalIgnoreCase)
                          ? "Недостатньо золота!"
                          : "Помилка покупки. Спробуйте ще раз.";
                unlockPanel.ShowError(msg);
                unlockPanel.SetInteractable(true);
            }
            else
            {
                Debug.LogError("[Garden] Unlock error: " + err);
            }
            yield break;
        }

        // Успіх → оновлюємо кеш/юай і закриваємо панель
        var cache = GardenStateCache.I;
        if (cache != null)
            cache.SetUnlockedSlots(newUnlocked);

#if UNITY_2023_1_OR_NEWER
    var plots = FindObjectsByType<GardenPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var plots = FindObjectsOfType<GardenPlot>(true);
#endif
        foreach (var p in plots)
        {
            int serverSlot = p.SlotIndexUi - 1; // UI 1-баз., сервер 0-баз.
            if (serverSlot == unlockedSlot)
            {
                p.SetLocked(false);
                p.SetWeedActive(false);
                p.SetEmpty();
                break;
            }
        }

        if (unlockPanel != null) unlockPanel.Close();
        Debug.Log($"[Garden] Unlocked slot {unlockedSlot} (spent {spent})");
    }


    GardenPlot[] GetPlotsInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<GardenPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return FindObjectsOfType<GardenPlot>(true);
#endif
    }

    public void SetPlayerLevel(int lvl) => playerLevel = Mathf.Max(1, lvl);
}
