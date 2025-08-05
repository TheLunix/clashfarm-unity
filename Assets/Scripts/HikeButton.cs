using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HikeButton : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public HikeSystem Hike;
    public GameObject HikePanel, PanelMain, Personage;
    public Image Fone;
    public Sprite[] FoneSprite;
    private float _timeLeft = 0f;
    private string TimeInfo;

    public void OpenHikePanel()
    {
        HikePanel.SetActive(true);
        PanelMain.SetActive(false);
        Personage.SetActive(false);
        Fone.sprite = FoneSprite[1];
        Hike.HikeLast.text = Player.Account.lasthike;

        if (Player.IsActiveHike == true)
        {
            Hike.HikeStartTimer();
            Hike.SelectMinutes.SetActive(false);
            Hike.ButtonMinutes.text = "Вернуться";
        }
        else if (Player.Account.hikemin <= 0)
        {
            Hike.SelectMinutes.SetActive(false);
            Hike.Button.SetActive(false);
            Hike.HikeInfo.text = "Сегодня ты слишком устал(а) чтобы идти в поход. Отдохни и приходи завтра!";
        }
        else
        {
            Hike.HikeInfo.text = "Осталось времени на поход: " + Player.Account.hikemin + " минут. Внимание - если ты решишь досрочно вернуться, то ничего не получишь!";
        }
    }

    public void CloseHikePanel()
    {
        HikePanel.SetActive(false);
        PanelMain.SetActive(true);
        Personage.SetActive(true);
        Fone.sprite = FoneSprite[0];
    }

    private IEnumerator StartTimer()
    {
        while (_timeLeft > 0)
        {
            _timeLeft -= Time.deltaTime;
            UpdateTimeText();
            yield return null;
        }
    }

    private void UpdateTimeText()
    {
        if (_timeLeft < 0)
        {
            _timeLeft = 0;
            Player.ReloadInfoBar();
        }

        DisplayTime(_timeLeft);
        Hike.HikeInfo.text = "\nВы отправились в поход, оставшееся время:\n" + TimeInfo;
    }

    void DisplayTime(float timeToDisplay)
    {
        System.TimeSpan remaining = System.TimeSpan.FromSeconds(timeToDisplay);
        TimeInfo = remaining.ToString(@"hh\:mm\:ss");
    }
}