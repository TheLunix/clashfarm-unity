using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TraderButton : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public TraderSystem Trader;
    public GameObject TraderPanel, PanelMain, Personage;
    public Image Fone;
    public Sprite[] FoneSprites;

    public void OpenTraderPanel()
    {
        SetPanelState(true, false, false, FoneSprites[1]);
        Trader.LoadTrader();
    }

    public void CloseTraderPanel()
    {
        SetPanelState(false, true, true, FoneSprites[0]);
    }

    private void SetPanelState(bool traderActive, bool panelMainActive, bool personageActive, Sprite foneSprite)
    {
        TraderPanel.SetActive(traderActive);
        PanelMain.SetActive(panelMainActive);
        Personage.SetActive(personageActive);
        Fone.sprite = foneSprite;
    }
}