using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Button = UnityEngine.UI.Button;
using TMPro;

public class MineSystem : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public GameObject MessageBox, ButtonMine, ButtonCancel, Button;
    public Text MineButton;
    public TextMeshProUGUI MineInfo, InfoText;
    public float _timeLeft = 0f, _timerLeft = 0f, _timeMineLeft = 0f;
    private IEnumerator Timer, MineTimer;
    private string TimeInfo;
    public string MineTimeInfo;

    public void GoToMine() { StartCoroutine(_GoToMine()); }

    public IEnumerator _GoToMine()
    {
        if (Player.IsActiveMine == false)
        {
            if (Player.IsMineToday == true)
            {
                int mining = Player.pMinedGold;
                MineInfo.text = "Сьогодні Ви вже були в шахті!\nВи видобули: <sprite=1> " + mining + " золота.";
                Button.SetActive(false);
            }
            else
            {
                Player.IsActiveMine = true;
                System.DateTime Time = System.DateTime.Now.AddSeconds(1200);
                _timeMineLeft = 1200;
                MineTimer = StartMineTimer();
                StartCoroutine(MineTimer);
                yield return StartCoroutine(UpdateCellAccount("timetoendmine", Time.ToString("dd.MM.yyyy HH:mm:ss"), Player.pID.ToString()));
                _timeLeft = 60;
                System.DateTime TimeNext = System.DateTime.Now.AddSeconds(60);
                yield return StartCoroutine(UpdateCellAccount("timetonextmine", TimeNext.ToString("dd.MM.yyyy HH:mm:ss"), Player.pID.ToString()));
                Timer = StartTimer();
                StartCoroutine(Timer);
                MineButton.text = "Вийти";
                InfoText.text = "Ви спустились в шахту\nЗалишилося часу на видобуток - " + MineTimeInfo +
                    "\nВидобуто: <sprite=1> " + Player.pMinedGold + "/" + Player.pMaxMinegGold + " золота";
            }
        }
        else
        {
            CancelMineRequest();
        }
    }

    public void CancelMineRequest()
    {
        if (Player.IsActiveMine == true)
        {
            MessageBox.SetActive(true);
        }
    }

    public void CancelMine() { StartCoroutine(_CancelMine()); }

    public IEnumerator _CancelMine()
    {
        if (Player.IsActiveMine == true)
        {
            Player.IsActiveMine = false;
            StopCoroutine(Timer);
            StopCoroutine(MineTimer);
            MessageBox.SetActive(false);
            Player.ReloadInfoBar();
            int mining = Player.pMinedGold;
            InfoText.text = "• У вас буде всього 20 хвилин на видобуток у шахті!";
            MineInfo.text = "Ви закінчили видобувати золото в шахті!\nВи видобули: <sprite=1> " + mining + " золота.";
            yield return StartCoroutine(UpdateCellAccount("ismine", "1", Player.pID.ToString()));
            int Gold = Player.pGold + Player.pMinedGold;
            yield return StartCoroutine(UpdateCellAccount("playergold", Gold.ToString(), Player.pID.ToString()));
            int minedgold = Player.pMinedGoldStats + Player.pMinedGold;
            yield return StartCoroutine(UpdateCellAccount("minedgold", minedgold.ToString(), Player.pID.ToString()));
            Player.ReloadInfoBar();
            Button.SetActive(false);
            ButtonMine.SetActive(false);
            ButtonCancel.SetActive(false);
        }
    }

    public void Mining() { StartCoroutine(_Mining()); }

    public IEnumerator _Mining()
    {
        if (Player.IsActiveMine == true)
        {
            if (Player.pMaxMinegGold > Player.pMinedGold)
            {
                Button.SetActive(true);
                ButtonMine.SetActive(false);
                ButtonCancel.SetActive(false);
                Player.pMinedGold = Player.pMinedGold + Player.Account.playerlvl;
                InfoText.text = "Ви спустились в шахту\nЗалишилося часу на видобуток - " + MineTimeInfo +
                    "\nВидобуто: <sprite=1> " + Player.pMinedGold + "/" + Player.pMaxMinegGold + " золота";

                if (Player.pMinedGold >= Player.pMaxMinegGold)
                {
                    yield return StartCoroutine(UpdateCellAccount("mining", Player.pMinedGold.ToString(), Player.pID.ToString()));
                    CancelMine();
                }
                else
                {
                    _timeLeft = 60;
                    Timer = StartTimer();
                    StartCoroutine(Timer);
                    yield return StartCoroutine(UpdateCellAccount("mining", Player.pMinedGold.ToString(), Player.pID.ToString()));
                    System.DateTime TimeNext = System.DateTime.Now.AddSeconds(5);
                    yield return StartCoroutine(UpdateCellAccount("timetonextmine", TimeNext.ToString("dd.MM.yyyy HH:mm:ss"), Player.pID.ToString()));
                }
            }
        }
    }

    private IEnumerator StartTimer()
    {
        // Таймер для видобутку
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
            Button.SetActive(false);
            ButtonMine.SetActive(true);
            ButtonCancel.SetActive(true);
            MineInfo.text = "Ви спустились до місця золота.\nШвидше добувайте!";
            return;
        }

        DisplayTime(_timeLeft);
        string TextMined = "";

        if (Player.pMinedGold > 0)
            TextMined = "Ви успішно видобули <sprite=1> " + Player.pLvl + " золота.";

        MineInfo.text = TextMined + "\nВи спускаєтеся до нового місця золота, залишилось часу - " + TimeInfo;
    }

    private IEnumerator StartMineTimer()
    {
        // Таймер для шахти
        while (_timeMineLeft > 0)
        {
            _timeMineLeft -= Time.deltaTime;
            UpdateMineTimeText();
            yield return null;
        }
    }

    private void UpdateMineTimeText()
    {
        if (_timeMineLeft < 0)
        {
            _timeMineLeft = 0;
        }

        DisplayMineTime(_timeMineLeft);
        InfoText.text = "Ви спустились в шахту\nЗалишилося часу на видобуток - " + MineTimeInfo +
            "\nВидобуто: <sprite=1> " + Player.pMinedGold + "/" + Player.pMaxMinegGold + " золота";
    }

    void DisplayMineTime(float timeToDisplay)
    {
        System.TimeSpan remaining = System.TimeSpan.FromSeconds(timeToDisplay);
        MineTimeInfo = remaining.ToString(@"mm\:ss");
    }

    void DisplayTime(float timeToDisplay)
    {
        System.TimeSpan remaining = System.TimeSpan.FromSeconds(timeToDisplay);
        TimeInfo = remaining.ToString(@"mm\:ss");
    }

    private IEnumerator UpdateCellAccount(string cellname, string value, string id)
    {
        // Оновлення даних користувача в базі даних
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("UpdateCell", cellname);
        FindDataBase.AddField("UpdateValue", value);
        FindDataBase.AddField("PlayerID", id);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
    }

    public void mTimer()
    {
        MineTimer = StartMineTimer();
        Timer = StartTimer();
        StartCoroutine(MineTimer);
    }
}