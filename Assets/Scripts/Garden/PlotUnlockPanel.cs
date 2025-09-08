using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public sealed class PlotUnlockPanel : MonoBehaviour
{
    [Header("Root & Content")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text   title;
    [SerializeField] private TMP_Text   priceText;

    [Header("Buttons")]
    [SerializeField] private Button     btnConfirm;
    [SerializeField] private Button     btnCancel;

    [Header("Inline error (shows for N seconds)")]
    [SerializeField] private GameObject errorPanel;   // 👈 перетягни сюди невелику панель/рядок помилки
    [SerializeField] private TMP_Text   errorText;    // 👈 текст усередині errorPanel
    [SerializeField] private float      errorDuration = 5f;

    private Action _onConfirm;
    private Coroutine _hideErrorCo;

    void Awake()
    {
        if (btnConfirm != null)
            btnConfirm.onClick.AddListener(() => { _onConfirm?.Invoke(); /* НЕ закриваємо тут */ });

        if (btnCancel  != null)
            btnCancel.onClick.AddListener(Close);

        Close();
        HideErrorImmediate();
    }

    public void Open(int uiSlotIndex, int price, Action onConfirm)
    {
        
        Debug.Log("6");
        _onConfirm = onConfirm;

        // Номер для відображення: завжди наступна грядка (UnlockedPlots + 1)
        int displaySlot = uiSlotIndex; // дефолт якщо кешу нема
        var cache = GardenStateCache.I;
        if (cache != null)
        {
            // у тебе UnlockedSlots — кількість уже відкритих; наступна = +1
            displaySlot = Mathf.Clamp(cache.UnlockedSlots + 1, 1, 12);
        }

        if (title)     title.text     = $"Відкрити грядку №{displaySlot}?";
        if (priceText) priceText.text = $"Ви впевнені, що хочете купити нову грядку за <sprite=1> {price}?";

        HideErrorImmediate();

        if (root) root.SetActive(true);
        else gameObject.SetActive(true);
        Debug.Log("7");
    }

    public void Close()
    {
        if (root) root.SetActive(false);
        else gameObject.SetActive(false);

        _onConfirm = null;
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
}
