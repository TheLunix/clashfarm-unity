using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ClashFarm.Garden
{
    public class SpriteCacheDebugPanel : MonoBehaviour
    {
        [Header("UI (будь-яке з полів може бути null)")]
        public Text reportText;
        public TMP_Text reportTMP;
        public Button btnRefresh;
        public Button btnClearMem;
        public Button btnOpenFolder;
        public Button btnPrefetchAll;  // дисковий префетч усіх іконок
        public TMP_InputField capacityTMP; // новий ліміт RAM (кількість спрайтів)
        public InputField capacityIF;
        public Button btnClearDisk;
        public Button btnWarmRam;
        public TMP_InputField warmCountTMP;
        public InputField warmCountIF;

        [Header("Prefetch")]
        public int prefetchParallel = 3;
        public int prefetchTimeoutMs = 6000;

        void Start()
        {
            if (btnRefresh) btnRefresh.onClick.AddListener(UpdateReport);
            if (btnClearMem) btnClearMem.onClick.AddListener(() => { RemoteSpriteCache.ClearMemory(); UpdateReport(); });
            if (btnOpenFolder) btnOpenFolder.onClick.AddListener(OpenFolder);
            if (btnPrefetchAll) btnPrefetchAll.onClick.AddListener(() => StartCoroutine(PrefetchAllCo()));
            if (btnClearDisk) btnClearDisk.onClick.AddListener(() => { RemoteSpriteCache.ClearDisk(); UpdateReport(); });
            if (btnWarmRam) btnWarmRam.onClick.AddListener(() => StartCoroutine(WarmTopRamCo()));
            if (warmCountTMP) warmCountTMP.onEndEdit.AddListener(_ => { /* no-op */ });
            if (warmCountIF) warmCountIF.onEndEdit.AddListener(_ => { /* no-op */ });

            if (capacityTMP) capacityTMP.onEndEdit.AddListener(OnCapacityChanged);
            if (capacityIF) capacityIF.onEndEdit.AddListener(OnCapacityChanged);

            UpdateReport();
        }

        void OnCapacityChanged(string s)
        {
            if (int.TryParse(s, out var n))
            {
                RemoteSpriteCache.SetMaxInMemory(n);
                UpdateReport();
            }
        }

        void UpdateReport()
        {
            var txt = RemoteSpriteCache.DebugReport();

            // Диск: файли + розмір
            RemoteSpriteCache.GetDiskStats(out var files, out var bytes);
            float mb = bytes / (1024f * 1024f);
            txt += $"\nDisk files: {files} (~{mb:0.0} MB)";

            if (reportTMP) reportTMP.text = txt;
            if (reportText) reportText.text = txt;

            if (capacityTMP) capacityTMP.text = RemoteSpriteCache.MaxInMemory.ToString();
            if (capacityIF) capacityIF.text = RemoteSpriteCache.MaxInMemory.ToString();
        }

        void OpenFolder()
        {
            var path = RemoteSpriteCache.GetDiskDir();
            Application.OpenURL("file://" + path);
        }

        IEnumerator PrefetchAllCo()
        {
            // Беремо повний каталог (без залежності від стану PlantSelectionPanel)
            var task = GardenApi.GetPlantsAsync();
            while (!task.IsCompleted) yield return null;

            var plants = task.Result;
            if (plants == null || plants.Count == 0) yield break;

            var keys = new HashSet<string>();
            foreach (var p in plants)
            {
                if (p.isActive == 0) continue;
                if (!string.IsNullOrEmpty(p.iconSeed)) keys.Add(p.iconSeed);
                if (!string.IsNullOrEmpty(p.iconPlant)) keys.Add(p.iconPlant);
                if (!string.IsNullOrEmpty(p.iconGrown)) keys.Add(p.iconGrown);
            }

            var prefetchTask = RemoteSpriteCache.PrefetchToDiskOnly(keys, prefetchParallel, prefetchTimeoutMs);
            while (!prefetchTask.IsCompleted) yield return null;

            UpdateReport();
        }
        IEnumerator WarmTopRamCo()
        {
            int count = 12; // дефолт
            if (warmCountTMP && int.TryParse(warmCountTMP.text, out var n1)) count = Mathf.Max(1, n1);
            if (warmCountIF && int.TryParse(warmCountIF.text, out var n2)) count = Mathf.Max(1, n2);

            // 1) Завантажуємо каталог
            var task = GardenApi.GetPlantsAsync();
            while (!task.IsCompleted) yield return null;
            var plants = task.Result;
            if (plants == null || plants.Count == 0) yield break;

            // 2) Формуємо ключі для підігріву (grown — найінформативніша іконка)
            var keys = new List<string>();
            foreach (var p in plants)
            {
                if (!string.IsNullOrEmpty(p.iconGrown)) keys.Add(p.iconGrown);
                if (keys.Count >= count) break;
            }

            // 3) Викликаємо підігрів у пам'ять (без RAM-піків, поважає MaxInMemory)
            var warmTask = RemoteSpriteCache.WarmToMemory(keys, count, maxParallel: 3, softTimeoutMs: 2000);
            while (!warmTask.IsCompleted) yield return null;

            UpdateReport();
        }
    }
}
