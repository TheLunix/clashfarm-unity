namespace ClashFarm.Garden
{
    using System;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public sealed class PlantActionPanel : MonoBehaviour
    {
        public enum ActionMode { None, Water, Weed, Harvest }

        [Header("Root & overlay")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button backdropButton;
        [SerializeField] private Image backdropImage;
        [SerializeField] private RectTransform card;

        [Header("UI (card)")]
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionLabel;

        [Header("FX")]
        [SerializeField] private float overlayTargetAlpha = 0.6f;
        [SerializeField] private float fadeDur = 0.15f;
        [SerializeField] private float popDur = 0.12f;
        [SerializeField] private Vector3 cardFromScale = new Vector3(0.96f, 0.96f, 1f);

        PlotModel _model;
        PlantInfo _plant;
        Action _onWater, _onWeed, _onHarvest, _onClose;
        ActionMode _mode;
        Coroutine _fadeCo, _popCo;

        public void Open(
            PlotModel model, PlantInfo plant,
            Action onWater, Action onWeed, Action onHarvest,
            Action onClose)
        {
            _model = model; _plant = plant;
            _onWater = onWater; _onWeed = onWeed; _onHarvest = onHarvest; _onClose = onClose;

            if (title) title.text = "Ой-йой!";
            DecideMode();
            RefreshCTA();

            if (backdropButton)
            {
                backdropButton.onClick.RemoveAllListeners();
                backdropButton.onClick.AddListener(Close); // тап по затемненню
            }

            if (root) root.SetActive(true); else gameObject.SetActive(true);

            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(Fade(0f, overlayTargetAlpha, fadeDur));

            if (card != null)
            {
                if (_popCo != null) StopCoroutine(_popCo);
                _popCo = StartCoroutine(PopIn(cardFromScale, Vector3.one, popDur));
            }
        }

        public void Close()
        {
            StartCoroutine(CloseRoutine());
        }

        void DecideMode()
        {
            if (_model.stage >= 3) _mode = ActionMode.Harvest;
            else if (_model.hasWeeds) _mode = ActionMode.Weed;
            else if (_model.needsWater) _mode = ActionMode.Water;
            else _mode = ActionMode.None;
        }

        void RefreshCTA()
        {
            string desc = "", btn = "OK";
            Action handler = Close;

            switch (_mode)
            {
                case ActionMode.Water:
                    desc = "Ваші рослини потрібно вчасно поливати, так вони будуть рости швидше!";
                    btn = "Полити";
                    handler = _onWater;
                    break;

                case ActionMode.Weed:
                    desc = "Ой! Твої вороги напевно заздрять тобі і підкинули насіння бур'янів на грядку. Прополи її швидше!";
                    btn = "Прополоти";
                    handler = _onWeed ?? Close;
                    break;

                case ActionMode.Harvest:
                    long price = _plant != null ? _plant.SellPrice : 0;
                    desc = $"Нарешті все дозріло! Збери врожай щоб отримати <sprite=0> {FormatMoney(price)}";
                    btn = "Зібрати";
                    handler = _onHarvest;
                    break;

                default:
                    desc = "Наразі немає доступних дій.";
                    btn = "Закрити";
                    handler = Close;
                    break;
            }

            if (descriptionText) descriptionText.text = desc;
            if (actionButton)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => handler?.Invoke());
            }
            if (actionLabel) actionLabel.text = btn;
        }

        System.Collections.IEnumerator Fade(float from, float to, float dur)
        {
            if (!backdropImage) yield break;
            var c = backdropImage.color;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                backdropImage.color = c;
                yield return null;
            }
            c.a = to; backdropImage.color = c;
        }

        System.Collections.IEnumerator PopIn(Vector3 from, Vector3 to, float dur)
        {
            if (!card) yield break;
            card.localScale = from;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f);
                card.localScale = Vector3.LerpUnclamped(from, to, e);
                yield return null;
            }
            card.localScale = to;
        }

        System.Collections.IEnumerator CloseRoutine()
        {
            float wait = Mathf.Max(fadeDur, popDur);
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(Fade(backdropImage ? backdropImage.color.a : overlayTargetAlpha, 0f, fadeDur));
            if (_popCo != null) StopCoroutine(_popCo);
            if (card) _popCo = StartCoroutine(PopIn(card.localScale, cardFromScale, popDur));

            float t = 0f; while (t < wait) { t += Time.unscaledDeltaTime; yield return null; }
            if (root) root.SetActive(false); else gameObject.SetActive(false);
            _onClose?.Invoke();
        }

        string FormatMoney(long v)
        {
            if (v >= 1_000_000) return (v / 1_000_000f).ToString("0.##") + "M";
            if (v >= 1000) return v.ToString("#,0").Replace(',', ' ');
            return v.ToString();
        }
        public void SetInteractable(bool v)
        {
            if (actionButton) actionButton.interactable = v;
            if (backdropButton) backdropButton.interactable = v;
        }
    }
}