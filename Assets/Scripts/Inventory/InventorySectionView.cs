using TMPro;
using UnityEngine;

public class InventorySectionView : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public RectTransform gridRoot;

    public void SetTitle(string title)
    {
        titleText.text = title;
    }
}
