using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PlayerSubpanel : MonoBehaviour
{
    [SerializeField] private bool startHidden = true;
    private CanvasGroup cg;

    void Awake()
    {
        EnsureCg();

        // Якщо панель вимкнена в інспекторі — Awake не викличеться до активації,
        // але Show() сам гарантує ініціалізацію.
        if (!gameObject.activeSelf) return;

        if (startHidden) HideImmediate();
        else Show();
    }

    private void EnsureCg()
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void Show()
    {
        EnsureCg();

        // Активуємо GO, потім одразу виставляємо CG
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // Піднімаємо вище сусідів (у межах свого контейнера)
        transform.SetAsLastSibling();

        // Миттєво перераховуємо лейаут
        Canvas.ForceUpdateCanvases();
        var rt = transform as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    public void Hide()
    {
        EnsureCg();

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    public void HideImmediate() => Hide();
}
