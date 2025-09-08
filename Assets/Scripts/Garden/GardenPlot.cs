using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class GardenPlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    [SerializeField] private string plotId = "plot_01";
    [SerializeField] private bool isEmpty = true;

    [Header("Visual (UI)")]
    [SerializeField] private Image background;           // можна не задавати — візьмемо з цього GO
    [SerializeField] private Color emptyTint   = Color.white;
    [SerializeField] private Color plantedTint = Color.white;
    [SerializeField] private GameObject weed;

    private GardenController _controller;

    public string PlotId => plotId;
    public bool IsEmpty  => isEmpty;

    private bool _isLocked;
    public bool IsLocked => _isLocked;

    void Awake()
    {
        #if UNITY_2022_2_OR_NEWER
                _controller = FindFirstObjectByType<GardenController>();
        #else
                _controller = FindObjectOfType<GardenController>();
        #endif
        if (background == null) background = GetComponent<Image>();
        ApplyVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_controller == null) { Debug.LogError("[GardenPlotUI] GardenController not found"); return; }
        _controller.OnPlotClicked(this); // нічого в контролері міняти не треба
    }

    // Викликає контролер після посадки
    public void SetPlanted(PlantInfo plant)
    {
        isEmpty = false;
        ApplyVisual();
        // TODO: онови іконку/текст, якщо потрібно
    }

    // Після збору врожаю
    public void SetEmpty()
    {
        isEmpty = true;
        ApplyVisual();
    }

    public void SetWeedActive(bool on)
    {
        if (weed != null) weed.SetActive(on);
    }

    private void ApplyVisual()
    {
        if (background != null) background.color = isEmpty ? emptyTint : plantedTint;
    }

#if UNITY_EDITOR
    void Reset()
    {
        // гарантуємо, що UI елемент приймає кліки: додаємо Image з прозорим кольором
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0); // повністю прозорий
        }
        img.raycastTarget = true;
    }
#endif
    [SerializeField] private int slotIndexUi = 1;  // у Canvas: 1..12
    public int SlotIndexUi => slotIndexUi;

    // Виклик із байндера після предлоада
    public void ApplyModel(PlotModel model, PlantInfo plant,
        string baseUrl = "https://api.clashfarm.com/plants", string suffix = ".png")
    {
        // locked
        _isLocked = (model == null) || model.isLocked;

        // показуємо бур’ян тільки на заблокованих
        SetWeedActive(_isLocked);

        if (_isLocked) { SetEmpty(); return; }

        // empty
        if (model.plantTypeId == null || model.stage == 0)
        {
            SetEmpty();
            SetWeedActive(false);  // бур’ян ховаємо
            return;
        }

        isEmpty = false;
        ApplyVisual();
        SetWeedActive(false);      // посаджена — бур’ян ховаємо

        // яку іконку показати
        string iconName = null;
        if (plant != null)
        {
            switch (model.stage)
            {
                case 1: iconName = plant.IconSeed;  break;
                case 2: iconName = plant.IconPlant; break;
                case 3: iconName = plant.IconGrown; break; // або Fruit — як забажаєш
            }
        }

        if (!string.IsNullOrEmpty(iconName))
        {
            var url = baseUrl.TrimEnd('/') + "/" + iconName + (iconName.Contains(".") ? "" : suffix);
            var img = GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                ImageCache.Instance.GetSprite(url, s => { if (s != null) img.sprite = s; });
            }
        }
    }
}
    