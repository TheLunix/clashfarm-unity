using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantItemView : MonoBehaviour
{
    public Button rootButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI lockText;
    public Image icon; // опційно

    System.Action onClick;

    public void Bind(ApiClient.PlantInfo pi, int playerLevel, System.Action onClick)
    {
        this.onClick = onClick;

        if (nameText) nameText.text = pi.name;
        if (descText) descText.text = pi.description ?? "";
        if (priceText) priceText.text = $"Ціна продажу: {pi.sellPrice}";
        
        bool locked = playerLevel < pi.requiredLevel;
        if (lockText)
        {
            lockText.gameObject.SetActive(locked);
            lockText.text = $"Відкриється на {pi.requiredLevel} рівні";
        }

        if (rootButton)
        {
            rootButton.interactable = (pi.isActive == 1) && !locked;
            rootButton.onClick.RemoveAllListeners();
            rootButton.onClick.AddListener(() => { if (this.onClick != null) this.onClick(); });
        }

        // Якщо маєш спрайти з Addressables/Resources — підстав через ключ тут.
        // if (icon) icon.sprite = PlantIconProvider.Get(pi.id);
    }
}
