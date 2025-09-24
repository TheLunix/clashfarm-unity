namespace ClashFarm.Garden
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;

    public sealed class PlantSelectionPanel : MonoBehaviour
    {
        [Header("UI wiring")]
        [SerializeField] private GameObject root;                    // контейнер панелі
        [SerializeField] private Transform content;                  // ScrollView/Viewport/Content
        [SerializeField] private PlantOptionItemView itemPrefab;     // префаб картки
        [SerializeField] private GameObject infobar;                 // інфобар (ховаємо при відкритті)
        [SerializeField] private Button closeButton;                 // 👈 кнопка закриття панелі

        [Header("Behaviour")]
        [SerializeField] private bool buildOnEnableIfDataReady = false;

        [Header("Reveal")]
        [SerializeField] private CanvasGroup cg;       // CanvasGroup на корені панелі (необов'язково, але бажано)
        [SerializeField] private int warmTopCount = 8; // скільки позицій підігрівати в пам'ять перед показом
        [SerializeField] private int warmTimeoutMs = 1200;

        // кеш вхідних даних
        List<PlantInfo> _all;
        int _playerLevel;
        Action<PlantInfo> _onPlant;
        readonly Stack<PlantOptionItemView> _pool = new();
        bool _prewarmed = false;  // панель і іконки прогріті на старті
        int _buildVersion = 0;                 // версія останнього BuildList
        static int _buildFrame, _buildCount;   // дроселінг інстансингу
        const int _buildMaxPerFrame = 5;
        bool IsOpen => root != null && root.activeInHierarchy;

        void OnEnable()
        {
            // кнопка закриття
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            // підписка на зміну сесії
            if (PlayerSession.I != null)
            {
                _playerLevel = ResolvePlayerLevelFallback(_playerLevel);
                PlayerSession.I.OnChanged += HandlePlayerSessionChanged;
            }

            if (!buildOnEnableIfDataReady) return;
            if (root != null && !root.activeSelf) return; // якщо root інший об’єкт
            if (_all != null && _all.Count > 0) BuildList();
        }

        void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();

            if (PlayerSession.I != null)
                PlayerSession.I.OnChanged -= HandlePlayerSessionChanged;
        }

        // Викликається контролером перед показом
        public void SetData(List<PlantInfo> all, int playerLevel, Action<PlantInfo> onPlant)
        {
            _all = all;
            _onPlant = onPlant;

            // якщо є PlayerSession — беремо звідти рівень, інакше переданий
            _playerLevel = ResolvePlayerLevelFallback(playerLevel);
        }
        public void Show()
        {
            if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            StartCoroutine(ShowCo());
        }

        public void Close()
        {
            // (за бажанням) миттєво прибрати інтерактивність/альфу
            if (cg != null) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }

            if (root != null) root.SetActive(false);
            if (infobar != null) infobar.SetActive(true);

            // ← головне: повністю ховаємо GO панелі
            gameObject.SetActive(false);
        }

        // --- реакція на зміну даних сесії ---
        void HandlePlayerSessionChanged()
        {
            int newLvl = ResolvePlayerLevelFallback(_playerLevel);
            if (newLvl != _playerLevel)
            {
                _playerLevel = newLvl;
                if (IsOpen && _all != null && _all.Count > 0)
                    BuildList();
            }
        }

        int ResolvePlayerLevelFallback(int fallback)
        {
            var s = PlayerSession.I;
            if (s != null && s.Data != null)
                return Mathf.Max(1, s.Data.playerlvl);
            return Mathf.Max(1, fallback);
        }

        // --- побудова списку ---
        void BuildList()
        {
            if (content == null || itemPrefab == null)
            {
                Debug.LogError("[PlantSelectionPanel] content/itemPrefab не призначено", this);
                return;
            }
            if (_all == null)
            {
                Debug.LogWarning("[PlantSelectionPanel] Дані ще не передані (SetData не викликано)", this);
                ClearContent();
                return;
            }

            ClearContent();

            // ↑ нове: фіксуємо “версію” цього білду
            _buildVersion++;
            int version = _buildVersion;

            // 0) фільтруємо неактивні
            var source = _all.Where(p => p.IsActive).ToList();

            // 1) саме (playerLevel + 1); якщо немає — найближча більша
            var exactNext = source.FirstOrDefault(p => p.UnlockLevel == _playerLevel + 1);
            var fallbackNext = source.Where(p => p.UnlockLevel > _playerLevel)
                                    .OrderBy(p => p.UnlockLevel)
                                    .FirstOrDefault();
            var nextLocked = exactNext ?? fallbackNext;

            // 2) доступні зараз (≤ рівня), від найвищого до найнижчого
            var available = source.Where(p => p.UnlockLevel <= _playerLevel)
                                .OrderByDescending(p => p.UnlockLevel);

            // --- Вивід ---
            if (nextLocked != null)
                StartCoroutine(CoAddItem(nextLocked, unlocked: false, version));   // тільки ОДНА недоступна зверху

            foreach (var p in available)
                StartCoroutine(CoAddItem(p, unlocked: true, version));
        }

        void ClearContent()
        {
#if UNITY_EDITOR
            if (Selection.activeTransform != null && content != null && Selection.activeTransform.IsChildOf(content))
                Selection.activeObject = null;
#endif
            if (content == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                child.SetActive(false);
                child.transform.SetParent(transform, false); // тимчасово під панель (не Content)
                var view = child.GetComponent<PlantOptionItemView>();
                if (view != null) _pool.Push(view);
            }
        }

        // попередня побудова (щоб UI миттєво відкривався)
        public void Prewarm(List<PlantInfo> all, int playerLevel, Action<PlantInfo> onPlant)
        {
            SetData(all, playerLevel, onPlant);
            BuildList();

            // Форсимо лейаут, навіть якщо root неактивний
            if (content is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
        public void SetInteractable(bool v)
        {
            if (closeButton) closeButton.interactable = v;
            if (content != null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    var btn = content.GetChild(i).GetComponentInChildren<UnityEngine.UI.Button>(true);
                    if (btn) btn.interactable = v;
                }
            }
        }
        System.Collections.IEnumerator PrewarmIconsCo()
        {
            // зберемо ключі іконок, які будемо показувати в картках (grown найінформативніший)
            var keys = new HashSet<string>();
            if (_all != null)
            {
                foreach (var p in _all)
                {
                    if (!p.IsActive) continue;
                    if (!string.IsNullOrEmpty(p.IconGrown)) keys.Add(p.IconGrown);
                }
            }

            if (keys.Count == 0) yield break;

            var task = RemoteSpriteCache.PrefetchToDiskOnly(keys, maxParallel: 3, softTimeoutMs: 1500);
            while (!task.IsCompleted) yield return null;

            // нічого не перемальовуємо спеціально: самі картки при Bind беруть із диска
        }
        System.Collections.IEnumerator CoAddItem(PlantInfo data, bool unlocked, int version)
        {
            // якщо під час очікування стартував новий білд — не додаємо нічого
            if (version != _buildVersion) yield break;

            // Дроселінг: не більш як N елементів за кадр
            while (true)
            {
                if (Time.frameCount != _buildFrame) { _buildFrame = Time.frameCount; _buildCount = 0; }
                if (_buildCount < _buildMaxPerFrame) { _buildCount++; break; }
                yield return null;
                if (version != _buildVersion) yield break; // підстраховка
            }

            if (version != _buildVersion) yield break;

            PlantOptionItemView view;
            if (_pool.Count > 0)
            {
                view = _pool.Pop();
                view.transform.SetParent(content, false);
                view.gameObject.SetActive(true);
            }
            else
            {
                view = Instantiate(itemPrefab, content);
            }

            if (version != _buildVersion)  // ще одна перевірка на випадок rebuild посередині
            {
                // повернемо у пул, якщо інстанс уже створили
                view.gameObject.SetActive(false);
                view.transform.SetParent(transform, false);
                _pool.Push(view);
                yield break;
            }

            view.Bind(data, unlocked, _onPlant);
        }
        System.Collections.IEnumerator ShowCo()
        {
            // 0) активуємо root, але робимо його невидимим / неінтерактивним
            if (root != null) root.SetActive(true);
            if (infobar != null) infobar.SetActive(false);
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            SetInteractable(false);

            // 1) ШВИДКИЙ підігрів у пам’ять Топ-N іконок (щоб картки одразу мали спрайти)
            if (!_prewarmed)
                StartCoroutine(PrewarmTopIconsToMemoryCo(warmTopCount, warmTimeoutMs)); // фоном, не блокуємо показ

            // 2) Тепер будуємо список (картки одразу візьмуть іконки з пам’яті → без плейсхолдерів на екрані)
            BuildList();

            // 3) (опційно) дисковий префетч решти — можна фоном
            if (_all != null) StartCoroutine(PrewarmIconsCo());

            // 4) проявляємо панель і робимо її інтерактивною
            if (cg != null)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * 6f;
                    cg.alpha = Mathf.Clamp01(t);
                    yield return null;
                }
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            SetInteractable(true);
        }
        System.Collections.IEnumerator PrewarmTopIconsToMemoryCo(int topCount, int timeoutMs)
        {
            // формуємо той самий порядок, що і в BuildList, але БЕЗ інстансингу карток
            if (_all == null || _all.Count == 0) yield break;

            int level = _playerLevel;
            var source = _all.Where(p => p.IsActive).ToList();

            var exactNext = source.FirstOrDefault(p => p.UnlockLevel == level + 1);
            var fallbackNext = source.Where(p => p.UnlockLevel > level).OrderBy(p => p.UnlockLevel).FirstOrDefault();
            var nextLocked = exactNext ?? fallbackNext;

            var available = source.Where(p => p.UnlockLevel <= level).OrderByDescending(p => p.UnlockLevel);

            var display = new System.Collections.Generic.List<PlantInfo>(topCount + 1);
            if (nextLocked != null) display.Add(nextLocked);
            foreach (var p in available)
            {
                if (display.Count >= topCount) break;
                display.Add(p);
            }

            // готуємо список ключів (іконка grown — найінформативніша в пікері)
            var keys = new System.Collections.Generic.List<string>(display.Count);
            foreach (var p in display)
            {
                if (!string.IsNullOrEmpty(p.IconGrown)) keys.Add(p.IconGrown);
            }
            if (keys.Count == 0) yield break;

            // тепер підігріємо спрайти в ПАМ’ЯТЬ (RemoteSpriteCache._mem),
            // щоб картки в Bind() змогли взяти їх миттєво через TryGetInMemory(...)
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var key in keys)
            {
                if (sw.ElapsedMilliseconds > timeoutMs) break; // м'який таймаут, щоб не затягувати відкриття
                var task = RemoteSpriteCache.GetSpriteAsync(key);
                while (!task.IsCompleted) yield return null; // даємо виконатись завантаженню/створенню Sprite
            }
        }
        void AddItemImmediate(PlantInfo data, bool unlocked)
        {
            if (content == null || itemPrefab == null) return;

            PlantOptionItemView view;
            if (_pool.Count > 0)
            {
                view = _pool.Pop();
                view.transform.SetParent(content, false);
                view.gameObject.SetActive(true);
            }
            else
            {
                view = Instantiate(itemPrefab, content);
            }
            view.Bind(data, unlocked, _onPlant);
        }
        void BuildListImmediate()
        {
            if (content == null || itemPrefab == null)
            {
                Debug.LogError("[PlantSelectionPanel] content/itemPrefab не призначено", this);
                return;
            }
            if (_all == null)
            {
                Debug.LogWarning("[PlantSelectionPanel] Дані ще не передані (SetData не викликано)", this);
                ClearContent();
                return;
            }

            ClearContent();

            var source = _all.Where(p => p.IsActive).ToList();
            int level = _playerLevel;

            var exactNext = source.FirstOrDefault(p => p.UnlockLevel == level + 1);
            var fallbackNext = source.Where(p => p.UnlockLevel > level).OrderBy(p => p.UnlockLevel).FirstOrDefault();
            var nextLocked = exactNext ?? fallbackNext;

            if (nextLocked != null) AddItemImmediate(nextLocked, unlocked: false);

            foreach (var p in source.Where(p => p.UnlockLevel <= level).OrderByDescending(p => p.UnlockLevel))
                AddItemImmediate(p, unlocked: true);
        }
        public void PrebuildSilently()
        {
            // 1) Будуємо контент СИНХРОННО (працює навіть коли GO неактивний)
            BuildListImmediate();

            // 2) Форсимо лейаут, щоб уже були розміри/позиції
            if (content is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            // 3) Виставляємо Left = 0, поки панель ще «за кадром»
            var rtRoot = (root != null ? root.GetComponent<RectTransform>() : GetComponent<RectTransform>());
            if (rtRoot != null)
            {
                var p = rtRoot.anchoredPosition;
                p.x = 0f;
                rtRoot.anchoredPosition = p;
            }

            // 4) Ховаємо панель: альфа 0, неінтерактивна, root OFF, сам GO OFF
            if (cg != null) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }
            if (root != null) root.SetActive(false);
            gameObject.SetActive(false);
        }
        public async System.Threading.Tasks.Task PrewarmAtStartupAsync()
        {
            // 0) повинні бути передані дані
            if (_all == null || _all.Count == 0) return;

            // 1) ПРЕФЕТЧ усіх видимих іконок (grown) на диск, без корутин
            var allKeys = new System.Collections.Generic.HashSet<string>();
            foreach (var p in _all)
                if (p.IsActive && !string.IsNullOrEmpty(p.IconGrown))
                    allKeys.Add(p.IconGrown);
            if (allKeys.Count > 0)
                await RemoteSpriteCache.PrefetchToDiskOnly(allKeys, maxParallel: 4, softTimeoutMs: 2000);

            // 2) Прогріваємо в ПАМ'ЯТЬ топ-іконки (у тому ж порядку, що й у списку)
            int level = _playerLevel;
            var source = _all.Where(p => p.IsActive).ToList();
            var exactNext = source.FirstOrDefault(p => p.UnlockLevel == level + 1);
            var fallbackNext = source.Where(p => p.UnlockLevel > level).OrderBy(p => p.UnlockLevel).FirstOrDefault();
            var nextLocked = exactNext ?? fallbackNext;

            var display = new System.Collections.Generic.List<PlantInfo>(warmTopCount + 1);
            if (nextLocked != null) display.Add(nextLocked);
            foreach (var p in source.Where(p => p.UnlockLevel <= level).OrderByDescending(p => p.UnlockLevel))
            {
                if (display.Count >= warmTopCount) break;
                display.Add(p);
            }

            foreach (var p in display)
            {
                if (string.IsNullOrEmpty(p.IconGrown)) continue;
                var t = RemoteSpriteCache.GetSpriteAsync(p.IconGrown);
                while (!t.IsCompleted) await System.Threading.Tasks.Task.Yield(); // без блокування кадру
            }

            // 3) Будуємо список СИНХРОННО (працює і коли GO неактивний)
            BuildListImmediate();

            // 4) Перерахунок лейауту і повернення позиції Left=0
            if (content is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            var rtRoot = (root != null ? root.GetComponent<RectTransform>() : GetComponent<RectTransform>());
            if (rtRoot != null)
            {
                var p = rtRoot.anchoredPosition; p.x = 0f; rtRoot.anchoredPosition = p;
            }

            // 5) Ховаємо панель повністю (як Close), щоб перший показ був миттєвим
            if (cg != null) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }
            if (root != null) root.SetActive(false);
            gameObject.SetActive(false);

            _prewarmed = true;
        }
    }
}