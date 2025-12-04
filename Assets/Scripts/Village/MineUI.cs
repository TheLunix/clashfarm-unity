using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MineUI : MonoBehaviour
{
    [Header("Refs: Panels & Texts")]
    [SerializeField] private TMP_Text titleText; // PanelTextMine/TextMine
    [SerializeField] private GameObject panelEnter;
    [SerializeField] private TMP_Text enterText; // PanelEnterMine/ConversationMonk
    [SerializeField] private Button enterButton; // PanelEnterMine/RewardButton
    [SerializeField] private TMP_Text enterBtnLabel;

    [SerializeField] private GameObject panelEvent;
    [SerializeField] private TMP_Text eventText; // PanelEventMine/ConversationMonk
    [SerializeField] private Button claimButton; // PanelEventMine/RewardButton
    [SerializeField] private TMP_Text claimBtnLabel;

    [Header("Backgrounds (MinePanel Image)")]
    [SerializeField] private Image mineBackground;     // Сам компонент Image на MinePanel
    [SerializeField] private Sprite surfaceSprite;     // Картинка, коли ще не всередині
    [SerializeField] private Sprite insideSprite;      // Картинка, коли гравець у шахті

    [Header("Misc")]
    [SerializeField] private Button exitButton;   // buttons/b_exit
    [SerializeField] private GameObject statusBar;
    [SerializeField] private TMP_Text statusDesc;

    private Coroutine topTimerCo;
    private Coroutine searchTimerCo;

    private void OnEnable()
    {
        enterButton.onClick.AddListener(OnEnter);
        claimButton.onClick.AddListener(OnClaim);
        if (exitButton) exitButton.onClick.AddListener(OnExit);

        panelEnter.SetActive(true);
        panelEvent.SetActive(false);
        titleText.text = "Шахта";

        StartCoroutine(RefreshState());
        if (MailManager.Instance != null)
            _ = MailManager.Instance.RefreshAsync();
    }

    private void OnDisable()
    {
        enterButton.onClick.RemoveListener(OnEnter);
        claimButton.onClick.RemoveListener(OnClaim);
        if (exitButton) exitButton.onClick.RemoveListener(OnExit);

        StopTimers();
    }

    private void StopTimers()
    {
        if (topTimerCo != null) { StopCoroutine(topTimerCo); topTimerCo = null; }
        if (searchTimerCo != null) { StopCoroutine(searchTimerCo); searchTimerCo = null; }
    }

    private IEnumerator RefreshState()
    {
        StopTimers();

        string name = PlayerPrefs.GetString("Name", PlayerPrefs.GetString("PlayerName", ""));
        string code = PlayerPrefs.GetString("SerialCode", PlayerPrefs.GetString("PlayerSerialCode", ""));
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
        {
            enterText.text = "Немає даних гравця.";
            enterButton.interactable = false;
            claimButton.interactable = false;
            yield break;
        }

        var task = ApiClient.MineStateAsync(name, code);
        while (!task.IsCompleted) yield return null;
        var s = task.Result;
        if (s == null || !string.IsNullOrEmpty(s.error))
        {
            enterText.text = "Помилка з’єднання.";
            yield break;
        }

        if (!s.inside)
        {
            // На поверхні
            panelEnter.SetActive(true);
            panelEvent.SetActive(false);
            titleText.text = "Шахта";
            if (mineBackground && surfaceSprite) mineBackground.sprite = surfaceSprite;

            if (s.canEnterToday)
            {
                enterText.text = "В шахті великі залежі золота. Щодня можна спуститися й добути трохи багатства. Увійти зараз?";
                enterButton.interactable = true;
                enterBtnLabel.text = "Увійти";
            }
            else
            {
                enterText.text = "Сьогодні вхід у шахту вже використано. Повертайся завтра.";
                enterButton.interactable = false;
                enterBtnLabel.text = "—";
            }
        }
        else
        {
            // Усередині
            panelEnter.SetActive(false);
            panelEvent.SetActive(true);
            if (mineBackground && insideSprite) mineBackground.sprite = insideSprite;

            // Верхній таймер 30 хв
            if (!ParseUtc(s.sessionEndsUtc, out var endUtc)) endUtc = DateTime.UtcNow;
            if (topTimerCo != null) StopCoroutine(topTimerCo);
            topTimerCo = StartCoroutine(TopTimer(endUtc));

            // Пошук
            ApplySearchState(s);
        }
    }

    private void ApplySearchState(ApiClient.MineStateDto s)
    {
        bool canClaim = s.canClaim;
        if (!ParseUtc(s.searchEndsUtc, out var searchEnd)) searchEnd = DateTime.UtcNow.AddSeconds(1);

        if (canClaim || DateTime.UtcNow >= searchEnd)
        {
            eventText.text = "Знайдено жилу золота — час добути!";
            claimButton.interactable = true;
            claimBtnLabel.text = "Добути золото";
            if (searchTimerCo != null) { StopCoroutine(searchTimerCo); searchTimerCo = null; }
        }
        else
        {
            claimButton.interactable = false;
            claimBtnLabel.text = "—";
            if (searchTimerCo != null) StopCoroutine(searchTimerCo);
            searchTimerCo = StartCoroutine(SearchTimer(searchEnd));
        }
    }

    private IEnumerator TopTimer(DateTime endUtc)
    {
        while (true)
        {
            var left = endUtc - DateTime.UtcNow;
            if (left.TotalSeconds <= 0)
            {
                ShowStatus("Час у шахті завершено. Повертайся завтра за новими знахідками!");
                yield return StartCoroutine(RefreshState());
                yield break;
            }
            titleText.text = $"До кінця {left.Minutes:00}:{left.Seconds:00}";
            yield return null;
        }
    }

    private IEnumerator SearchTimer(DateTime endUtc)
    {
        while (true)
        {
            var left = endUtc - DateTime.UtcNow;
            if (left.TotalSeconds <= 0)
            {
                eventText.text = "Знайдено жилу золота — час добути!";
                claimButton.interactable = true;
                claimBtnLabel.text = "Добути золото";
                yield break;
            }
            eventText.text = $"Триває пошук золота\nДо завершення пошуку: {left.Minutes:00}:{left.Seconds:00}";
            yield return null;
        }
    }

    private void OnEnter() => StartCoroutine(EnterRoutine());

    private IEnumerator EnterRoutine()
    {
        string name = PlayerPrefs.GetString("Name", PlayerPrefs.GetString("PlayerName", ""));
        string code = PlayerPrefs.GetString("SerialCode", PlayerPrefs.GetString("PlayerSerialCode", ""));
        var task = ApiClient.MineEnterAsync(name, code);
        while (!task.IsCompleted) yield return null;

        var r = task.Result;
        if (r == null)
        {
            ShowStatus("Помилка входу в шахту.");
            yield break;
        }
        if (!string.IsNullOrEmpty(r.error))
        {
            if (r.error == "LEVEL_TOO_LOW") ShowStatus("Шахта доступна з 5 рівня.");
            else if (r.error == "ALREADY_USED_TODAY") ShowStatus("Сьогодні вхід уже використано.");
            else ShowStatus("Вхід недоступний.");
            yield break;
        }
        yield return RefreshState();
    }

    private void OnClaim() => StartCoroutine(ClaimRoutine());

    private IEnumerator ClaimRoutine()
    {
        string name = PlayerPrefs.GetString("Name", PlayerPrefs.GetString("PlayerName", ""));
        string code = PlayerPrefs.GetString("SerialCode", PlayerPrefs.GetString("PlayerSerialCode", ""));
        var task = ApiClient.MineClaimAsync(name, code);
        while (!task.IsCompleted) yield return null;

        var r = task.Result;
        if (r == null)
        {
            ShowStatus("Помилка добування.");
            yield break;
        }
        if (!string.IsNullOrEmpty(r.error))
        {
            if (r.error == "NOT_READY") ShowStatus("Пошук ще триває.");
            else if (r.error == "SESSION_ENDED") ShowStatus("Час у шахті завершено.");
            else ShowStatus("Помилка добування.");
            yield return RefreshState();
            yield break;
        }

        if (r.ok)
        {
            ShowStatus($"+{r.award} золота");
            var accTask = ApiClient.GetAccountAsync(name, code);
            while (!accTask.IsCompleted) yield return null;
            var info = accTask.Result;
            if (info != null) PlayerSession.I?.Apply(info);
        }

        yield return RefreshState();
    }

    private void OnExit()
    {
        // Якщо панель уже деактивується — не запускаємо корутину
        if (!isActiveAndEnabled)
            return;

        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        string name = PlayerPrefs.GetString("Name", PlayerPrefs.GetString("PlayerName", ""));
        string code = PlayerPrefs.GetString("SerialCode", PlayerPrefs.GetString("PlayerSerialCode", ""));
        var task = ApiClient.MineExitAsync(name, code);
        while (!task.IsCompleted) yield return null;
        ShowStatus("Ти покинув шахту.");
        yield return RefreshState();
    }

    private void ShowStatus(string msg)
    {
        if (!statusBar || !statusDesc) return;
        statusDesc.text = msg;
        statusBar.SetActive(true);
        StartCoroutine(HideStatusLater(4.5f));
    }

    private IEnumerator HideStatusLater(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusBar) statusBar.SetActive(false);
        yield return StartCoroutine(RefreshState());
    }

    private static bool ParseUtc(string iso, out DateTime utc)
    {
        if (!string.IsNullOrWhiteSpace(iso) &&
            DateTime.TryParse(iso, null,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var dt))
        {
            utc = dt;
            return true;
        }
        utc = default;
        return false;
    }
}
