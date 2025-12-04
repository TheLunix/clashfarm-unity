using UnityEngine;
using ClashFarm.Village; // щоб бачити VillageController

public enum NavState
{
    MainMenu,

    // Village
    VillageHub,
    VillageMonk,
    VillageMine,
    VillageTravel,
    VillageGuard,
    VillageMarket,
    VillagevPetStore,
    VillagevShop,
    VillagevDruid,
    VillagevWitch,
    VillagevBlackmith,
    VillagevHome,
    VillagevClan,

    // Інші root-панелі
    Garden,
    Arena,
    Player
}

public class Navigation : MonoBehaviour
{
    public static Navigation Instance { get; private set; }

    [Header("Core controllers")]
    [SerializeField] private MainSceneController main;
    [SerializeField] private VillageController village;
    [SerializeField] private PetStoreUI pet;

    private NavState _state = NavState.MainMenu;

    public bool IsOnMainMenu => _state == NavState.MainMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Якщо не підкинули з інспектора – знайдемо у сцені
        if (!main)
            main = FindFirstObjectByType<MainSceneController>();

        if (!village)
            village = FindFirstObjectByType<VillageController>();
    }

    // ========= ROOT НАВІГАЦІЯ (між великими панелями) =========

    /// <summary>Викликається, коли відкрили головне меню.</summary>
    public void OnMainMenuOpened()
    {
        _state = NavState.MainMenu;
    }

    /// <summary>Викликається, коли відкрили хаб села.</summary>
    public void OnVillageHubOpened()
    {
        _state = NavState.VillageHub;
    }

    /// <summary>Викликається, коли відкрили конкретну локацію в селі.</summary>
    public void OnVillageLocationOpened(LocationType type)
    {
        switch (type)
        {
            case LocationType.Monk:
                _state = NavState.VillageMonk;
                break;
            case LocationType.Mine:
                _state = NavState.VillageMine;
                break;
            case LocationType.Travel:
                _state = NavState.VillageTravel;
                break;
            case LocationType.Outskirts:
                _state = NavState.VillageGuard;
                break;
            case LocationType.Market:
                _state = NavState.VillageMarket;
                break;
            default:
                _state = NavState.VillageHub;
                break;
        }
    }

    /// <summary>Викликається, коли відкрили меню гравця (PlayerScene).</summary>
    public void OnPlayerMenuOpened()
    {
        _state = NavState.Player;
    }

    /// <summary>
    /// Універсальна кнопка "Назад" (UniversalBackButton викликає саме це).
    /// </summary>
    public void GoBack()
    {
        switch (_state)
        {
            case NavState.MainMenu:
                // Уже в головному меню – нічого не робимо
                break;

            // --- Village ---

            case NavState.VillageHub:
                // Були в хабі села → йдемо в головне меню
                if (village != null)
                    village.ReturnToMain(); // всередині викличе OnMainMenuOpened()
                else if (main != null)
                {
                    main.BackToMenu();
                    OnMainMenuOpened();
                }
                break;

            case NavState.VillageMonk:
            case NavState.VillageMine:
            case NavState.VillageTravel:
            case NavState.VillageGuard:
            case NavState.VillageMarket:
            case NavState.VillagevShop:
            case NavState.VillagevDruid:
            case NavState.VillagevWitch:
            case NavState.VillagevBlackmith:
            case NavState.VillagevHome:
            case NavState.VillagevClan:
                // Були в якійсь внутрішній панелі села → повертаємось у Village-хаб
                if (main != null)
                    main.OpenVillage();
                if (village != null)
                    village.OpenVillageHub();
                // VillageController.OpenVillageHub сам викличе OnVillageHubOpened()
                break;

            // --- PetStore / профіль тварини ---

            case NavState.VillagevPetStore:
                if (pet != null)
                    pet.HideAllPanels();  // ← ТЕПЕР 100% все вимикається

                GoToMainMenu();
                break;

            // --- Інші root-панелі ---

            case NavState.Garden:
            case NavState.Arena:
            case NavState.Player:
                // З цих панелей "Назад" завжди веде у головне меню
                GoToMainMenu();
                break;
        }
    }

    /// <summary>Перейти у головне меню (тільки mainMenuPanel активний).</summary>
    public void GoToMainMenu()
    {
        if (main == null) return;

        main.BackToMenu(); // всередині: CloseAllPanel() + mainMenuPanel.SetActive(true)
        OnMainMenuOpened();
    }

    /// <summary>Перейти у Город (Garden панель).</summary>
    public void GoToGarden()
    {
        if (main == null) return;

        main.OpenGarden(); // всередині: CloseAllPanel() + gardenPanel.SetActive(true)
        _state = NavState.Garden;
    }

    /// <summary>Перейти на Арену.</summary>
    public void GoToArena()
    {
        if (main == null) return;

        main.OpenArena();
        _state = NavState.Arena;
    }

    /// <summary>Перейти в Профіль гравця.</summary>
    public void GoToPlayer()
    {
        if (main == null) return;

        main.OpenPlayerMenu();
        _state = NavState.Player;
        // Внутрішні вкладки (статистика/тренування/тощо) керуються PlayerSceneController.
    }

    /// <summary>Перейти в профіль/тваринник залежно від стану тварини.</summary>
    public void GoToPet()
    {
        var data = PlayerSession.I?.Data;
        var petInfo = data?.petInfo;

        if (main != null)
            main.CloseAllPanel(); // ← Вимикає garden/arena/village/player/mainmenu

        // Вмикаємо PetStore родительський об’єкт
        if (pet != null)
            pet.gameObject.SetActive(true);

        // Вимикаємо villagePanel, якщо воно залишилось активним
        if (main != null && main.villagePanel != null)
            main.villagePanel.SetActive(false);

        if (petInfo == null)
        {
            // немає тварини → PetStore список
            pet.OpenPetListPanel();
            _state = NavState.VillagevPetStore;
            return;
        }

        if (!petInfo.isalive)
        {
            pet.OpenRevivePanel();
            _state = NavState.VillagevPetStore;
            return;
        }

        // otherwise → профіль
        main.backButton.SetActive(true);
        pet.OpenProfilePanel();
        _state = NavState.VillagevPetStore;
    }


    // ========= VILLAGE =========

    /// <summary>
    /// Перейти в село у режим "хаб" (фон + кнопки локацій).
    /// Використовуй для кнопки "Село" з мейну.
    /// </summary>
    public void GoToVillageHub()
    {
        if (main != null)
            main.OpenVillage();     // вимикає інші root-панелі, вмикає villagePanel

        if (village != null)
            village.OpenVillageHub(); // всередині: mainHub=false, villageHub=true, закриває всі внутрішні панелі
        // VillageController.OnVillageHubOpened виставить _state = VillageHub
    }

    public void GoToVillageMine()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenMine();
        // VillageController.OpenMine → OnVillageLocationOpened(Mine) → _state = VillageMine
    }

    public void GoToVillageMonk()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenMonk();
        // _state виставиться через OnVillageLocationOpened(Monk)
    }

    public void GoToVillageOutskirts()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenOutskirts();
        // _state → VillageGuard
    }

    public void GoToVillageTravel()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenTravel();
        // _state → VillageTravel
    }

    public void GoToVillageBlacksmith()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenBlacksmith();
    }

    public void GoToVillageDruid()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenDruid();
    }

    public void GoToVillageWitch()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenWitch();
    }

    public void GoToVillageHome()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenHome();
    }

    public void GoToVillageClan()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenClan();
    }

    public void GoToVillageShop()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenShop();
    }

    public void GoToVillageMarket()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenMarket();
    }

    public void GoToVillageLivestock()
    {
        if (main != null)
            main.OpenVillage();

        if (village != null)
            village.OpenLivestock();
    }

    /// <summary>
    /// Вийти з села назад у головне меню.
    /// (Якщо є VillageController — він закриє внутрішні панелі й покрутить лоадер.)
    /// </summary>
    public void ReturnFromVillageToMain()
    {
        if (village != null)
        {
            village.ReturnToMain(); // всередині викликає OnMainMenuOpened()
        }
        else if (main != null)
        {
            main.BackToMenu();
            OnMainMenuOpened();
        }
    }
}
