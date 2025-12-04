using UnityEngine;
using UnityEngine.UI;

public class MailIconHUD : MonoBehaviour
{
    [Header("Icon")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite spriteNoMail;   // сіра
    [SerializeField] private Sprite spriteHasMail;  // жовта

    [Header("Mail panel")]
    [SerializeField] private GameObject mailPanel;  // твій MailPanel, який ти активуєш/деактивуєш

    private void Start()
    {
        if (MailManager.Instance != null)
        {
            MailManager.Instance.OnMailUpdated += UpdateIcon;
            _ = MailManager.Instance.RefreshAsync();
        }
        UpdateIcon();
    }

    private void OnDestroy()
    {
        if (MailManager.Instance != null)
            MailManager.Instance.OnMailUpdated -= UpdateIcon;
    }

    private void UpdateIcon()
    {
        if (iconImage == null) return;
        bool hasUnread = MailManager.Instance != null && MailManager.Instance.HasUnread();
        iconImage.sprite = hasUnread ? spriteHasMail : spriteNoMail;
    }

    /// <summary> Викликається з кнопки на конверті. </summary>
    public void OnClick()
    {
        if (mailPanel != null)
            mailPanel.SetActive(true);

        if (MailManager.Instance != null)
            _ = MailManager.Instance.RefreshAsync();
    }
}
