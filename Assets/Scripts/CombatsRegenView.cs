using UnityEngine;
using TMPro;

/// <summary>
/// Суто UI-віджет для відображення лічильника боїв і таймера.
/// ЖОДНОЇ мережевої логіки тут нема — усім керує MainSceneController.
/// </summary>
public sealed class CombatsRegenView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text combatsText;      // "3 / 6"
    [SerializeField] private TMP_Text timerText;        // "09:12" або "—" коли повний запас

    int _current;
    int _max = 6;
    int _remainingSec;

    /// <summary>Одноразово задати початкові значення (необов’язково).</summary>
    public void StartFlow(int current, int max, int remainingToNextSec)
    {
        _current      = Mathf.Max(0, current);
        _max          = Mathf.Max(1, max);
        _remainingSec = Mathf.Max(0, remainingToNextSec);
        Render();
    }

    /// <summary>Оновлення від контролера раз на секунду (або коли завгодно).</summary>
    public void OnCombatsHeartbeat(int current, int max, int remainingSec)
    {
        _current      = Mathf.Max(0, current);
        _max          = Mathf.Max(1, max);
        _remainingSec = Mathf.Max(0, remainingSec);
        Render();
    }

    void Render()
    {
        if (combatsText)
            combatsText.text = $"{_current} / {_max}";

        if (timerText)
        {
            if (_current >= _max) { timerText.text = "—"; return; }
            if (_current < 0) { timerText.text = "…"; return; }
            int mm = _remainingSec / 60;
            int ss = _remainingSec % 60;
            timerText.text = $"{mm:00}:{ss:00}";
        }
    }
}
