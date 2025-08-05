using UnityEngine;
using UnityEngine.UI;

public class GardenFirst : MonoBehaviour
{
    public Image img;

    public void SetImage(Image source)
    {
        img = source;
    }

    public void GardenSystem(int stepLevel)
    {
        if (stepLevel == 0)
        {
            img.sprite = Resources.Load<Sprite>("OtherSprites/garden_pull");
        }
    }
}