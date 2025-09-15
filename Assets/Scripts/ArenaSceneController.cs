using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ArenaSceneController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MainSceneController main;   // можна підкинути з інспектора або знайти
    [SerializeField] private Button fightButton;
    [SerializeField] private TMP_Text combatsText;       // опціонально: локальний відображувач у підсцені
    [SerializeField] private TMP_Text timerText;         // MM:SS або "—"

    void Awake()
    {
        if (main == null) main = FindFirstObjectByType<MainSceneController>();
    }

    void OnEnable()
    {
        if (main != null)
            main.OnCombatsUpdated += HandleCombatsUpdated;

        // ініціалізація з поточного стану
        if (main != null)
            HandleCombatsUpdated(main.CombatsCurrent, main.CombatsMax, main.RemainToNext, main.RemainToFull);
    }

    void OnDisable()
    {
        if (main != null)
            main.OnCombatsUpdated -= HandleCombatsUpdated;
    }

    void HandleCombatsUpdated(int current, int max, int toNext, int toFull)
    {
        if (combatsText) combatsText.text = $"{current} / {max}";
        if (timerText)
        {
            if (current >= max) timerText.text = "—";
            else
            {
                int mm = Mathf.Max(0, toNext) / 60;
                int ss = Mathf.Max(0, toNext) % 60;
                timerText.text = $"{mm:00}:{ss:00}";
            }
        }

        if (fightButton)
        {
            // Кнопка неактивна, якщо боїв 0
            fightButton.interactable = (current > 0);
        }
    }

    // Викликається із кнопки “Бій”
    public void OnFightPressed()
    {
        if (PlayerSession.I?.Data == null) return;
        Debug.Log("АТАКА!!!");
        StartCoroutine(UseCombatFlow());
    }

    IEnumerator UseCombatFlow()
    {
        var d = PlayerSession.I.Data;

        // 1) Атомарний список бою на сервері
        var task = ApiClient.CombatsUseAsync(d.nickname, d.serialcode);
        while (!task.IsCompleted) yield return null;

        var dto = task.Result;

        // 2) Обробка помилки/успіху
        if (dto == null)
        {
            Debug.LogWarning("CombatsUseAsync failed (null).");
            yield break;
        }
        if (!string.IsNullOrEmpty(dto.error))
        {
            if (dto.error == "NO_COMBATS")
            {
                Debug.Log("Немає боїв.");
                // Можеш показати попап/тост
            }
            else
            {
                Debug.LogWarning("CombatsUseAsync error: " + dto.error);
            }
            // Запросимо стан ще раз для синхронізації
            yield return RefreshCombatsOnce();
            yield break;
        }

        // 3) Успіх — пушимо стан у MainSceneController (щоб HUD теж оновився)
        if (main != null) main.IngestCombats(dto);

        // Тут же можна запустити бій: завантажити потрібну сцену/анімацію/логіку
        Debug.Log("Combat consumed. Left: " + dto.combats);
    }

    IEnumerator RefreshCombatsOnce()
    {
        var d = PlayerSession.I?.Data;
        if (d == null) yield break;

        var task = ApiClient.CombatsHeartbeatAsync(d.nickname, d.serialcode);
        while (!task.IsCompleted) yield return null;

        var dto = task.Result;
        if (dto != null && string.IsNullOrEmpty(dto.error) && main != null)
            main.IngestCombats(dto);
    }
}
