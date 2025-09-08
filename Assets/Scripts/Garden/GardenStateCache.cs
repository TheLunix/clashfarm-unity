using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class GardenStateCache : MonoBehaviour
{
    public static GardenStateCache I { get; private set; }

    [SerializeField] private string apiBase = "https://api.clashfarm.com";

    public bool CatalogReady { get; private set; }
    public bool PlotsReady { get; private set; }
    public bool IsReady => CatalogReady && PlotsReady;

    public int UnlockedSlots { get; private set; } = 3;

    public List<PlantInfo> PlantCatalog { get; private set; } = new();
    public List<PlotModel> Plots { get; private set; } = new();

    public event Action OnReady;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // предлоад за обліковими даними (під твій бекенд)
    public void PreloadByCredentials(string playerName, string serialCode)
    {
        StartCoroutine(CoPreload(playerName, serialCode));
    }

    IEnumerator CoPreload(string playerName, string serialCode)
    {
        CatalogReady = PlotsReady = false;

        // рослини
        yield return global::PlantCatalog.Load(apiBase,
            list => { PlantCatalog = list; CatalogReady = true; },
            err => { Debug.LogError("[GardenPreload] plants: " + err); CatalogReady = true; });

        // грядки
        yield return PlotsStateApi.LoadState(apiBase, playerName, serialCode,
            (unlocked, list) => { UnlockedSlots = unlocked; Plots = list; PlotsReady = true; },
            err => { Debug.LogError("[GardenPreload] plots: " + err); PlotsReady = true; });

        if (IsReady) OnReady?.Invoke();
    }

    public PlotModel GetBySlot(int slot) => Plots.FirstOrDefault(p => p.slotIndex == slot);
    
    public void SetUnlockedSlots(int value)
    {
        UnlockedSlots = Mathf.Clamp(value, 0, 12);
    }
}
