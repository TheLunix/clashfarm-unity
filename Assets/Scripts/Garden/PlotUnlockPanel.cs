using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public sealed class PlotUnlockPanel : MonoBehaviour
{
    [Header("Root & Content")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text priceText;

    [Header("Buttons")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    [Header("Inline error (shows for N seconds)")]
    [SerializeField] private GameObject errorPanel;   // 👈 перетягни сюди невелику панель/рядок помилки
    [SerializeField] private TMP_Text errorText;    // 👈 текст усередині errorPanel
    [SerializeField] private float errorDuration = 5f;

    private Action _onConfirm;
    private Coroutine _hideErrorCo;

    void Awake()
    {
        if (btnConfirm != null)
            btnConfirm.onClick.AddListener(() => { _onConfirm?.Invoke(); /* НЕ закриваємо тут */ });

        if (btnCancel != null)
            btnCancel.onClick.AddListener(() => { Close(); });

        //Close();
        HideErrorImmediate();
    }

    public void Open(int uiSlotIndexClicked, int price, Action onConfirm)
    {
        _onConfirm = onConfirm;

        // скидаємо стан з попереднього відкриття
        SetInteractable(true);
        HideErrorImmediate();

        int nextUi = (GardenStateCache.I != null ? GardenStateCache.I.UnlockedSlots : 3) + 1;

        if (title) title.text = $"Відкрити грядку №{nextUi}?";
        if (priceText) priceText.text = $"Ціна: <sprite=1> {price}";

        if (root) root.SetActive(true);
        else gameObject.SetActive(true);

        transform.SetAsLastSibling();
    }

    public void Close()
    {
        if (root) root.SetActive(false);
        else gameObject.SetActive(false);

        _onConfirm = null;
        SetInteractable(true);   // щоб наступне відкриття точно мало активні кнопки
        HideErrorImmediate();
    }

    // === Error helpers ===

    public void ShowError(string msg, float? seconds = null)
    {
        if (_hideErrorCo != null) { StopCoroutine(_hideErrorCo); _hideErrorCo = null; }

        if (errorText != null && !string.IsNullOrEmpty(msg))
            errorText.text = msg;

        if (errorPanel != null)
            errorPanel.SetActive(true);

        _hideErrorCo = StartCoroutine(HideErrorAfter(seconds ?? errorDuration));
    }

    private IEnumerator HideErrorAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        HideErrorImmediate();
        _hideErrorCo = null;
    }

    private void HideErrorImmediate()
    {
        if (errorPanel != null) errorPanel.SetActive(false);
        if (_hideErrorCo != null) { StopCoroutine(_hideErrorCo); _hideErrorCo = null; }
    }

    public void SetInteractable(bool v)
    {
        if (btnConfirm) btnConfirm.interactable = v;
        if (btnCancel) btnCancel.interactable = v;
    }
}
