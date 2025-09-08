using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public sealed class GardenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private PlantSelectionPanel plantPanel;
    [SerializeField] private PlantSelectionPanel plantPanelPrefab; // опційно, якщо тримаєш панель тільки як префаб
    [SerializeField] private Transform uiParent;                   // куди інстанціювати (Canvas)

    [Header("Player")]
    [SerializeField] private int playerLevel = 1;

    [Header("Catalog source")]
    [SerializeField] private bool loadFromServer = true;
    [SerializeField] private string apiBase = "https://api.clashfarm.com";
    [SerializeField] private List<PlantInfo> localCatalog = new();
    [SerializeField] private PlotUnlockPanel unlockPanel;
    [SerializeField] private bool uiSlotsAreOneBased = true; // як у байндері

    private List<PlantInfo> _catalog;
    private bool _loading;

    void Awake()
    {
    #if UNITY_2023_1_OR_NEWER
        if (plantPanel == null) plantPanel = FindFirstObjectByType<PlantSelectionPanel>();
    #else
        if (plantPanel == null) plantPanel = FindObjectOfType<PlantSelectionPanel>();
    #endif
    }

    void Start()
    {
        // якщо вже є кеш — беремо звідти
        var cache = GardenStateCache.I;
        if (cache != null && cache.CatalogReady)
            _catalog = cache.PlantCatalog;

        if (_catalog == null || _catalog.Count == 0)
        {
            if (loadFromServer) StartCoroutine(LoadCatalog());
        }
        else
        {
            // ✅ Прогріти список: зібрати картки і прорахувати лейаут
            if (plantPanel != null)
                plantPanel.Prewarm(_catalog, playerLevel, plant => StartCoroutine(Plant(null, plant)));
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
                    plantPanel.Prewarm(_catalog, playerLevel, plant => StartCoroutine(Plant(null, plant)));
            },
            err  => { _catalog = new List<PlantInfo>(); _loading = false; Debug.LogError("[Garden] Load fail: " + err); }
        );
    }

    public void OnPlotClicked(GardenPlot plot)
    {
        Debug.Log("1");
        if (plot == null) return;

        // Дані про unlock — з кешу (або дефолтні 3)
        var cache = GardenStateCache.I;
        int unlocked  = (cache != null && cache.IsReady) ? cache.UnlockedSlots : 3;

        // визначимо серверний індекс слоту (0..11)
        int uiSlot = plot.SlotIndexUi;
        int serverSlot = uiSlotsAreOneBased ? (uiSlot - 1) : uiSlot;

        // Вважаємо слот заблокованим, якщо АБО сам plot каже, що locked,
        // АБО за кешем цей індекс >= кількості відкритих
        bool lockedByCache = serverSlot >= unlocked;
        bool isLockedNow   = plot.IsLocked || lockedByCache;
        Debug.Log("2");

        // якщо слот заблокований — пропонуємо покупку ТІЛЬКИ наступного слоту
        if (isLockedNow)
        {
            Debug.Log("3");
            int price = 300 * Mathf.Max(1, unlocked - 2); // 4-а = 300, 5-а = 600, ...
            int nextUiSlot = uiSlotsAreOneBased ? (unlocked + 1) : unlocked; // номер для показу

            unlockPanel.Open(uiSlot, price, () =>
            {
                Debug.Log("4");
                var name = PlayerSession.I?.Data?.nickname ?? "";
                var serial = PlayerSession.I?.Data?.serialcode ?? "";

                StartCoroutine(PlotsUnlockApi.UnlockNext(apiBase, name, serial,
                    (newUnlocked, openedSlot, cost) =>
                    {
                        Debug.Log("5");
                        // оновити кеш
                        GardenStateCache.I?.SetUnlockedSlots(newUnlocked);
                        var m = GardenStateCache.I?.GetBySlot(openedSlot);
                        if (m == null && GardenStateCache.I != null)
                        {
                            m = new PlotModel { slotIndex = openedSlot, isLocked = false, stage = 0, plantTypeId = null };
                            GardenStateCache.I.Plots.Add(m);
                        }
                        else if (m != null)
                        {
                            m.isLocked = false; m.stage = 0; m.plantTypeId = null;
                        }

                        // оновити конкретну грядку
                        plot.ApplyModel(new PlotModel { slotIndex = openedSlot, isLocked = false, stage = 0, plantTypeId = null }, null);
                        plot.SetWeedActive(false);

                        unlockPanel.Close();
                    },
                    err =>
                    {
                        if (err == "NO_GOLD")          unlockPanel.ShowError("Недостатньо золота", 5f);
                        else if (err == "MAX_REACHED") unlockPanel.ShowError("Досягнуто максимуму грядок", 5f);
                        else                            unlockPanel.ShowError("Помилка покупки", 5f);
                    }
                ));
            });

            return;
        }
        
        // 2) Відкрита, але зайнята → (збір/полив або що потрібно)
        if (!plot.IsEmpty)
        {
            Debug.Log($"[Garden] Plot {plot.PlotId} not empty");
            return;
        }
        // --- нижче як було: порожня (але не locked) -> відкрити панель рослин ---
        if (_catalog == null || _catalog.Count == 0)
        {
            Debug.LogWarning("[Garden] Catalog is empty. Loading...");
            if (loadFromServer) StartCoroutine(LoadCatalog());
            return;
        }

        if (!EnsurePanel())
        {
            Debug.LogError("[Garden] PlantSelectionPanel missing");
            return;
        }

        plantPanel.SetData(_catalog, playerLevel, plant => StartCoroutine(Plant(plot, plant)));
        plantPanel.Show();
    }

    private bool EnsurePanel()
    {
        if (plantPanel != null) return true;

        // спробуємо знайти ще раз
    #if UNITY_2023_1_OR_NEWER
        plantPanel = FindFirstObjectByType<PlantSelectionPanel>();
    #else
        plantPanel = FindObjectOfType<PlantSelectionPanel>();
    #endif
        if (plantPanel != null) return true;

        // якщо заданий префаб — інстанціюємо
        if (plantPanelPrefab != null)
        {
            Transform parent = uiParent;
            if (parent == null)
            {
    #if UNITY_2023_1_OR_NEWER
                var canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
    #else
                var canvas = FindObjectOfType<Canvas>();
    #endif
                parent = canvas != null ? canvas.transform : null;
            }

            plantPanel = Instantiate(plantPanelPrefab, parent, false);
            return plantPanel != null;
        }
        return false;
    }

    private IEnumerator Plant(GardenPlot plot, PlantInfo plant)
    {
        Debug.Log($"[Garden] Plant '{plant.DisplayName}' on plot '{plot.PlotId}'");
        plot.SetPlanted(plant);
        plantPanel.Close();
        yield break;
    }

    public void SetPlayerLevel(int lvl) => playerLevel = Mathf.Max(1, lvl);
}
