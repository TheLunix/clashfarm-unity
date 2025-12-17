using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSellQuantityPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;                 // корінь панелі (ShopSellQuantityPanel)

    [Header("Header")]
    public TextMeshProUGUI nameText;        // NameText
    public TextMeshProUGUI descText;        // DescText (текст "Продати N шт. за X зелень?")

    [Header("Icon")]
    public Image rarityFrame;               // IconFrame (рамка по Rarity)
    public Image icon;                      // Icon (спрайт предмета)

    [Header("Count")]
    public GameObject countRoot;            // CountRoot (показуємо тільки якщо стек >= 2)
    public Button buttonMinus;              // ButtonMinus
    public TextMeshProUGUI countText;       // Count (TMP)
    public Button buttonPlus;               // ButtonPlus
    public Button buttonMax;                // ButtonMax

    [Header("Buttons")]
    public Button buttonYes;                // ButtonYes
    public Button buttonNo;                 // ButtonNo

    private int _max = 1;
    private int _current = 1;
    private int _pricePerOne = 0;

    private Action<int> _onConfirm;
    private Action _onCancel;

    private Coroutine _iconCoroutine;

    public void Init()
    {
        if (root == null)
            root = gameObject;

        root.SetActive(false);

        if (buttonMinus != null)
            buttonMinus.onClick.AddListener(OnMinusClicked);

        if (buttonPlus != null)
            buttonPlus.onClick.AddListener(OnPlusClicked);

        if (buttonMax != null)
            buttonMax.onClick.AddListener(OnMaxClicked);

        if (buttonYes != null)
            buttonYes.onClick.AddListener(OnYesClicked);

        if (buttonNo != null)
            buttonNo.onClick.AddListener(OnNoClicked);
    }

    public void Show(
        InventoryItemViewModel vm,
        string localizedName,
        int pricePerOne,
        Action<int> onConfirm,
        Action onCancel)
    {
        if (root == null)
            root = gameObject;

        root.SetActive(true);

        _onConfirm   = onConfirm;
        _onCancel    = onCancel;
        _pricePerOne = Mathf.Max(0, pricePerOne);

        var d = vm.Data;

        _max = d.StackCount <= 0 ? 1 : d.StackCount;

        bool hasMultiple = _max > 1;
        if (countRoot != null)
            countRoot.SetActive(hasMultiple);

        _current = 1;

        if (nameText != null)
            nameText.text = localizedName;

        UpdateCountLabel();
        UpdateDescLabel();

        // Раріті фрейм (ВАЖЛИВО: по Rarity, не по ItemLevel)
        if (rarityFrame != null)
        {
            Sprite frameSprite = null;
            if (RarityFrameProvider.Instance != null)
                frameSprite = RarityFrameProvider.Instance.GetFrame(d.Rarity);

            rarityFrame.sprite = frameSprite;
            rarityFrame.enabled = frameSprite != null;
        }

        // Іконка предмета
        if (_iconCoroutine != null)
        {
            StopCoroutine(_iconCoroutine);
            _iconCoroutine = null;
        }

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;

            if (ItemIconProvider.Instance != null)
                _iconCoroutine = StartCoroutine(LoadIconCoroutine(d.IconKey, d.Id));
            else
                Debug.LogWarning("[ShopSellQuantityPanel] ItemIconProvider.Instance is null");
        }
    }

    public void Hide()
    {
        if (_iconCoroutine != null)
        {
            StopCoroutine(_iconCoroutine);
            _iconCoroutine = null;
        }

        if (root != null)
            root.SetActive(false);

        _onConfirm = null;
        _onCancel = null;
    }

    private void OnMinusClicked() => SetCurrent(_current - 1);
    private void OnPlusClicked() => SetCurrent(_current + 1);
    private void OnMaxClicked() => SetCurrent(_max);

    private void OnYesClicked()
    {
        if (_current <= 0)
            _current = 1;

        _onConfirm?.Invoke(_current);
        Hide();
    }

    private void OnNoClicked()
    {
        _onCancel?.Invoke();
        Hide();
    }

    private void SetCurrent(int value)
    {
        int clamped = Mathf.Clamp(value, 1, _max);
        if (_current == clamped)
            return;

        _current = clamped;
        UpdateCountLabel();
        UpdateDescLabel();
    }

    private void UpdateCountLabel()
    {
        if (countText != null)
            countText.text = _current.ToString();
    }

    private void UpdateDescLabel()
    {
        if (descText == null)
            return;

        int total = _current * _pricePerOne;

        if (_max > 1)
            descText.text = $"Продати {_current} шт. за {total} зелень?";
        else
            descText.text = $"Продати 1 шт. за {total} зелень?";
    }

    private IEnumerator LoadIconCoroutine(string iconKey, long itemId)
    {
        yield return ItemIconProvider.Instance.LoadIcon(iconKey, sprite =>
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        });

        _iconCoroutine = null;
    }
}
