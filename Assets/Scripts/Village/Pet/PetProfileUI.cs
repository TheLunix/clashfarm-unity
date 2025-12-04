using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;          // 🔹 як у MonkUI

public class PetProfileUI : MonoBehaviour
{
    [Header("Основне")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text petNameLabel;
    [SerializeField] private TMP_Text descriptionName;
    [SerializeField] private GameObject lockGrid;      // сама решітка поверх аватарки

    [Header("Стати (контейнери)")]
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text protectionText;
    [SerializeField] private TMP_Text dexterityText;
    [SerializeField] private TMP_Text skillText;
    [SerializeField] private TMP_Text survivabilityText;

    [Header("Кнопки")]
    [SerializeField] private Button closeButton;   // це буде toggle IsClosed
    [SerializeField] private Button exitButton;    // просто вихід з панелі
    [SerializeField] private Button settingsButton;

    [Header("Аватарки")]
    [SerializeField] private Sprite avatar1Sprite;
    [SerializeField] private Sprite avatar2Sprite;
    [SerializeField] private Sprite avatar3Sprite;
    

    [Header("Локалізація")]
    [SerializeField] private TMP_Text lockGridLabel;   // текст на решітці
    [SerializeField] private TMP_Text buttonTrainingText;   // текст на решітці
    [SerializeField] private TMP_Text buttonEquipmentText;   // текст на решітці
    [SerializeField] private TMP_Text buttonPetSettingsText;   // текст на решітці

    [Header("Training buttons")]
    [SerializeField] private Button powerUpButton;
    [SerializeField] private Button protectionUpButton;
    [SerializeField] private Button dexterityUpButton;
    [SerializeField] private Button skillUpButton;
    [SerializeField] private Button survivabilityUpButton;

    private bool _upgradeBusy;

    // 🔹 Локалізовані рядки (аналогічно MonkUI: LocalizedString("Table", "key"))
    private readonly LocalizedString L_Closed = new LocalizedString("Pet", "pet_profile_closed");
    private readonly LocalizedString L_Open   = new LocalizedString("Pet", "pet_profile_open");
    private readonly LocalizedString L_Settings = new LocalizedString("Pet", "pet_profile_setting");
    private readonly LocalizedString L_Training   = new LocalizedString("Pet", "pet_profile_training");
    private readonly LocalizedString L_Equipment = new LocalizedString("Pet", "pet_profile_equipment");

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(OnToggleClosedClicked);
        if (exitButton)  exitButton.onClick.AddListener(ClosePanel);
        // settingsButton потім використаємо для перейменування / зміни аватарки
        if (powerUpButton)        powerUpButton.onClick.AddListener(() => OnUpgradeStat("power"));
        if (skillUpButton)        skillUpButton.onClick.AddListener(() => OnUpgradeStat("skill"));
        if (survivabilityUpButton)survivabilityUpButton.onClick.AddListener(() => OnUpgradeStat("survivability"));
        if (protectionUpButton)   protectionUpButton.onClick.AddListener(() => OnUpgradeStat("protection"));
        if (dexterityUpButton)    dexterityUpButton.onClick.AddListener(() => OnUpgradeStat("dexterity"));

    }

    private void OnEnable()
    {
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        var session = PlayerSession.I;
        if (session == null)
        {
            Debug.LogError("PetProfileUI: PlayerSession.I is null");
            return;
        }

        var pet = session.Data.petInfo;
        if (pet == null)
        {
            Debug.Log("PetProfileUI: no pet, hiding panel");
            gameObject.SetActive(false);
            return;
        }

        // 🔹 керуємо решіткою
        if (lockGrid)
            lockGrid.SetActive(pet.isclosed);

        // 🔹 локалізований текст на решітці
        if (lockGridLabel)
        {
            if (pet.isclosed)
                _ = SetLoc(L_Open, lockGridLabel);    // закрита → показати "Відкрити"
            else
                _ = SetLoc(L_Closed, lockGridLabel);  // відкрита → показати "Закрити"
        }
        _ = SetLoc(L_Equipment, buttonEquipmentText);
        _ = SetLoc(L_Settings, buttonPetSettingsText);
        _ = SetLoc(L_Training, buttonTrainingText);

        // Ім’я
        if (petNameLabel)    petNameLabel.text = pet.name;
        if (descriptionName) descriptionName.text = pet.name;

        // Стати
        if (powerText)         powerText.text         = pet.petpower.ToString();
        if (protectionText)    protectionText.text    = pet.petprotection.ToString();
        if (dexterityText)     dexterityText.text     = pet.petdexterity.ToString();
        if (skillText)         skillText.text         = pet.petskill.ToString();
        if (survivabilityText) survivabilityText.text = pet.petsurvivability.ToString();

        // Аватарка
        if (avatarImage)
            avatarImage.sprite = GetAvatarSprite(pet.avatar);
    }

    private Sprite GetAvatarSprite(string avatarKey)
    {
        switch (avatarKey)
        {
            case "pet_avatar_1": return avatar1Sprite;
            case "pet_avatar_2": return avatar2Sprite;
            case "pet_avatar_3": return avatar3Sprite;
            default:
                return avatar1Sprite != null ? avatar1Sprite : avatar2Sprite;
        }
    }
    private void OnUpgradeStat(string statKey)
    {
        if (_upgradeBusy) return;
        _ = UpgradeStatAsync(statKey);
    }

    private async System.Threading.Tasks.Task UpgradeStatAsync(string statKey)
    {
        _upgradeBusy = true;

        var session = PlayerSession.I;
        if (session == null)
        {
            _upgradeBusy = false;
            return;
        }

        var data = session.Data;
        var pet  = data.petInfo;
        if (pet == null)
        {
            _upgradeBusy = false;
            return;
        }

        int fromLevel = statKey switch
        {
            "power"         => pet.petpower,
            "skill"         => pet.petskill,
            "survivability" => pet.petsurvivability,
            "protection"    => pet.petprotection,
            "dexterity"     => pet.petdexterity,
            _               => 0
        };

        var resp = await ApiClient.PetUpgradeAsync(data.nickname, data.serialcode, statKey, fromLevel);
        if (resp == null)
        {
            Debug.LogError("PetUpgradeAsync: null response");
            _upgradeBusy = false;
            return;
        }

        if (resp.error != "OK")
        {
            Debug.LogWarning("PetUpgradeAsync error: " + resp.error);
            // TODO: показати попап "Не вистачає зелені" / "Рівень змінився" / ін.
            _upgradeBusy = false;
            return;
        }

        // Оновлюємо PlayerSession
        session.Patch(info =>
        {
            info.playergreen = resp.playergreen;
            info.playergold  = resp.playergold;

            if (resp.haspet && resp.pet != null)
            {
                if (info.petInfo == null) info.petInfo = new PetInfo();

                info.petInfo.id = resp.pet.id;
                info.petInfo.name = resp.pet.name;
                info.petInfo.avatar = resp.pet.avatar;
                info.petInfo.petpower = resp.pet.petpower;
                info.petInfo.petprotection = resp.pet.petprotection;
                info.petInfo.petdexterity = resp.pet.petdexterity;
                info.petInfo.petskill = resp.pet.petskill;
                info.petInfo.petsurvivability = resp.pet.petsurvivability;
                info.petInfo.petcollar = resp.pet.petcollar;
                info.petInfo.isalive = resp.pet.isalive;
                info.petInfo.isclosed = resp.pet.isclosed;
                info.petInfo.pethp = resp.pet.pethp;
                info.petInfo.petmaxhp = resp.pet.petmaxhp;
                info.petInfo.petkills = resp.pet.petkills;
                info.petInfo.petdeaths = resp.pet.petdeaths;
            }
        });

        // Перемалювати UI
        RefreshFromSession();

        _upgradeBusy = false;
    }
    /// <summary>
    /// Кнопка Close: перемикає IsClosed у БД та в PlayerSession (відкрити/закрити тварину).
    /// </summary>
    private void OnToggleClosedClicked()
    {
        _ = ToggleClosedAsync();
    }

    private async System.Threading.Tasks.Task ToggleClosedAsync()
    {
        var session = PlayerSession.I;
        if (session == null) return;

        var data = session.Data;
        var pet  = data.petInfo;
        if (pet == null) return;

        bool newClosed = !pet.isclosed;

        var resp = await ApiClient.PetSetClosedAsync(data.nickname, data.serialcode, newClosed);
        if (resp == null)
        {
            Debug.LogError("PetSetClosedAsync: null response");
            return;
        }

        if (resp.error != "OK")
        {
            Debug.LogWarning("PetSetClosedAsync error: " + resp.error);
            return;
        }

        // Оновлюємо локально
        session.Patch(info =>
        {
            if (info.petInfo != null)
                info.petInfo.isclosed = newClosed;
        });

        RefreshFromSession();
    }

    /// <summary>
    /// Проста дія "вийти з панелі" (для b_exit).
    /// </summary>
    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    // === Localization helper (аналогічно MonkUI.SetLoc) ===
    private async System.Threading.Tasks.Task SetLoc(LocalizedString key, TMP_Text label)
    {
        var op = key.GetLocalizedStringAsync();
        await op.Task;
        label.text = op.Result;
    }
}
