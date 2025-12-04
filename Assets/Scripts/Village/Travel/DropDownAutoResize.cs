using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DropDownAutoResize : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    [Header("UI References")]
    public RectTransform template;     // Dropdown/Template
    public RectTransform viewport;     // Template/Viewport
    public RectTransform content;      // Content with LayoutGroup

    [Header("Settings")]
    public float itemHeight = 160f;    // Висота одного елемента
    public float maxHeight = 2000f;    // Максимальна висота Template (запас)

    private void Start()
    {
        UpdateDropdownHeight();
    }

    public void UpdateDropdownHeight()
    {
        int itemCount = dropdown.options.Count;

        // Потрібна висота
        float targetHeight = itemCount * itemHeight;

        // Обмеження (на всякий випадок)
        targetHeight = Mathf.Min(targetHeight, maxHeight);

        // Змінюємо висоту Template
        template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        // Viewport має бути такої ж висоти
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        // Якщо Content менший за Viewport → скрол не потрібен
        bool needScroll = content.rect.height > viewport.rect.height;
    }
}
