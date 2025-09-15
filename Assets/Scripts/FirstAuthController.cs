using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Text;

public class FirstAuthController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_InputField serialInput;     // можна зробити readonly
    [SerializeField] private Button randomSerialButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI hintLabel;      // повідомлення/помилки

    [Header("Scenes")]
    [SerializeField] private string loadingScene = "Loading";

    const string NickKey   = "Name";
    const string SerialKey = "SerialCode";

    void Start()
    {
        // Підтягнемо, якщо вже щось було
        nicknameInput.text = PlayerPrefs.GetString(NickKey, "").Trim();

        // Якщо серіалу не було — згенеруємо
        var s = PlayerPrefs.GetString(SerialKey, "").Trim();
        serialInput.text = string.IsNullOrEmpty(s) ? GenerateSerial(16) : s;

        randomSerialButton.onClick.AddListener(() =>
        {
            serialInput.text = GenerateSerial(16);
        });

        continueButton.onClick.AddListener(async () => await OnContinue());
    }

    async Task OnContinue()
    {
        var nick = (nicknameInput.text ?? "").Trim();
        var serial = (serialInput.text ?? "").Trim();

        if (nick.Length < 3)
        {
            SetHint("Введіть нікнейм (мін. 3 символи).");
            return;
        }
        if (serial.Length < 8)
        {
            SetHint("Серійний код надто короткий.");
            return;
        }

        // Зберігаємо локально
        PlayerPrefs.SetString(NickKey, nick);
        PlayerPrefs.SetString(SerialKey, serial);
        PlayerPrefs.Save();

        // (Не обов’язково) Спробуємо перевірити акаунт — чисто щоб попередити юзера одразу
        SetHint("Перевіряємо обліковий запис…");
        var acc = await ApiClient.GetAccountAsync(nick, serial);
        if (acc == null)
        {
            // Це не критично: можливо, реальна реєстрація відбувається пізніше
            // або в тебе інша ендпойнт-логіка. Просто попереджаємо.
            SetHint("Акаунт не знайдено — продовжуємо, спроба створення/підтвердження буде пізніше.");
            await Task.Delay(500);
        }

        // Повертаємось у Loading — він зробить далі всю магію
        SceneManager.LoadScene(loadingScene);
    }

    void SetHint(string msg)
    {
        if (hintLabel != null) hintLabel.text = msg;
    }

    // Дуже проста генерація псевдосеріалу
    static string GenerateSerial(int len)
    {
        const string abc = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var sb = new StringBuilder(len);
        var rnd = new System.Random();
        for (int i = 0; i < len; i++) sb.Append(abc[rnd.Next(abc.Length)]);
        return sb.ToString();
    }
}
