using System.Collections;
using System.Threading.Tasks;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ClashFarm.Garden
{
    public sealed class GardenPlotView : MonoBehaviour
    {
        [Header("Bind")]
        public int SlotIndex;
        public Image bg;                 // фон пустої/активної грядки
        public Image crop;               // спрайт стадії
        public GameObject weedGO;        // візуал бур’янів
        public GameObject infoIconGO;    // правий верхній
        public Image infoIconImg;        // крапля/спрей
        public Sprite iconWater;         // присвой у інспекторі
        public Sprite iconWeed;          // присвой у інспекторі
        public GameObject timePanel;
        public TextMeshProUGUI timeText;
        public GameObject lockOverlay;
        public Button mainButton;

        [Header("Fallback Sprites (локальні)")]
        public Sprite emptySprite;
        public Sprite seedSprite;
        public Sprite plantSprite;
        public Sprite grownSprite;
        [Header("Water Tint")]
        public bool waterTintEnabled = true;
        [Tooltip("Колір для NEED WATER (ст.1–2)")]
        public Color waterNeedsColor = Color.white; // #FFFFFF
        [Tooltip("Колір для вже политого (ст.1–2)")]
        public Color waterDoneColor = new Color32(0xBA, 0xBA, 0xBA, 0xFF); // #BABABA

        Color _bgDefaultColor;
        GardenSession.PlotState _last;
        long _lastNow;
        string _lastIconKey;
        int _lastRemainSec = -1;
        bool _lastTimerVisible;

        void Awake()
        {
            if (bg) _bgDefaultColor = bg.color;
            if (mainButton != null)
                mainButton.onClick.AddListener(() => GardenController.I?.OnPlotClicked(SlotIndex));
        }

        public void SetState(GardenSession.PlotState s, long nowMs)
        {
            _last = s; _lastNow = nowMs;

            if (!s.Unlocked)
            {
                // locked: показуємо "зарослу грядку"
                if (lockOverlay) lockOverlay.SetActive(true);   // у тебе це той же GO, що weedGO
                if (weedGO) weedGO.SetActive(true);             // ← важливо: не вимикати
                if (crop) { crop.enabled = false; crop.sprite = null; }
                if (infoIconGO) infoIconGO.SetActive(false);
                if (timePanel) timePanel.SetActive(false);
                return;
            }

            // далі — логіка для відкритих слотів
            if (lockOverlay) lockOverlay.SetActive(false);

            // Стадійний спрайт
            if (crop)
            {
                Sprite local = s.Stage switch { 1 => seedSprite, 2 => plantSprite, 3 => grownSprite, _ => emptySprite };
                crop.sprite = local;
                crop.enabled = s.Stage > 0 && local != null;
            }

            // Бур’яни/вода — пріоритет бур’ян > вода
            if (weedGO) weedGO.SetActive(s.Weeds && s.Stage < 3);
            if (infoIconGO && infoIconImg)
            {
                if (s.Weeds && s.Stage < 3) { infoIconGO.SetActive(true); infoIconImg.sprite = iconWeed; }
                else if (s.NeedWater) { infoIconGO.SetActive(true); infoIconImg.sprite = iconWater; }
                else infoIconGO.SetActive(false);
            }

            // Таймер до повного дозрівання (показ/початковий текст, далі — UpdateTimer())
            bool showTimer = s.OnPlanted && s.TimeEndGrowthMs > 0 && s.Stage < 3;
            if (timePanel && timePanel.activeSelf != showTimer) timePanel.SetActive(showTimer);
            _lastTimerVisible = showTimer;
            _lastRemainSec = -1; // змусимо UpdateTimer() перемалювати текст
            if (showTimer && timeText)
            {
                long remain = s.TimeEndGrowthMs - nowMs;
                if (remain < 0) remain = 0;
                _lastRemainSec = Mathf.CeilToInt(remain / 1000f);
                FormatRemain(_lastRemainSec);
            }
            // --- Візуальний полив: тінт фону на стадіях 1–2 ---
            if (waterTintEnabled && bg)
            {
                if (s.Stage == 1 || s.Stage == 2)
                {
                    // якщо потрібен полив — білий; якщо вже полито — сірий
                    var target = s.NeedWater ? waterNeedsColor : waterDoneColor;

                    // (опційно) якщо є бур’ян, можна примусово показувати "потрібно" як білий:
                    // if (s.Weeds) target = waterNeedsColor;

                    if (bg.color != target) bg.color = target;
                }
                else
                {
                    // для Stage 0 і 3 — повертаємо дефолтний колір
                    if (bg.color != _bgDefaultColor) bg.color = _bgDefaultColor;
                }
            }
#if REMOTE_ICONS || true // завжди дозволяємо, бо кеш вже є
            if (s.Stage > 0)
            {
                if (!PlantCatalogCache.IsReady)
                    StartCoroutine(WaitCatalogThenLoad(s.PlantedID, s.Stage));
                else
                    StartCoroutine(LoadRemoteStageSprite(s.PlantedID, s.Stage));
            }
#endif

        }
        public void UpdateTimer(long nowMs)
        {
            if (_last == null) return;

            bool show = _last.OnPlanted && _last.TimeEndGrowthMs > 0 && _last.Stage < 3;
            if (timePanel && timePanel.activeSelf != show) timePanel.SetActive(show);
            _lastTimerVisible = show;

            if (!show || timeText == null) return;

            long remainMs = _last.TimeEndGrowthMs - nowMs;
            if (remainMs < 0) remainMs = 0;
            int sec = Mathf.CeilToInt(remainMs / 1000f);

            if (sec != _lastRemainSec)
            {
                _lastRemainSec = sec;
                FormatRemain(_lastRemainSec);
            }
        }
        [Header("Click / Action Feedback")]
        public bool clickShake = true;
        public float shakeDuration = 0.2f;
        public float shakeAmplitude = 8f;
        Coroutine _shakeCo;

        public void ShakeOnce()
        {
            if (!clickShake || bg == null) return;
            if (_shakeCo != null) StopCoroutine(_shakeCo);
            _shakeCo = StartCoroutine(ShakeCo());
        }

        System.Collections.IEnumerator ShakeCo()
        {
            var rt = bg.rectTransform;
            Vector2 start = rt.anchoredPosition;
            float t = 0f;
            // випадковий напрямок
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.right;

            while (t < shakeDuration)
            {
                t += Time.deltaTime;
                float k = 1f - (t / shakeDuration); // затухання
                float s = Mathf.Sin(t * 40f) * k * shakeAmplitude;
                rt.anchoredPosition = start + dir * s;
                yield return null;
            }
            rt.anchoredPosition = start;
            _shakeCo = null;
        }
        [Header("Click Feedback")]
        public bool clickFlash = true;
        public Color flashColor = new Color32(0xFF, 0xF1, 0xA6, 0xFF); // м'яке жовте підсвічування
        public float flashDuration = 0.25f;
        Coroutine _flashCo;

        public void FlashOnce()
        {
            if (!clickFlash || bg == null) return;
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(FlashCo());
        }

        IEnumerator FlashCo()
        {
            // збережемо поточний колір, щоб повернути після ефекту
            Color from = bg.color;
            Color to = flashColor;

            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / flashDuration; bg.color = Color.Lerp(from, to, t); yield return null; }

            t = 0f;
            while (t < 1f) { t += Time.deltaTime / flashDuration; bg.color = Color.Lerp(to, from, t); yield return null; }

            // на випадок зміни стейту під час фіда — просто перестворювати не будемо:
            // SetState поверне все як треба, а для тіну — відновимо логікою нижче
            ReapplyTint();
        }

        void ReapplyTint()
        {
            if (!bg) return;
            if (waterTintEnabled && (_last != null) && (_last.Stage == 1 || _last.Stage == 2))
            {
                var target = _last.NeedWater ? waterNeedsColor : waterDoneColor;
                if (bg.color != target) bg.color = target;
            }
            else
            {
                if (bg.color != _bgDefaultColor) bg.color = _bgDefaultColor;
            }
        }
        void FormatRemain(int sec)
        {
            if (timeText == null) return;

            if (sec <= 0)
            {
                // фолбек, якщо ключа "миттєво" нема — можна зробити окремий key, якщо захочеш
                StartCoroutine(LocalizeFormat(timeText, "UI", "time.instant"));
                return;
            }

            var t = TimeSpan.FromSeconds(sec);

            if (t.TotalHours >= 24)
            {
                int days = (int)Math.Floor(t.TotalHours / 24);
                int hours = (int)Math.Ceiling(t.TotalHours - (days * 24));
                StartCoroutine(LocalizeFormat(timeText, "UI", "time.dh", days, hours));
                return;
            }
            if (t.TotalHours >= 1)
            {
                int hours = (int)Math.Floor(t.TotalHours);
                StartCoroutine(LocalizeFormat(timeText, "UI", "time.hm", hours, t.Minutes));
                return;
            }
            if (t.TotalMinutes >= 1)
            {
                StartCoroutine(LocalizeFormat(timeText, "UI", "time.ms", t.Minutes, t.Seconds));
                return;
            }

            StartCoroutine(LocalizeFormat(timeText, "UI", "time.s", t.Seconds));
        }

        // ====== (опціонально) Завантаження іконок з CDN пізніше ======
        // TODO: коли підключиш RemoteSpriteCache — додай тут метод:
        // public async Task LoadRemoteStageSprite(int plantedId, byte stage) { ... }

        System.Collections.IEnumerator LoadRemoteStageSprite(int plantedId, byte stage)
        {
            if (plantedId <= 0 || crop == null) yield break;
            if (!PlantCatalogCache.TryGet(plantedId, out var plant)) yield break;

            string key = stage switch
            {
                1 => plant.iconSeed,
                2 => plant.iconPlant,
                3 => plant.iconGrown,
                _ => null
            };
            if (string.IsNullOrEmpty(key)) yield break;
            if (_lastIconKey == key && crop.sprite != null) yield break; // вже є

            _lastIconKey = key;
            var task = RemoteSpriteCache.GetSpriteAsync(key);
            while (!task.IsCompleted) yield return null;
            var sp = task.Result;
            if (_last != null && _last.PlantedID == plantedId && _last.Stage == stage)
            {
                crop.sprite = sp;
                crop.enabled = (sp != null);
            }
        }
        System.Collections.IEnumerator WaitCatalogThenLoad(int plantedId, byte stage)
        {
            while (!PlantCatalogCache.IsReady) yield return null;
            yield return LoadRemoteStageSprite(plantedId, stage);
        }
        
        private System.Collections.IEnumerator LocalizeFormat(TMP_Text target, string table, string key, params object[] args)
        {
            if (target == null || string.IsNullOrEmpty(key)) yield break;
            var ls = new LocalizedString(table, key);
            if (args != null && args.Length > 0) ls.Arguments = args;

            var handle = ls.GetLocalizedStringAsync();
            yield return handle;
            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && target != null)
                target.text = handle.Result;
        }
    }
}
