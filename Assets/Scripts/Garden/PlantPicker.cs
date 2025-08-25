using System.Collections.Generic;
using UnityEngine;

public class PlantPicker : MonoBehaviour
{
    [Header("UI")]
    public RectTransform content;
    public PlantItemView itemPrefab;

    System.Action<int> _onPicked; // повертаємо plantId
    List<PlantItemView> _pool = new List<PlantItemView>();

    public void Open(System.Action<int> onPicked)
    {
        gameObject.SetActive(true);
        _onPicked = onPicked;
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        _onPicked = null;
        // можна не знищувати — перевикористаємо
    }

    async void Refresh()
    {
        // 1) тягнемо список рослин
        var list = await ApiClient.GetPlantsAsync(onlyActive: false);
        if (list == null) list = new List<ApiClient.PlantInfo>();

        // 2) визначимо рівень гравця (з PlayerSession)
        int playerLevel = 1;
        if (PlayerSession.I != null && PlayerSession.I.Data != null)
            playerLevel = Mathf.Max(1, PlayerSession.I.Data.playerlvl);

        // 3) відфільтруємо: показуємо всі, але заблоковані — сірою кнопкою
        // (вже реалізовано в Bind через interactable)
        EnsurePool(list.Count);

        for (int i = 0; i < _pool.Count; i++)
        {
            bool show = i < list.Count;
            _pool[i].gameObject.SetActive(show);
            if (!show) continue;

            var pi = list[i];
            _pool[i].Bind(pi, playerLevel, () => Pick(pi.id));
        }
    }

    void Pick(int plantId)
    {
        _onPicked?.Invoke(plantId);
        Close();
    }

    void EnsurePool(int need)
    {
        while (_pool.Count < need)
        {
            var item = Instantiate(itemPrefab, content);
            _pool.Add(item);
        }
    }
}
