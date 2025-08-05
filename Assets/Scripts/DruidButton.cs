using UnityEngine;
using UnityEngine.UI;

public class DruidButton : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public GameObject DruidPanel, PanelMain, Personage;
    public Image Fone;
    public Sprite[] FoneSprite;

    public void OpenDruidPanel()
    {
        SetPanelsAndFone(true, false, false, 1);
        //Trader.LoadTrader();
    }

    public void CloseDruidPanel()
    {
        SetPanelsAndFone(false, true, true, 0);
    }

    private void SetPanelsAndFone(bool druidActive, bool mainActive, bool personageActive, int foneIndex)
    {
        DruidPanel.SetActive(druidActive);
        PanelMain.SetActive(mainActive);
        Personage.SetActive(personageActive);
        Fone.sprite = FoneSprite[foneIndex];
    }
}