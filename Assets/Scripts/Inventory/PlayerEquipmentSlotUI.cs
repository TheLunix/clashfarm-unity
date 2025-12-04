using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PlayerEquipmentSlotUI : MonoBehaviour
{
    [Header("Тип слоту (налаштовується в інспекторі)")]
    public PlayerEquipmentSlot slotType;

    [Header("UI елементи")]
    public Button button;              // сама кнопка
    public Image iconImage;            // іконка предмета/бафа
    public Image rarityFrameImage;     // рамка рідкості (якщо є)

    [Header("Заглушка коли слот пустий")]
    public Sprite emptySprite;         // 🔥 твоя заглушка
    [Header("Базова рамка для порожнього слоту")]
    public Sprite defaultRarityFrame;


    // Поточний прив’язаний предмет (може бути null)
    [HideInInspector] public InventoryItemClientDto boundItem;
    [HideInInspector] public bool isBuff;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        // Нічого не робимо, якщо слот порожній
        if (boundItem == null)
            return;

        // Баф-слоти поки що не чіпаємо (пізніше зробимо іншу логіку)
        if (slotType == PlayerEquipmentSlot.BuffStamina ||
            slotType == PlayerEquipmentSlot.BuffPower   ||
            slotType == PlayerEquipmentSlot.BuffDefense ||
            slotType == PlayerEquipmentSlot.BuffGold)
        {
            // TODO: окремий екран/логіка для бафів
            return;
        }

        // Просимо менеджер слотів зняти предмет через API
        if (PlayerEquipmentSlotsUI.Instance != null)
        {
            PlayerEquipmentSlotsUI.Instance.RequestUnequipFromHUD(this);
        }
    }

    /// <summary>Показати пустий слот (заглушка).</summary>
    public void SetEmpty()
    {
        boundItem = null;
        isBuff = false;

        // Показуємо заглушку, якщо вона призначена
        if (iconImage != null)
        {
            if (emptySprite != null)
            {
                iconImage.sprite = emptySprite;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        // 🔹 Рамка: показуємо базову, навіть якщо предмет не екіпнутий
        if (rarityFrameImage != null)
        {
            if (defaultRarityFrame != null)
            {
                rarityFrameImage.sprite = defaultRarityFrame;
                rarityFrameImage.enabled = true;
            }
            else
            {
                rarityFrameImage.sprite = null;
                rarityFrameImage.enabled = false;
            }
        }
    }

    /// <summary>Показати екіпірований предмет.</summary>
    public void SetItem(InventoryItemClientDto item, Sprite iconSprite, Sprite raritySprite = null)
    {
        boundItem = item;
        isBuff = false;

        if (iconImage != null)
        {
            iconImage.sprite = iconSprite != null ? iconSprite : emptySprite;
            iconImage.enabled = true;
        }

        if (rarityFrameImage != null)
        {
            // Якщо для конкретного айтема передали свою рамку – ставимо її
            if (raritySprite != null)
            {
                rarityFrameImage.sprite = raritySprite;
                rarityFrameImage.enabled = true;
            }
            else if (defaultRarityFrame != null)
            {
                // Інакше – хоча б базову рамку
                rarityFrameImage.sprite = defaultRarityFrame;
                rarityFrameImage.enabled = true;
            }
            else
            {
                rarityFrameImage.sprite = null;
                rarityFrameImage.enabled = false;
            }
        }
    }

    /// <summary>Показати баф.</summary>
    public void SetBuff(Sprite buffIcon, bool active)
    {
        boundItem = null;
        isBuff = true;

        if (iconImage != null)
        {
            iconImage.sprite = active && buffIcon != null ? buffIcon : emptySprite;
            iconImage.enabled = true;
        }

        if (rarityFrameImage != null)
            rarityFrameImage.enabled = false;
    }
}
