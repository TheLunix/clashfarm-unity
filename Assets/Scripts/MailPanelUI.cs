using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

public class MailPanelUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text headerTitle;   // Текст в шапці ("Пошта", "Події"...)

    [Header("Root panels")]
    [SerializeField] private GameObject mailPanel; // Екран зі списком розділів
    [SerializeField] private GameObject sectionsRoot; // Екран зі списком розділів
    [SerializeField] private GameObject eventsTab;    // Панель з ScrollView подій
    [SerializeField] private GameObject footerRoot;   // Нижня панель з кнопкою "Повернутись"

    [Header("Section labels (на головному екрані)")]
    [SerializeField] private TMP_Text newsLabel;
    [SerializeField] private TMP_Text eventsLabel;
    [SerializeField] private TMP_Text messagesLabel;
    [SerializeField] private TMP_Text clanLabel;
    [SerializeField] private TMP_Text friendsLabel;
    [SerializeField] private TMP_Text requestsLabel;
    [SerializeField] private TMP_Text supportLabel;

    [Header("Events tab")]
    [SerializeField] private RectTransform eventsContent;    // Content всередині ScrollView
    [SerializeField] private MailEventItemUI eventItemPrefab; // Префаб MailEventItem

    private void OnEnable()
    {
        // Початковий стан UI
        if (headerTitle != null)
            headerTitle.text = "Пошта";

        if (sectionsRoot != null) sectionsRoot.SetActive(true);
        if (eventsTab    != null) eventsTab.SetActive(false);
        if (footerRoot   != null) footerRoot.SetActive(false);

        if (MailManager.Instance != null)
        {
            MailManager.Instance.OnMailUpdated += OnMailUpdated;
            _ = MailManager.Instance.RefreshAsync();
        }

        RefreshCounts();
    }

    private void OnDisable()
    {
        if (MailManager.Instance != null)
            MailManager.Instance.OnMailUpdated -= OnMailUpdated;
    }

    private void OnMailUpdated()
    {
        RefreshCounts();

        // Якщо зараз відкрита вкладка "Події" — оновлюємо список
        if (eventsTab != null && eventsTab.activeSelf)
            PopulateEvents();
    }
    /// <summary>
    /// Оновлює лічильники непрочитаних для всіх розділів.
    /// Викликається при оновленні пошти та при відкритті панелі.
    /// </summary>
    private void RefreshCounts()
    {
        if (MailManager.Instance == null) return;

        int newsUnread    = MailManager.Instance.GetUnreadCountByCategory(ApiClient.MailCategory.News);
        int eventsUnread  = MailManager.Instance.GetUnreadCountByCategory(ApiClient.MailCategory.Event);
        int directUnread  = MailManager.Instance.GetUnreadCountByCategory(ApiClient.MailCategory.Direct);
        int clanUnread    = MailManager.Instance.GetUnreadCountByCategory(ApiClient.MailCategory.Clan);
        int friendsUnread = MailManager.Instance.GetUnreadCountByCategory(ApiClient.MailCategory.Friends);
        int requestUnread = MailManager.Instance.GetUnreadCountByCategory(ApiClient.MailCategory.Request);
        int supportUnread = MailManager.Instance.GetUnreadCountByCategory(ApiClient.MailCategory.Support);

        if (newsLabel    != null) newsLabel.text    = $"Новини ({newsUnread})";
        if (eventsLabel  != null) eventsLabel.text  = $"Події ({eventsUnread})";
        if (messagesLabel!= null) messagesLabel.text= $"Особисті повідомлення ({directUnread})";
        if (clanLabel    != null) clanLabel.text    = $"Клан ({clanUnread})";
        if (friendsLabel != null) friendsLabel.text = $"Друзі ({friendsUnread})";
        if (requestsLabel!= null) requestsLabel.text= $"Заявки ({requestUnread})";
        if (supportLabel != null) supportLabel.text = $"Підтримка ({supportUnread})";
    }

    // ================== Перемикання екранів ==================

    /// <summary>
    /// Клік по пункту "Події" на головному екрані пошти.
    /// </summary>
    public void OnEventsSectionClick()
    {
        if (headerTitle != null)
            headerTitle.text = "Події";

        if (sectionsRoot != null) sectionsRoot.SetActive(false);
        if (eventsTab    != null) eventsTab.SetActive(true);
        if (footerRoot   != null) footerRoot.SetActive(true);

        PopulateEvents();
    }

    /// <summary>
    /// Кнопка "Повернутись" внизу.
    /// Повертає до списку розділів.
    /// </summary>
    public async void OnBackClick()
    {
        if (eventsTab != null && eventsTab.activeSelf && MailManager.Instance != null)
        {
            await MailManager.Instance.MarkAllEventsAsReadAsync();
        }

        if (headerTitle != null)
            headerTitle.text = "Пошта";

        if (sectionsRoot != null) sectionsRoot.SetActive(true);
        if (eventsTab    != null) eventsTab.SetActive(false);
        if (footerRoot   != null) footerRoot.SetActive(false);
    }

    /// <summary>
    /// Хрестик у шапці — закрити всю панель пошти.
    /// </summary>
    public async void OnCloseClick()
    {
        if (eventsTab != null && eventsTab.activeSelf && MailManager.Instance != null)
        {
            await MailManager.Instance.MarkAllEventsAsReadAsync();
        }

        mailPanel.SetActive(false);
    }

    // ================== Вкладка "Події" ==================

    private void PopulateEvents()
    {
        if (eventsContent == null || eventItemPrefab == null) return;
        if (MailManager.Instance == null) return;

        var list = MailManager.Instance
            .GetByCategory(ApiClient.MailCategory.Event)
            .OrderByDescending(m => m.createdatutc)
            .ToList();

        // Прибираємо старі елементи
        for (int i = eventsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(eventsContent.GetChild(i).gameObject);
        }

        // Створюємо нові
        foreach (var item in list)
        {
            var ui = Instantiate(eventItemPrefab, eventsContent);
            ui.Setup(item);
        }
    }
}
