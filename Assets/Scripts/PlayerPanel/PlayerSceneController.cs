using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // для перевірки фокусу на інпутах

public class PlayerSceneController : MonoBehaviour
{
    [Header("Avatar")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Sprite defaultAvatar;

    [Header("UI Elements (optional)")]
    [SerializeField] private GameObject dimmer;    // може бути null (Image з RaycastTarget=true)
    [SerializeField] private AudioSource uiClick;  // може бути null

    [Header("Panels")]
    [SerializeField] private PlayerSubpanel panelStatistic;
    [SerializeField] private PlayerSubpanel panelTraining;
    [SerializeField] private PlayerSubpanel panelEquipment;
    [SerializeField] private PlayerSubpanel panelGifts;
    [SerializeField] private PlayerSubpanel panelAchievements;
    [SerializeField] private PlayerSubpanel panelProfile;

    [Header("Buttons (left/right columns)")]
    [SerializeField] private Button btnStatistic;
    [SerializeField] private Button btnTraining;
    [SerializeField] private Button btnEquipment;
    [SerializeField] private Button btnGifts;
    [SerializeField] private Button btnAchievements;
    [SerializeField] private Button btnProfile;

    [Header("Back/Esc behavior")]
    [Tooltip("Обробляти натиск Esc/Back для закриття панелей/виходу")]
    [SerializeField] private bool handleBackKey = true;

    [Tooltip("Необов’язково: кнопка виходу/назад. Буде натиснута, якщо панель не відкрита.")]
    [SerializeField] private Button backFallbackButton; // сюди можна підкинути твій b_exit

    private readonly List<PlayerSubpanel> allPanels = new();
    private PlayerSubpanel current;
    private Coroutine openRoutine;

    void Awake()
    {
        // Аватар (фолбек)
        if (avatarImage && defaultAvatar && avatarImage.sprite == null)
            avatarImage.sprite = defaultAvatar;

        // Зібрати всі панелі
        if (panelStatistic)    allPanels.Add(panelStatistic);
        if (panelTraining)     allPanels.Add(panelTraining);
        if (panelEquipment)    allPanels.Add(panelEquipment);
        if (panelGifts)        allPanels.Add(panelGifts);
        if (panelAchievements) allPanels.Add(panelAchievements);
        if (panelProfile)      allPanels.Add(panelProfile);

        // Підписки кнопок (єдине місце)
        if (btnStatistic)    btnStatistic.onClick.AddListener(() => Open(panelStatistic));
        if (btnTraining)     btnTraining.onClick.AddListener(() => Open(panelTraining));
        if (btnEquipment)    btnEquipment.onClick.AddListener(() => Open(panelEquipment));
        if (btnGifts)        btnGifts.onClick.AddListener(() => Open(panelGifts));
        if (btnAchievements) btnAchievements.onClick.AddListener(() => Open(panelAchievements));
        if (btnProfile)      btnProfile.onClick.AddListener(() => Open(panelProfile));
    }

    void Start()
    {
        // Прогрів панелей: щоб перший клік одразу показував UI
        StartCoroutine(PrewarmPanels());
    }

    void Update()
    {
        if (!handleBackKey) return;

        // Android клавіша Back мапиться на KeyCode.Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // якщо фокус у полі вводу — ігноруємо (не перехоплюємо Back)
            if (IsTypingInInput()) return;

            if (current != null)
            {
                // закриваємо відкриту панель
                CloseCurrent();
            }
            else
            {
                // якщо панелі нема — клікнемо по fallback-кнопці (якщо задана)
                if (backFallbackButton) backFallbackButton.onClick.Invoke();
                // інакше нічого не робимо — залишайся на сцені
            }
        }
    }

    private bool IsTypingInInput()
    {
        var go = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (!go) return false;

        // Стандартний UI InputField
        if (go.GetComponent<InputField>() != null) return true;

        // TMP_InputField без залежності від namespace (через строкову перевірку)
        var tmp = go.GetComponent("TMP_InputField");
        return tmp != null;
    }

    private IEnumerator PrewarmPanels()
    {
        yield return null;

        foreach (var p in allPanels)
        {
            if (p == null) continue;
            p.Show();
            Canvas.ForceUpdateCanvases();
            var rt = p.transform as RectTransform;
            if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            Canvas.ForceUpdateCanvases();
            p.Hide();
        }
    }

    public void Open(PlayerSubpanel target)
    {
        if (!target) return;

        // Закрити попередню (якщо інша)
        if (current && current != target)
            current.Hide();

        // Увімкнути димер під усім
        if (dimmer)
        {
            dimmer.SetActive(true);
            dimmer.transform.SetAsFirstSibling();
        }

        if (uiClick) uiClick.Play();

        // Відкрити "на наступному кадрі" (на випадок активних лейаутів)
        if (openRoutine != null) StopCoroutine(openRoutine);
        openRoutine = StartCoroutine(OpenNextFrame(target));
    }

    private IEnumerator OpenNextFrame(PlayerSubpanel target)
    {
        yield return null;

        target.Show();
        target.transform.SetAsLastSibling(); // панель над усім у своєму контейнері
        current = target;

        // Форс ребілд — одразу малюємо правильний лейаут
        Canvas.ForceUpdateCanvases();
        var rt = current.transform as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        Canvas.ForceUpdateCanvases();

        openRoutine = null;
    }

    public void CloseCurrent()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (current) current.Hide();

        if (dimmer)  dimmer.SetActive(false);

        current = null;
        Canvas.ForceUpdateCanvases();
    }

    // Публічний метод для заміни аватару
    public void SetAvatar(Sprite s)
    {
        if (avatarImage && s) avatarImage.sprite = s;
    }
}
