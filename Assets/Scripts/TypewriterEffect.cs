using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    [Tooltip("Секунд на символ (0.03…0.08 рекомендується)")]
    public float typingSpeed = 0.05f;

    [TextArea(3, 10)]
    public string fullRawText = "";   // Може містити як \n, так і \\n та маркери <fx:.../>

    public TextMeshProUGUI textComponent;

    public bool IsPlaying { get; private set; }

    /// <summary> Викликається, коли друк завершено (після останнього видимого символу)</summary>
    public event Action OnCompleted;

    /// <summary> Мітка маркера під час друку, наприклад 'grow' з <fx:grow/> </summary>
    public event Action<string> OnMarker;

    Coroutine _co;

    // Регекс знаходить теги виду <fx:grow/> або <fx:grow param="..."/> (параметри ігноруємо)
    // і НЕ додає їх у видимий текст.
    static readonly Regex FxTag = new Regex(@"<\s*fx\s*:\s*([a-zA-Z0-9_]+)(\s+[^/>]*)?/\s*>",
        RegexOptions.Compiled);

    public void Play() => Play(fullRawText);

    public void Play(string text)
    {
        fullRawText = text ?? "";
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoType(fullRawText.Replace("\\n", "\n")));
    }

    public void Skip()
    {
        if (!IsPlaying) return;
        if (_co != null) StopCoroutine(_co);
        _co = null;

        // При скіпі прибираємо fx-теги з фінального тексту
        var visible = FxTag.Replace(fullRawText.Replace("\\n", "\n"), "");
        if (textComponent) textComponent.text = visible;

        IsPlaying = false;
        OnCompleted?.Invoke();
    }

    IEnumerator CoType(string parsed)
    {
        IsPlaying = true;
        if (textComponent) textComponent.text = "";

        // Розберемо рядок на токени: або звичайні символи, або fx-теги
        int i = 0;
        while (i < parsed.Length)
        {
            var m = FxTag.Match(parsed, i);
            if (m.Success && m.Index == i)
            {
                // Знайшли маркер на поточній позиції
                string markerName = m.Groups[1].Value.Trim().ToLowerInvariant();
                OnMarker?.Invoke(markerName);
                i += m.Length;
                continue; // тег не виводимо
            }

            // Звичайний символ — друкуємо
            if (textComponent) textComponent.text += parsed[i];
            i++;
            yield return new WaitForSeconds(typingSpeed);
        }

        IsPlaying = false;
        OnCompleted?.Invoke();
    }

    /// <summary> Оцінка тривалості друку без урахування маркерів: символи*typingSpeed. </summary>
    public float EstimateDuration(string text)
    {
        var visible = FxTag.Replace((text ?? "").Replace("\\n", "\n"), "");
        return Mathf.Max(0f, visible.Length * typingSpeed);
    }
}
