using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;          // PaneShop
    [SerializeField] private TMP_Text headerText;           // BuyPanel/Image/Text

    [Header("Panels")]
    [SerializeField] private GameObject buyPanel;           // BuyPanel
    [SerializeField] private GameObject infoBar;            // InfoBar
    [SerializeField] private Button buyPanelCloseButton;

    [Header("Scroll")]
    [SerializeField] private Transform itemsContentRoot;
    [SerializeField] private ShopItemView itemPrefab;

    [Header("Top category buttons")]
    [SerializeField] private Button btnEquipments;
    [SerializeField] private Button btnSpellScroll;
    [SerializeField] private Button btnGiftCurse;
    [SerializeField] private Button btnRingCollar;

    [Header("Subcategory panels")]
    [SerializeField] private GameObject containerButtonsEquipment;
    [SerializeField] private GameObject containerButtonsSpell;
    [SerializeField] private GameObject containerButtonsGift;
    [SerializeField] private GameObject containerButtonsCollar;

    [Serializable]
    public class SubcategoryConfig
    {
        public string debugName;
        public Button button;
        public byte category;
        public byte equipSlot = 255;
        public string headerUkr;
        public GameObject parentPanel;
    }

    [Header("Subcategories")]
    [SerializeField] private SubcategoryConfig[] subcategories;

    [Header("Networking")]
    [SerializeField] private string shopListUrl = "https://api.clashfarm.com/api/player/shop/list";
    [SerializeField] private string shopBuyUrl  = "https://api.clashfarm.com/api/player/shop/buy";
    [SerializeField] private string shopSellUrl = "https://api.clashfarm.com/api/player/shop/sell";
    [SerializeField] private string inventoryUrl = "https://api.clashfarm.com/api/player/inventory";

    [Header("Confirm panels")]
    [SerializeField] private ShopBuyQuantityPanel buyQuantityPanel;

    [Header("StatusBar")]
    [SerializeField] private GameObject statusBar;
    [SerializeField] private TMP_Text statusBarText;
    [SerializeField] private float statusBarAutoHideSeconds = 5f;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingSpinner;
    [SerializeField] private TMP_Text errorText;

    private SubcategoryConfig _currentSubcategory;
    private readonly List<ShopItemView> _spawnedItems = new();
    private bool _isLoading;
    private Coroutine _statusBarRoutine;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (btnEquipments != null)  btnEquipments.onClick.AddListener(() => ShowGroup(containerButtonsEquipment));
        if (btnSpellScroll != null) btnSpellScroll.onClick.AddListener(() => ShowGroup(containerButtonsSpell));
        if (btnGiftCurse != null)   btnGiftCurse.onClick.AddListener(() => ShowGroup(containerButtonsGift));
        if (btnRingCollar != null)  btnRingCollar.onClick.AddListener(() => ShowGroup(containerButtonsCollar));

        if (subcategories != null)
        {
            foreach (var sub in subcategories)
            {
                if (sub.button == null) continue;
                var local = sub;
                sub.button.onClick.AddListener(() => OnSubcategoryClicked(local));
            }
        }

        if (buyPanelCloseButton != null)
            buyPanelCloseButton.onClick.AddListener(CloseBuyPanelOnly);

        if (buyQuantityPanel != null)
            buyQuantityPanel.Init();

        HideStatusBarImmediate();
    }

    private void OnEnable() => ResetUI();

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        ResetUI();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ResetUI()
    {
        _isLoading = false;
        ClearItems();
        SetError(null);

        if (buyPanel != null) buyPanel.SetActive(false);
        if (infoBar != null)  infoBar.SetActive(true);

        ShowGroup(null);

        if (headerText != null)
            headerText.text = string.Empty;

        HideStatusBarImmediate();
    }

    private void ShowGroup(GameObject groupPanel)
    {
        if (containerButtonsEquipment != null) containerButtonsEquipment.SetActive(groupPanel == containerButtonsEquipment);
        if (containerButtonsSpell != null)     containerButtonsSpell.SetActive(groupPanel == containerButtonsSpell);
        if (containerButtonsGift != null)      containerButtonsGift.SetActive(groupPanel == containerButtonsGift);
        if (containerButtonsCollar != null)    containerButtonsCollar.SetActive(groupPanel == containerButtonsCollar);
    }

    private void CloseBuyPanelOnly()
    {
        ClearItems();
        if (buyPanel != null) buyPanel.SetActive(false);
        if (infoBar != null)  infoBar.SetActive(true);

        if (headerText != null)
            headerText.text = string.Empty;
    }

    private void OnSubcategoryClicked(SubcategoryConfig sub)
    {
        if (_isLoading) return;

        _currentSubcategory = sub;
        Debug.Log($"[ShopUI] Subcategory clicked: {sub.debugName}, category={sub.category}, equipSlot={sub.equipSlot}");

        if (buyPanel != null) buyPanel.SetActive(true);
        if (infoBar != null)  infoBar.SetActive(false);

        if (headerText != null)
            headerText.text = sub.headerUkr;

        StartCoroutine(LoadItemsForCurrentSubcategory());
    }

    private IEnumerator LoadItemsForCurrentSubcategory()
    {
        _isLoading = true;
        SetLoading(true);
        ClearItems();
        SetError(null);

        WWWForm form = new WWWForm();
        form.AddField("PlayerName", PlayerSession.I.Data.nickname);
        form.AddField("PlayerSerialCode", PlayerSession.I.Data.serialcode);
        form.AddField("Category", _currentSubcategory.category);
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
        try { resp = JsonUtility.FromJson<ShopResponseClient>(json); }
        catch (Exception ex) { Debug.LogError($"[ShopUI] JSON parse error: {ex}"); }

        if (resp == null)
        {
            SetError("Сервер повернув некоректні дані магазину.");
            SetLoading(false);
            _isLoading = false;
            yield break;
        }

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

        foreach (var dto in resp.items)
        {
            var view = Instantiate(itemPrefab, itemsContentRoot);
            view.gameObject.SetActive(true);

            // кільця та нашийники НЕ продаємо в магазині
            bool canSell = !(dto.IsRing || dto.IsPetCollar);

            view.Bind(dto, OnItemBuyClicked, OnItemSellClicked, canSell);

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

    // ---------------- BUY ----------------

    private void OnItemBuyClicked(ShopItemClientDto item)
    {
        if (item.IsLocked)
            return;

        // не-стакові: якщо вже є — не купуємо вдруге
        if (!item.IsStackable && item.IsOwned)
            return;

        // якщо інвентар повний — навіть не відкриваємо панель покупки (для предметів що займають слоти)
        StartCoroutine(PrepareAndShowBuyPanel(item));
    }

    private IEnumerator PrepareAndShowBuyPanel(ShopItemClientDto item)
    {
        // max по грошам
        int green = PlayerSession.I?.Data?.playergreen ?? 0;
        int price = Mathf.Max(0, item.BasePrice);

        int maxByMoney = (price <= 0) ? 999 : Mathf.Clamp(green / price, 0, 999);

        // не-стакові: 1 штука
        if (!item.IsStackable)
        {
            // якщо займає слоти — перевірка на 1 вільний слот
            if (item.CountsForCapacity)
            {
                int freeSlots = 0;
                yield return StartCoroutine(GetFreeSlots(result => freeSlots = result));

                if (freeSlots <= 0)
                {
                    ShowStatusBar("Інвентар повний, покращте сховище або продайте непотрібне!");
                    yield break;
                }
            }

            if (maxByMoney <= 0)
            {
                ShowStatusBar("Недостатньо зелені!");
                yield break;
            }

            if (buyQuantityPanel == null)
            {
                StartCoroutine(BuyItemCoroutine(item, 1));
                yield break;
            }

            buyQuantityPanel.Show(
                item,
                localizedName: item.ItemId,
                pricePerOne: price,
                maxCount: 1,
                onConfirm: (_) => StartCoroutine(BuyItemCoroutine(item, 1)),
                onCancel: () => { }
            );
            yield break;
        }

        // стакові: max по слотах (строго як ти просив: 20/20 => 0, 10/20 => 10)
        int maxBySpace = 999;

        if (item.CountsForCapacity)
        {
            int freeSlots = 0;
            yield return StartCoroutine(GetFreeSlots(result => freeSlots = result));

            maxBySpace = Mathf.Max(0, freeSlots);

            if (maxBySpace <= 0)
            {
                ShowStatusBar("Інвентар повний, покращте сховище або продайте непотрібне!");
                yield break;
            }
        }

        int max = Mathf.Clamp(Mathf.Min(maxByMoney, maxBySpace), 0, 999);

        if (max <= 0)
        {
            ShowStatusBar("Недостатньо зелені!");
            yield break;
        }

        if (buyQuantityPanel == null)
        {
            StartCoroutine(BuyItemCoroutine(item, 1));
            yield break;
        }

        buyQuantityPanel.Show(
            item,
            localizedName: item.ItemId,
            pricePerOne: price,
            maxCount: max,
            onConfirm: (count) =>
            {
                count = Mathf.Clamp(count, 1, max);
                StartCoroutine(BuyItemCoroutine(item, count));
            },
            onCancel: () => { }
        );
    }

    private IEnumerator GetFreeSlots(Action<int> cb)
    {
        WWWForm form = new WWWForm();
        form.AddField("PlayerName", PlayerSession.I.Data.nickname);
        form.AddField("PlayerSerialCode", PlayerSession.I.Data.serialcode);

        using UnityWebRequest req = UnityWebRequest.Post(inventoryUrl, form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[ShopUI] Inventory load failed: " + req.error);
            cb?.Invoke(999); // якщо не змогли прочитати — не блокуємо покупку
            yield break;
        }

        var json = req.downloadHandler.text;
        InventoryResponseClient inv = null;
        try { inv = JsonUtility.FromJson<InventoryResponseClient>(json); }
        catch { }

        if (inv == null || inv.error != "OK")
        {
            cb?.Invoke(999);
            yield break;
        }

        int freeSlots = Mathf.Max(0, inv.maxslots - inv.usedslots);
        cb?.Invoke(freeSlots);
    }

    private IEnumerator BuyItemCoroutine(ShopItemClientDto item, int count)
    {
        SetLoading(true);
        SetError(null);

        count = Mathf.Max(1, count);

        WWWForm form = new WWWForm();
        form.AddField("PlayerName", PlayerSession.I.Data.nickname);
        form.AddField("PlayerSerialCode", PlayerSession.I.Data.serialcode);
        form.AddField("ItemId", item.ItemId);
        form.AddField("Count", count);

        using UnityWebRequest req = UnityWebRequest.Post(shopBuyUrl, form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            SetError("Не вдалося виконати покупку.");
            Debug.LogError($"[ShopUI] Buy error: {req.error}");
            Debug.LogError($"[ShopUI] Buy body: {req.downloadHandler.text}");
            SetLoading(false);
            yield break;
        }

        Debug.Log($"[ShopUI] Buy response: {req.downloadHandler.text}");

        // оновлюємо список
        StartCoroutine(LoadItemsForCurrentSubcategory());
        SetLoading(false);
    }

    // ---------------- SELL ----------------

    private void OnItemSellClicked(ShopItemClientDto item)
    {
        if (item.IsLocked)
            return;

        // кільця/нашийники не продаємо в магазині
        if (item.IsRing || item.IsPetCollar)
            return;

        if (!item.IsOwned)
            return;

        // поки що без підтвердження (ти вже казав — потім підв'яжемо ShopSellQuantityPanel)
        StartCoroutine(SellItemCoroutine(item, 1));
    }

    private IEnumerator SellItemCoroutine(ShopItemClientDto item, int count)
    {
        SetLoading(true);
        SetError(null);

        count = Mathf.Max(1, count);

        WWWForm form = new WWWForm();
        form.AddField("PlayerName", PlayerSession.I.Data.nickname);
        form.AddField("PlayerSerialCode", PlayerSession.I.Data.serialcode);
        form.AddField("ItemId", item.ItemId);
        form.AddField("Count", count);

        using UnityWebRequest req = UnityWebRequest.Post(shopSellUrl, form);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            SetError("Не вдалося виконати продаж.");
            Debug.LogError($"[ShopUI] Sell error: {req.error}");
            Debug.LogError($"[ShopUI] Sell body: {req.downloadHandler.text}");
            SetLoading(false);
            yield break;
        }

        Debug.Log($"[ShopUI] Sell response: {req.downloadHandler.text}");
        StartCoroutine(LoadItemsForCurrentSubcategory());
        SetLoading(false);
    }

    // ---------------- StatusBar ----------------

    private void ShowStatusBar(string message)
    {
        if (statusBar == null || statusBarText == null)
            return;

        statusBar.SetActive(true);
        statusBarText.text = message;

        if (_statusBarRoutine != null)
            StopCoroutine(_statusBarRoutine);

        _statusBarRoutine = StartCoroutine(HideStatusBarAfter(statusBarAutoHideSeconds));
    }

    private IEnumerator HideStatusBarAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideStatusBarImmediate();
    }

    private void HideStatusBarImmediate()
    {
        if (_statusBarRoutine != null)
        {
            StopCoroutine(_statusBarRoutine);
            _statusBarRoutine = null;
        }

        if (statusBar != null)
            statusBar.SetActive(false);

        if (statusBarText != null)
            statusBarText.text = "";
    }
}
