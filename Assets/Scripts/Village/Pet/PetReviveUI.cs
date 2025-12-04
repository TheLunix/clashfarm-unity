using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetReviveUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text descriptionText;   // текст "Тварина мертва, воскресити?"
    [SerializeField] private Button reviveButton;        // кнопка "Воскресити"
    [SerializeField] private TMP_Text reviveButtonLabel; // текст на кнопці (локалізований)
    [SerializeField] private Button cancelButton;        // кнопка "Назад/Закрити"

    private void Awake()
    {
        if (reviveButton) reviveButton.onClick.AddListener(OnReviveClicked);
        if (cancelButton) cancelButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        var session = PlayerSession.I;
        if (session == null) return;

        var pet = session.Data.petInfo;
        if (pet == null)
        {
            // Якщо тварини взагалі немає – ця панель не має бути відкрита
            gameObject.SetActive(false);
            return;
        }

        // Тут можна підставити локалізований текст вручну або через LocalizedString
        if (descriptionText)
            descriptionText.text = "Ваш компаньйон мертвий. Хочете воскресити його?";

        if (reviveButtonLabel)
            reviveButtonLabel.text = "Воскресити";
    }

    private void OnReviveClicked()
    {
        _ = ReviveAsync();
    }

    private async Task ReviveAsync()
    {
        var session = PlayerSession.I;
        if (session == null) return;

        string nickname = session.Data.nickname;
        string serial   = session.Data.serialcode;

        var resp = await ApiClient.PetReviveAsync(nickname, serial);
        if (resp == null)
        {
            Debug.LogError("PetReviveAsync: null response");
            return;
        }

        if (resp.error != "OK" && resp.error != "ALREADY_ALIVE")
        {
            Debug.LogWarning("PetReviveAsync error: " + resp.error);
            return;
        }

        // Оновлюємо локальну сесію
        session.Patch(info =>
        {
            info.playergold = resp.playergold;

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
                info.petInfo.petkills = resp.pet.petkills;
                info.petInfo.petdeaths = resp.pet.petdeaths;
            }
        });

        // закриваємо панель воскресіння
        Close();

        // і відкриваємо профіль (через Navigation або PetStoreUI)
        if (Navigation.Instance != null)
            Navigation.Instance.GoToPet(); // тепер тварина жива → піде в профіль
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
