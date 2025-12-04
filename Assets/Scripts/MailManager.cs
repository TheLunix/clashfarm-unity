using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MailManager : MonoBehaviour
{
    public static MailManager Instance { get; private set; }

    public event Action OnMailUpdated;

    private readonly List<ApiClient.MailItemDto> _mail = new();

    public IReadOnlyList<ApiClient.MailItemDto> AllMail => _mail;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    /// <summary> Завантажити пошту з сервера. Викликає подію OnMailUpdated. </summary>
    public async Task RefreshAsync()
    {
        string nick = PlayerPrefs.GetString("Name", "");
        string serial = PlayerPrefs.GetString("SerialCode", "");
        if (string.IsNullOrEmpty(nick) || string.IsNullOrEmpty(serial))
        {
            Debug.LogWarning("MailManager: no nickname/serial in PlayerPrefs.");
            return;
        }

        var list = await ApiClient.GetMailAsync(nick, serial);
        if (list == null) return;

        _mail.Clear();
        _mail.AddRange(list);
        OnMailUpdated?.Invoke();
    }

    public bool HasUnread()
        => _mail.Any(m => !m.isread);

    public int GetUnreadCountByCategory(ApiClient.MailCategory cat)
        => _mail.Count(m => m.category == cat && !m.isread);

    public List<ApiClient.MailItemDto> GetByCategory(ApiClient.MailCategory cat)
        => _mail.Where(m => m.category == cat).OrderByDescending(m => m.createdatutc).ToList();

    public async Task MarkReadAsync(IEnumerable<long> ids)
    {
        string nick = PlayerPrefs.GetString("Name", "");
        string serial = PlayerPrefs.GetString("SerialCode", "");
        await ApiClient.MailMarkReadAsync(nick, serial, ids);

        foreach (var id in ids)
        {
            var msg = _mail.FirstOrDefault(m => m.id == id);
            if (msg != null) msg.isread = true;
        }
        OnMailUpdated?.Invoke();
    }
    public async Task MarkAllEventsAsReadAsync()
    {
        if (_mail.Count == 0) return;

        // Беремо всі непрочитані саме "Події"
        var unreadIds = _mail
            .Where(m => m.category == ApiClient.MailCategory.Event && !m.isread)
            .Select(m => m.id)
            .ToList();

        if (unreadIds.Count == 0) return;

        string nick   = PlayerPrefs.GetString("Name", "");
        string serial = PlayerPrefs.GetString("SerialCode", "");

        bool ok = await ApiClient.MailMarkReadAsync(nick, serial, unreadIds);
        if (!ok) return;

        // Локально помічаємо як прочитані
        foreach (var msg in _mail)
        {
            if (msg.category == ApiClient.MailCategory.Event && unreadIds.Contains(msg.id))
                msg.isread = true;
        }

        OnMailUpdated?.Invoke();
    }
}
