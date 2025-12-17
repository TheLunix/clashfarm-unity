using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopBuyQuantityPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Header")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText; // "Купити N шт. за X <sprite=0>?"

    [Header("Icon")]
    public Image rarityFrame;
    public Image icon;

    [Header("Count")]
    public GameObject countRoot;
    public Button buttonMinus;
    public TextMeshProUGUI countText;
    public Button buttonPlus;
    public Button buttonMax;

    [Header("Buttons")]
    public Button buttonYes;
    public Button buttonNo;

    private int _max = 1;
    private int _current = 1;
    private int _pricePerOne = 0;
    private bool _allowMulti = false;

    private Action<int> _onConfirm;
    private Action _onCancel;

    private Coroutine _iconCoroutine;

    public void Init()
    {
        if (root == null)
            root = gameObject;

        root.SetActive(false);

        if (buttonMinus != null) buttonMinus.onClick.AddListener(() => SetCurrent(_current - 1));
        if (buttonPlus != null)  buttonPlus.onClick.AddListener(() => SetCurrent(_current + 1));
        if (buttonMax != null)   buttonMax.onClick.AddListener(() => SetCurrent(_max));

        if (buttonYes != null) buttonYes.onClick.AddListener(OnYesClicked);
        if (buttonNo != null)  buttonNo.onClick.AddListener(OnNoClicked);
    }

    public void Show(
        ShopItemClientDto dto,
        string localizedName,
        int pricePerOne,
        int maxCount,
        Action<int> onConfirm,
        Action onCancel)
    {
        if (root == null)
            root = gameObject;

        root.SetActive(true);

        _onConfirm = onConfirm;
        _onCancel  = onCancel;

        _pricePerOne = Mathf.Max(0, pricePerOne);
        _max = Mathf.Clamp(maxCount, 1, 999);
        _current = 1;

        // multi тільки для stackable і коли max > 1
        _allowMulti = dto.IsStackable && _max > 1;

        if (countRoot != null)
            countRoot.SetActive(_allowMulti);

        if (nameText != null)
            nameText.text = localizedName;

        UpdateAllLabels();

        // frame by Rarity
        if (rarityFrame != null)
        {
            Sprite frame = null;
            if (RarityFrameProvider.Instance != null)
                frame = RarityFrameProvider.Instance.GetFrame(dto.Rarity);

            rarityFrame.sprite = frame;
            rarityFrame.enabled = frame != null;
        }

        // icon
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
                _iconCoroutine = StartCoroutine(LoadIconCoroutine(dto.IconKey));
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

    private void OnYesClicked()
    {
        int count = _allowMulti ? _current : 1;
        _onConfirm?.Invoke(count);
        Hide();
    }

    private void OnNoClicked()
    {
        _onCancel?.Invoke();
        Hide();
    }

    private void SetCurrent(int value)
    {
        if (!_allowMulti)
        {
            _current = 1;
            UpdateAllLabels();
            return;
        }

        int clamped = Mathf.Clamp(value, 1, _max);
        if (_current == clamped) return;

        _current = clamped;
        UpdateAllLabels();
    }

    private void UpdateAllLabels()
    {
        if (countText != null)
            countText.text = _current.ToString();

        if (descText != null)
        {
            if (_allowMulti)
            {
                int total = _current * _pricePerOne;
                descText.text = $"Купити {_current} шт. за {total} <sprite=0>?";
            }
            else
            {
                descText.text = $"Купити 1 шт. за {_pricePerOne} <sprite=0>?";
            }
        }

        // реакція кнопок +/-
        if (buttonMinus != null) buttonMinus.interactable = _allowMulti && _current > 1;
        if (buttonPlus != null)  buttonPlus.interactable  = _allowMulti && _current < _max;
        if (buttonMax != null)   buttonMax.interactable   = _allowMulti && _current < _max;
    }

    private IEnumerator LoadIconCoroutine(string iconKey)
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
