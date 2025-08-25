using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // якщо використовуєш TextMeshPro

public class GardenView : MonoBehaviour
{
    [Header("Refs")]
    public SlotUI[] slots = new SlotUI[12];
    public float pollInterval = 5f; // як часто тягнути state
    public PlantPicker plantPicker; // присвой у інспекторі

    // локальний runtime-стан лічильників
    float[] ttnSeconds = new float[12]; // time-to-next
    byte[]  stageCache = new byte[12];
    bool[]  waterNeeded = new bool[12];
    bool[]  locked = new bool[12];

    int unlocked = 0;

    void OnEnable()
    {
        StartCoroutine(PollLoop());
        StartCoroutine(TimersLoop());
    }
    void OnDisable()
    {
        StopAllCoroutines();
    }

    void OnClickPlant(int slot)
    {
        if (!plantPicker)
        {
            // fallback: садимо 1
            StartCoroutine(DoPlant(slot, 1));
            return;
        }

        // відкриваємо вікно вибору, і коли користувач вибере — викликаємо DoPlant
        plantPicker.Open(plantId => StartCoroutine(DoPlant(slot, plantId)));
    }

    IEnumerator PollLoop()
    {
        while (!HasCreds()) yield return null;
        var wait = new WaitForSeconds(pollInterval);

        while (true)
        {
            yield return wait;
            if (!HasCreds()) continue;

            var s = PlayerSession.I.Data;
            var task = ApiClient.GetGardenStateAsync(s.nickname, s.serialcode);
            while (!task.IsCompleted) yield return null;

            var st = task.Result;
            if (st == null) continue;

            unlocked = st.unlocked;

            // оновлюємо локальний кеш
            for (int i = 0; i < 12; i++)
            {
                var pd = (i < st.plots.Count) ? st.plots[i] : null;

                if (pd == null)
                {
                    stageCache[i] = 255; // locked (на випадок)
                    locked[i] = true;
                    waterNeeded[i] = false;
                    ttnSeconds[i] = 0;
                    continue;
                }

                stageCache[i] = pd.stage;
                locked[i]     = (pd.stage == 255) || (i >= unlocked);
                waterNeeded[i]= pd.needsWater;
                ttnSeconds[i] = Mathf.Max(0, (float)pd.timeToNextSec);

                // оновити UI-кнопки
                slots[i].SetState(stageCache[i], locked[i], waterNeeded[i], (int)ttnSeconds[i]);

                // підв’язуємо події; важливо передати поточний індекс i
                slots[i].SetupHandlers(
                    onPlant:    () => OnClickPlant(i),
                    onWater:    () => OnClickWater(i),
                    onHarvest:  () => OnClickHarvest(i),
                    onUnlock:   () => OnClickUnlock(i));
            }
        }
    }

    IEnumerator TimersLoop()
    {
        while (true)
        {
            yield return null;
            for (int i = 0; i < 12; i++)
            {
                if (locked[i]) { slots[i].SetTimer("—"); continue; }

                if (stageCache[i] == 1 || stageCache[i] == 2)
                {
                    if (ttnSeconds[i] > 0f) ttnSeconds[i] -= Time.deltaTime;
                    var secs = Mathf.Max(0, (int)ttnSeconds[i]);
                    slots[i].SetTimer(FormatMMSS(secs));
                }
                else
                {
                    // empty/grown/locked — таймер неактуальний
                    slots[i].SetTimer(stageCache[i] == 3 ? "Ready" : "");
                }
            }
        }
    }

    string FormatMMSS(int secs)
    {
        int m = secs / 60;
        int s = secs % 60;
        return $"{m:00}:{s:00}";
    }

    bool HasCreds()
    {
        var ps = PlayerSession.I;
        return ps != null && ps.Data != null &&
               !string.IsNullOrWhiteSpace(ps.Data.nickname) &&
               !string.IsNullOrWhiteSpace(ps.Data.serialcode);
    }

    // === Handlers ===

    IEnumerator DoPlant(int slot, int plantId)
    {
        var s = PlayerSession.I.Data;
        var t = ApiClient.PlantAsync(s.nickname, s.serialcode, slot, plantId);
        while (!t.IsCompleted) yield return null;

        // після успіху — форс-оновлення state
        if (t.Result) StartCoroutine(ForceRefresh());
    }

    void OnClickWater(int slot)
    {
        StartCoroutine(DoWater(slot));
    }

    IEnumerator DoWater(int slot)
    {
        var s = PlayerSession.I.Data;
        var t = ApiClient.WaterAsync(s.nickname, s.serialcode, slot);
        while (!t.IsCompleted) yield return null;

        if (t.Result)
        {
            // локально зменш час на 10% — щоб було видно ефект одразу,
            // реальний стан все одно підтягнеться з сервера
            ttnSeconds[slot] = Mathf.Ceil(ttnSeconds[slot] * 0.9f);
            waterNeeded[slot] = false;
            slots[slot].SetState(stageCache[slot], locked[slot], waterNeeded[slot], (int)ttnSeconds[slot]);
        }
        StartCoroutine(ForceRefresh());
    }

    void OnClickHarvest(int slot)
    {
        StartCoroutine(DoHarvest(slot));
    }

    IEnumerator DoHarvest(int slot)
    {
        var s = PlayerSession.I.Data;
        var t = ApiClient.HarvestAsync(s.nickname, s.serialcode, slot);
        while (!t.IsCompleted) yield return null;

        if (t.Result)
        {
            // Можеш також оновити золото в PlayerSession, якщо сервер одразу не віддає його в іншому ендпоінті
            StartCoroutine(ForceRefresh());
        }
    }

    void OnClickUnlock(int slot)
    {
        StartCoroutine(DoUnlock(slot));
    }

    IEnumerator DoUnlock(int slot)
    {
        var s = PlayerSession.I.Data;
        var t = ApiClient.UnlockAsync(s.nickname, s.serialcode, slot);
        while (!t.IsCompleted) yield return null;

        if (t.Result)
            StartCoroutine(ForceRefresh());
        else
            Debug.LogWarning($"Unlock failed for slot {slot}");
    }

    IEnumerator ForceRefresh()
    {
        // невелика пауза, щоб сервер встиг записати
        yield return new WaitForSeconds(0.15f);

        var s = PlayerSession.I.Data;
        var task = ApiClient.GetGardenStateAsync(s.nickname, s.serialcode);
        while (!task.IsCompleted) yield return null;

        var st = task.Result;
        if (st == null) yield break;

        unlocked = st.unlocked;
        for (int i = 0; i < 12; i++)
        {
            var pd = (i < st.plots.Count) ? st.plots[i] : null;
            if (pd == null) continue;

            stageCache[i] = pd.stage;
            locked[i]     = (pd.stage == 255) || (i >= unlocked);
            waterNeeded[i]= pd.needsWater;
            ttnSeconds[i] = Mathf.Max(0, (float)pd.timeToNextSec);
            slots[i].SetState(stageCache[i], locked[i], waterNeeded[i], (int)ttnSeconds[i]);
        }
    }
}

[System.Serializable]
public class SlotUI
{
    public Button plantBtn;
    public Button waterBtn;
    public Button harvestBtn;
    public Button unlockBtn;

    public TextMeshProUGUI timerText;
    public TextMeshProUGUI stateText; // опціонально
    public Image plantIcon;           // опціонально (можеш міняти за stage/plantId)

    public void SetTimer(string t)
    {
        if (timerText != null) timerText.text = t ?? "";
    }

    public void SetState(byte stage, bool isLocked, bool needsWater, int ttnSec)
    {
        // вмикаємо/вимикаємо кнопки
        if (unlockBtn) unlockBtn.gameObject.SetActive(isLocked);

        bool empty   = (!isLocked) && stage == 0;
        bool growing = (!isLocked) && (stage == 1 || stage == 2);
        bool grown   = (!isLocked) && stage == 3;

        if (plantBtn)   plantBtn.gameObject.SetActive(empty);
        if (waterBtn)   waterBtn.gameObject.SetActive(growing && needsWater);
        if (harvestBtn) harvestBtn.gameObject.SetActive(grown);

        if (stateText)
        {
            if (isLocked) stateText.text = "Locked";
            else if (empty) stateText.text = "Empty";
            else if (growing) stateText.text = needsWater ? "Need water" : "Growing";
            else if (grown) stateText.text = "Ready";
            else stateText.text = "";
        }

        if (timerText)
        {
            if (growing)
            {
                int m = Mathf.Max(0, ttnSec) / 60;
                int s = Mathf.Max(0, ttnSec) % 60;
                timerText.text = $"{m:00}:{s:00}";
            }
            else timerText.text = grown ? "Ready" : "";
        }
    }

    // Призначаємо колбеки; перед призначенням — чистимо
    public void SetupHandlers(System.Action onPlant, System.Action onWater, System.Action onHarvest, System.Action onUnlock)
    {
        if (plantBtn)
        {
            plantBtn.onClick.RemoveAllListeners();
            if (onPlant != null) plantBtn.onClick.AddListener(() => onPlant());
        }
        if (waterBtn)
        {
            waterBtn.onClick.RemoveAllListeners();
            if (onWater != null) waterBtn.onClick.AddListener(() => onWater());
        }
        if (harvestBtn)
        {
            harvestBtn.onClick.RemoveAllListeners();
            if (onHarvest != null) harvestBtn.onClick.AddListener(() => onHarvest());
        }
        if (unlockBtn)
        {
            unlockBtn.onClick.RemoveAllListeners();
            if (onUnlock != null) unlockBtn.onClick.AddListener(() => onUnlock());
        }
    }
}
