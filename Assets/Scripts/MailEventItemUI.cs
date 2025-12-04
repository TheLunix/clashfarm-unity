using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

public class MailEventItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private CanvasGroup canvasGroup; // щоб приглушати прочитане

    [Header("Localization")]
    [SerializeField] private string stringTableName = "Mail";

    [Header("Icons")]
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Sprite mineIcon;
    [SerializeField] private Sprite tripIcon;
    [SerializeField] private Sprite guardIcon;
    [SerializeField] private Sprite monkIcon;
    [SerializeField] private Sprite systemIcon;

    private ApiClient.MailItemDto _item;
    private LocalizedString _titleLoc;
    private LocalizedString _bodyLoc;

    [Serializable]
    private class MineClaimPayload
    {
        public int gold;
        public int minedToday;
    }

    [Serializable]
    private class MineSummaryPayload
    {
        public int gold;
    }

    [Serializable]
    private class MonkPayload
    {
        public int day;
        public int green;
        public int gold;
        public int diamonds;
    }
    [Serializable]
    private class TravelPayload
    {
        public int greenBase;
        public int greenExtra;
        public int greenTotal;
        public int gold;
        public int exp;
    }
    [Serializable]
    private class GuardPayload
    {
        public int hours;
        public int greenBase;
        public int greenExtra;
        public int greenTotal;
        public int exp;
    }

    [Serializable]
    private class GuardEventPayload
    {
        public int extraGreen;
    }
    public void Setup(ApiClient.MailItemDto item)
    {
        _item = item;

        // 1) Іконка
        if (iconImage != null)
            iconImage.sprite = GetIconForItem(item);

        // 2) Заголовок: просто підставляємо ключ у таблицю Mail
        SetupTitleLocalized(item);

        // 3) Тіло з урахуванням payload (gold, green, diamonds...)
        SetupBodyLocalized(item);

        // 4) Час
        if (timeText != null)
            timeText.text = FormatTimeAgo(item.createdatutc);

        // 5) Статус прочитано / непрочитано
        ApplyReadState(item.isread);
    }

    private void ApplyReadState(bool isRead)
    {
        if (canvasGroup == null) return;

        // Непрочитане яскравіше, прочитане трохи приглушене
        canvasGroup.alpha = isRead ? 0.6f : 1f;
    }

    // === Заголовок ===
    private void SetupTitleLocalized(ApiClient.MailItemDto item)
    {
        if (titleText == null)
            return;

        if (!string.IsNullOrEmpty(item.titlekey))
        {
            _titleLoc = new LocalizedString(stringTableName, item.titlekey);
            _ = SetLoc(_titleLoc, titleText);
        }
        else
        {
            titleText.text = "Подія";
        }
    }

    // === Тіло ===
    private void SetupBodyLocalized(ApiClient.MailItemDto item)
    {
        if (bodyText == null)
            return;

        string key = item.bodykey ?? string.Empty;

        if (string.IsNullOrEmpty(key))
        {
            bodyText.text = string.Empty;
            return;
        }

        _bodyLoc = new LocalizedString(stringTableName, key);

        // За замовчуванням – без аргументів
        object[] args = Array.Empty<object>();

        // Спец: mail.event.mine.claim.body → Добуто {0} <sprite=1> золота
        if (key == "mail.event.mine.claim.body")
        {
            var payload = SafeFromJson<MineClaimPayload>(item.payloadjson);
            int gold = payload?.gold ?? 0;
            args = new object[] { gold };
        }
        // Спец: mail.event.mine.summary.body → Сьогодні ви добули {0} <sprite=1> золота
        else if (key == "mail.event.mine.summary.body")
        {
            var payload = SafeFromJson<MineSummaryPayload>(item.payloadjson);
            int gold = payload?.gold ?? 0;
            args = new object[] { gold };
        }
        // Спец: mail.event.monk.claim.body → {0} = "18588 <sprite=0>, 50 <sprite=1>, 3 <sprite=2>"
        else if (key == "mail.event.monk.claim.body")
        {
            var payload = SafeFromJson<MonkPayload>(item.payloadjson);
            string rewards = BuildMonkRewardsString(payload);
            args = new object[] { rewards };
        }
        // Спец: подорожі
        // mail.event.travel.claim.body – базова нагорода за подорож без івенту
        // travel.event.* – нагороди з івентів (купець, поле бою, Скралли тощо)
        else if (key == "mail.event.travel.claim.body" ||
         key.StartsWith("mail.event.travel.", StringComparison.Ordinal))
        {
            var payload = SafeFromJson<TravelPayload>(item.payloadjson);
            string rewards = BuildTravelRewardsString(payload);

            // локалізація розрахована на:
            // {0} = "5420 <sprite=0>, 7 <sprite=1>, 2 <sprite=3>"
            args = new object[] { rewards };
        }
        // Спец: охорона околиць
        // mail.event.guard.claim.body – нагорода за завершення варти
        else if (key == "mail.event.guard.claim.body")
        {
            var payload = SafeFromJson<GuardPayload>(item.payloadjson);

            // рядок нагороди в стилі "450 <sprite=0>, 2 <sprite=3>"
            string rewards = BuildGuardRewardsString(payload);

            int hours = payload?.hours ?? 0;

            // локалізація розрахована на:
            // {0} = години, {1} = "450 <sprite=0>, 2 <sprite=3>"
            args = new object[] { hours, rewards };
        }
        // Спец: основний лист за варту
        // mail.event.guard.claim.body
        else if (key == "mail.event.guard.claim.body")
        {
            var payload = SafeFromJson<GuardPayload>(item.payloadjson);

            string rewards = BuildGuardRewardsString(payload);
            int hours = payload?.hours ?? 0;

            // {0} = години, {1} = "570 <sprite=0>, 2 <sprite=3>"
            args = new object[] { hours, rewards };
        }
        // Спец: окремі події під час варти
        // mail.event.guard.skrall_scout.body, ... і т.д.
        else if (key.StartsWith("mail.event.guard.", StringComparison.Ordinal) &&
                 key.EndsWith(".body", StringComparison.Ordinal) &&
                 key != "mail.event.guard.claim.body")
        {
            var payload = SafeFromJson<GuardEventPayload>(item.payloadjson);
            int extraGreen = payload?.extraGreen ?? 0;

            // {0} = додаткова зелень, напр. "120"
            args = new object[] { extraGreen };
        }
        _bodyLoc.Arguments = args;
        _ = SetLoc(_bodyLoc, bodyText);
    }

    private T SafeFromJson<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch
        {
            return null;
        }
    }

    private string BuildMonkRewardsString(MonkPayload payload)
    {
        if (payload == null) return string.Empty;

        var parts = new List<string>();

        if (payload.green > 0)
            parts.Add($"{payload.green} <sprite=0>");
        if (payload.gold > 0)
            parts.Add($"{payload.gold} <sprite=1>");
        if (payload.diamonds > 0)
            parts.Add($"{payload.diamonds} <sprite=2>");

        return string.Join(", ", parts);
    }
    private string BuildTravelRewardsString(TravelPayload payload)
    {
        if (payload == null)
            return string.Empty;

        var parts = new List<string>();

        // зелень (sprite=0)
        int greenTotal = payload.greenTotal > 0
            ? payload.greenTotal
            : payload.greenBase + payload.greenExtra;

        if (greenTotal > 0)
            parts.Add($"{greenTotal} <sprite=0>");

        // золото (sprite=1)
        if (payload.gold > 0)
            parts.Add($"{payload.gold} <sprite=1>");

        // досвід (sprite=3)
        if (payload.exp > 0)
            parts.Add($"{payload.exp} <sprite=3>");

        return string.Join(", ", parts);
    }
    private string BuildGuardRewardsString(GuardPayload payload)
    {
        if (payload == null)
            return string.Empty;

        var parts = new List<string>();

        int greenTotal = payload.greenTotal > 0
            ? payload.greenTotal
            : payload.greenBase + payload.greenExtra;

        if (greenTotal > 0)
            parts.Add($"{greenTotal} <sprite=0>"); // зелень

        if (payload.exp > 0)
            parts.Add($"{payload.exp} <sprite=3>"); // досвід

        return string.Join(", ", parts);
    }
    private Sprite GetIconForItem(ApiClient.MailItemDto item)
    {
        switch (item.hudmarker)
        {
            case ApiClient.MailHudMarker.MineFinished:
                return mineIcon != null ? mineIcon : defaultIcon;
            case ApiClient.MailHudMarker.TripFinished:
                return tripIcon != null ? tripIcon : defaultIcon;
            case ApiClient.MailHudMarker.GuardFinished:
                return guardIcon != null ? guardIcon : defaultIcon;
            case ApiClient.MailHudMarker.SystemNews:
                return systemIcon != null ? systemIcon : defaultIcon;
            case ApiClient.MailHudMarker.MonkReward:
                return monkIcon != null ? monkIcon : defaultIcon;
            default:
                return defaultIcon;
        }
    }

    private string FormatTimeAgo(string utcIsoString)
    {
        if (string.IsNullOrEmpty(utcIsoString))
            return "";

        if (!DateTime.TryParse(utcIsoString, null, DateTimeStyles.RoundtripKind, out var createdUtc))
            return "";

        return FormatTimeAgo(createdUtc);
    }

    private string FormatTimeAgo(DateTime createdUtc)
    {
        var now = DateTime.UtcNow;
        var diff = now - createdUtc;

        if (diff.TotalSeconds < 60)
            return "щойно";
        if (diff.TotalMinutes < 60)
            return $"{Mathf.FloorToInt((float)diff.TotalMinutes)} хв тому";
        if (diff.TotalHours < 24)
            return $"{Mathf.FloorToInt((float)diff.TotalHours)} год тому";

        int days = Mathf.FloorToInt((float)diff.TotalDays);
        if (days == 1) return "вчора";
        return $"{days} дн. тому";
    }

    private async Task SetLoc(LocalizedString key, TMP_Text label)
    {
        var op = key.GetLocalizedStringAsync();
        await op.Task;
        // якщо за цей час текст вже змінили/панель закрили — можна перевірити, але поки не ускладнюємо
        if (label != null)
            label.text = op.Result;
    }

    // Клік по картці – позначаємо як прочитане (конкретно цю подію)
    public async void OnClick()
    {
        if (_item == null || MailManager.Instance == null)
            return;

        await MailManager.Instance.MarkReadAsync(new[] { _item.id });
        _item.isread = true;
        ApplyReadState(true);
    }
}
