using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class InventoryItemView : MonoBehaviour
{
    [Header("UI")]
    public Image rarityFrame;
    public Image icon;
    public TextMeshProUGUI stackCountText;
    public TextMeshProUGUI levelText;
    public Button button;

    private InventoryItemViewModel _vm;

    public void Bind(InventoryItemViewModel vm, UnityAction<InventoryItemViewModel> onClick)
    {
        _vm = vm;

        // Рамка по Rarity
        ApplyRarityFrame(vm.Data.ItemLevel);

        // Поки немає іконки – ховаємо, щоб не світилась стара
        icon.sprite = null;
        icon.enabled = false;

        // Асинхронно вантажимо іконку по IconKey
        StartCoroutine(LoadAndSetIcon(vm.Data.IconKey, vm.Data.Id));

        // Стек
        if (vm.Data.StackCount > 1)
        {
            stackCountText.gameObject.SetActive(true);
            stackCountText.text = "x" + vm.Data.StackCount;
        }
        else
        {
            stackCountText.gameObject.SetActive(false);
        }

        // Рівень для зброї/броні
        if (vm.Data.Category == 0 || vm.Data.Category == 1)
        {
            levelText.gameObject.SetActive(true);
            levelText.text = "L" + vm.Data.ItemLevel;
        }
        else
        {
            levelText.gameObject.SetActive(false);
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(_vm));
    }

    private System.Collections.IEnumerator LoadAndSetIcon(string iconKey, long itemId)
    {
        if (ItemIconProvider.Instance == null)
            yield break;

        yield return ItemIconProvider.Instance.LoadIcon(iconKey, sprite =>
        {
            // Перевіряємо, що це все ще той же айтем (щоб не ловити баги при реюзі view)
            if (_vm != null && _vm.Data.Id == itemId)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        });
    }

    private void ApplyRarityFrame(byte ItemLevel)
    {
        if (rarityFrame == null)
            return;

        Sprite frameSprite = null;

        if (RarityFrameProvider.Instance != null)
            frameSprite = RarityFrameProvider.Instance.GetFrame(ItemLevel);

        rarityFrame.sprite = frameSprite;
        rarityFrame.enabled = frameSprite != null;
    }
}
