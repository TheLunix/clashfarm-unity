using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class GuardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TMP_Dropdown hoursDropdown;
    [SerializeField] private Button actionButton;               // "Стати на варту / Скасувати"
    [SerializeField] private TextMeshProUGUI actionButtonLabel;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Status Bar")]
    [SerializeField] private GameObject statusBar;
    [SerializeField] private TextMeshProUGUI statusBarText;
    [SerializeField] private float statusBarShowSeconds = 4f;

    private Coroutine statusBarRoutine;

    private enum GuardUiMode
    {
        Idle,
        Guarding
    }

    private GuardUiMode mode = GuardUiMode.Idle;

    private bool initialized;
    private float countdownSeconds;
    private bool countdownRunning;

    // локалізація
    private readonly LocalizedString L_ButtonStart    = new LocalizedString("Guard", "guard.ui.button.start");
    private readonly LocalizedString L_ButtonCancel   = new LocalizedString("Guard", "guard.ui.button.cancel");
    private readonly LocalizedString L_DescIdle       = new LocalizedString("Guard", "guard.ui.idle.text");
    private readonly LocalizedString L_DescGuarding   = new LocalizedString("Guard", "guard.ui.active.text");
    private readonly LocalizedString L_StatusCanceled = new LocalizedString("Guard", "guard.ui.status.cancelled");
    private readonly LocalizedString L_StatusReward   = new LocalizedString("Guard", "guard.ui.status.reward");
    private readonly LocalizedString L_Title          = new LocalizedString("Guard", "guard.ui.title");


    private async void OnEnable()
    {
        await InitializeAsync();
        await RefreshFromServer();
        if (MailManager.Instance != null)
            _ = MailManager.Instance.RefreshAsync();
    }

    private void OnDisable()
    {
        if (statusBarRoutine != null)
        {
            StopCoroutine(statusBarRoutine);
            statusBarRoutine = null;
        }
    }

    private async Task InitializeAsync()
    {
        if (initialized) return;

        // чекаємо, поки підтягнеться Unity Localization
        var initOp = LocalizationSettings.InitializationOperation;
        if (!initOp.IsDone)
            await initOp.Task;

        await BuildHoursDropdownAsync();

        initialized = true;
    }

    private async Task BuildHoursDropdownAsync()
    {
        if (hoursDropdown == null)
            return;

        hoursDropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();

        // 1..10 годин, кожен пункт через локалізацію Guard/guard.ui.dropdown.hours.X
        for (int h = 1; h <= 10; h++)
        {
            var key = new LocalizedString("Guard", $"guard.ui.dropdown.hours.{h}");
            var op = key.GetLocalizedStringAsync();
            await op.Task;

            var label = op.Result;
            if (string.IsNullOrEmpty(label))
                label = h.ToString(); // fallback на випадок, якщо ключ не знайдено

            options.Add(new TMP_Dropdown.OptionData(label));
        }

        hoursDropdown.AddOptions(options);
        hoursDropdown.value = 0;
        hoursDropdown.RefreshShownValue();
    }

    private async Task RefreshFromServer()
    {
        var sess = PlayerSession.I;
        if (sess == null || sess.Data == null)
        {
            mode = GuardUiMode.Idle;
            countdownRunning = false;
            countdownSeconds = 0;
            await UpdateTextsAsync();
            return;
        }

        var nick   = sess.Data.nickname;
        var serial = sess.Data.serialcode;

        var state = await ApiClient.GuardStateAsync(nick, serial);
        if (state == null)
        {
            Debug.LogWarning("GuardStateAsync returned null");
            mode = GuardUiMode.Idle;
            countdownRunning = false;
            countdownSeconds = 0;
            await UpdateTextsAsync();
            return;
        }

        if (!string.IsNullOrEmpty(state.error))
        {
            Debug.LogWarning("GuardState error: " + state.error);
            mode = GuardUiMode.Idle;
            countdownRunning = false;
            countdownSeconds = 0;
            await UpdateTextsAsync();
            return;
        }

        // якщо під час цього виклику сервер автодонарахував нагороду
        if (state.rewardGreen > 0 || state.rewardXp > 0 ||
            (state.events != null && state.events.Length > 0))
        {
            // локальне оновлення ресурсів (щоб HUD одразу підріс)
            PlayerSession.I.Patch(info =>
            {
                info.playergreen      += state.rewardGreen;
                info.playerexpierence += state.rewardXp;
            });

            ShowRewardStatus(state.rewardGreen, state.rewardXp);
        }

        if (state.active && !string.IsNullOrEmpty(state.timeToEndUtc) && state.timeToEndUtc != "0")
        {
            if (DateTime.TryParse(state.timeToEndUtc, null,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var endUtc))
            {
                countdownSeconds = (float)Math.Max(0, (endUtc - DateTime.UtcNow).TotalSeconds);
                countdownRunning = countdownSeconds > 0f;
                mode = GuardUiMode.Guarding;
            }
            else
            {
                mode = GuardUiMode.Idle;
                countdownRunning = false;
                countdownSeconds = 0f;
            }
        }
        else
        {
            mode = GuardUiMode.Idle;
            countdownRunning = false;
            countdownSeconds = 0f;
        }

        await UpdateTextsAsync();
    }

    public async void OnActionButtonClick()
    {
        var sess = PlayerSession.I;
        if (sess == null || sess.Data == null) return;

        var nick   = sess.Data.nickname;
        var serial = sess.Data.serialcode;

        if (mode == GuardUiMode.Idle)
        {
            if (hoursDropdown == null || hoursDropdown.options.Count == 0)
                return;

            int selectedHours = Mathf.Clamp(hoursDropdown.value + 1, 1, 10);

            var dto = await ApiClient.GuardStartAsync(nick, serial, selectedHours);
            if (dto == null)
            {
                Debug.LogWarning("GuardStartAsync returned null");
                return;
            }

            if (!string.IsNullOrEmpty(dto.error))
            {
                Debug.LogWarning("GuardStartAsync error: " + dto.error);
                // можна показати statusBar з якимось guard.ui.error.*
                return;
            }

            // якщо під час старту ще прилетіла нагорода за попередню варту
            if (dto.rewardGreen > 0 || dto.rewardXp > 0 ||
                (dto.events != null && dto.events.Length > 0))
            {
                PlayerSession.I.Patch(info =>
                {
                    info.playergreen      += dto.rewardGreen;
                    info.playerexpierence += dto.rewardXp;
                });

                ShowRewardStatus(dto.rewardGreen, dto.rewardXp);
            }

            if (dto.active && !string.IsNullOrEmpty(dto.timeToEndUtc) && dto.timeToEndUtc != "0" &&
                DateTime.TryParse(dto.timeToEndUtc, null,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var endUtc))
            {
                countdownSeconds = (float)Math.Max(0, (endUtc - DateTime.UtcNow).TotalSeconds);
                countdownRunning = countdownSeconds > 0f;
                mode = GuardUiMode.Guarding;
            }
            else
            {
                mode = GuardUiMode.Idle;
                countdownRunning = false;
                countdownSeconds = 0f;
            }

            await UpdateTextsAsync();
        }
        else // Guarding -> Cancel
        {
            var dto = await ApiClient.GuardCancelAsync(nick, serial);
            if (dto == null)
            {
                Debug.LogWarning("GuardCancelAsync returned null");
                return;
            }

            if (!string.IsNullOrEmpty(dto.error))
            {
                Debug.LogWarning("GuardCancelAsync error: " + dto.error);
                return;
            }

            // якщо в момент cancel сервер побачив, що час уже вийшов — все одно може прилетіти нагорода
            if (dto.rewardGreen > 0 || dto.rewardXp > 0 ||
                (dto.events != null && dto.events.Length > 0))
            {
                PlayerSession.I.Patch(info =>
                {
                    info.playergreen      += dto.rewardGreen;
                    info.playerexpierence += dto.rewardXp;
                });

                ShowRewardStatus(dto.rewardGreen, dto.rewardXp);
            }
            else
            {
                // варта реально скасована без нагороди
                ShowLocalizedStatusBar(L_StatusCanceled);
            }

            mode = GuardUiMode.Idle;
            countdownRunning = false;
            countdownSeconds = 0f;

            await UpdateTextsAsync();
        }
    }

    private void Update()
    {
        if (mode == GuardUiMode.Guarding && countdownRunning)
        {
            countdownSeconds -= Time.deltaTime;
            if (countdownSeconds <= 0f)
            {
                countdownSeconds = 0f;
                countdownRunning = false;
                // автодонарахування зробить сервер при наступному GuardState/Start/Cancel
            }

            UpdateDescriptionWithTimer();
        }
    }

    private async Task UpdateTextsAsync()
    {
        if (!initialized) return;

        if (mode == GuardUiMode.Idle)
        {
            await SetLoc(L_ButtonStart, actionButtonLabel);
        }
        else
        {
            await SetLoc(L_ButtonCancel, actionButtonLabel);
        }
        await SetLoc(L_Title, titleText);
        UpdateDescriptionWithTimer();
    }

    private async void UpdateDescriptionWithTimer()
    {
        if (descriptionText == null)
            return;

        if (mode == GuardUiMode.Idle)
        {
            // просто локалізований текст без таймера
            var op = L_DescIdle.GetLocalizedStringAsync();
            await op.Task;
            descriptionText.text = op.Result;
            return;
        }

        // режим варти
        var op2 = L_DescGuarding.GetLocalizedStringAsync();
        await op2.Task;
        string baseText = op2.Result;

        if (countdownSeconds > 0f)
        {
            var t = TimeSpan.FromSeconds(countdownSeconds);
            string timer = $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
            descriptionText.text = baseText + "\n\n" + timer;
        }
        else
        {
            descriptionText.text = baseText + "\n\n00:00:00";
        }
    }

    // === StatusBar helpers ===

    private void ShowRewardStatus(int green, int xp)
    {
        if (statusBar == null || statusBarText == null)
            return;

        // guard.ui.status.reward = "Варта завершена. Нагорода: {0} зелені, {1} досвіду."
        ShowLocalizedStatusBar(L_StatusReward, green, xp);
    }

    private void ShowLocalizedStatusBar(LocalizedString key, params object[] args)
    {
        if (statusBar == null || statusBarText == null)
            return;

        // обгортаємо в окрему async-функцію, щоб можна було викликати з non-async контексту
        _ = ShowLocalizedStatusBarAsync(key, args);
    }

    private async Task ShowLocalizedStatusBarAsync(LocalizedString key, params object[] args)
    {
        var op = key.GetLocalizedStringAsync(args);
        await op.Task;

        var txt = op.Result;
        if (string.IsNullOrEmpty(txt))
            return;

        statusBarText.text = txt;
        statusBar.SetActive(true);

        if (statusBarRoutine != null)
            StopCoroutine(statusBarRoutine);

        statusBarRoutine = StartCoroutine(HideStatusBarAfterDelay());
    }

    private System.Collections.IEnumerator HideStatusBarAfterDelay()
    {
        yield return new WaitForSeconds(statusBarShowSeconds);

        if (statusBar != null)
            statusBar.SetActive(false);

        statusBarRoutine = null;
    }

    // === Localization helper ===

    private async Task SetLoc(LocalizedString key, TMP_Text label)
    {
        if (label == null) return;

        var op = key.GetLocalizedStringAsync();
        await op.Task;
        label.text = op.Result;
    }
}
