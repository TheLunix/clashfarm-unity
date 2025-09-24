// Assets/Scripts/Garden/GardenController.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ClashFarm.Garden
{
    public sealed class GardenController : MonoBehaviour
    {
        public static GardenController I { get; private set; }

        [Header("Bind")]
        public GardenPlotView[] plotViews = new GardenPlotView[12];
        public GameObject clickGate;   // опційний оверлей "Loading..."

        [Header("Polling")]
        public float pollIntervalSec = 12f;
        float _pollT;
        [Header("UI Timers")]
        public float uiTimerIntervalSec = 0.5f;
        float _uiTimerT;
        [Header("Panels")]
        public PlotUnlockPanel unlockPanel;      // перетягни у інспекторі
        public PlantSelectionPanel plantSelect;  // перетягни у інспекторі
        public GardenStatusBar statusBar;

        GardenSession S => GardenSession.I;
        bool _busy;
        void Awake() => I = this;

        async void Start()
        {
            if (!statusBar) statusBar = FindFirstObjectByType<GardenStatusBar>(FindObjectsInactive.Include);
            if (clickGate) clickGate.SetActive(!PlantCatalogCache.IsReady);

            // Якщо сесія ще не створена — зробимо її тут
            if (S == null)
            {
                var go = new GameObject("GardenSession");
                go.AddComponent<GardenSession>();
            }

            if (!S.IsReady)
                S.OnReady += OnSessionReady;
            else
                await FirstApplyAndRefresh();

            await EnsurePlants();
        }

        async Task FirstApplyAndRefresh()
        {
            ApplyAll();
            await RefreshFromServer(); // перша синхронізація
            ApplyAll();
        }

        void OnSessionReady() { _ = FirstApplyAndRefresh(); }

        void Update()
        {
            if (S == null || !S.IsReady) return;

            _uiTimerT += Time.deltaTime;
            if (_uiTimerT >= uiTimerIntervalSec)
            {
                _uiTimerT = 0f;
                long now = S.NowServerLikeMs();
                for (int i = 0; i < plotViews.Length; i++)
                    plotViews[i]?.UpdateTimer(now);
            }

            _pollT += Time.deltaTime;
            if (_pollT >= pollIntervalSec)
            {
                _pollT = 0f;
                _ = RefreshFromServer();
            }
        }

        public async Task RefreshFromServer()
        {
            try
            {
                var resp = await GardenApi.GetStateAsync(S.PlayerName, S.PlayerSerialCode);
                S.ServerTimeAtLoginMs = resp.serverTimeMs;
                S.RealtimeAtLoginS = Time.realtimeSinceStartup;

                for (int i = 0; i < S.Plots.Length; i++)
                    S.Plots[i].Unlocked = i < resp.unlockedSlots;

                if (resp.plots != null)
                    foreach (var p in resp.plots)
                        S.MergeFromDto(p);

                ApplyAll();
            }
            catch (Exception e)
            {
                Debug.LogError($"RefreshFromServer failed: {e}");
            }
        }

        void ApplyAll()
        {
            if (!S.IsReady) return;
            long now = S.NowServerLikeMs();
            for (int i = 0; i < plotViews.Length; i++)
                if (plotViews[i] != null) plotViews[i].SetState(S.Plots[i], now);
        }

        // ====== Взаємодії ======

        public async void OnPlotClicked(int slot)
        {
            if (_busy) return;
            // легкий візуальний фідбек
            if (slot >= 0 && slot < plotViews.Length) plotViews[slot]?.FlashOnce();
            var st = S.Plots[slot];
            if (!st.Unlocked)
            {
                int nextIdx = NextUnlockIndex();                    // зазвичай це кількість відкритих
                if (slot != nextIdx)
                {
                    // Якщо натиснули не на наступний — просто покажемо підказку/панель із ціною на наступний
                    int price = PriceForSlot(nextIdx + 1);
                    unlockPanel?.Open(nextIdx, price, async () => { await Unlock(nextIdx); });
                    return;
                }

                int myPrice = PriceForSlot(slot + 1);
                unlockPanel?.Open(slot, myPrice, async () => { await Unlock(slot); });
                return;
            }

            if (st.Stage == 3) { await Harvest(slot); return; }
            // Порядок дій: бур’ян → полив → збір → посадка
            if (st.Weeds && st.Stage < 3)
            {
                Debug.Log("Weed first");
                await Weed(slot);
                return;
            }

            if (st.NeedWater)
            {
                Debug.Log("Water");
                await Water(slot);
                return;
            }

            if (st.Stage == 3)
            {
                Debug.Log("Harvest");
                await Harvest(slot);
                return;
            }

            if (st.Stage == 0)
            {
                await EnsurePlants(); // це тягне GardenApi.GetPlantsAsync()
                var d = PlayerSession.I?.Data;

                int playerLevel = d.playerlvl; // або свій спосіб
                var ordered = SortForPlayerLevel(playerLevel); // IEnumerable<GardenApi.PlantCatalogItem>

                // Конвертація у PlantInfo для панелі:
                var list = new List<PlantInfo>();
                foreach (var p in ordered)
                {
                    list.Add(new PlantInfo
                    {
                        Id = p.id,
                        DisplayName = p.displayName,
                        Description = p.description,
                        UnlockLevel = p.unlockLevel,
                        GrowthTimeMinutes = p.growthTimeMinutes,
                        SellPrice = p.sellPrice,
                        IconSeed = p.iconSeed,
                        IconPlant = p.iconPlant,
                        IconGrown = p.iconGrown,
                        IconFruit = p.iconFruit,
                        IsActive = p.isActive == 1
                    });
                }

                // Твоя панель: спершу передаємо дані, потім Show()
                plantSelect?.SetData(list, playerLevel, onPlant: info =>
                {
                    _ = PlantViaPanel(slot, info.Id);
                });
                plantSelect?.Show();
                return;
            }
            if (st.Stage == 1 || st.Stage == 2)
            {
                // На цій точці НІ бур’янів, НІ потреби в поливі — просто чекаємо.
                string msg = "Рослина полита, бур'янів немає — просто чекаємо врожай!";

                // Додамо коротку підказку скільки залишилось (до повного дозрівання)
                long now = S.NowServerLikeMs();
                if (st.TimeEndGrowthMs > now)
                {
                    long remain = st.TimeEndGrowthMs - now;
                    msg += " (~" + FormatShort(remain) + ")";
                }

                if (statusBar) statusBar.Show(msg);
                else ShowToast(msg); // фолбек, якщо статус-бар не присвоєний
                GardenBarks.I?.SayIdle();
                return;
            }
            // Інакше — нічого не робимо
        }

        public async Task Plant(int slot, int plantId)
        {
            if (_busy) return; _busy = true;
            try
            {
                try
                {
                    var res = await GardenApi.PlantAsync(S.PlayerName, S.PlayerSerialCode, slot, plantId);
                    if (res == null || !res.ok)
                    {
                        ShowToast(MapError(res != null ? res.error : "network"));
                        GardenSfx.I?.PlayError();
                        if (slot >= 0 && slot < plotViews.Length) plotViews[slot]?.ShakeOnce();
                        return;
                    }
                    S.MergeFromDto(res.dto);
                    ApplyAll();
                    GardenBarks.I?.SayPlant();
                    GardenSfx.I?.PlayPlant();
                }
                catch (Exception e) { Debug.LogError(e); }
            }
            finally { _busy = false; }
        }

        public async Task Water(int slot)
        {
            if (_busy) return; _busy = true;
            try
            {
                try
                {
                    var res = await GardenApi.WaterAsync(S.PlayerName, S.PlayerSerialCode, slot);
                    if (!res.ok)
                    {
                        ShowToast(MapError(res.error)); // TODO: твоя система тостів
                        GardenSfx.I?.PlayError();
                        if (slot >= 0 && slot < plotViews.Length) plotViews[slot]?.ShakeOnce();
                        return;
                    }
                    S.MergeFromDto(res.dto);
                    ApplyAll();
                    GardenBarks.I?.SayWater();
                    GardenSfx.I?.PlayWater();
                }
                catch (Exception e) { Debug.LogError(e); }
            }
            finally { _busy = false; }
        }

        public async Task Weed(int slot)
        {
            if (_busy) return; _busy = true;
            try
            {
                try
                {
                    var res = await GardenApi.WeedAsync(S.PlayerName, S.PlayerSerialCode, slot);
                    if (!res.ok)
                    {
                        ShowToast(MapError(res.error));
                        GardenSfx.I?.PlayError();
                        if (slot >= 0 && slot < plotViews.Length) plotViews[slot]?.ShakeOnce();
                        return;
                    }
                    S.MergeFromDto(res.dto);
                    ApplyAll();
                    GardenBarks.I?.SayWeed();
                    GardenSfx.I?.PlayWeed();
                }
                catch (Exception e) { Debug.LogError(e); }
            }
            finally { _busy = false; }
        }

        public async Task Harvest(int slot)
        {
            if (_busy) return; _busy = true;
            try
            {
                int optimistic = 0; // скільки додали миттєво
                try
                {
                    // 0) Порахуємо очікуваний дохід локально (з даних слоту)
                    optimistic = Mathf.Max(0, S.Plots[slot]?.SellPrice ?? 0);

                    // 1) Миттєво оновимо HUD (оптимістично)
                    if (optimistic > 0 && PlayerSession.I != null)
                        PlayerSession.I.Patch(pi => pi.playergreen = Mathf.Max(0, pi.playergreen + optimistic));

                    // 2) Реальний виклик на сервер
                    var res = await GardenApi.HarvestAsync(S.PlayerName, S.PlayerSerialCode, slot);
                    if (res == null || !res.ok)
                    {
                        // Відкатимо оптимістичне нарахування, якщо було
                        if (optimistic > 0 && PlayerSession.I != null)
                            PlayerSession.I.Patch(pi => pi.playergreen = Mathf.Max(0, pi.playergreen - optimistic));

                        ShowToast(MapError(res != null ? res.error : "network"));
                        GardenSfx.I?.PlayError();
                        return;
                    }
                    var dto = res.dto;

                    // 3) Якщо бек дав фактичну суму і вона відрізняється — підкоригуємо HUD
                    if (PlayerSession.I != null && dto.greenAdded > 0 && dto.greenAdded != optimistic)
                        PlayerSession.I.Patch(pi => pi.playergreen = Mathf.Max(0, pi.playergreen - optimistic + dto.greenAdded));

                    // 4) Оновимо стан грядки
                    if (dto.newPlot != null)
                    {
                        S.MergeFromDto(dto.newPlot);
                        ApplyAll();
                    }
                    else
                    {
                        await RefreshFromServer(); // добираємо актуальний стан, якщо бек не прислав newPlot
                    }

                    GardenBarks.I?.SayHarvest();
                    GardenSfx.I?.PlayHarvest();
                }
                catch (Exception e)
                {
                    // Якщо запит упав — відкатимо оптимістичне оновлення, щоб HUD не брехав
                    if (optimistic > 0 && PlayerSession.I != null)
                        PlayerSession.I.Patch(pi => pi.playergreen = Mathf.Max(0, pi.playergreen - optimistic));
                    Debug.LogError(e);
                }
            }
            finally { _busy = false; }
        }

        public async Task Unlock(int slot)
        {
            if (_busy) return; _busy = true;
            try
            {
                try
                {
                    var res = await GardenApi.UnlockAsync(S.PlayerName, S.PlayerSerialCode);
                    if (res == null)
                    {
                        if (unlockPanel != null) unlockPanel.ShowError("Сервер недоступний. Спробуй ще раз.");
                        return;
                    }
                    if (!res.ok)
                    {
                        // мапа кодів помилок у зрозумілий текст
                        string msg = res.error switch
                        {
                            "insufficient_gold" => "Не вистачає золота.",
                            "max_slots_reached" => "Досягнуто максимуму грядок.",
                            "not_next_slot" => "Спочатку відкрий попередню грядку.",
                            _ => "Не вдалося відкрити грядку. Спробуй ще раз."
                        };
                        if (unlockPanel != null) unlockPanel.ShowError(msg);
                        return;
                    }

                    // успіх
                    var dto = res.dto;

                    // (за потреби) списуємо золото локально
                    if (PlayerSession.I != null)
                        PlayerSession.I.Patch(pi => pi.playergold = Mathf.Max(0, pi.playergold - dto.cost));

                    // мерджимо новий слот і кількість відкритих
                    if (dto.newPlot != null) S.MergeFromDto(dto.newPlot);
                    // обережно оновлюємо: якщо сервер не надіслав коректне unlockedSlots (0/відсутнє),
                    // не блокуємо всі — просто відкриємо хоча б цільовий слот локально
                    int prevUnlocked = 0;
                    for (int i = 0; i < S.Plots.Length; i++)
                        if (S.Plots[i].Unlocked) prevUnlocked++;

                    int targetUnlocked = dto.unlockedSlots > 0
                        ? dto.unlockedSlots
                        : Math.Max(prevUnlocked, slot + 1);

                    // не зменшуємо кількість відкритих (захист від миготіння)
                    if (targetUnlocked < prevUnlocked)
                        targetUnlocked = prevUnlocked;

                    // застосовуємо новий стан
                    for (int i = 0; i < S.Plots.Length; i++)
                        S.Plots[i].Unlocked = (i < targetUnlocked);

                    ApplyAll();
                    if (unlockPanel != null) unlockPanel.Close();
                    ShowToast("Грядку відкрито!");
                    GardenBarks.I?.SayIdle();
                    GardenSfx.I?.PlayUnlock();
                }
                catch (System.Exception e)
                {
                    if (unlockPanel != null) unlockPanel.ShowError("Не вдалося відкрити грядку. Спробуй ще раз.");
                    Debug.LogError(e);
                    GardenSfx.I?.PlayError();
                    if (slot >= 0 && slot < plotViews.Length) plotViews[slot]?.ShakeOnce();
                }
            }
            finally { _busy = false; }
        }

        public static int PriceForSlot(int oneBased) => Mathf.Max(0, (oneBased - 3) * 300);

        int NextUnlockIndex()
        {
            for (int i = 0; i < S.Plots.Length; i++)
                if (!S.Plots[i].Unlocked) return i;
            return S.Plots.Length - 1;
        }

        List<GardenApi.PlantCatalogItem> _plants;

        async Task EnsurePlants()
        {
            if (_plants != null) return;
            _plants = await GardenApi.GetPlantsAsync();
            _plants.RemoveAll(p => p.isActive == 0);
            PlantCatalogCache.SetAll(_plants);           // щоб грядки могли тягнути іконки  
            Debug.Log($"[Plants] Loaded: count={_plants.Count}");
        }

        List<GardenApi.PlantCatalogItem> SortForPlayerLevel(int playerLevel)
        {
            int minAbove = int.MaxValue;
            GardenApi.PlantCatalogItem nextLocked = null;
            foreach (var p in _plants)
                if (p.unlockLevel > playerLevel && p.unlockLevel < minAbove)
                { minAbove = p.unlockLevel; nextLocked = p; }

            var avail = _plants.FindAll(p => p.unlockLevel <= playerLevel);
            avail.Sort((a, b) =>
            {
                int cmp = b.unlockLevel.CompareTo(a.unlockLevel);
                if (cmp != 0) return cmp;
                return a.id.CompareTo(b.id);
            });

            var res = new List<GardenApi.PlantCatalogItem>(avail.Count + 1);
            if (nextLocked != null) res.Add(nextLocked); // "наступний рівень" першим
            res.AddRange(avail);
            return res;
        }

        public async void ShowPlantPanel(int slot)
        {
            await EnsurePlants();
            var d = PlayerSession.I?.Data;

            int playerLevel = d.playerlvl; // або свій спосіб
            var ordered = SortForPlayerLevel(playerLevel);

            var list = new List<PlantInfo>();
            foreach (var p in ordered)
            {
                list.Add(new PlantInfo
                {
                    Id = p.id,
                    DisplayName = p.displayName,
                    Description = p.description,
                    UnlockLevel = p.unlockLevel,
                    GrowthTimeMinutes = p.growthTimeMinutes,
                    SellPrice = p.sellPrice,
                    IconSeed = p.iconSeed,
                    IconPlant = p.iconPlant,
                    IconGrown = p.iconGrown,
                    IconFruit = p.iconFruit,
                    IsActive = p.isActive == 1
                });
            }

            plantSelect?.SetData(list, playerLevel, onPlant: info =>
            {
                _ = PlantViaPanel(slot, info.Id);
            });
            plantSelect?.Show();
        }

        string MapError(string code) => code switch
        {
            // Загальні
            "network" => "Сервер недоступний. Спробуй ще раз.",
            "timeout" => "Час очікування вичерпано. Спробуй ще раз.",
            "unexpected_payload" => "Невідома відповідь сервера.",
            "empty_response" => "Порожня відповідь сервера.",
            "slot_not_empty"        => "Грядка вже зайнята.",
            "no_player"             => "Профіль гравця не знайдено.",
            "plant_inactive"        => "Ця рослина недоступна.",
            "level_too_low"         => "Замалий рівень для цієї рослини.",

            // Water
            "weed_first" => "Спочатку приберіть бур'ян.",
            "already_watered" => "Вже полито.",
            "already_watered_stage" => "У цій стадії вже полито.",
            "nothing_to_water" => "Нема що поливати.",

            // Weed
            "no_weeds" => "Бур’янів немає.",

            // Harvest
            "not_ready" => "Ще не дозріло.",
            "empty_slot" => "Грядка порожня.",
            "cooldown" => "Спробуй трохи згодом.",
            "slot_busy" => "Слот зайнятий, спробуй пізніше.",

            // Unlock (на всяк)
            "insufficient_gold" => "Не вистачає золота.",
            "max_slots_reached" => "Досягнуто максимуму грядок.",
            "not_next_slot" => "Спочатку відкрий попередню грядку.",

            _ => "Упс! Щось пішло не так."
        };

        async Task PlantViaPanel(int slot, int plantId)
        {
            if (plantSelect != null) plantSelect.SetInteractable(false);
            try { await Plant(slot, plantId); }
            finally
            {
                if (plantSelect != null)
                {
                    plantSelect.SetInteractable(true);
                    plantSelect.Close();
                }
            }
        }
        void ShowToast(string msg)
        {
            if (Toasts.I != null) Toasts.I.Show(msg);
            else Debug.Log($"[Toast] {msg}");
        }
        void OnEnable() { PlantCatalogCache.OnReady += OnPlantCatalogReady; }
        void OnDisable() { PlantCatalogCache.OnReady -= OnPlantCatalogReady; }

        void OnPlantCatalogReady()
        {
            ApplyAll();
            if (clickGate) clickGate.SetActive(false); // якщо тримав гейт — сховай
        }
        static string FormatShort(long remainMs)
        {
            if (remainMs < 0) remainMs = 0;
            long sec = (remainMs + 999) / 1000;
            if (sec < 60) return sec + "с";
            long min = sec / 60;
            if (min < 60) return min + "хв";
            long hrs = min / 60;
            min = min % 60;
            if (hrs >= 24) { long d = hrs / 24; hrs = hrs % 24; return d + "д " + hrs + "г"; }
            return hrs + "г " + (min > 0 ? (min + "хв") : "");
        }
    }
    
}
