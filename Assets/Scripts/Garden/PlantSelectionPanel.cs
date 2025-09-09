#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI; // для Button та LayoutRebuilder

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

    // кеш вхідних даних
    List<PlantInfo> _all;
    int _playerLevel;
    Action<PlantInfo> _onPlant;
    readonly Stack<PlantOptionItemView> _pool = new();

    // зручно знати, чи панель відкрита
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
        if (root != null) root.SetActive(true);
        if (infobar != null) infobar.SetActive(false);
        BuildList();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        if (infobar != null) infobar.SetActive(true);
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
            AddItem(nextLocked, unlocked: false);   // тільки ОДНА недоступна зверху

        foreach (var p in available)
            AddItem(p, unlocked: true);
    }

    void AddItem(PlantInfo data, bool unlocked)
    {
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
}
