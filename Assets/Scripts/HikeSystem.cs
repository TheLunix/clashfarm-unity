using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class HikeSystem : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public UnityEngine.UI.InputField MinutesInput = null;
    public GameObject SelectMinutes, MessageBox, Button;
    public Text ButtonMinutes;
    public TextMeshProUGUI HikeLast, HikeInfo;
    private float _timeLeft = 0f;
    private IEnumerator Timer;
    private string TimeInfo;
    private float TimerLeft;

    public void GoToHike()
    {
        if (Player.IsActiveHike == false)
        {
            if (MinutesInput.text != "")
            {
                int minutesToHike = int.Parse(MinutesInput.text);
                int maxMinutes = Player.Account.hikemin;

                if (minutesToHike > 0 && minutesToHike <= maxMinutes)
                {
                    StartHike(minutesToHike);
                }
                else
                {
                    HikeInfo.text = $"Осталось времени на поход: {maxMinutes} минут. <color=red>Значение должно быть больше 0 и не больше {maxMinutes}.</color>";
                }
            }
        }
        else
        {
            MessageBox.SetActive(true);
        }
    }

    public void CancelHike()
    {
        if (Player.IsActiveHike == true)
        {
            Player.IsActiveHike = false;
            SelectMinutes.SetActive(true);
            StopCoroutine(Timer);
            MessageBox.SetActive(false);
            HikeLast.text = Player.Account.lasthike;
            HikeInfo.text = $"Осталось времени на поход: {Player.Account.hikemin} минут. Внимание - если ты решишь досрочно вернуться, то ничего не получишь!";
            ResetHikeData();
        }
    }

    private void StartHike(int minutes)
    {
        Player.IsActiveHike = true;
        System.DateTime endTime = System.DateTime.Now.AddMinutes(minutes);
        Player.Account.timetoendhike = endTime.ToString("dd.MM.yyyy HH:mm:ss");
        StartCoroutine(UpdateCellAccount("timetoendhike", endTime.ToString("dd.MM.yyyy HH:mm:ss"), Player.pID.ToString()));

        int remainingMinutes = Player.Account.hikemin - minutes;
        Player.Account.hikemin = remainingMinutes;
        StartCoroutine(UpdateCellAccount("hikemin", remainingMinutes.ToString(), Player.pID.ToString()));

        int activeMinutes = minutes;
        Player.Account.hikeactivemin = activeMinutes;
        StartCoroutine(UpdateCellAccount("hikeactivemin", activeMinutes.ToString(), Player.pID.ToString()));

        _timeLeft = (int)(endTime - System.DateTime.Now).TotalSeconds;
        Timer = StartTimer();
        StartCoroutine(Timer);
        SelectMinutes.SetActive(false);
        ButtonMinutes.text = "Вернуться";
    }

    private void ResetHikeData()
    {
        StartCoroutine(UpdateCellAccount("timetoendguard", "0", Player.pID.ToString()));
        StartCoroutine(UpdateCellAccount("guardhour", "0", Player.pID.ToString()));
        ButtonMinutes.text = "В поход";
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
            if (Player.Account.hikemin <= 0)
            {
                SelectMinutes.SetActive(false);
                Button.SetActive(false);
            }
        }

        DisplayTime(_timeLeft);
        HikeInfo.text = $"\nВы отправились в поход, оставшееся время:\n{TimeInfo}";
    }

    void DisplayTime(float timeToDisplay)
    {
        System.TimeSpan remaining = System.TimeSpan.FromSeconds(timeToDisplay);
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

    public void HikeStartTimer()
    {
        System.DateTime time = System.DateTime.Parse(Player.Account.timetoendhike);
        _timeLeft = (int)(time - System.DateTime.Now).TotalSeconds;
        Timer = StartTimer();
        StartCoroutine(Timer);
    }
}