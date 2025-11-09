using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // ← для завантаження сцени
using System.Collections;
using System.Collections.Generic;

#if USE_GOOGLE_SIGNIN
using Google; // з плагіна
#endif

public class SettingsUI : MonoBehaviour
{
    [Header("Основні об’єкти")]
    [SerializeField] private GameObject panelSettings;
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite iconGear;
    [SerializeField] private Sprite iconBack;
    [SerializeField] private Button buttonGoogleAuth;

    [Header("Швидкий логін (тест)")]
    [Tooltip("Кнопка, яка проставляє User3 / ZDM6ATHJSUMABZJ4 і перезавантажує сцену 'loading'")]
    [SerializeField] private Button buttonQuickLogin;

    [Header("Що сховати під час відкритої панелі")]
    [Tooltip("Універсальний кореневий GO (напр., 'GO Training Elements'), ховається коли відкриті налаштування.")]
    [SerializeField] private GameObject hideRoot;

    [Tooltip("Додаткові об’єкти, які теж треба ховати/показувати разом із панеллю.")]
    [SerializeField] private List<GameObject> extraToHide = new List<GameObject>();

    private bool isOpen = false;

    void Start()
    {
        if (panelSettings != null) panelSettings.SetActive(false);
        if (iconImage != null && iconGear != null) iconImage.sprite = iconGear;

        if (buttonGoogleAuth != null)
            buttonGoogleAuth.onClick.AddListener(OnGoogleAuthClicked);

        if (buttonQuickLogin != null)
            buttonQuickLogin.onClick.AddListener(OnQuickLoginClicked);

        SetHiddenElementsActive(true);
    }

    public void OnSettingsButtonClicked()
    {
        isOpen = !isOpen;

        if (panelSettings != null)
            panelSettings.SetActive(isOpen);

        if (iconImage != null)
            iconImage.sprite = isOpen ? iconBack : iconGear;

        SetHiddenElementsActive(!isOpen);
    }

    private void SetHiddenElementsActive(bool visible)
    {
        if (hideRoot != null) hideRoot.SetActive(visible);

        if (extraToHide != null)
        {
            for (int i = 0; i < extraToHide.Count; i++)
                if (extraToHide[i] != null) extraToHide[i].SetActive(visible);
        }
    }

    private void OnQuickLoginClicked()
    {
        // Записуємо тестові креденшали
        PlayerPrefs.SetString("Name",   "User3");
        PlayerPrefs.SetString("SerialCode", "ZDM6ATHJSUMABZJ4");
        PlayerPrefs.Save();

        // (опціонально) закриємо панель і повернемо іконку-шестерню
        if (panelSettings != null) panelSettings.SetActive(false);
        if (iconImage != null && iconGear != null) iconImage.sprite = iconGear;
        isOpen = false;
        SetHiddenElementsActive(true);

        // Перезавантажуємо сцену "loading"
        SceneManager.LoadScene("loading");
    }

    private void OnGoogleAuthClicked()
    {
#if USE_GOOGLE_SIGNIN && UNITY_ANDROID && !UNITY_EDITOR
        GoogleSignIn.Configuration = new GoogleSignInConfiguration {
            WebClientId    = "717687880808-b6e5qqsarg06pq91oss8agikgo4bj8vp.apps.googleusercontent.com",
            RequestIdToken = true,
            RequestEmail   = true
        };

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception);
                return;
            }
            var account = task.Result;
            var idToken = account.IdToken;

            StartCoroutine(ApiClient.Instance.LinkGoogleAccount(idToken));
        });
#else
        Debug.LogWarning("Google Sign-In працює лише на Android пристрої. Збери APK і протестуй на девайсі.");
#endif
    }
}
