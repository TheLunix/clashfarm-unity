using UnityEngine;
using UnityEngine.UI;

public class GuardBackgroundByFaction : MonoBehaviour
{
    [SerializeField] private Image fone;
    [SerializeField] private Sprite elvesBackground;
    [SerializeField] private Sprite orcsBackground;

    private void Start()
    {
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        var data = PlayerSession.I?.Data;
        if (data == null || fone == null) return;

        if (data.playerfraction == 1 && elvesBackground != null)
        {
            // Ельфи
            fone.sprite = elvesBackground;
        }
        else if (data.playerfraction == 2 && orcsBackground != null)
        {
            // Орки
            fone.sprite = orcsBackground;
        }
    }
}
