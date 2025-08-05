using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HomeButton : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public HomeSystem Home;
    public GameObject HomePanel, PanelMain, Personage;
    public Image Fone;
    public Sprite[] FoneSprite;

    public void OpenHomePanel()
    {
        // Відкриваємо панель для дому
        HomePanel.SetActive(true);
        PanelMain.SetActive(false);
        Personage.SetActive(false);
        Fone.sprite = FoneSprite[1];

        // Перевіряємо, чи гравцеві належить кінь, і якщо так, запускаємо таймер
        if (Player.pHorse != "0")
        {
            Home.HorseStartTimer();
        }
    }

    public void CloseHomePanel()
    {
        // Закриваємо панель для дому та повертаємося до головного меню
        HomePanel.SetActive(false);
        PanelMain.SetActive(true);
        Personage.SetActive(true);
        Fone.sprite = FoneSprite[0];
    }
}