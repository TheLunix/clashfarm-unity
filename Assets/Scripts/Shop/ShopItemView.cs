using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemView : MonoBehaviour
{
    [Header("Left")]
    [SerializeField] private Image rarityFrameImage;    // RarityPlaceholder → Image
    [SerializeField] private Image iconImage;           // RarityPlaceholder/Image
    [SerializeField] private TMP_Text priceText;        // PricePlaceholder/PriceText

    [Header("Right")]
    [SerializeField] private TMP_Text nameText;         // RightContainer/NameItemText
    [SerializeField] private TMP_Text descriptionText;  // RightContainer/DescriptionText
    [SerializeField] private Button  buyButton;         // ButtonBuy
    [SerializeField] private TMP_Text buyText;          // ButtonBuy/BuyText

    private ShopItemClientDto _data;
    private Action<ShopItemClientDto> _onBuyClicked;

    public void Bind(ShopItemClientDto dto, Action<ShopItemClientDto> onBuyClicked)
    {
        _data = dto;
        _onBuyClicked = onBuyClicked;

        // Назва — поки хардкор українською, потім поставимо локалі по NameLocKey
        nameText.text = dto.ItemId; // тимчасово

        // Опис з JSON-статів
        descriptionText.text = BuildDescription(dto);

        // 🔥 Ціна: BasePrice в зелені
        if (dto.BasePrice > 0)
            priceText.text = $"{dto.BasePrice} <sprite=0>";
        else
            priceText.text = "Безкоштовно";

        // 🔥 Рамка по Rarity (а не по Level)
        if (RarityFrameProvider.Instance != null)
            rarityFrameImage.sprite = RarityFrameProvider.Instance.GetFrame(dto.Rarity);

        // Іконка
        if (ItemIconProvider.Instance != null)
            StartCoroutine(LoadIconCoroutine(dto.IconKey));

        // Кнопка
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() =>
        {
            _onBuyClicked?.Invoke(_data);
        });

        bool isUnlocked = !dto.IsLocked;

        if (!isUnlocked)
        {
            buyButton.interactable = false;
            buyText.text = $"Відкриється на {dto.MinPlayerLevel} рівні";
        }
        else
        {
            buyButton.interactable = true;
            buyText.text = dto.IsOwned ? "Продати" : "Купити";
        }
    }

    private IEnumerator LoadIconCoroutine(string iconKey)
    {
        bool done = false;
        Sprite result = null;

        yield return ItemIconProvider.Instance.LoadIcon(iconKey, s =>
        {
            result = s;
            done = true;
        });

        if (done && result != null)
            iconImage.sprite = result;
    }

    // 🔥 Ось ТУТ — логіка побудови опису з JSON-статів
    private string BuildDescription(ShopItemClientDto item)
    {
        var sb = new System.Text.StringBuilder();

        // 1) Рівень
        sb.AppendLine($"<b>Рівень: {item.MinPlayerLevel}</b>");

        // 2) Парсимо JSON зі статами
        ShopItemStatsData stats = null;

        if (!string.IsNullOrEmpty(item.BaseStatsJson))
        {
            try
            {
                stats = JsonUtility.FromJson<ShopItemStatsData>(item.BaseStatsJson);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShopItemView] Не вдалося розпарсити BaseStatsJson для {item.ItemId}: {ex}");
            }
        }

        if (stats == null)
            stats = new ShopItemStatsData();

        if (stats.PlayerPower > 0)
            sb.AppendLine($"Сила: +{stats.PlayerPower}");
        if (stats.PlayerProtection > 0)
            sb.AppendLine($"Захист: +{stats.PlayerProtection}");
        if (stats.PlayerDexterity > 0)
            sb.AppendLine($"Спритність: +{stats.PlayerDexterity}");
        if (stats.PlayerSkill > 0)
            sb.AppendLine($"Майстерність: +{stats.PlayerSkill}");
        if (stats.PlayerSurvivability > 0)
            sb.AppendLine($"Живучість: +{stats.PlayerSurvivability}");

        return sb.ToString().TrimEnd();
    }
}
