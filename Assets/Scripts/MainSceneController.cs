using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System; // ← для Action
using System.Threading.Tasks;
using ClashFarm.Garden;
using System.Linq;

public class MainSceneController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gardenPanel;
    [SerializeField] private GameObject arenaPanel;
    [SerializeField] private GameObject villagepanel;

    [Header("HUD (Texts)")]
    [SerializeField] private TextMeshProUGUI nickText;
    [SerializeField] private TextMeshProUGUI lvlText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI greenText;
    [SerializeField] private TextMeshProUGUI diamondsText;

    [Header("HUD (Sliders)")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private Slider hpSlider;

    [Header("HUD (Views)")]
    [SerializeField] private HpRegenerationView hpSmooth;        // optional
    [Header("HUD (Combats)")]
    [SerializeField] private TextMeshProUGUI combatsText;       // "3 / 6"
    [SerializeField] private TextMeshProUGUI combatsTimerText;  // "MM:SS" або "—"


    [Header("Balancing")]
    public int baseVitality = 0;

    //[Header("Network")]
    //[SerializeField] private string apiBase = "https://api.clashfarm.com";

    [Header("HP: локальна регенерація")]
    public bool hpLocalRegenEnabled = true;
    [Range(5, 240)] public int hpFullRegenMinutes = 90;

    float _hpLocalRegenPerSec = 0f;
    float _hpFracCarry = 0f;

    // останні «авторитетні» значення з сервера
    int _lastServerMaxHp = -1;
    int _lastServerHp = -1;

    // корутини
    Coroutine _tick1Hz;
    Coroutine _lightSync;
    Coroutine _hpHeartbeatLoop;
    Coroutine _accountHeartbeatLoop;
    Coroutine _combatsHeartbeatLoop;

    // Локальні тіні для плавного тику між heartbeat`ами
    int _combatsCurrent = 0;
    int _combatsMax = 6;
    int _remainToNext = 0;
    int _remainToFull = 0;

    // Подія для підсцен (арени): (combats, max, toNextSec, toFullSec)
    public event Action<int, int, int, int> OnCombatsUpdated;

    // Публічні read-only геттери (арені зручно читати)
    public int CombatsCurrent => _combatsCurrent;
    public int CombatsMax => _combatsMax;
    public int RemainToNext => _remainToNext;
    public int RemainToFull => _remainToFull;

    void OnEnable()
    {
        if (PlayerSession.I != null)
            PlayerSession.I.OnChanged += RefreshAll;
    }

    void OnDisable()
    {
        if (PlayerSession.I != null)
            PlayerSession.I.OnChanged -= RefreshAll;

        if (_tick1Hz != null) StopCoroutine(_tick1Hz);
        if (_lightSync != null) StopCoroutine(_lightSync);
        if (_hpHeartbeatLoop != null) StopCoroutine(_hpHeartbeatLoop);
        if (_accountHeartbeatLoop != null) StopCoroutine(_accountHeartbeatLoop);
        if (_combatsHeartbeatLoop != null) StopCoroutine(_combatsHeartbeatLoop);

        _combatsHeartbeatLoop = null;
        _tick1Hz = _lightSync = _hpHeartbeatLoop = _accountHeartbeatLoop = null;
    }

    void Start()
    {
        var d = PlayerSession.I?.Data;
        if (d == null || string.IsNullOrEmpty(d.nickname))
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
            if (gardenPanel) gardenPanel.SetActive(false);
            if (arenaPanel) arenaPanel.SetActive(false);
            if (villagepanel) villagepanel.SetActive(false);
            return;
        }

        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (gardenPanel) gardenPanel.SetActive(false);
        if (arenaPanel) arenaPanel.SetActive(false);
        if (villagepanel) villagepanel.SetActive(false);

        RefreshAll();                     // первинний HUD
        RecomputeHpRegenRate();
        PrebuildPlantPanelEarly();

        if (_tick1Hz == null) _tick1Hz = StartCoroutine(UiTick1Hz());
        if (_hpHeartbeatLoop == null) _hpHeartbeatLoop = StartCoroutine(HpHeartbeatLoop());
        if (_accountHeartbeatLoop == null) _accountHeartbeatLoop = StartCoroutine(AccountHeartbeatLoop());
        if (_combatsHeartbeatLoop == null) _combatsHeartbeatLoop = StartCoroutine(CombatsHeartbeatLoop());
    }

    // ===== 1Hz локальний тік (hp+combats) =====
    IEnumerator UiTick1Hz()
    {
        var wait = new WaitForSecondsRealtime(1f);
        while (true)
        {
            // Локальна HP-реген між heartbeat'ами
            if (hpLocalRegenEnabled)
            {
                var d = PlayerSession.I?.Data;
                if (d != null)
                {
                    int maxHp = ComputeMaxHp();
                    if (d.playerhp < maxHp && _hpLocalRegenPerSec > 0f)
                    {
                        _hpFracCarry += _hpLocalRegenPerSec;
                        int delta = Mathf.FloorToInt(_hpFracCarry);
                        if (delta > 0)
                        {
                            _hpFracCarry -= delta;
                            d.playerhp = Mathf.Min(maxHp, d.playerhp + delta);
                        }
                    }
                }
            }

            UpdateCombatsUiEverySecond();
            UpdateHpUiEverySecond();

            yield return wait;
        }
    }

    // ===== HP heartbeat =====
    IEnumerator HpHeartbeatLoop()
    {
        var waitMin = 8f;
        var waitMax = 15f;

        while (true)
        {
            var d = PlayerSession.I?.Data;
            if (d == null) { yield return new WaitForSecondsRealtime(waitMin); continue; }

            var task = ApiClient.HpHeartbeatAsync(d.nickname, d.serialcode);
            while (!task.IsCompleted) yield return null;

            var res = task.Result;
            if (res.HasValue)
            {
                var (hp, max) = res.Value;

                _lastServerHp = hp;
                _lastServerMaxHp = max;

                if (PlayerSession.I?.Data != null)
                {
                    PlayerSession.I.Data.maxhp = max;
                    PlayerSession.I.Data.playerhp = Mathf.Clamp(hp, 0, max);
                }

                RecomputeHpRegenRate();
                _hpFracCarry = 0f;
                UpdateHpUiEverySecond();
            }

            float jitter = UnityEngine.Random.Range(-2f, 2f);
            yield return new WaitForSecondsRealtime(Mathf.Clamp(12f + jitter, waitMin, waitMax));
        }
    }

    // ===== Аккаунт heartbeat (оновлюємо PlayerSession.Data) =====
    IEnumerator AccountHeartbeatLoop()
    {
        var waitMin = 15f;
        var waitMax = 25f;

        while (true)
        {
            var d = PlayerSession.I?.Data;
            if (d != null)
            {
                var task = ApiClient.GetAccountAsync(d.nickname, d.serialcode);
                while (!task.IsCompleted) yield return null;

                var acc = task.Result;
                if (acc != null)
                {
                    bool changed = MergeAccountIntoSession(acc);
                    if (changed)
                    {
                        RecomputeHpRegenRate();
                        UpdateHpUiEverySecond();
                        RefreshAll();
                    }
                }
            }

            float jitter = UnityEngine.Random.Range(-2f, 2f);
            yield return new WaitForSecondsRealtime(Mathf.Clamp(20f + jitter, waitMin, waitMax));
        }
    }

    IEnumerator CombatsHeartbeatLoop()
    {
        var waitMin = 15f;
        var waitMax = 25f;

        while (true)
        {
            yield return RefreshCombatsNowCo();

            float jitter = UnityEngine.Random.Range(-2f, 2f);
            yield return new WaitForSecondsRealtime(Mathf.Clamp(20f + jitter, waitMin, waitMax));
        }
    }

    IEnumerator RefreshCombatsNowCo()
    {
        var d = PlayerSession.I?.Data;
        if (d == null) yield break;

        var task = ApiClient.CombatsHeartbeatAsync(d.nickname, d.serialcode);
        while (!task.IsCompleted) yield return null;

        var dto = task.Result;
        if (dto != null && string.IsNullOrEmpty(dto.error))
        {
            _combatsCurrent = Mathf.Max(0, dto.combats);
            _combatsMax = Mathf.Max(1, dto.combatsMax);
            _remainToNext = Mathf.Max(0, dto.remainingToNextSec);
            _remainToFull = Mathf.Max(0, dto.remainingToFullSec);

            OnCombatsUpdated?.Invoke(_combatsCurrent, _combatsMax, _remainToNext, _remainToFull);
            UpdateCombatsUiEverySecond();
        }
    }

    // ===== HUD refresh =====
    void RefreshAll()
    {
        var d = PlayerSession.I?.Data;
        if (d == null) return;

        if (nickText) nickText.text = d.nickname;
        if (lvlText) lvlText.text = d.playerlvl.ToString();
        if (goldText) goldText.text = d.playergold.ToString();
        if (greenText) greenText.text = d.playergreen.ToString();
        if (diamondsText) diamondsText.text = d.playerdiamonds.ToString();

        int needExp = Mathf.FloorToInt(Mathf.Pow(d.playerlvl, 2.2f) + 9);
        if (expText) expText.text = $"{d.playerexpierence}/{needExp}";
        if (expSlider)
        {
            expSlider.minValue = 0;
            expSlider.maxValue = needExp;
            expSlider.wholeNumbers = true;
            expSlider.value = Mathf.Clamp(d.playerexpierence, 0, needExp);
        }

        var s = PlayerSession.I?.Data;
        if (s != null)
        {
            _combatsCurrent = Mathf.Max(0, s.combats);
            _combatsMax = 6; // тимчасово фіксовано; з heartbeat дістанемо точне значення
            if (combatsText) combatsText.text = $"{_combatsCurrent} / {_combatsMax}";
            if (combatsTimerText) combatsTimerText.text = (_combatsCurrent >= _combatsMax) ? "—" : "00:00";
        }
        RecomputeHpRegenRate();
    }

    // ===== Merge account =====
    bool MergeAccountIntoSession(PlayerInfo acc)
    {
        var s = PlayerSession.I?.Data;
        if (s == null) return false;

        bool changed = false;

        void SetIfDifferentInt(ref int field, int newVal)
        { if (field != newVal) { field = newVal; changed = true; } }

        void SetIfDifferentString(ref string field, string newVal)
        { if (field != newVal) { field = newVal; changed = true; } }

        SetIfDifferentString(ref s.nickname, acc.nickname);
        SetIfDifferentInt(ref s.playerlvl, acc.playerlvl);
        SetIfDifferentInt(ref s.playerexpierence, acc.playerexpierence);
        SetIfDifferentInt(ref s.playergold, acc.playergold);
        SetIfDifferentInt(ref s.playergreen, acc.playergreen);
        SetIfDifferentInt(ref s.playerdiamonds, acc.playerdiamonds);
        SetIfDifferentInt(ref s.playerfraction, acc.playerfraction);
        SetIfDifferentInt(ref s.playerpower, acc.playerpower);
        SetIfDifferentInt(ref s.playerprotection, acc.playerprotection);
        SetIfDifferentInt(ref s.playerdexterity, acc.playerdexterity);
        SetIfDifferentInt(ref s.playerskill, acc.playerskill);
        SetIfDifferentInt(ref s.combats, acc.combats);
        SetIfDifferentInt(ref s.pet, acc.pet);
        SetIfDifferentInt(ref s.hourreward, acc.hourreward);
        SetIfDifferentInt(ref s.guardhour, acc.guardhour);
        SetIfDifferentInt(ref s.guardhours, acc.guardhours);
        SetIfDifferentInt(ref s.mining, acc.mining);
        SetIfDifferentInt(ref s.minedgold, acc.minedgold);
        SetIfDifferentInt(ref s.ismine, acc.ismine);
        SetIfDifferentInt(ref s.maxminedgold, acc.maxminedgold);
        SetIfDifferentInt(ref s.horse, acc.horse);
        SetIfDifferentInt(ref s.hikeminutes, acc.hikeminutes);
        SetIfDifferentInt(ref s.hikemin, acc.hikemin);
        SetIfDifferentInt(ref s.hikeactivemin, acc.hikeactivemin);
        SetIfDifferentInt(ref s.monkreward, acc.monkreward);
        SetIfDifferentString(ref s.timetoendguard, acc.timetoendguard);
        SetIfDifferentString(ref s.timetoendmine, acc.timetoendmine);
        SetIfDifferentString(ref s.timetonextmine, acc.timetonextmine);
        SetIfDifferentString(ref s.horsetime, acc.horsetime);
        SetIfDifferentString(ref s.timetoendhike, acc.timetoendhike);
        SetIfDifferentString(ref s.lasthike, acc.lasthike);

        int oldSurv = s.playersurvivability;
        SetIfDifferentInt(ref s.playersurvivability, acc.playersurvivability);

        if (acc.maxhp > 0 && s.maxhp != acc.maxhp) { s.maxhp = acc.maxhp; changed = true; _lastServerMaxHp = acc.maxhp; }

        if (s.playersurvivability != oldSurv && acc.maxhp <= 0)
            _lastServerMaxHp = 0;

        return changed;
    }

    //====== Combats UI =======
    void UpdateCombatsUiEverySecond()
    {
        // Плавне зменшення локального таймера між heartbeat`ами
        if (_combatsCurrent >= _combatsMax)
        {
            if (combatsTimerText) combatsTimerText.text = "—";
        }
        else
        {
            if (_remainToNext > 0) _remainToNext = Mathf.Max(0, _remainToNext - 1);
            if (combatsTimerText)
            {
                int mm = _remainToNext / 60;
                int ss = _remainToNext % 60;
                combatsTimerText.text = $"{mm:00}:{ss:00}";
            }
        }

        if (combatsText) combatsText.text = $"{_combatsCurrent} / {_combatsMax}";
    }

    // ===== HP UI =====
    void UpdateHpUiEverySecond()
    {
        var d = PlayerSession.I?.Data;
        if (d == null) return;

        int maxHp = ComputeMaxHp();
        if (d.playerhp > maxHp) d.playerhp = maxHp;

        if (hpSmooth != null) hpSmooth.OnHpUpdated(d.playerhp, maxHp);

        if (hpSlider != null)
        {
            hpSlider.value = (maxHp > 0) ? Mathf.Clamp01((float)d.playerhp / maxHp) : 0f;
        }

        if (hpText != null)
        {
            hpText.text = $"{d.playerhp}/{maxHp}";
        }
    }

    // ===== Навігація панелей =====
    public void OpenGarden()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gardenPanel) gardenPanel.SetActive(true);
    }
    public void OpenArena()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (arenaPanel) arenaPanel.SetActive(true);
    }
    public void OpenVillage()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (villagepanel) villagepanel.SetActive(true);
    }
    public void BackToMenu()
    {
        if (arenaPanel) arenaPanel.SetActive(false);
        if (villagepanel) villagepanel.SetActive(false);
        if (gardenPanel) gardenPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    // ===== Допоміжні =====
    string FormatMMSS(int sec)
    {
        int mm = Mathf.Max(0, sec) / 60;
        int ss = Mathf.Max(0, sec) % 60;
        return $"{mm:00}:{ss:00}";
    }

    bool NeedHpRegen()
    {
        var d = PlayerSession.I?.Data;
        if (d == null) return false;
        int maxHp = Mathf.FloorToInt(Mathf.Pow(d.playersurvivability + baseVitality, 2.2f) + 66);
        return d.playerhp < maxHp;
    }

    int ComputeMaxHp()
    {
        if (_lastServerMaxHp > 0) return _lastServerMaxHp;

        var d = PlayerSession.I?.Data;
        if (d == null) return 0;

        if (d.maxhp > 0) return d.maxhp;

        return Mathf.FloorToInt(Mathf.Pow(d.playersurvivability + baseVitality, 2.2f) + 66);
    }

    void RecomputeHpRegenRate()
    {
        if (!hpLocalRegenEnabled) { _hpLocalRegenPerSec = 0f; return; }

        int maxHp = ComputeMaxHp();
        _hpLocalRegenPerSec = (maxHp > 0 && hpFullRegenMinutes > 0)
            ? (maxHp / (float)(hpFullRegenMinutes * 60))
            : 0f;
    }
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && PlayerSession.I?.Data != null)
            StartCoroutine(RefreshCombatsNowCo()); // миттєво підтягнути офлайн-прогрес
    }

    public void IngestCombats(ApiClient.CombatsDto dto)
    {
        if (dto == null || !string.IsNullOrEmpty(dto.error)) return;
        _combatsCurrent = Mathf.Max(0, dto.combats);
        _combatsMax = Mathf.Max(1, dto.combatsMax);
        _remainToNext = Mathf.Max(0, dto.remainingToNextSec);
        _remainToFull = Mathf.Max(0, dto.remainingToFullSec);

        OnCombatsUpdated?.Invoke(_combatsCurrent, _combatsMax, _remainToNext, _remainToFull);
        UpdateCombatsUiEverySecond();
    }
    async void PrebuildPlantPanelEarly()
    {
        // знайдемо панель навіть якщо вона під неактивним батьком
        var panel = UnityEngine.Object.FindFirstObjectByType<ClashFarm.Garden.PlantSelectionPanel>(FindObjectsInactive.Include);
        if (panel == null) return;

        // дістанемо каталог (з кешу, якщо готовий; інакше напряму з API)
        var plants = PlantCatalogCache.IsReady
            ? PlantCatalogCache.GetAll().ToList()
            : await GardenApi.GetPlantsAsync();

        if (plants == null || plants.Count == 0) return;

        // фільтр активних
        plants.RemoveAll(p => p.isActive == 0);

        // рівень гравця
        int lvl = Mathf.Max(1, PlayerSession.I?.Data?.playerlvl ?? 1);

        // Порядок: "наступна заблокована" → доступні за спаданням
        var exactNext = plants.FirstOrDefault(p => p.unlockLevel == lvl + 1);
        var fallbackNext = plants.Where(p => p.unlockLevel > lvl).OrderBy(p => p.unlockLevel).FirstOrDefault();
        var nextLocked = exactNext ?? fallbackNext;

        var available = plants.Where(p => p.unlockLevel <= lvl)
                              .OrderByDescending(p => p.unlockLevel);

        // Збираємо список для панелі
        var list = new System.Collections.Generic.List<ClashFarm.Garden.PlantInfo>();
        if (nextLocked != null)
            list.Add(new ClashFarm.Garden.PlantInfo
            {
                Id = nextLocked.id,
                DisplayName = nextLocked.displayName,
                Description = nextLocked.description,
                UnlockLevel = nextLocked.unlockLevel,
                GrowthTimeMinutes = nextLocked.growthTimeMinutes,
                SellPrice = nextLocked.sellPrice,
                IconSeed = nextLocked.iconSeed,
                IconPlant = nextLocked.iconPlant,
                IconGrown = nextLocked.iconGrown,
                IconFruit = nextLocked.iconFruit,
                IsActive = nextLocked.isActive == 1
            });
        foreach (var p in available)
            list.Add(new ClashFarm.Garden.PlantInfo
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

        // Передаємо дані й будуємо офскрін
        panel.SetData(list, lvl, onPlant: null); // колбек поставимо вже при реальному показі
        await panel.PrewarmAtStartupAsync();   // повний прогрів: диск+RAM+побудова+приховати
    }
}
