using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    public float typingSpeed = 0.05f;

    [TextArea(3, 10)]
    public string fullRawText = ""; // Текст із видимими \n
    private string parsedText;

    public TextMeshProUGUI textComponent;

   public void Start()
    {
        // Заміна всіх текстових "\\n" на реальний символ нового рядка
        parsedText = fullRawText.Replace("\\n", "\n");
        StartCoroutine(ShowText());
    }

    IEnumerator ShowText()
    {
        textComponent.text = "";
        string currentText = "";

        foreach (char c in parsedText)
        {
            currentText += c;
            textComponent.text = currentText;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
