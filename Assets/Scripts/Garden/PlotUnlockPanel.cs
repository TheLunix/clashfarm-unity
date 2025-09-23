namespace ClashFarm.Garden
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System;
    using System.Collections;
    using System.Threading.Tasks;

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

        private Func<Task> _onConfirm;
        private Coroutine _hideErrorCo;

        void Awake()
        {
            if (btnConfirm != null)
                btnConfirm.onClick.AddListener(OnConfirmClick);

            if (btnCancel != null)
                btnCancel.onClick.AddListener(() => { Close(); });

            //Close();
            HideErrorImmediate();
        }
        private async void OnConfirmClick()
        {
            if (_onConfirm == null) return;
            SetInteractable(false);
            try { await _onConfirm(); }      // контролер сам вирішить: закривати панель чи показати помилку
            finally { SetInteractable(true); }
        }
        public void Open(int uiSlotIndexClicked, int price, Func<System.Threading.Tasks.Task> onConfirm)
        {
            _onConfirm = onConfirm;

            SetInteractable(true);
            HideErrorImmediate();

            // Показуємо номер наступної грядки з даних сесії (або дефолт 4)
            int nextUi = 4;
            if (ClashFarm.Garden.GardenSession.I != null)
            {
                int unlocked = 0;
                foreach (var p in ClashFarm.Garden.GardenSession.I.Plots)
                    if (p != null && p.Unlocked) unlocked++;
                nextUi = Mathf.Clamp(unlocked + 1, 1, 12);
            }
            if (ClashFarm.Garden.GardenSession.I != null)
            {
                int unlocked = 0;
                foreach (var p in ClashFarm.Garden.GardenSession.I.Plots) if (p != null && p.Unlocked) unlocked++;
                nextUi = Mathf.Clamp(unlocked + 1, 1, 12);
            }

            if (title) title.text = $"Відкрити грядку №{nextUi}?";
            if (priceText) priceText.text = $"Ціна: <sprite=1> {price}";
            // ➊ Визначаємо, чи можна собі дозволити покупку
            int gold = (PlayerSession.I != null && PlayerSession.I.Data != null) ? PlayerSession.I.Data.playergold : 0;
            bool canAfford = gold >= price;

            // ➋ Підсвічуємо ціну, якщо не вистачає
            if (priceText)
                priceText.text = canAfford ? $"Ціна: <sprite=1> {price}" : $"Ціна: <sprite=1> <color=#FF5555>{price}</color>";

            // ➌ Вимикаємо "Підтвердити", якщо не вистачає золота
            if (btnConfirm) btnConfirm.interactable = canAfford;

            // (не обов’язково) Якщо грядок уже максимум — теж блокуємо
            int maxSlots = 12;
            if (title && nextUi > maxSlots)
            {
                title.text = "Максимум грядок відкрито";
                if (btnConfirm) btnConfirm.interactable = false;
            }
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
}