using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class MonkUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text conversationText;   // PanelConversationMonk/ConversationMonk
    [SerializeField] private Button rewardButton;       // PanelConversationMonk/RewardButton
    [SerializeField] private TMP_Text rewardButtonLabel;  // PanelConversationMonk/RewardButton/Text (TMP)
    [SerializeField] private StarRef[] stars = new StarRef[4]; // Star1..Star4 (див. StarRef нижче)

    [Header("Star colors")]
    [SerializeField] private Color bgEmpty = new Color(1, 1, 1, 0.35f);
    [SerializeField] private Color bgFull = Color.white;

    [Header("API")]
    [SerializeField] private string apiBase = "https://<YOUR-HOST>/api/player/monk";

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    // Localization keys (String Table: Monk)
    private readonly LocalizedString L_Claim = new LocalizedString("Monk", "monk_button_claim");
    private readonly LocalizedString L_Claimed = new LocalizedString("Monk", "monk_button_claimed");
    private readonly LocalizedString[] Quotes =
    {
        new LocalizedString("Monk", "monk_quote_01"),
        new LocalizedString("Monk", "monk_quote_02"),
        new LocalizedString("Monk", "monk_quote_03"),
        new LocalizedString("Monk", "monk_quote_04"),
        new LocalizedString("Monk", "monk_quote_05")
    };

    private Coroutine timerCo;

    [Serializable] private struct MonkState { public bool canClaim; public int day; public string nextClaimAtUtc; }
    [Serializable]
    private struct ClaimReply
    {
        public bool canClaim; public int day; public Reward reward; public string nextClaimAtUtc; public string messageKey;
        [Serializable] public struct Reward { public int green, gold, diamonds; }
    }
    [Serializable]
    private struct StarRef
    {
        public Image bg;    // кореневий Image (фон)
        public Image fill;  // дочірній Image (Type=Filled, Vertical, Origin=Bottom)
    }

    private void Reset()
    {
        // авто-підхоплення зірок і їх fill, якщо не задані
        if (stars == null || stars.Length == 0)
        {
            var starsRoot = transform.Find("PanelStars");
            if (!starsRoot) return;
            stars = new StarRef[4];
            for (int i = 0; i < 4; i++)
            {
                var star = starsRoot.Find($"Star{i + 1}");
                if (!star) continue;
                var bg = star.GetComponentInChildren<Image>();
                Image fill = null;
                foreach (Transform t in star)
                    if (t.TryGetComponent<Image>(out var img)) { fill = img; break; }

                // гарантовано налаштовуємо fill
                if (fill)
                {
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Vertical;
                    fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                    fill.fillAmount = 0f;
                    fill.raycastTarget = false;
                }
                if (bg) bg.raycastTarget = false;

                stars[i] = new StarRef { bg = star.GetComponent<Image>(), fill = fill };
            }
        }
    }

    private void OnEnable()
    {
        rewardButton.onClick.AddListener(OnClaim);
        StartCoroutine(InitAndRefresh());
    }
    private void OnDisable()
    {
        rewardButton.onClick.RemoveListener(OnClaim);
        if (timerCo != null) StopCoroutine(timerCo);
    }

    private IEnumerator InitAndRefresh()
    {
        // 1) Localization must be ready
        var init = LocalizationSettings.InitializationOperation;
        if (!init.IsDone) yield return init;

        // 2) PlayerPrefs presence
        var name = PlayerPrefs.GetString("Name", "");
        var code = PlayerPrefs.GetString("SerialCode", "");
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
        {
            LogE("PlayerName/PlayerSerialCode відсутні в PlayerPrefs. Панель Монаха не може звернутися до API.");
            SetButtonState(false);
            rewardButtonLabel.text = "—";
            conversationText.text = "No player credentials.";
            yield break;
        }

        // 3) State
        yield return RefreshState(name, code);
    }

    private IEnumerator RefreshState(string playerName, string serialCode)
    {
        if (timerCo != null) { StopCoroutine(timerCo); timerCo = null; }

        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);

        using (var req = UnityWebRequest.Post(apiBase + "/state", form))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                LogE($"STATE HTTP error: {req.responseCode} {req.error}\n{req.downloadHandler?.text}");
                SetButtonState(false);
                conversationText.text = "Network error.";
                rewardButtonLabel.text = "—";
                yield break;
            }

            var body = req.downloadHandler.text;
            if (verboseLogs) Debug.Log("STATE: " + body);
            var s = JsonUtility.FromJson<MonkState>(body);
            ApplyState(s);
        }
    }

    private void ApplyState(MonkState s)
    {
        if (s.canClaim)
        {
            SetButtonState(true);
            _ = SetLoc(L_Claim, rewardButtonLabel);
            SetRandomQuote(conversationText);
        }
        else
        {
            SetButtonState(false);
            timerCo = StartCoroutine(TimerToNextMidnight(s.nextClaimAtUtc));
            SetRandomQuote(conversationText);
        }
        ApplyStars(s.day);
    }

    private IEnumerator TimerToNextMidnight(string nextClaimAtUtcIso)
    {
        if (!DateTime.TryParse(nextClaimAtUtcIso, null,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var nextUtc))
        {
            LogE("Bad nextClaimAtUtc: " + nextClaimAtUtcIso);
            yield break;
        }

        while (true)
        {
            var ts = nextUtc - DateTime.UtcNow;
            if (ts.TotalSeconds <= 0) { StartCoroutine(InitAndRefresh()); yield break; }

            L_Claimed.Arguments = new object[] { ts };
            _ = SetLoc(L_Claimed, rewardButtonLabel);
            yield return new WaitForSeconds(1f);
        }
    }

    private void OnClaim()
    {
        var name = PlayerPrefs.GetString("Name", "");
        var code = PlayerPrefs.GetString("SerialCode", "");
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code)) { LogE("No player credentials"); return; }
        StartCoroutine(ClaimRoutine(name, code));
    }

    private IEnumerator ClaimRoutine(string playerName, string serialCode)
    {
        var form = new WWWForm();
        form.AddField("PlayerName", playerName);
        form.AddField("PlayerSerialCode", serialCode);

        using (var req = UnityWebRequest.Post(apiBase + "/claim", form))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                LogE($"CLAIM HTTP error: {req.responseCode} {req.error}\n{req.downloadHandler?.text}");
                yield break;
            }

            var body = req.downloadHandler.text;
            if (verboseLogs) Debug.Log("CLAIM: " + body);
            // Можна розпарсити і показати попап із винагородою
            var reply = JsonUtility.FromJson<ClaimReply>(body);

            // Оновлюємо стан й зірки
            StartCoroutine(InitAndRefresh());
        }
    }

    // === Stars ===
    private void ApplyStars(int day) // 0..28
    {
        day = Mathf.Clamp(day, 0, 28);
        int completed = day / 7;  // повні чверті
        int rem = day % 7;        // прогрес у поточній

        for (int i = 0; i < stars.Length; i++)
        {
            var s = stars[i];
            if (!s.bg || !s.fill) continue;

            if (i < completed)
            {
                s.bg.color = bgFull;
                s.fill.fillAmount = 1f;
                s.fill.rectTransform.localScale = Vector3.one;
            }
            else if (i == completed && day != 28)
            {
                s.bg.color = bgFull;
                s.fill.fillAmount = rem / 7f;
            }
            else
            {
                s.bg.color = bgEmpty;
                s.fill.fillAmount = 0f;
                s.fill.rectTransform.localScale = Vector3.one;
            }
        }
    }

    // === Localization helpers ===
    private async Task SetLoc(LocalizedString key, TMP_Text label)
    {
        var op = key.GetLocalizedStringAsync();
        await op.Task;
        label.text = op.Result;
    }
    private void SetRandomQuote(TMP_Text label)
    {
        var idx = UnityEngine.Random.Range(0, Quotes.Length);
        _ = SetLoc(Quotes[idx], label);
    }

    private void LogE(string msg)
    {
        if (verboseLogs) Debug.LogError("[MonkUI] " + msg);
    }

    private void SetButtonState(bool canClick)
    {
        // Кнопка лишається активною
        rewardButton.gameObject.SetActive(true);

        // Вмикаємо/вимикаємо інтеракцію
        rewardButton.interactable = canClick;
        rewardButton.enabled = canClick;

        // Ховаємо/показуємо бекграунд кнопки (Image або інший Graphic)
        var bg = rewardButton.targetGraphic as Graphic;
        if (bg)
        {
            bg.enabled = canClick;   // ➜ коли не можна клікати — фон прихований
            bg.raycastTarget = canClick;   // щоб не блокував інші елементи
        }

        // Текст таймера завжди видно, але не блокує кліки під собою
        if (rewardButtonLabel) rewardButtonLabel.raycastTarget = false;
    }
}
