using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class TraderSystem : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public TextMeshProUGUI GreenInfo, GoldInfo, DiamondInfo, DiamondInfoColdown;
    public Text GreenError, GoldError, DiamondError;
    public GameObject objDiamondInfoColdown, objGreenButton, objGoldButton, objDiamondButton;
    public UnityEngine.UI.InputField GreenInput = null, GoldInput = null;
    public PlayerInfo Account;
    private float _timeLeft = 0f, _timeError = 0f;
    private IEnumerator Timer;
    private string TimeInfo;

    public void LoadTrader()
    {
        _timeLeft = 0;
        StartCoroutine(_LoadTrader());
    }

    private IEnumerator _LoadTrader()
    {
        yield return StartCoroutine(LoadAcc());
        UpdateInfoText();
        UpdateButtonStates();
    }

    private void UpdateInfoText()
    {
        GreenInfo.text = GetTradeInfo("green", "<sprite=2> 1 на <sprite=0> ", Account.tradertogreen);
        GoldInfo.text = GetTradeInfo("gold", "<sprite=2> 1 на <sprite=1> ", Account.tradertogold);
        DiamondInfo.text = GetDiamondInfo();

        if (Account.nexttraderdiamond != "0")
        {
            objDiamondInfoColdown.SetActive(true);
            System.DateTime time = System.DateTime.Parse(Account.nexttraderdiamond);
            _timeLeft = (int)(time - System.DateTime.Now).TotalSeconds;
            Timer = StartTimer();
            StartCoroutine(Timer);
            objDiamondButton.SetActive(false);
        }
        else
        {
            objDiamondInfoColdown.SetActive(false);
            objDiamondButton.SetActive(true);
        }

        if (Account.traderdiamonds <= 0)
        {
            objGreenButton.SetActive(false);
            objGoldButton.SetActive(false);
            GreenInfo.text = GetTradeInfo("green", "<sprite=2> 1 на <sprite=0> ", Account.tradertogreen, "Вы больше не можете проводить обмен");
            GoldInfo.text = GetTradeInfo("gold", "<sprite=2> 1 на <sprite=1> ", Account.tradertogold, "Вы больше не можете проводить обмен");
        }
    }

    private string GetTradeInfo(string type, string tradeRate, int tradeAmount, string additionalInfo = "")
    {
        string result = $"Сегодня купец обменивает:\n{tradeRate}{tradeAmount}\nСегодня {additionalInfo}";
        return result;
    }

    private string GetDiamondInfo()
    {
        return $"Сегодня купец обменивает:\n<sprite=1> {Account.traderdiamond * 10} на <sprite=2> {Account.traderdiamond}";
    }

    private void UpdateButtonStates()
    {
        if (Account.traderdiamond == 0)
        {
            int diamondAmount = UnityEngine.Random.Range(9, 21);
            if (Player.Account.playerlvl >= 10 && Player.Account.playerlvl < 15) diamondAmount += 20;
            if (Player.Account.playerlvl >= 15) diamondAmount += 40;

            StartCoroutine(UpdateCellAccount("traderdiamond", diamondAmount.ToString(), Player.Account.id.ToString()));
            DiamondInfo.text = GetDiamondInfo();
        }
    }

    public void ExchangeGreen() { StartCoroutine(_Exchange("playergreen", "GreenInput", GreenError, Account.tradertogreen)); }
    public void ExchangeGold() { StartCoroutine(_Exchange("playergold", "GoldInput", GoldError, Account.tradertogold)); }
    public void ExchangeDiamond() { StartCoroutine(_ExchangeDiamond()); }

    private IEnumerator _Exchange(string cellName, string inputName, Text errorText, int tradeRate)
    {
        UnityEngine.UI.InputField input = cellName == "playergreen" ? GreenInput : GoldInput;

        if (input.text != "")
        {
            if (int.Parse(input.text) <= Account.traderdiamonds && int.Parse(input.text) > 0)
            {
                if (Player.Account.playerdiamonds >= int.Parse(input.text))
                {
                    int updatedValue = Player.Account.playergreen + (int.Parse(input.text) * tradeRate);
                    yield return StartCoroutine(UpdateCellAccount(cellName, updatedValue.ToString(), Player.Account.id.ToString()));

                    int playerDiamonds = Player.Account.playerdiamonds - int.Parse(input.text);
                    yield return StartCoroutine(UpdateCellAccount("playerdiamonds", playerDiamonds.ToString(), Player.Account.id.ToString()));

                    int traderDiamonds = Account.traderdiamonds - int.Parse(input.text);
                    yield return StartCoroutine(UpdateCellAccount("traderdiamonds", traderDiamonds.ToString(), Player.Account.id.ToString()));

                    Player.ReloadInfoBar();
                    StartCoroutine(_LoadTrader());
                }
                else
                {
                    errorText.text = "У Вас недостаточно алмазов для обмена!";
                    _timeError = 10;
                    StartCoroutine(TimerError(errorText));
                }
            }
            else
            {
                errorText.text = $"Введите значение от 1 до {Account.traderdiamonds}";
                _timeError = 10;
                StartCoroutine(TimerError(errorText));
            }
        }
        else
        {
            errorText.text = "Поле не может быть пустым!";
            _timeError = 10;
            StartCoroutine(TimerError(errorText));
        }
    }

    private IEnumerator _ExchangeDiamond()
    {
        if (Player.Account.playergold >= Account.traderdiamond * 10)
        {
            int updatedGold = Player.Account.playergold - (Account.traderdiamond * 10);
            yield return StartCoroutine(UpdateCellAccount("playergold", updatedGold.ToString(), Player.Account.id.ToString()));

            int updatedDiamonds = Player.Account.playerdiamonds + Account.traderdiamond;
            yield return StartCoroutine(UpdateCellAccount("playerdiamonds", updatedDiamonds.ToString(), Player.Account.id.ToString()));

            System.DateTime timeNext = System.DateTime.Now.AddDays(7);
            yield return StartCoroutine(UpdateCellAccount("nexttraderdiamond", timeNext.ToString("dd.MM.yyyy HH:mm:ss"), Player.Account.id.ToString()));

            Player.ReloadInfoBar();
            StartCoroutine(_LoadTrader());
        }
        else
        {
            DiamondError.text = "У Вас недостаточно алмазов для обмена!";
            _timeError = 10;
            StartCoroutine(TimerError(DiamondError));
        }
    }

    private IEnumerator LoadAcc()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadAccount", "Yes");
        FindDataBase.AddField("PlayerName", Player.Account.nickname);
        FindDataBase.AddField("PlayerSerialCode", Player.Account.serialcode);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Ошибка: " + www.error);
        }

        string jsonformat = www.downloadHandler.text;
        Account = JsonUtility.FromJson<PlayerInfo>(jsonformat);
        www.Dispose();
    }

    private IEnumerator UpdateCellAccount(string cellName, string value, string id)
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("UpdateCell", cellName);
        FindDataBase.AddField("UpdateValue", value);
        FindDataBase.AddField("PlayerID", id);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
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
            objDiamondInfoColdown.SetActive(false);
            objDiamondButton.SetActive(true);
        }

        DisplayTime((int)(System.DateTime.Parse(Account.nexttraderdiamond) - System.DateTime.Now).TotalSeconds);
        DiamondInfoColdown.text = "В связи со скупостью купца обмен проводится 1 раз в 7 дней.\nСледующий раз ты сможешь обменять через " + TimeInfo;
    }

    void DisplayTime(float timeToDisplay)
    {
        System.TimeSpan remaining = System.TimeSpan.FromSeconds(timeToDisplay);
        TimeInfo = remaining.ToString(@"dd\:hh\:mm\:ss");
    }

    private IEnumerator TimerError(Text errorText)
    {
        while (_timeError > 0)
        {
            _timeError -= Time.deltaTime;
            UpdateTime(errorText);
            yield return null;
        }
    }

    private void UpdateTime(Text errorText)
    {
        if (_timeError < 0)
        {
            _timeError = 0;
            errorText.text = "";
        }
    }

    [System.Serializable]
    public class PlayerInfo
    {
        public int traderdiamonds, tradertogreen, tradertogold, traderdiamond;
        public string nexttraderdiamond;
    }
}