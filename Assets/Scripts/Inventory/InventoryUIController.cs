using System; 
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Localization.Settings;

public class InventoryUIController : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string inventoryUrl = "https://api.clashfarm.com/api/player/inventory";
    [SerializeField] private string equipUrl     = "https://api.clashfarm.com/api/player/inventory/equip";
    [SerializeField] private string unequipUrl  = "https://api.clashfarm.com/api/player/inventory/unequip";
    [SerializeField] private string sellUrl     = "https://api.clashfarm.com/api/player/inventory/sell";
    [SerializeField] private string itemsStringTable = "Items"; // ім'я String Table для предметів
    [SerializeField] private string inventoryUiTable = "InventoryUI";

    [Header("UI")]
    public TextMeshProUGUI capacityText;
    public RectTransform sectionsContentRoot;  // ScrollView/Content
    public InventorySectionView sectionPrefab;
    public InventoryItemView itemPrefab;
    public InventoryDetailsPanel detailsPanel;
    public InventoryWarningPanel warningPanel;
    public InventorySellQuantityPanel sellQuantityPanel;

    [Header("Scene UI")]
    [SerializeField] private GameObject infoBar; 

    private readonly List<GameObject> _spawnedSections = new();

    private void Awake()
    {
        detailsPanel.Init(OnEquipClicked, OnUseClicked, OnSellClicked);

        if (sellQuantityPanel != null)
            sellQuantityPanel.Init();
    }

    public void Open()
    {
        if (infoBar != null)
            infoBar.SetActive(false);

        gameObject.SetActive(true);
        StartCoroutine(LoadAndRenderCoroutine());
    }

    public void Close()
    {
        gameObject.SetActive(false);
        
        if (infoBar != null)
            infoBar.SetActive(true);
    }

    private IEnumerator LoadAndRenderCoroutine()
    {
         // 🔹 Підстрахуємось, що PlayerSession існує
        if (PlayerSession.I == null)
        {
            Debug.LogError("InventoryUIController: PlayerSession.I is null");
            yield break;
        }

        var playerData = PlayerSession.I.Data;

        WWWForm form = new WWWForm();
        form.AddField("PlayerName", playerData.nickname);
        form.AddField("PlayerSerialCode", playerData.serialcode);

        using (UnityWebRequest www = UnityWebRequest.Post(inventoryUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Inventory error: " + www.error);
                Debug.LogError(www.downloadHandler.text);
                yield break;
            }

            var json = www.downloadHandler.text;
            Debug.Log("[INVENTORY] Raw JSON: " + json);
            var resp = JsonUtility.FromJson<InventoryResponseClient>(json);
            if (resp == null || resp.error != "OK")
            {
                Debug.LogError("Inventory response error: " + (resp?.error ?? "null"));
                yield break;
            }

            RenderInventory(resp);
        }
    }

    private void RenderInventory(InventoryResponseClient resp)
    {
        // 1) Оновлюємо HUD – йому потрібні всі предмети, включно з екіпнутими
        PlayerEquipmentSlotsUI.Instance?.ApplyFromInventory(resp.items);

        // 2) Чистимо секції інвентаря
        foreach (var go in _spawnedSections)
            Destroy(go);
        _spawnedSections.Clear();

        capacityText.text = $"{resp.usedslots} / {resp.maxslots}";

        if (resp.items == null || resp.items.Length == 0)
            return;

        // 3) Фільтр тільки для UI інвентаря
        var itemsForInventory = resp.items
            .Where(i => i.VisibleInMainInventory && !i.IsEquipped)
            .ToArray();

        if (itemsForInventory.Length == 0)
            return;

        var vms = itemsForInventory
            .Select(ToViewModel)
            .ToList();

        var groups = vms
            .GroupBy(vm => vm.Group)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var itemsInGroup = group
                .OrderBy(vm => vm.SortKeyPrimary)
                .ThenByDescending(vm => vm.SortKeyLevel)
                .ThenBy(vm => vm.SortKeyId)
                .ToList();

            if (itemsInGroup.Count == 0)
                continue;

            var section = Instantiate(sectionPrefab, sectionsContentRoot);
            _spawnedSections.Add(section.gameObject);

            section.SetTitle(GetGroupTitle(group.Key));

            foreach (var vm in itemsInGroup)
            {
                var itemView = Instantiate(itemPrefab, section.gridRoot);
                itemView.Bind(vm, OnItemClicked);
            }
        }
    }

    private InventoryItemViewModel ToViewModel(InventoryItemClientDto dto)
    {
        return new InventoryItemViewModel
        {
            Data           = dto,
            Group          = MapToUiGroup(dto),
            SortKeyPrimary = dto.ItemId,
            SortKeyLevel   = dto.ItemLevel,
            SortKeyId      = dto.Id
        };
    }

    private InventoryUiGroup MapToUiGroup(InventoryItemClientDto dto)
    {
        switch (dto.Category)
        {
            case 0: return InventoryUiGroup.Equipment;
            case 1: return InventoryUiGroup.Rings;
            case 2: return InventoryUiGroup.Potions;
            case 3: return InventoryUiGroup.Scrolls;
            case 4: return InventoryUiGroup.Gifts;
            case 5: return InventoryUiGroup.Curses;
            case 6: return InventoryUiGroup.Event;
            default: return InventoryUiGroup.Other;
        }
    }

    private string GetGroupTitle(InventoryUiGroup group)
    {
        string key = group switch
        {
            InventoryUiGroup.Equipment => "inventory.group.equipment",
            InventoryUiGroup.Rings     => "inventory.group.rings",
            InventoryUiGroup.Potions   => "inventory.group.potions",
            InventoryUiGroup.Scrolls   => "inventory.group.scrolls",
            InventoryUiGroup.Gifts     => "inventory.group.gifts",
            InventoryUiGroup.Curses    => "inventory.group.curses",
            InventoryUiGroup.Event     => "inventory.group.event",
            _                          => "inventory.group.other",
        };

        return LocalizationSettings.StringDatabase.GetLocalizedString(inventoryUiTable, key);
    }

    private void OnItemClicked(InventoryItemViewModel vm)
    {
        var d = vm.Data;

        string nameKey = d.NameLocKey;
        string descKey = d.DescLocKey;

        var db = LocalizationSettings.StringDatabase;

        string name = !string.IsNullOrWhiteSpace(nameKey)
            ? db.GetLocalizedString(itemsStringTable, nameKey)
            : d.ItemId; // запасний варіант – показати ItemId

        string desc = !string.IsNullOrWhiteSpace(descKey)
            ? db.GetLocalizedString(itemsStringTable, descKey)
            : string.Empty;

        detailsPanel.Show(vm, name, desc);
    }

    private void OnEquipClicked(InventoryItemViewModel vm)
    {
        var d = vm.Data;

        if (d.IsEquipped)
        {
            Debug.Log("[INVENTORY] Unequip clicked: " + d.ItemId + " (PlayerItemId=" + d.Id + ")");
            StartCoroutine(UnequipCoroutine(vm));
        }
        else
        {
            Debug.Log("[INVENTORY] Equip clicked: " + d.ItemId + " (PlayerItemId=" + d.Id + ")");
            StartCoroutine(EquipCoroutine(vm));
        }
    }

    private void OnUseClicked(InventoryItemViewModel vm)
    {
        Debug.Log("Use clicked: " + vm.Data.ItemId);
        // TODO: API /use
    }

    private void OnSellClicked(InventoryItemViewModel vm)
    {
        var d = vm.Data;

        if (!d.CanSell)
        {
            Debug.Log("[INVENTORY] Item cannot be sold: " + d.ItemId);
            return;
        }

        if (d.IsEquipped)
        {
            Debug.Log("[INVENTORY] Cannot sell equipped item: " + d.ItemId);
            return;
        }

        if (sellQuantityPanel == null)
        {
            Debug.LogError("[INVENTORY] SellQuantityPanel is not assigned");
            return;
        }

        // Підтягуємо локалізовану назву (як у OnItemClicked)
        var db = LocalizationSettings.StringDatabase;

        string nameKey = d.NameLocKey;
        string localizedName = !string.IsNullOrWhiteSpace(nameKey)
            ? db.GetLocalizedString(itemsStringTable, nameKey)
            : d.ItemId;

        int pricePerOne = d.BasePrice / 2;
        if (pricePerOne < 0) pricePerOne = 0;

        // Ховаємо DetailsPanel, відкриваємо панель кількості
        detailsPanel.Hide();

        sellQuantityPanel.Show(
            vm,
            localizedName,
            pricePerOne,
            countToSell =>
            {
                // YES → продаємо обрану кількість
                StartCoroutine(SellCoroutine(vm, countToSell));
            },
            () =>
            {
                // NO → повертаємо панель деталей
                // (знову підтягуємо опис з локалізації)
                string descKey = d.DescLocKey;
                string localizedDesc = !string.IsNullOrWhiteSpace(descKey)
                    ? db.GetLocalizedString(itemsStringTable, descKey)
                    : string.Empty;

                detailsPanel.Show(vm, localizedName, localizedDesc);
            });
    }

    [System.Serializable]
    private class EquipResponse
    {
        public string error;
    }

    [System.Serializable]
    private class SellResponse
    {
        public string error;
        public int    green;
        public int    soldCount;
        public int    reward;
    }
    private IEnumerator EquipCoroutine(InventoryItemViewModel vm)
    {
        var d = vm.Data;

        var player = PlayerSession.I?.Data;
        if (player == null)
        {
            Debug.LogError("[INVENTORY] Equip: PlayerSession.I.Data is null");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("PlayerName", player.nickname);
        form.AddField("PlayerSerialCode", player.serialcode);
        form.AddField("PlayerItemId", d.Id.ToString());   // Id = PlayerItems.Id

        using (UnityWebRequest www = UnityWebRequest.Post(equipUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[INVENTORY] Equip error: " + www.error);
                Debug.LogError("[INVENTORY] Equip body: " + www.downloadHandler.text);
                yield break;
            }

            var json = www.downloadHandler.text;
            Debug.Log("[INVENTORY] Equip response: " + json);

            EquipResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<EquipResponse>(json);
            }
            catch
            {
                Debug.LogError("[INVENTORY] Equip: bad JSON");
                yield break;
            }

            if (resp == null || resp.error != "OK")
            {
                Debug.LogError("[INVENTORY] Equip failed: " + (resp?.error ?? "null"));
                yield break;
            }

            // -----------------------------------------
            // УСПІХ: закриваємо панель деталей 👇
            // -----------------------------------------
            if (detailsPanel != null)
                detailsPanel.Hide();

            // Оновлюємо інвентар (і слоти екіпу)
            StartCoroutine(LoadAndRenderCoroutine());
        }
    }

    private IEnumerator UnequipCoroutine(InventoryItemViewModel vm)
    {
        var d = vm.Data;

        var player = PlayerSession.I?.Data;
        if (player == null)
        {
            Debug.LogError("[HUD] Unequip: PlayerSession.I.Data is null");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("PlayerName", player.nickname);
        form.AddField("PlayerSerialCode", player.serialcode);
        form.AddField("PlayerItemId", d.Id.ToString());

        using (UnityWebRequest www = UnityWebRequest.Post(unequipUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[INVENTORY] Unequip error: " + www.error);
                Debug.LogError("[INVENTORY] Unequip body: " + www.downloadHandler.text);
                yield break;
            }

            var json = www.downloadHandler.text;
            Debug.Log("[INVENTORY] Unequip response: " + json);

            EquipResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<EquipResponse>(json);
            }
            catch
            {
                Debug.LogError("[INVENTORY] Unequip: bad JSON");
                yield break;
            }

            if (resp == null || resp.error != "OK")
            {
                Debug.LogError("[INVENTORY] Unequip failed: " + (resp?.error ?? "null"));
                yield break;
            }

            // УСПІХ: ховаємо панель деталей
            if (detailsPanel != null)
                detailsPanel.Hide();

            // Перезавантажуємо інвентар (оновиться список + HUD-слоти, якщо ти їх підʼєднав)
            StartCoroutine(LoadAndRenderCoroutine());
        }
    }

    private IEnumerator SellCoroutine(InventoryItemViewModel vm, int countToSell)
    {
        var d = vm.Data;

        var player = PlayerSession.I?.Data;
        if (player == null)
        {
            Debug.LogError("[INVENTORY] Sell: PlayerSession.I.Data is null");
            yield break;
        }

        countToSell = Mathf.Clamp(countToSell, 1, d.StackCount <= 0 ? 1 : d.StackCount);

        WWWForm form = new WWWForm();
        form.AddField("PlayerName",       player.nickname);
        form.AddField("PlayerSerialCode", player.serialcode);
        form.AddField("PlayerItemId",     d.Id.ToString());    // PlayerItems.Id
        form.AddField("Count",            countToSell.ToString());

        using (UnityWebRequest www = UnityWebRequest.Post(sellUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[INVENTORY] Sell error: " + www.error);
                Debug.LogError("[INVENTORY] Sell body: " + www.downloadHandler.text);
                yield break;
            }

            var json = www.downloadHandler.text;
            Debug.Log("[INVENTORY] Sell response: " + json);

            SellResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<SellResponse>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[INVENTORY] Sell: bad JSON: " + ex);
                yield break;
            }

            if (resp == null || resp.error != "OK")
            {
                Debug.LogError("[INVENTORY] Sell failed: " + (resp?.error ?? "null"));
                yield break;
            }

            // 🔹 МИТТЄВО оновлюємо валюти в сесії
            if (PlayerSession.I != null)
            {
                PlayerSession.I.Patch(p =>
                {
                    p.playergreen = resp.green;
                    // якщо в майбутньому будеш продавати за золото/діаманти –
                    // тут же оновиш p.playergold / p.playerdiamonds
                });
            }

            // Успішний продаж → оновлюємо інвентар
            if (detailsPanel != null)
                detailsPanel.Hide(); // деталі вже не актуальні

            StartCoroutine(LoadAndRenderCoroutine());
        }
    }
    
    public void OnCloseButtonClicked()
    {
        // Ховаємо панель деталей, якщо була відкрита
        detailsPanel.Hide();

        // Ховаємо варнінг-панель (на всякий)
        if (warningPanel != null)
            warningPanel.Hide();

        // Закриваємо сам інвентар
        Close();
    }
}
