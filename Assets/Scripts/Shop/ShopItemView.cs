using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemView : MonoBehaviour
{
    [Header("Left")]
    [SerializeField] private Image rarityFrameImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text priceText;

    [Header("Right")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Actions")]
    [SerializeField] private Button buyButton;      // ButtonBuy
    [SerializeField] private TMP_Text buyText;      // ButtonBuy/BuyText
    [SerializeField] private Button sellButton;     // ButtonSell
    [SerializeField] private TMP_Text sellText;     // ButtonSell/SellText
    [SerializeField] private TMP_Text lockText;     // LockText

    private ShopItemClientDto _data;
    private Action<ShopItemClientDto> _onBuyClicked;
    private Action<ShopItemClientDto> _onSellClicked;

    [Serializable]
    private class ShopItemStatsData
    {
        public int PlayerPower;
        public int PlayerSkill;
        public int PlayerDexterity;
        public int PlayerProtection;
        public int PlayerSurvivability;
    }

    public void Bind(
        ShopItemClientDto dto,
        Action<ShopItemClientDto> onBuyClicked,
        Action<ShopItemClientDto> onSellClicked,
        bool canSell)
    {
        _data = dto;
        _onBuyClicked = onBuyClicked;
        _onSellClicked = onSellClicked;

        // тимчасово без локалізації
        if (nameText != null) nameText.text = dto.ItemId;

        if (descriptionText != null)
            descriptionText.text = BuildDescription(dto);

        if (priceText != null)
        {
            if (dto.BasePrice > 0) priceText.text = $"{dto.BasePrice} <sprite=0>";
            else priceText.text = "Безкоштовно";
        }

        if (RarityFrameProvider.Instance != null && rarityFrameImage != null)
            rarityFrameImage.sprite = RarityFrameProvider.Instance.GetFrame(dto.Rarity);

        if (ItemIconProvider.Instance != null && iconImage != null)
            StartCoroutine(LoadIconCoroutine(dto.IconKey));

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => _onBuyClicked?.Invoke(_data));
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => _onSellClicked?.Invoke(_data));
        }

        // ---------- UI state ----------
        if (dto.IsLocked)
        {
            SetActiveSafe(buyButton, false);
            SetActiveSafe(sellButton, false);
            SetActiveSafe(lockText, true);
            if (lockText != null)
                lockText.text = $"Відкриється на {dto.MinPlayerLevel} рівні";
            return;
        }

        SetActiveSafe(lockText, false);

        // якщо продавати не можна (кільця/нашийники) — кнопка продажу ховається завжди
        if (!canSell)
        {
            SetActiveSafe(sellButton, false);
        }

        if (dto.IsStackable)
        {
            // стакові: купити завжди, продати — якщо можна і є що
            SetActiveSafe(buyButton, true);
            if (buyText != null) buyText.text = "Купити";

            if (sellButton != null)
            {
                sellButton.gameObject.SetActive(canSell);
                sellButton.interactable = canSell && dto.OwnedCount > 0;
            }
            if (sellText != null) sellText.text = "Продати";
        }
        else
        {
            // не-стакові: або купити, або продати (якщо дозволено)
            if (dto.IsOwned)
            {
                SetActiveSafe(buyButton, false);
                SetActiveSafe(sellButton, canSell);
                if (sellText != null) sellText.text = "Продати";
            }
            else
            {
                SetActiveSafe(sellButton, false);
                SetActiveSafe(buyButton, true);
                if (buyText != null) buyText.text = "Купити";
            }
        }
    }

    private static void SetActiveSafe(Component c, bool active)
    {
        if (c != null) c.gameObject.SetActive(active);
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

        if (done && result != null && iconImage != null)
            iconImage.sprite = result;
    }

    private string BuildDescription(ShopItemClientDto item)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>Рівень: {item.MinPlayerLevel}</b>");

        ShopItemStatsData stats = null;

        if (!string.IsNullOrEmpty(item.BaseStatsJson))
        {
            try { stats = JsonUtility.FromJson<ShopItemStatsData>(item.BaseStatsJson); }
            catch (Exception ex) { Debug.LogWarning($"[ShopItemView] BaseStatsJson parse fail {item.ItemId}: {ex}"); }
        }

        stats ??= new ShopItemStatsData();

        if (stats.PlayerPower > 0) sb.AppendLine($"Сила: +{stats.PlayerPower}");
        if (stats.PlayerProtection > 0) sb.AppendLine($"Захист: +{stats.PlayerProtection}");
        if (stats.PlayerDexterity > 0) sb.AppendLine($"Спритність: +{stats.PlayerDexterity}");
        if (stats.PlayerSkill > 0) sb.AppendLine($"Майстерність: +{stats.PlayerSkill}");
        if (stats.PlayerSurvivability > 0) sb.AppendLine($"Живучість: +{stats.PlayerSurvivability}");

        return sb.ToString().TrimEnd();
    }
}
