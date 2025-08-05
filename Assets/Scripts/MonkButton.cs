using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonkButton : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public GameObject MonkPanel, PanelMain, Personage, Button; //BattlePlayer
    public TextMeshProUGUI MonkInfo;
    public Image Fone;
    public Sprite[] FoneSprite;

    public void OpenMonkPanel()
    {
        MonkPanel.SetActive(true);
        PanelMain.SetActive(false);
        Personage.SetActive(false);
        Fone.sprite = FoneSprite[1];

        if (Player.Account.monkreward == 1)
        {
            Button.SetActive(false);
            MonkInfo.text = "Монах: Ти вже отримав(ла) нагороду сьогодні. Йди с миром!";
        }
        else
        {
            MonkInfo.text = "Монах: Не трать свій час даремно. Забери нагороду та йди з миром!";
        }
    }

    public void CloseMonkPanel()
    {
        MonkPanel.SetActive(false);
        PanelMain.SetActive(true);
        Personage.SetActive(true);
        Fone.sprite = FoneSprite[0];
    }
}