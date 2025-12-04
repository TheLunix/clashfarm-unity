using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetStoreUI : MonoBehaviour
{
    [Header("Основні панелі")]
    [SerializeField] private GameObject panelPetStore;   // PanelPetStore
    [SerializeField] private GameObject petListPanel;    // PettListPanel (список Pet1-3)
    [SerializeField] private GameObject petStore; // панель профілю тварини (зробиш окремо)
    [SerializeField] private GameObject petProfilePanel; // панель профілю тварини (зробиш окремо)
    [SerializeField] private GameObject petRevivePanel;  // панель воскресіння (зробиш окремо)

    [Header("Кнопки зверху")]
    [SerializeField] private Button petButton;           // Layout Horizontal / PetButton
    [SerializeField] private Button collarsButton;       // Layout Horizontal / CollarsButton (поки можна просто заглушка)

    [Header("Кнопки у PettListPanel")]
    [SerializeField] private Button closeListButton;     // BackGround / Button (хрестик/назад)
    [SerializeField] private Button pet1Button;          // Content / Pet1 / Button (можеш додати)
    [SerializeField] private Button pet2Button;          // Content / Pet2 / Button
    [SerializeField] private Button pet3Button;          // Content / Pet3 / Button

    [Header("Ціна (текст під іконкою)")]
    [SerializeField] private TMP_Text pet1PriceText;     // Pet1 / Left / PetPrice / PriceText
    [SerializeField] private TMP_Text pet2PriceText;     // Pet2 / Left / PetPrice / PriceText
    [SerializeField] private TMP_Text pet3PriceText;     // Pet3 / Left / PetPrice / PriceText

    [Header("Navigation")]
    [SerializeField] private Button backToVillageButton; // універсальна кнопка назад
    [SerializeField] private Navigation nav;

    private const int PetPriceGold = 600;
    private bool _busy;

    private void Awake()
    {
        if (petButton)       petButton.onClick.AddListener(OnPetButtonClicked);
        if (collarsButton)   collarsButton.onClick.AddListener(OnCollarsClicked);

        if (closeListButton) closeListButton.onClick.AddListener(() => petListPanel.SetActive(false));

        if (pet1Button) pet1Button.onClick.AddListener(() => OnPetChoiceClicked("pet_avatar_1"));
        if (pet2Button) pet2Button.onClick.AddListener(() => OnPetChoiceClicked("pet_avatar_2"));
        if (pet3Button) pet3Button.onClick.AddListener(() => OnPetChoiceClicked("pet_avatar_3"));
    }

    private void OnEnable()
    {
        RefreshStateFromSession();

        if (backToVillageButton != null)
            backToVillageButton.onClick.RemoveListener(OnBackToVillageClicked);
    }

    private void OnDisable()
    {
        // нічого спеціального робити не треба

        if (backToVillageButton != null)
            backToVillageButton.onClick.RemoveListener(OnBackToVillageClicked);
    }

    private void OnBackToVillageClicked()
    {
        if (Navigation.Instance != null)
        {
            Navigation.Instance.GoToVillageHub();
        }
        else
        {
            Debug.LogWarning("[PetStoreUI] Navigation.Instance is null – не можу перейти в село");
        }
    }

    /// <summary>
    /// Оновлюємо стан панелей в залежності від наявності/стану тварини.
    /// </summary>
    private void RefreshStateFromSession()
    {
        var session = PlayerSession.I;
        if (session == null)
        {
            Debug.LogError("PetStoreUI: PlayerSession.I is null");
            return;
        }

        var data = session.Data;
        var pet  = data.petInfo;

        bool hasPet   = pet != null;                // ← тепер закритий теж рахується
        bool isDead   = hasPet && !pet.isalive;
        bool isClosed = hasPet && pet.isclosed;
        int  gold     = data.playergold;
        bool canBuy   = gold >= PetPriceGold;

        // Головна панель магазину завжди активна, просто перемикаємо підпанелі
        if (panelPetStore) panelPetStore.SetActive(true);

        // Панель списку (PettListPanel) схована по дефолту
        if (petListPanel)  petListPanel.SetActive(false);

        // Профіль / воскресіння покажеш сам коли будуть готові
        if (petProfilePanel) petProfilePanel.SetActive(false);
        if (petRevivePanel)  petRevivePanel.SetActive(false);

        // Підписи цін
        if (pet1PriceText) pet1PriceText.text = PetPriceGold.ToString();
        if (pet2PriceText) pet2PriceText.text = PetPriceGold.ToString();
        if (pet3PriceText) pet3PriceText.text = PetPriceGold.ToString();

        // Якщо немає тварини – кнопки вибору активні тільки якщо вистачає золота
        if (pet1Button) pet1Button.interactable = !hasPet && canBuy;
        if (pet2Button) pet2Button.interactable = !hasPet && canBuy;
        if (pet3Button) pet3Button.interactable = !hasPet && canBuy;
    }

    /// <summary>
    /// Реакція на кнопку "Тварини" зверху (PetButton).
    /// </summary>
    private void OnPetButtonClicked()
    {
        var session = PlayerSession.I;
        if (session == null) return;

        var pet = session.Data.petInfo;
        bool hasPet = pet != null;
        bool isDead = hasPet && !pet.isalive;

        if (!hasPet)
        {
            // Немає тварини – відкриваємо панель вибору/покупки
            OpenPetListPanel();
        }
        else if (isDead)
        {
            // Є, але мертва – показуємо панель воскресіння
            OpenRevivePanel();
        }
        else
        {
            // Є й жива – показуємо профіль
            nav.GoToPet();
        }
    }

    public void OpenPetListPanel()
    {
        if (petListPanel) petListPanel.SetActive(true);
        if (petProfilePanel) petProfilePanel.SetActive(false);
        if (petRevivePanel)  petRevivePanel.SetActive(false);
    }

    public void OpenProfilePanel()
    {
        if (petProfilePanel)
        {
            panelPetStore.SetActive(false);
            petProfilePanel.SetActive(true);
            if (petListPanel) petListPanel.SetActive(false);
            if (petRevivePanel) petRevivePanel.SetActive(false);

            // оновлюємо дані профілю
            var profile = petProfilePanel.GetComponent<PetProfileUI>();
            if (profile != null)
                profile.RefreshFromSession();
        }
        else
        {
            Debug.Log("PetStoreUI: profile panel is not assigned");
        }
    }


    public void OpenRevivePanel()
    {
        if (petRevivePanel)
        {
            petRevivePanel.SetActive(true);
            if (petListPanel)  petListPanel.SetActive(false);
            if (petProfilePanel) petProfilePanel.SetActive(false);

            var revive = petRevivePanel.GetComponent<PetReviveUI>();
            if (revive != null)
                revive.RefreshFromSession();
        }
        else
        {
            Debug.Log("PetStoreUI: revive panel is not assigned");
        }
    }

    /// <summary>
    /// Вибір одного з трьох Pet1/Pet2/Pet3 у PettListPanel.
    /// </summary>
    private void OnPetChoiceClicked(string avatarKey)
    {
        if (_busy) return;
        _ = BuyPetAsync(avatarKey);
    }

    private async Task BuyPetAsync(string avatarKey)
    {
        _busy = true;

        var session = PlayerSession.I;
        if (session == null)
        {
            _busy = false;
            return;
        }

        var data = session.Data;
        string nickname = data.nickname;
        string serial   = data.serialcode;

        // Ім'я пета можемо задати за замовчанням або потім зробиш input
        string petName = "Компаньйон";

        var resp = await ApiClient.PetBuyAsync(nickname, serial, avatarKey, petName);
        if (resp == null)
        {
            Debug.LogError("PetBuyAsync: null response");
            _busy = false;
            return;
        }

        if (resp.error != "OK")
        {
            Debug.LogWarning("PetBuyAsync error: " + resp.error);
            // TODO: тут можна показати попап "Немає золота" / "Пет вже існує"
            _busy = false;
            return;
        }

        // Оновлюємо сесію: золото + petInfo
        session.Patch(info =>
        {
            info.playergold = resp.playergold;

            if (resp.haspet && resp.pet != null)
            {
                info.petInfo = new PetInfo
                {
                    id = resp.pet.id,
                    name = resp.pet.name,
                    avatar = resp.pet.avatar,
                    petpower = resp.pet.petpower,
                    petprotection = resp.pet.petprotection,
                    petdexterity = resp.pet.petdexterity,
                    petskill = resp.pet.petskill,
                    petsurvivability = resp.pet.petsurvivability,
                    petcollar = resp.pet.petcollar,
                    isalive = resp.pet.isalive,
                    isclosed = resp.pet.isclosed,
                    pethp = resp.pet.pethp,
                    petmaxhp = resp.pet.petmaxhp,
                    petkills = resp.pet.petkills,
                    petdeaths = resp.pet.petdeaths
                };
            }
        });

        _busy = false;

        // Після покупки – одразу переходимо в профіль
        OpenProfilePanel();
    }

    private void OnCollarsClicked()
    {
        // Поки нашийники не реалізовані – можна просто показати повідомлення або нічого не робити
        Debug.Log("PetStoreUI: Collars clicked (ще не реалізовано).");
    }
    public void HideAllPanels()
    {
        if (panelPetStore)    panelPetStore.SetActive(false);
        if (petListPanel)     petListPanel.SetActive(false);
        if (petProfilePanel)  petProfilePanel.SetActive(false);
        if (petRevivePanel)   petRevivePanel.SetActive(false);
    }
}
