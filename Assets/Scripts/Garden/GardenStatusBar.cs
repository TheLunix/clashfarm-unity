using UnityEngine;
using TMPro;

namespace ClashFarm.Garden
{
    public sealed class GardenStatusBar : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text text;
        public CanvasGroup cg;

        [Header("Timing")]
        public float showSeconds = 2.5f;
        public float fade = 0.15f;

        float _hideAt = -1f;

        void Awake()
        {
            if (!cg) cg = GetComponent<CanvasGroup>();
            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
        }

        void Update()
        {
            if (_hideAt > 0f && Time.unscaledTime >= _hideAt)
            {
                _hideAt = -1f;
                StopAllCoroutines();
                StartCoroutine(FadeTo(0f));
            }
        }

        public void Show(string msg, float seconds = -1f)
        {
            if (text) text.text = msg;
            StopAllCoroutines();
            StartCoroutine(FadeTo(1f));
            _hideAt = Time.unscaledTime + ((seconds > 0f) ? seconds : showSeconds);
        }

        public void ShowPersistent(string msg)
        {
            if (text) text.text = msg;
            _hideAt = -1f;
            StopAllCoroutines();
            StartCoroutine(FadeTo(1f));
        }

        public void HideImmediate()
        {
            _hideAt = -1f;
            StopAllCoroutines();
            if (cg) cg.alpha = 0f;
        }

        System.Collections.IEnumerator FadeTo(float target)
        {
            if (!cg) yield break;
            float start = cg.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fade);
                cg.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }
            cg.alpha = target;
        }
    }
}
