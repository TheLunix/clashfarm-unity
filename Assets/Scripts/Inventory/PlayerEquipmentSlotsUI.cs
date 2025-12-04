using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerEquipmentSlotsUI : MonoBehaviour
{
    public static PlayerEquipmentSlotsUI Instance { get; private set; }

    [Header("Усі 13 слотів навколо гравця")]
    public PlayerEquipmentSlotUI[] slots;
    [Header("API")]
    [SerializeField] private string unequipUrl = "https://api.clashfarm.com/api/player/inventory/unequip";

    [System.Serializable]
    private class SimpleErrorResponse
    {
        public string error;
    }
    public void RequestUnequipFromHUD(PlayerEquipmentSlotUI slot)
    {
        if (slot == null || slot.boundItem == null)
            return;

        // тільки екіп/кільця, бо бафи ми відсіяли в OnClick
        StartCoroutine(UnequipCoroutine(slot.boundItem.Id, slot));
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ClearAllSlots();
    }

    public void ClearAllSlots()
    {
        if (slots == null) return;
        foreach (var s in slots)
        {
            if (s != null)
                s.SetEmpty();
        }
    }

    /// <summary>
    /// Викликається, коли у нас є актуальний інвентар з сервера.
    /// Сюди передаємо resp.items з Inventory API.
    /// </summary>
    public void ApplyFromInventory(InventoryItemClientDto[] items)
    {
        if (slots == null || slots.Length == 0)
            return;

        // 1) чистимо тільки equipment/ring слоти (бафи не чіпаємо)
        foreach (var s in slots)
        {
            if (s == null) continue;

            switch (s.slotType)
            {
                case PlayerEquipmentSlot.Weapon:
                case PlayerEquipmentSlot.Chest:
                case PlayerEquipmentSlot.Helmet:
                case PlayerEquipmentSlot.Bracers:
                case PlayerEquipmentSlot.Legs:
                case PlayerEquipmentSlot.Boots:
                case PlayerEquipmentSlot.Tech:
                case PlayerEquipmentSlot.Ring1:
                case PlayerEquipmentSlot.Ring2:
                    s.SetEmpty();
                    break;

                // Buff* – залишаємо як є
            }
        }

        if (items == null || items.Length == 0)
            return;

        // 2) Беремо тільки екіпнуті предмети
        var equipped = items.Where(x => x.IsEquipped).ToList();
        if (equipped.Count == 0)
            return;

        // 3) Для кожного слота шукаємо предмет з відповідним EquippedSlot
        foreach (var s in slots)
        {
            if (s == null) continue;

            int slotCode = (int)s.slotType;
            if (slotCode >= 200)
                continue;

            var item = equipped.FirstOrDefault(x => x.EquippedSlot == slotCode);
            if (item == null)
                continue;

            StartCoroutine(ApplyEquippedItemToSlot(s, item));
        }
    }

    /// <summary>
    /// Окремий метод для бафів (пізніше, коли зробимо систему бафів).
    /// </summary>
    public void SetBuffSlot(PlayerEquipmentSlot buffSlot, Sprite icon, bool active)
    {
        var slot = slots.FirstOrDefault(s => s.slotType == buffSlot);
        if (slot == null) return;

        slot.SetBuff(icon, active);
    }

    private System.Collections.IEnumerator UnequipCoroutine(long playerItemId, PlayerEquipmentSlotUI slot)
    {
        var player = PlayerSession.I?.Data;
        if (player == null)
        {
            Debug.LogError("[HUD] Unequip: PlayerSession.I.Data is null");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("PlayerName", player.nickname);
        form.AddField("PlayerSerialCode", player.serialcode);
        form.AddField("PlayerItemId", playerItemId.ToString());

        using (UnityWebRequest www = UnityWebRequest.Post(unequipUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[HUD] Unequip error: " + www.error);
                Debug.LogError("[HUD] Unequip body: " + www.downloadHandler.text);
                yield break;
            }

            var json = www.downloadHandler.text;
            Debug.Log("[HUD] Unequip response: " + json);

            SimpleErrorResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<SimpleErrorResponse>(json);
            }
            catch
            {
                Debug.LogError("[HUD] Unequip: bad JSON");
                yield break;
            }

            if (resp == null || resp.error != "OK")
            {
                Debug.LogError("[HUD] Unequip failed: " + (resp?.error ?? "null"));
                yield break;
            }

            // УСПІХ: локально чистимо слот
            slot.SetEmpty();

            // (Опційно) якщо інвентар відкритий – можна дати сигнал InventoryUIController оновитись
            //InventoryUIController.Instance?.ReloadInventory(); // якщо захочеш зробити сінглтон
        }
        
    }
    private System.Collections.IEnumerator ApplyEquippedItemToSlot(PlayerEquipmentSlotUI slot, InventoryItemClientDto item)
    {
        // Спочатку можемо поставити базову рамку + заглушку
        var baseRarityFrame = RarityFrameProvider.Instance != null
            ? RarityFrameProvider.Instance.GetFrame(item.ItemLevel)
            : null;

        slot.SetItem(item, null, baseRarityFrame);

        // Тепер вантажимо реальну іконку
        yield return ItemIconProvider.Instance.LoadIcon(item.IconKey, sprite =>
        {
            if (slot.boundItem != null && slot.boundItem.Id == item.Id)
            {
                var rarityFrame = RarityFrameProvider.Instance != null
                    ? RarityFrameProvider.Instance.GetFrame(item.ItemLevel)
                    : null;

                slot.SetItem(item, sprite, rarityFrame);
            }
        });
    }

}
