using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class InventoryDetailsPanel : MonoBehaviour
{
    [Header("UI")]
    public Image rarityFrame;
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;

    public Button buttonEquip;
    public Button buttonUse;
    public Button buttonSell;

    public TextMeshProUGUI equipButtonText;

    [Header("Close / Background")]
    public Button buttonClose;        // кнопка "Х" на самій панелі (опціонально)

    [Header("Localization")]
    [SerializeField] private string inventoryUiTable = "InventoryUI";
    [SerializeField] private string equipKey        = "inventory.button.equip";
    [SerializeField] private string unequipKey      = "inventory.button.unequip";

    private InventoryItemViewModel _vm;
    private Action<InventoryItemViewModel> _onEquip;
    private Action<InventoryItemViewModel> _onUse;
    private Action<InventoryItemViewModel> _onSell;

    public bool IsEquipped;

    private Coroutine _iconCoroutine;

    public void Init(
        Action<InventoryItemViewModel> onEquip,
        Action<InventoryItemViewModel> onUse,
        Action<InventoryItemViewModel> onSell)
    {
        _onEquip = onEquip;
        _onUse   = onUse;
        _onSell  = onSell;

        // Кнопка "Закрити"
        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveAllListeners();
            buttonClose.onClick.AddListener(Hide);
        }

        gameObject.SetActive(false);
    }

    public void Show(InventoryItemViewModel vm, string localizedName, string localizedDesc)
    {
        _vm = vm;

        if (_iconCoroutine != null)
        {
            StopCoroutine(_iconCoroutine);
            _iconCoroutine = null;
        }

        var d = vm.Data;

        if (nameText != null)
            nameText.text = localizedName;

        // 🔹 ТУТ міняємо:
        if (descText != null)
            descText.text = localizedDesc;
        //    descText.text = BuildDescriptionWithStats(vm, localizedDesc);

        bool isEquip = d.Category == 0;
        bool isRing  = d.Category == 1;
        bool isUse   = d.CanUse;
        bool canSell = d.CanSell;

        buttonEquip.gameObject.SetActive(isEquip || isRing);
        buttonUse.gameObject.SetActive(isUse);
        buttonSell.gameObject.SetActive(canSell);

        buttonEquip.onClick.RemoveAllListeners();
        buttonUse.onClick.RemoveAllListeners();
        buttonSell.onClick.RemoveAllListeners();

        buttonEquip.onClick.AddListener(() => _onEquip?.Invoke(_vm));
        buttonUse.onClick.AddListener(() => _onUse?.Invoke(_vm));
        buttonSell.onClick.AddListener(() => _onSell?.Invoke(_vm));

        // Текст кнопки "Одягти / Зняти"
        if (equipButtonText != null)
        {
            bool isEquipped = _vm.Data.IsEquipped;

            var db = LocalizationSettings.StringDatabase;
            string key = isEquipped ? unequipKey : equipKey;

            equipButtonText.text = db.GetLocalizedString(inventoryUiTable, key);
        }

        // 🔹 Рамка по Rarity
        if (rarityFrame != null)
        {
            Sprite frameSprite = null;
            if (RarityFrameProvider.Instance != null)
                frameSprite = RarityFrameProvider.Instance.GetFrame(d.ItemLevel);

            rarityFrame.sprite  = frameSprite;
            rarityFrame.enabled = frameSprite != null;
        }

        // 🔹 Потім саму панель
        gameObject.SetActive(true);

        // 🔹 Іконка по IconKey
        if (icon != null)
        {
            icon.sprite  = null;
            icon.enabled = false;

            if (ItemIconProvider.Instance != null)
            {
                _iconCoroutine = StartCoroutine(LoadIconCoroutine(d.IconKey, d.Id));
            }
            else
            {
                Debug.LogWarning("[InventoryDetailsPanel] ItemIconProvider.Instance is null – немає провайдера іконок у сцені");
            }
        }
    }

    private string BuildDescriptionWithStats(InventoryItemViewModel vm, string localizedDesc)
    {
        var d = vm.Data;
        var sb = new System.Text.StringBuilder();

        // ---- 1. НАЗВА секції ----
        sb.AppendLine("<b>Опис</b>");
        sb.AppendLine();

        // ---- 2. Тип предмета ----
        // Category:
        // 0 = weapon/armor/etc
        // 1 = rings? (в тебе так)
        // інше = зілля/юзабельний

        // ⭐⭐ ЕКІПІРОВКА ⭐⭐
        if (d.Category == 0 || d.Category == 1)
        {
            sb.AppendLine("<b>Тип:</b> Екіпіровка");
            sb.AppendLine($"<b>Рівень:</b> {d.ItemLevel}");
            sb.AppendLine($"<b>Рідкість:</b> {d.ItemLevel}");
            sb.AppendLine();

            sb.AppendLine("<b>Характеристики:</b>");

            // Коли додамо статові поля на клієнт:
            // if (d.Power != 0) sb.AppendLine($"Сила: <b>{d.Power}</b>");
            // if (d.Protection != 0) sb.AppendLine($"Захист: <b>{d.Protection}</b>");
            // if (d.Dexterity != 0) sb.AppendLine($"Спритність: <b>{d.Dexterity}</b>");
            // ...

            sb.AppendLine("<i>(Статів ще немає — додамо, коли зʼявляться в DTO)</i>");
        }
        // ⭐⭐ ЮЗАБЕЛЬНІ ПРЕДМЕТИ / ЗІЛЛЯ ⭐⭐
        else if (d.CanUse)
        {
            sb.AppendLine("<b>Тип:</b> Зілля/Еліксир");
            sb.AppendLine();

            // localizedDesc тут якраз потрібний для зілля
            if (!string.IsNullOrWhiteSpace(localizedDesc))
                sb.AppendLine(localizedDesc);
            else
                sb.AppendLine("Цей предмет можна використати один раз.");

            // Майбутнє:
            // sb.AppendLine($"Відновлює: <b>{d.RestoreHp}</b> HP");
            // sb.AppendLine($"КД: <b>{d.CooldownMinutes} хв</b>");

        }
        // ⭐⭐ ІНШЕ (наприклад, предмети для продажу, бафи, інгредієнти) ⭐⭐
        else
        {
            sb.AppendLine("<b>Тип:</b> Предмет");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(localizedDesc))
                sb.AppendLine(localizedDesc);
            else
                sb.AppendLine("Стандартний опис предмета.");
        }

        return sb.ToString();
    }

    private IEnumerator LoadIconCoroutine(string iconKey, long itemId)
    {
        yield return ItemIconProvider.Instance.LoadIcon(iconKey, sprite =>
        {
            if (_vm != null && _vm.Data.Id == itemId && icon != null)
            {
                icon.sprite  = sprite;
                icon.enabled = sprite != null;
            }
        });

        _iconCoroutine = null;
    }

    public void Hide()
    {
        if (_iconCoroutine != null)
        {
            StopCoroutine(_iconCoroutine);
            _iconCoroutine = null;
        }

        gameObject.SetActive(false);
    }
}
