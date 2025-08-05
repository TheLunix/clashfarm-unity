using UnityEngine;
using UnityEngine.UI;

public class ImageColorChange : MonoBehaviour
{
    [SerializeField] private Image SetImage;
    [SerializeField] private Color SetColor;

    // Встановити зображення для зміни кольору
    public void SetImg(Image Source)
    {
        SetImage = Source;
    }

    // Викликається при створенні об'єкта
    private void Start()
    {
        // Встановити альфа-канал кольору на 1
        SetColor.a = 1;
    }

    // Змінити колір зображення на встановлений колір
    public void ChangeColor()
    {
        SetImage.color = SetColor;
    }
}