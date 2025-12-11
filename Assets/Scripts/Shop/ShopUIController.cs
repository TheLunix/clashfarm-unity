using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Головний контролер магазину.
/// </summary>
public class ShopUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;          // PaneShop
    [SerializeField] private TMP_Text headerText;           // BuyPanel/Image/Text (назва підкатегорії)

    [Header("Panels")]
    [SerializeField] private GameObject buyPanel;           // BuyPanel – панель зі списком товарів
    [SerializeField] private GameObject infoBar;            // InfoBar – верхня панель з інфою/іконками
    [SerializeField] private Button buyPanelCloseButton;    // кнопка "Закрити" всередині BuyPanel

    [Header("Scroll")]
    [SerializeField] private Transform itemsContentRoot;    // BuyPanel/Scroll View/Viewport/Content
    [SerializeField] private ShopItemView itemPrefab;       // префаб Item_shop

    [Header("Групи кнопок (верхні 4)")]
    [SerializeField] private GameObject containerButtonsRoot;      // ContainerButtons
    [SerializeField] private Button btnEquipments;                 // b_equipments
    [SerializeField] private Button btnSpellScroll;                // b_spell_scroll
    [SerializeField] private Button btnGiftCurse;                  // b_gift_curse
    [SerializeField] private Button btnRingCollar;                 // b_ring_collar

    [Header("Панелі підкнопок (субкатегорії)")]
    [SerializeField] private GameObject containerButtonsEquipment; // ContainerButtonsEquipment
    [SerializeField] private GameObject containerButtonsSpell;     // ContainerButtonsSpell
    [SerializeField] private GameObject containerButtonsGift;      // ContainerButtonsGift
    [SerializeField] private GameObject containerButtonsCollar;    // ContainerButtonsCollar

    [Serializable]
    public class SubcategoryConfig
    {
        public string debugName;       // Наприклад: "Weapon"
        public Button button;          // Кнопка: b_weapons, b_armor, ...
        public byte category;          // 0..7 – Category з БД
        public byte equipSlot = 255;   // EquipSlot для екіпу, 255 якщо не використовується
        public string headerUkr;       // "Зброя", "Шоломи", ...
        public GameObject parentPanel; // До якої панелі підкнопок належить
    }

    [Header("Підкатегорії (конкретні типи предметів)")]
    [SerializeField] private SubcategoryConfig[] subcategories;

    [Header("Networking")]
    [SerializeField] private string shopListUrl = "https://api.clashfarm.com/api/player/shop/list";
    [SerializeField] private string shopBuyUrl  = "https://api.clashfarm.com/api/player/shop/buy";

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingSpinner;
    [SerializeField] private TMP_Text errorText;

    private SubcategoryConfig _currentSubcategory;
    private readonly List<ShopItemView> _spawnedItems = new();
    private bool _isLoading;

    #region Unity

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        // Верхні 4 кнопки – тільки перемикають групи підкатегорій
        if (btnEquipments != null)
            btnEquipments.onClick.AddListener(() => ShowGroup(containerButtonsEquipment));
        if (btnSpellScroll != null)
            btnSpellScroll.onClick.AddListener(() => ShowGroup(containerButtonsSpell));
        if (btnGiftCurse != null)
            btnGiftCurse.onClick.AddListener(() => ShowGroup(containerButtonsGift));
        if (btnRingCollar != null)
            btnRingCollar.onClick.AddListener(() => ShowGroup(containerButtonsCollar));

        // Підкатегорії – вішаємо колбек на кожну кнопку
        if (subcategories != null)
        {
            foreach (var sub in subcategories)
            {
                if (sub.button == null) continue;
                var localSub = sub;
                sub.button.onClick.AddListener(() => OnSubcategoryClicked(localSub));
            }
        }

        // Кнопка закриття BuyPanel
        if (buyPanelCloseButton != null)
            buyPanelCloseButton.onClick.AddListener(CloseBuyPanelOnly);
    }

    private void OnEnable()
    {
        ResetUI();
    }

    #endregion

    #region Public API

    /// <summary>Відкрити магазин.</summary>
    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        ResetUI();
    }

    /// <summary>Закрити магазин повністю.</summary>
    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    #endregion

    #region UI State

    /// <summary>Початковий стан при відкритті магазину.</summary>
    private void ResetUI()
    {
        _isLoading = false;
        ClearItems();
        SetError(null);

        // Ховаємо BuyPanel, показуємо InfoBar
        if (buyPanel != null)
            buyPanel.SetActive(false);

        if (infoBar != null)
            infoBar.SetActive(true);

        // Ховаємо всі панелі підкатегорій – поки не натиснули верхню категорію
        ShowGroup(null);

        // Заголовок очищаємо
        if (headerText != null)
            headerText.text = string.Empty;
    }

    /// <summary>Показати потрібну панель підкатегорій (або сховати всі, якщо groupPanel == null).</summary>
    private void ShowGroup(GameObject groupPanel)
    {
        if (containerButtonsEquipment != null)
            containerButtonsEquipment.SetActive(groupPanel == containerButtonsEquipment);
        if (containerButtonsSpell != null)
            containerButtonsSpell.SetActive(groupPanel == containerButtonsSpell);
        if (containerButtonsGift != null)
            containerButtonsGift.SetActive(groupPanel == containerButtonsGift);
        if (containerButtonsCollar != null)
            containerButtonsCollar.SetActive(groupPanel == containerButtonsCollar);
    }

    /// <summary>Закрити тільки BuyPanel, не закриваючи весь магазин.</summary>
    private void CloseBuyPanelOnly()
    {
        ClearItems();
        if (buyPanel != null)
            buyPanel.SetActive(false);
        if (infoBar != null)
            infoBar.SetActive(true);

        // Можна очистити заголовок
        if (headerText != null)
            headerText.text = string.Empty;
    }

    #endregion

    #region Subcategories

    /// <summary>Клік по кнопці підкатегорії (Зброя, Броня, Зілля...).</summary>
    private void OnSubcategoryClicked(SubcategoryConfig sub)
    {
        if (_isLoading)
            return;

        _currentSubcategory = sub;

        Debug.Log($"[ShopUI] Subcategory clicked: {sub.debugName}, category={sub.category}, equipSlot={sub.equipSlot}");

        // Показуємо BuyPanel, ховаємо InfoBar
        if (buyPanel != null)
            buyPanel.SetActive(true);
        if (infoBar != null)
            infoBar.SetActive(false);

        // Оновлюємо заголовок
        if (headerText != null)
            headerText.text = sub.headerUkr;

        StartCoroutine(LoadItemsForCurrentSubcategory());
    }

    #endregion

    #region Loading items

    private IEnumerator LoadItemsForCurrentSubcategory()
    {
        _isLoading = true;
        SetLoading(true);
        ClearItems();
        SetError(null);

        WWWForm form = new WWWForm();
        form.AddField("PlayerName",       PlayerSession.I.Data.nickname);
        form.AddField("PlayerSerialCode", PlayerSession.I.Data.serialcode);

        form.AddField("Category",  _currentSubcategory.category);
        form.AddField("EquipSlot", _currentSubcategory.equipSlot);

        using UnityWebRequest req = UnityWebRequest.Post(shopListUrl, form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            SetError("Помилка з'єднання з сервером магазину.");
            Debug.LogError($"[ShopUI] Error: {req.error}");
            SetLoading(false);
            _isLoading = false;
            yield break;
        }

        var json = req.downloadHandler.text;
        Debug.Log($"[ShopUI] Response: {json}");

        ShopResponseClient resp = null;
        try
        {
            resp = JsonUtility.FromJson<ShopResponseClient>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ShopUI] JSON parse error: {ex}");
        }

        if (resp == null)
        {
            SetError("Сервер повернув некоректні дані магазину.");
            SetLoading(false);
            _isLoading = false;
            yield break;
        }

        // "OK" або "" — це успіх
        if (!string.IsNullOrEmpty(resp.error) && resp.error != "OK")
        {
            SetError(resp.error);
            SetLoading(false);
            _isLoading = false;
            yield break;
        }

        if (resp.items == null || resp.items.Length == 0)
        {
            SetError("Немає товарів для цієї категорії.");
            SetLoading(false);
            _isLoading = false;
            yield break;
        }

        // Створюємо айтеми
        foreach (var dto in resp.items)
        {
            var view = Instantiate(itemPrefab, itemsContentRoot);
            view.gameObject.SetActive(true);
            view.Bind(dto, OnItemBuyClicked);
            _spawnedItems.Add(view);
        }

        SetLoading(false);
        _isLoading = false;
    }

    private void ClearItems()
    {
        foreach (var view in _spawnedItems)
        {
            if (view != null)
                Destroy(view.gameObject);
        }
        _spawnedItems.Clear();
    }

    private void SetLoading(bool isLoading)
    {
        if (loadingSpinner != null)
            loadingSpinner.SetActive(isLoading);
    }

    private void SetError(string msg)
    {
        if (errorText == null) return;

        if (string.IsNullOrEmpty(msg))
        {
            errorText.gameObject.SetActive(false);
            errorText.text = "";
        }
        else
        {
            errorText.gameObject.SetActive(true);
            errorText.text = msg;
        }
    }

    #endregion

    #region Buy / Sell

    private void OnItemBuyClicked(ShopItemClientDto item)
    {
        // Якщо предмет заблокований по рівню – нічого не робимо
        if (item.IsLocked)
        {
            Debug.Log("[ShopUI] Item is locked, cannot buy: " + item.ItemId);
            return;
        }

        StartCoroutine(BuyItemCoroutine(item));
    }

    private IEnumerator BuyItemCoroutine(ShopItemClientDto item)
    {
        SetLoading(true);

        WWWForm form = new WWWForm();
        form.AddField("PlayerName",       PlayerSession.I.Data.nickname);
        form.AddField("PlayerSerialCode", PlayerSession.I.Data.serialcode);
        form.AddField("ItemId",           item.ItemId);

        using UnityWebRequest req = UnityWebRequest.Post(shopBuyUrl, form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            SetError("Не вдалося виконати покупку.");
            Debug.LogError($"[ShopUI] Buy error: {req.error}");
            SetLoading(false);
            yield break;
        }

        var json = req.downloadHandler.text;
        Debug.Log($"[ShopUI] Buy response: {json}");

        // TODO: тут розпарсити відповідь, оновити PlayerSession, інвентар і т.д.

        // Поки – просто перезавантажуємо поточну підкатегорію
        StartCoroutine(LoadItemsForCurrentSubcategory());
    }

    #endregion
}
