using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatRowView : MonoBehaviour
{
    [Header("UI")]
    public Image icon;

    [Tooltip("Якщо заповнено — весь текст малюємо сюди одним блоком.")]
    public TMP_Text combinedText;

    [Header("Роздільні поля (опційно)")]
    public TMP_Text title;        // (опц.) якщо combinedText = null
    public TMP_Text description;  // (опц.)
    public TMP_Text levelText;    // (опц.)
    public TMP_Text priceText;    // (опц.)

    public Button upgradeBtn;

    [Header("Busy/Feedback (optional)")]
    public GameObject spinner;      // будь-який GO з анімацією/іконкою "processing"
    public CanvasGroup rowCanvas;   // легкий fade під час апдейту (необов'язково)

    private int currentLevel;
    private int currentPrice;
    private System.Action onUpgradeClick;

    /// <summary>
    /// titleLoc / descLoc — локалізовані рядки
    /// level / price — поточні значення
    /// canAfford — чи вистачає зелені
    /// onUpgradeClick — колбек по кнопці "+"
    /// </summary>
    public void Bind(Sprite iconSprite, string titleLoc, string descLoc,
                     int level, int price, bool canAfford,
                     System.Action onUpgradeClick)
    {
        if (icon) icon.sprite = iconSprite;

        currentLevel = level;
        currentPrice = price;
        this.onUpgradeClick = onUpgradeClick;

        // Якщо є combinedText — рендеримо все в одному полі
        if (combinedText)
        {
            combinedText.text =
                $"<b>{titleLoc}</b>\n" +
                $"{descLoc}\n" +
                $"Рівень: {currentLevel}\n" +
                $"Ціна: <sprite=0> {currentPrice}";
        }
        else
        {
            if (title)       title.text       = titleLoc;
            if (description) description.text = descLoc;
            if (levelText)   levelText.text   = $"Рівень: {currentLevel}";
            if (priceText)   priceText.text   = $"Ціна: <sprite=0> {currentPrice}";
        }

        if (upgradeBtn)
        {
            upgradeBtn.onClick.RemoveAllListeners();
            if (onUpgradeClick != null) upgradeBtn.onClick.AddListener(() => this.onUpgradeClick?.Invoke());
            upgradeBtn.interactable = canAfford && currentPrice > 0;
        }

        ShowBusy(false);
    }

    /// <summary>
    /// Оновлює лише рівень/ціну/стан доступності.
    /// </summary>
    public void UpdateLevelAndPrice(int newLevel, int newPrice, bool canAfford)
    {
        currentLevel = newLevel;
        currentPrice = newPrice;

        if (combinedText)
        {
            // перезбираємо текст із уже наявних рядків у combinedText
            // Витягнути title/desc звідти не можемо, тож очікуємо, що Bind викликався до цього
            var lines = combinedText.text.Split('\n');
            string titleLine = lines.Length > 0 ? lines[0] : "";
            string descLine  = lines.Length > 1 ? lines[1] : "";
            combinedText.text =
                $"{titleLine}\n" +
                $"{descLine}\n" +
                $"Рівень: {currentLevel}\n" +
                $"Ціна: <sprite=0> {currentPrice}";
        }
        else
        {
            if (levelText) levelText.text = $"Рівень: {currentLevel}";
            if (priceText) priceText.text = $"Ціна: <sprite=0> {currentPrice}";
        }

        if (upgradeBtn) upgradeBtn.interactable = canAfford && currentPrice > 0;
    }

    public void ShowBusy(bool busy)
    {
        if (spinner) spinner.SetActive(busy);
        if (rowCanvas) rowCanvas.alpha = busy ? 0.85f : 1f;

        if (upgradeBtn)
        {
            // не перезатираємо логіку доступності по грошах — лише блокуємо на час busy
            upgradeBtn.interactable = !busy && upgradeBtn.interactable;
        }
    }
}
