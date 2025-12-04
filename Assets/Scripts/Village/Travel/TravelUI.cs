using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class TravelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TMP_Dropdown minutesDropdown;
    [SerializeField] private Button actionButton;                 // кнопка "Вирушити / Скасувати"
    [SerializeField] private TextMeshProUGUI actionButtonLabel;   // текст на кнопці
    [SerializeField] private TextMeshProUGUI minutesLeftText;     // текст опису
    [Space]
    [SerializeField] private GameObject statusBar;               // GO StatusBar
    [SerializeField] private TextMeshProUGUI statusBarText;      // TMP-текст всередині StatusBar
    [SerializeField] private float statusBarShowSeconds = 4f;    // скільки секунд тримати повідомлення

    [Header("Config")]
    [SerializeField] private bool hasTravelKit = false;           // чи орендований набір для подорожей
    [SerializeField] private int baseMaxMinutesPerDay = 180;      // базовий ліміт на добу
    [SerializeField] private int kitMaxMinutesPerDay = 360;       // ліміт при наборі (якщо треба)

    // внутрішні поля
    private readonly List<int> baseDurations = new List<int> { 10, 20, 30, 60, 120, 180 };
    private readonly List<int> kitExtraDurations = new List<int> { 240, 300, 360 };

    private List<int> allDurations = new List<int>();

    private int minutesLeftToday;
    private int maxMinutesToday;

    private enum TravelUiMode
    {
        Idle,
        Travelling
    }

    private TravelUiMode mode = TravelUiMode.Idle;

    private float countdownSeconds;
    private bool countdownRunning;
    private Coroutine statusBarRoutine;

    // === Localization keys (String Table: Travel) ===
    private readonly LocalizedString L_ButtonStart      = new LocalizedString("Travel", "travel.ui.button.start");
    private readonly LocalizedString L_ButtonCancel     = new LocalizedString("Travel", "travel.ui.button.cancel");
    private readonly LocalizedString L_DescIdle         = new LocalizedString("Travel", "travel.ui.idle.text");
    private readonly LocalizedString L_DescTravelling   = new LocalizedString("Travel", "travel.ui.active.text");
    private readonly LocalizedString L_DropdownEmpty    = new LocalizedString("Travel", "travel.ui.dropdown.empty");
    private readonly LocalizedString L_TravelCanceled   = new LocalizedString("Travel", "travel.ui.status.cancelled");
    private readonly LocalizedString L_Title            = new LocalizedString("Travel", "travel.ui.title");

    private string dropdownEmptyText = "Немає доступного часу";

    private void Awake()
    {
        // визначаємо добовий ліміт (локально, реальний підтягнемо із сервера)
        maxMinutesToday = hasTravelKit ? kitMaxMinutesPerDay : baseMaxMinutesPerDay;
        minutesLeftToday = maxMinutesToday;

        BuildDurationsList();

        // Локалізація може ще не бути готовою в Awake, тому робимо мінімальну підготовку,
        // а потім оновимо тексти в OnEnable після ініціалізації LocalizationSettings.
        RefreshDropdown();
        RefreshMinutesLeftText();
        UpdateActionButtonState();
    }

    private async void OnEnable()
    {
        // 1) Чекаємо готовність системи локалізації (як у MonkUI)
        var init = LocalizationSettings.InitializationOperation;
        if (!init.IsDone) await init.Task;

        // 2) Підтягнемо локалізований текст для порожнього dropdown
        var emptyOp = L_DropdownEmpty.GetLocalizedStringAsync();
        await emptyOp.Task;
        dropdownEmptyText = string.IsNullOrEmpty(emptyOp.Result) ? "Немає доступного часу" : emptyOp.Result;

        // 3) Оновлюємо конфіг
        maxMinutesToday = hasTravelKit ? kitMaxMinutesPerDay : baseMaxMinutesPerDay;

        BuildDurationsList();
        RefreshDropdown();

        await SetLoc(L_Title, titleText);

        // 4) Тягнемо стейт із сервера
        await RefreshFromServer();
        if (MailManager.Instance != null)
            _ = MailManager.Instance.RefreshAsync();
    }

    private void OnDisable()
    {
        // нічого спеціального робити не треба
    }

    private async Task RefreshFromServer()
    {
        var sess = PlayerSession.I;
        if (sess == null || sess.Data == null)
        {
            minutesLeftToday = maxMinutesToday;
            mode = TravelUiMode.Idle;
            RefreshMinutesLeftText();
            UpdateActionButtonState();
            return;
        }

        var nick = sess.Data.nickname;
        var serial = sess.Data.serialcode;

        var state = await ApiClient.TravelStateAsync(nick, serial);
        if (state == null)
        {
            minutesLeftToday = maxMinutesToday;
            mode = TravelUiMode.Idle;
        }
        else
        {
            maxMinutesToday   = state.dailyLimit > 0 ? state.dailyLimit : maxMinutesToday;
            minutesLeftToday  = Mathf.Clamp(state.minutesLeft, 0, maxMinutesToday);

            if (state.active && !string.IsNullOrEmpty(state.timeToEndUtc) && state.timeToEndUtc != "0")
            {
                if (DateTime.TryParse(state.timeToEndUtc, null,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var endUtc))
                {
                    countdownSeconds = (float)Math.Max(0, (endUtc - DateTime.UtcNow).TotalSeconds);
                    countdownRunning = countdownSeconds > 0;
                    mode = TravelUiMode.Travelling;
                }
                else
                {
                    mode = TravelUiMode.Idle;
                }
            }
            else
            {
                mode = TravelUiMode.Idle;
            }

            // патчимо сесію, щоб VillageController міг це знати
            PlayerSession.I.Patch(info =>
            {
                info.hikeminutes   = minutesLeftToday;
                info.hikemin       = maxMinutesToday;
                info.timetoendhike = state.timeToEndUtc ?? "0";
                info.hikeactivemin = state.active ? 1 : 0;
            });

            TryShowTravelRewardStatus(state);
        }

        RefreshMinutesLeftText();
        UpdateActionButtonState();
    }

    public void SetMinutesLeftFromServer(int minutesLeft)
    {
        minutesLeftToday = Mathf.Clamp(minutesLeft, 0, maxMinutesToday);
        RefreshDropdown();
        RefreshMinutesLeftText();
        UpdateActionButtonState();
    }

    /// <summary>
    /// Викликати, коли стало відомо, що набір для подорожей активовано/деактивовано.
    /// </summary>
    public void SetTravelKitActive(bool active)
    {
        hasTravelKit = active;
        maxMinutesToday = hasTravelKit ? kitMaxMinutesPerDay : baseMaxMinutesPerDay;

        // тут логічно ще раз оновити залишок, якщо ти будеш тягнути його з сервера
        if (minutesLeftToday > maxMinutesToday)
            minutesLeftToday = maxMinutesToday;

        BuildDurationsList();
        RefreshDropdown();
        RefreshMinutesLeftText();
        UpdateActionButtonState();
    }

    /// <summary>
    /// Створюємо список усіх можливих тривалостей (6 або 9 значень).
    /// </summary>
    private void BuildDurationsList()
    {
        allDurations.Clear();
        allDurations.AddRange(baseDurations);

        if (hasTravelKit)
        {
            allDurations.AddRange(kitExtraDurations);
        }
    }

    /// <summary>
    /// Оновлюємо Dropdown згідно з залишком хвилин.
    /// </summary>
    private void RefreshDropdown()
    {
        if (!minutesDropdown) return;

        minutesDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        List<int> allowedDurations = new List<int>();

        foreach (int duration in allDurations)
        {
            // показуємо тільки ті тривалості, які влазять у залишок хвилин
            if (duration <= minutesLeftToday && minutesLeftToday > 0)
            {
                allowedDurations.Add(duration);
                options.Add(new TMP_Dropdown.OptionData($"{duration} хв"));
            }
        }

        if (allowedDurations.Count == 0)
        {
            minutesDropdown.interactable = false;
            minutesDropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData(dropdownEmptyText)
            };
            minutesDropdown.value = 0;
        }
        else
        {
            minutesDropdown.interactable = true;
            minutesDropdown.AddOptions(options);
            minutesDropdown.value = 0; // завжди перший доступний варіант
        }

        // Якщо на Dropdown висить DropDownAutoResize — оновимо висоту списку
        var autoResize = minutesDropdown.GetComponent<DropDownAutoResize>();
        if (autoResize != null)
        {
            autoResize.UpdateDropdownHeight();
        }
    }

    private void RefreshMinutesLeftText()
    {
        if (minutesLeftText == null) return;

        if (mode == TravelUiMode.Idle)
        {
            // "Відправляйтесь в подорож ... Залишилось часу на подорож: {minutesLeftToday} хвилин"
            L_DescIdle.Arguments = new object[] { minutesLeftToday };
            _ = SetLoc(L_DescIdle, minutesLeftText);
        }
        else // Travelling
        {
            var sec = Mathf.Max(0, countdownSeconds);
            var t = TimeSpan.FromSeconds(sec);
            string mmss = $"{(int)t.Minutes:00}:{(int)t.Seconds:00}";

            // "Ви вирушили в подорож. До кінця подорожі {mmss} хв."
            L_DescTravelling.Arguments = new object[] { mmss };
            _ = SetLoc(L_DescTravelling, minutesLeftText);
        }
    }

    private void UpdateActionButtonState()
    {
        if (actionButton == null || actionButtonLabel == null || minutesDropdown == null) return;

        if (mode == TravelUiMode.Idle)
        {
            minutesDropdown.interactable = minutesLeftToday >= 10; // мінімум 10 хв лишилось
            actionButton.interactable   = minutesDropdown.interactable;

            // локалізована кнопка "Вирушити в подорож"
            _ = SetLoc(L_ButtonStart, actionButtonLabel);
        }
        else
        {
            minutesDropdown.interactable = false;
            actionButton.interactable   = true;

            // локалізована кнопка "Скасувати подорож"
            _ = SetLoc(L_ButtonCancel, actionButtonLabel);
        }
    }

    public async void OnActionButtonClick()
    {
        var sess = PlayerSession.I;
        if (sess == null || sess.Data == null) return;

        var nick   = sess.Data.nickname;
        var serial = sess.Data.serialcode;

        if (mode == TravelUiMode.Idle)
        {
            if (minutesDropdown.options.Count == 0) return;

            int selectedMinutes = allDurations.Count > 0
                ? allDurations[Mathf.Clamp(minutesDropdown.value, 0, allDurations.Count - 1)]
                : 0;

            if (selectedMinutes <= 0 || selectedMinutes > minutesLeftToday)
                return;

            var dto = await ApiClient.TravelStartAsync(nick, serial, selectedMinutes);
            if (dto == null || !string.IsNullOrEmpty(dto.error))
            {
                Debug.LogWarning("TravelStart error: " + dto?.error);
                return;
            }

            minutesLeftToday = dto.minutesLeft;
            maxMinutesToday  = dto.dailyLimit > 0 ? dto.dailyLimit : maxMinutesToday;

            if (!string.IsNullOrEmpty(dto.timeToEndUtc) && dto.timeToEndUtc != "0" &&
                DateTime.TryParse(dto.timeToEndUtc, null,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var endUtc))
            {
                countdownSeconds = (float)Math.Max(0, (endUtc - DateTime.UtcNow).TotalSeconds);
                countdownRunning = countdownSeconds > 0;
                mode = TravelUiMode.Travelling;
            }
            else
            {
                mode = TravelUiMode.Idle;
            }

            PlayerSession.I.Patch(info =>
            {
                info.hikeminutes   = minutesLeftToday;
                info.hikemin       = maxMinutesToday;
                info.hikeactivemin = selectedMinutes;
                info.timetoendhike = dto.timeToEndUtc ?? "0";
                info.lasthike      = DateTime.UtcNow.ToString("o");
            });
        }
        else // Travelling -> cancel
        {
            var dto = await ApiClient.TravelCancelAsync(nick, serial);
            if (dto == null || !string.IsNullOrEmpty(dto.error))
            {
                Debug.LogWarning("TravelCancel error: " + dto?.error);
                return;
            }

            minutesLeftToday = dto.minutesLeft;
            maxMinutesToday  = dto.dailyLimit > 0 ? dto.dailyLimit : maxMinutesToday;

            mode = TravelUiMode.Idle;
            countdownRunning = false;
            countdownSeconds = 0;

            PlayerSession.I.Patch(info =>
            {
                info.hikeminutes   = minutesLeftToday;
                info.hikemin       = maxMinutesToday;
                info.hikeactivemin = 0;
                info.timetoendhike = "0";
            });

            // показуємо повідомлення гравцю, що подорож скасована без нагороди
            ShowLocalizedStatusBar(L_TravelCanceled);
        }

        RefreshDropdown();
        RefreshMinutesLeftText();
        UpdateActionButtonState();
    }

    private void Update()
    {
        if (mode == TravelUiMode.Travelling && countdownRunning)
        {
            countdownSeconds -= Time.deltaTime;
            if (countdownSeconds <= 0f)
            {
                countdownSeconds = 0f;
                countdownRunning = false;
                // подорож закінчилась — нагороду й події обробляє сервер у travel/state
            }
            RefreshMinutesLeftText();
        }
    }

    // === Localization helper (аналогічно MonkUI) ===
    private async Task SetLoc(LocalizedString key, TMP_Text label)
    {
        if (label == null) return;

        var op = key.GetLocalizedStringAsync();
        await op.Task;
        label.text = op.Result;
    }

    // На випадок, якщо десь ще використаєш
    private int GetSelectedDuration()
    {
        if (minutesDropdown == null || minutesDropdown.options.Count == 0)
            return 0;

        string text = minutesDropdown.options[minutesDropdown.value].text;

        int spaceIndex = text.IndexOf(' ');
        if (spaceIndex > 0)
        {
            string numberPart = text.Substring(0, spaceIndex);
            if (int.TryParse(numberPart, out int minutes))
                return minutes;
        }

        if (int.TryParse(text, out int plainMinutes))
            return plainMinutes;

        return 0;
    }
    // ===================== STATUS BAR REWARD POPUP =====================

    private void TryShowTravelRewardStatus(ApiClient.TravelStateDto state)
    {
        if (state == null)
            return;

        // якщо з сервера не прийшло нагороди – нічого не показуємо
        bool hasReward =
            state.rewardGreenBase > 0 ||
            state.rewardGreenExtra > 0 ||
            state.rewardGoldExtra > 0 ||
            state.rewardExpExtra > 0;

        if (!hasReward)
            return;

        string message = BuildTravelRewardMessage(state);
        if (string.IsNullOrEmpty(message))
            return;

        ShowStatusBarMessage(message);
    }

    private void ShowStatusBarMessage(string message)
    {
        if (statusBar == null || statusBarText == null)
            return;

        if (string.IsNullOrEmpty(message))
            return;

        statusBar.SetActive(true);
        statusBarText.text = message;

        if (statusBarRoutine != null)
            StopCoroutine(statusBarRoutine);

        statusBarRoutine = StartCoroutine(HideStatusBarAfterDelay());
    }

    private string BuildTravelRewardMessage(ApiClient.TravelStateDto state)
    {
        int greenTotal = state.rewardGreenBase + state.rewardGreenExtra;

        var parts = new List<string>();

        if (greenTotal > 0)
            parts.Add($"+{greenTotal} зелені");

        if (state.rewardGoldExtra > 0)
            parts.Add($"+{state.rewardGoldExtra} золота");

        if (state.rewardExpExtra > 0)
            parts.Add($"+{state.rewardExpExtra} досвіду");

        if (parts.Count == 0)
            return null;

        // можна винести в локалізацію пізніше
        return "Повернення з подорожі: " + string.Join(", ", parts);
    }

    private System.Collections.IEnumerator HideStatusBarAfterDelay()
    {
        yield return new WaitForSeconds(statusBarShowSeconds);

        if (statusBar != null)
            statusBar.SetActive(false);

        statusBarRoutine = null;
    }

    private async void ShowLocalizedStatusBar(LocalizedString key)
    {
        if (statusBar == null || statusBarText == null)
            return;

        var op = key.GetLocalizedStringAsync();
        await op.Task;

        var text = op.Result;
        if (string.IsNullOrEmpty(text))
            return;

        statusBarText.text = text;
        statusBar.SetActive(true);

        if (statusBarRoutine != null)
            StopCoroutine(statusBarRoutine);

        statusBarRoutine = StartCoroutine(HideStatusBarAfterDelay());
    }

}
