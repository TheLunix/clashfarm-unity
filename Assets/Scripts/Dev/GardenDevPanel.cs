using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ClashFarm.Garden
{
    // Легка дев-панель QA: кнопки зсуву часу та індикатор поточного offset
    public sealed class GardenDevPanel : MonoBehaviour
    {
        [Header("UI (будь-яке поле може бути null)")]
        public Button btnPlus1m;
        public Button btnPlus10m;
        public Button btnPlus1h;
        public Button btnMinus1m;
        public Button btnReset;
        public TMP_Text  lblInfoTMP;
        public Text      lblInfo;

        void Awake()
        {
            // Прив’язуємо кнопки
            if (btnPlus1m)  btnPlus1m.onClick.AddListener(() => ShiftSeconds( 60));
            if (btnPlus10m) btnPlus10m.onClick.AddListener(() => ShiftSeconds(600));
            if (btnPlus1h)  btnPlus1h.onClick.AddListener(() => ShiftSeconds(3600));
            if (btnMinus1m) btnMinus1m.onClick.AddListener(() => ShiftSeconds(-60));
            if (btnReset)   btnReset.onClick.AddListener(() => SetSeconds(0));

            UpdateLabel();
        }

        [ContextMenu("Shift +10m")]
        public void DebugShift10m() => ShiftSeconds(600);

        void ShiftSeconds(int delta)
        {
            #if UNITY_EDITOR
            if (GardenSession.I != null)
                GardenSession.I.DebugTimeOffsetSec += delta;
            ApplyAllNow();
            UpdateLabel();
            #endif
        }

        void SetSeconds(int value)
        {
            #if UNITY_EDITOR
            if (GardenSession.I != null)
                GardenSession.I.DebugTimeOffsetSec = value;
            ApplyAllNow();
            UpdateLabel();
            #endif
        }

        void ApplyAllNow()
        {
            // Спроба викликати ApplyAll() навіть якщо він не public
            var gc = GardenController.I;
            if (gc != null)
            {
                var m = gc.GetType().GetMethod(
                    "ApplyAll",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                );
                if (m != null)
                {
                    m.Invoke(gc, null);
                    return; // повний рефреш зроблено
                }
            }

            // Фолбек: перерахувати тільки таймери, щоб миттєво побачити зсув часу
            if (GardenSession.I != null)
            {
                long now = GardenSession.I.NowServerLikeMs();
                var views = UnityEngine.Object.FindObjectsByType<GardenPlotView>(
                    UnityEngine.FindObjectsInactive.Include,
                    UnityEngine.FindObjectsSortMode.None
                );
                for (int i = 0; i < views.Length; i++) views[i].UpdateTimer(now);
            }
        }

        void UpdateLabel()
        {
            #if UNITY_EDITOR
            int sec = (GardenSession.I != null) ? GardenSession.I.DebugTimeOffsetSec : 0;
            string txt = (sec == 0) ? "Time offset: 0s"
                                    : $"Time offset: {(sec>0?"+":"")}{sec}s  (~{sec/60}m)";
            if (lblInfoTMP) lblInfoTMP.text = txt;
            if (lblInfo)    lblInfo.text    = txt;
            #else
            if (lblInfoTMP) lblInfoTMP.text = "";
            if (lblInfo)    lblInfo.text    = "";
            #endif
        }
    }
}
