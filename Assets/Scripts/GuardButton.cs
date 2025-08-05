using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuardButton : MonoBehaviour
{
    public GameObject GuardPanel, PanelMain, Personage;
    public Image Fone;
    public Sprite[] FoneSprite;
    public LoadAndUpdateAccount Player;
    public GuardSystem Guard;
    public TextMeshProUGUI GuardInfo;

    private float _timeLeft = 0f;
    private string TimeInfo;

    public void OpenGuardPanel()
    {
        GuardPanel.SetActive(true);
        PanelMain.SetActive(false);
        Personage.SetActive(false);
        Fone.sprite = FoneSprite[1];
        UpdateGuardInfo();

        if (Player.IsActiveGuard)
        {
            UpdateGuardTimer();
            Guard.SelectHour.SetActive(false);
            Guard.ButtonHour.text = "Прекратить охрану";
        }
    }

    public void CloseGuardPanel()
    {
        GuardPanel.SetActive(false);
        PanelMain.SetActive(true);
        Personage.SetActive(true);
        Fone.sprite = FoneSprite[0];
    }

    private void UpdateGuardInfo()
    {
        GuardInfo.text = "• На данный момент деревня готова платить тебе    <sprite=0> " + (Player.pLvl * 50) + " в час, чем выше уровень - тем выше оплата!";
    }

    private void UpdateGuardTimer()
    {
        System.DateTime time = System.DateTime.Parse(Player.pTimeToEndGuard);
        _timeLeft = (float)(time - System.DateTime.Now).TotalSeconds;
        StartCoroutine(StartTimer());
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
        Guard.GuardInfo.text = "\nВы отправились охранять околицы, оставшееся время - " + TimeInfo;
    }

    private void DisplayTime(float timeToDisplay)
    {
        TimeSpan remaining = TimeSpan.FromSeconds(timeToDisplay);
        TimeInfo = remaining.ToString(@"hh\:mm\:ss");
    }
}