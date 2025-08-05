using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class GuardSystem : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public TMP_Dropdown SelectMenu;
    public GameObject SelectHour, MessageBox;
    public Text ButtonHour;
    public TextMeshProUGUI GuardInfo;
    private float _timeLeft = 0f;
    private int _timeSeconds = 0;
    private IEnumerator timer;
    private string TimeInfo;

    public void GoToGuard()
    {
        if (Player.IsActiveGuard == false)
        {
            StartGuard();
        }
        else
        {
            MessageBox.SetActive(true);
        }
    }

    public void CancelGuard()
    {
        if (Player.IsActiveGuard == true)
        {
            StopGuard();
        }
    }

    private void StartGuard()
    {
        Player.IsActiveGuard = true;
        DateTime time = DateTime.Now.AddSeconds((SelectMenu.value + 1) * 3600);
        StartCoroutine(UpdateCellAccount("timetoendguard", time.ToString("dd.MM.yyyy HH:mm:ss"), Player.pID.ToString()));
        StartCoroutine(UpdateCellAccount("guardhour", (SelectMenu.value + 1).ToString(), Player.pID.ToString()));
        _timeSeconds = (int)(time - DateTime.Now).TotalSeconds;
        _timeLeft = _timeSeconds;
        timer = StartTimer();
        StartCoroutine(timer);
        SelectHour.SetActive(false);
        ButtonHour.text = "Прекратить охрану";
    }

    private void StopGuard()
    {
        Player.IsActiveGuard = false;
        SelectHour.SetActive(true);
        StopCoroutine(timer);
        MessageBox.SetActive(false);
        GuardInfo.text = "Выбери на сколько часов ты пойдешь охранять околицы.\nВнимание - если ты решишь досрочно прекратить охрану то оплата не будет выдана!";
        StartCoroutine(UpdateCellAccount("timetoendguard", "0", Player.pID.ToString()));
        StartCoroutine(UpdateCellAccount("guardhour", "0", Player.pID.ToString()));
        ButtonHour.text = "Охранять околицы";
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
        GuardInfo.text = "\nВы отправились охранять околицы, оставшееся время - " + TimeInfo;
    }

    private void DisplayTime(float timeToDisplay)
    {
        TimeSpan remaining = TimeSpan.FromSeconds(timeToDisplay);
        TimeInfo = remaining.ToString(@"hh\:mm\:ss");
    }

    private IEnumerator UpdateCellAccount(string cellname, string value, string id)
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("UpdateCell", cellname);
        FindDataBase.AddField("UpdateValue", value);
        FindDataBase.AddField("PlayerID", id);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
    }
}