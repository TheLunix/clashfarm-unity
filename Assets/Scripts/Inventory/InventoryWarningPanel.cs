using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryWarningPanel : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Button buttonConfirm;
    public Button buttonCancel;

    private Action _onConfirm;
    private Action _onCancel;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(string message, Action onConfirm, Action onCancel = null)
    {
        _onConfirm = onConfirm;
        _onCancel  = onCancel;

        messageText.text = message;

        buttonConfirm.onClick.RemoveAllListeners();
        buttonCancel.onClick.RemoveAllListeners();

        buttonConfirm.onClick.AddListener(() =>
        {
            _onConfirm?.Invoke();
            Hide();
        });

        buttonCancel.onClick.AddListener(() =>
        {
            _onCancel?.Invoke();
            Hide();
        });

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
