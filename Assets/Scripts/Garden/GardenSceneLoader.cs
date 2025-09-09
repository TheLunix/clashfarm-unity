using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class GardenSceneLoader : MonoBehaviour
{
    [Header("Scene roots")]
    [SerializeField] private GameObject gardenRoot;       // корінь підсцени городу
    [SerializeField] private GameObject mainRoot;       // корінь підсцени городу
    [SerializeField] private GameObject loadingOverlay;   // темний фон + спінер (може бути завжди в Canvas)

    [Header("UI panels")]
    [SerializeField] private PlantSelectionPanel plantPanel;   // панель вибору рослини

    [Header("Options")]
    [SerializeField] private bool prewarmPlantPanel = true;

    void Awake()
    {
        // На старті город прихований (щоб не було миготіння)
        if (gardenRoot) gardenRoot.SetActive(false);

        // Overlay може бути видимий, але він НЕ має перехоплювати кліки
        DisableRaycasts(loadingOverlay, blocks:false);
    }

    public void OpenGarden()
    {
        // Викликаємо це з кнопки “Огород” замість простого SetActive(true)
        StartCoroutine(CoOpenGarden());
    }

    IEnumerator CoOpenGarden()
    {
        // 1) Показуємо overlay (але без raycasts)
        if (loadingOverlay) loadingOverlay.SetActive(true);
        DisableRaycasts(loadingOverlay, blocks:false);

        // 2) Гарантуємо предлоад кешу
        var cache = GardenStateCache.I;
        var session = PlayerSession.I;

        if (cache != null && !cache.IsReady && session != null && session.Data != null)
        {
            cache.PreloadByCredentials(session.Data.nickname, session.Data.serialcode);
        }

        // 3) Чекаємо готовність кешу
        while (cache != null && !cache.IsReady)
            yield return null;

        // 4) Попередньо будуємо список посадки (щоб не було затримки при відкритті)
        if (prewarmPlantPanel && plantPanel != null && cache != null)
        {
            // поточний рівень гравця беремо з сесії (fallback = 1)
            int lvl = (session != null && session.Data != null) ? Mathf.Max(1, session.Data.playerlvl) : 1;
            plantPanel.Prewarm(cache.PlantCatalog, lvl, onPlant: _ => { /* no-op тут */ });
        }

        // 5) Активуємо город і деактивуємо мейн
        if (gardenRoot) gardenRoot.SetActive(true);
        if (mainRoot) mainRoot.SetActive(false);

        // 6) Озброюємо “клік-ґейт” (2 кадри): навіть якщо overlay ще кадр існує, клік не пропаде
        GardenClickGate.Arm(this, frames: 2);

        // 7) Чекаємо 1 кадр для оновлення layout/raycast-стану
        yield return null;

        // 8) Повністю ховаємо overlay
        if (loadingOverlay) loadingOverlay.SetActive(false);
    }

    static void DisableRaycasts(GameObject go, bool blocks)
    {
        if (!go) return;
        var cg  = go.GetComponent<CanvasGroup>();
        var img = go.GetComponent<Image>();
        if (cg)
        {
            cg.blocksRaycasts = blocks;
            cg.interactable   = blocks;
        }
        if (img) img.raycastTarget = blocks;
    }
}
