using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class HomeSystem : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public HomeButton Home;
    [Header("Horse System")]
    public GameObject HorseMessageBox;
    public GameObject HorseErrorText;
    public Button HorseButton;
    public TextMeshProUGUI HorseInfo;
    private string MineHorseInfo;
    public float _timeHorseLeft = 0f, _timeErrorsLeft = 0f;
    public IEnumerator HorseTimer;

    private void Start()
    {
        HorseTimer = StartHorseTimer();
        HorseButton.onClick.AddListener(BuyHorseRequest);
    }

    public void BuyHorseRequest()
    {
        // Відображення повідомлення про купівлю коня
        HorseMessageBox.SetActive(true);
    }

    public void BuyHorse()
    {
        StartCoroutine(_BuyHorse());
    }

    private IEnumerator _BuyHorse()
    {
        // Закриття повідомлення про купівлю коня
        HorseMessageBox.SetActive(false);

        if (Player.pGold >= 50)
        {
            if (Player.pHorse == "0")
            {
                // Зменшення золота та оновлення часу дії коня
                int Gold = Player.pGold - 50;
                yield return StartCoroutine(UpdateCellAccount("playergold", Gold.ToString(), Player.pID.ToString()));
                System.DateTime TimeNext = System.DateTime.Now.AddHours(340);
                yield return StartCoroutine(UpdateCellAccount("horsetime", TimeNext.ToString("dd.MM.yyyy HH:mm:ss"), Player.pID.ToString()));
                yield return StartCoroutine(UpdateCellAccount("horse", "1", Player.pID.ToString()));
                _timeHorseLeft = 1152000;
                StartCoroutine(HorseTimer);
            }
            else
            {
                // Зменшення золота та оновлення часу дії коня
                int Gold = Player.pGold - 50;
                yield return StartCoroutine(UpdateCellAccount("playergold", Gold.ToString(), Player.pID.ToString()));
                System.DateTime TimeNext = System.DateTime.Parse(Player.pHorse).AddHours(340);
                yield return StartCoroutine(UpdateCellAccount("horsetime", TimeNext.ToString("dd.MM.yyyy HH:mm:ss"), Player.pID.ToString()));
                Player.pHorse = TimeNext.ToString("dd.MM.yyyy HH:mm:ss");
                Player.ReloadInfoBar();
                Home.CloseHomePanel();
                Home.OpenHomePanel();
            }
        }
        else
        {
            // Відображення повідомлення про помилку купівлі коня
            HorseErrorText.SetActive(true);
            _timeErrorsLeft = 10;
            StartCoroutine(StartErrorsTimer());
        }
    }

    private IEnumerator UpdateCellAccount(string cellname, string value, string id)
    {
        // Оновлення даних гравця в базі даних
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("UpdateCell", cellname);
        FindDataBase.AddField("UpdateValue", value);
        FindDataBase.AddField("PlayerID", id);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
    }

    public void HorseStartTimer()
    {
        System.DateTime time = System.DateTime.Parse(Player.pHorse);
        _timeHorseLeft = (int)(time - System.DateTime.Now).TotalSeconds;
        HorseTimer = StartHorseTimer();
        StartCoroutine(HorseTimer);
    }

    private IEnumerator StartHorseTimer()
    {
        // Запуск таймера для розрахунку часу дії коня
        System.DateTime time = System.DateTime.Parse(Player.pHorse);
        while (_timeHorseLeft > 0)
        {
            _timeHorseLeft = (int)(time - System.DateTime.Now).TotalSeconds;
            UpdateHorseTimeText();
            yield return null;
        }
    }

    private void UpdateHorseTimeText()
    {
        if (_timeHorseLeft < 0)
        {
            // Обробка закінчення дії коня
            _timeHorseLeft = 0;
            StopCoroutine(HorseTimer);
            StartCoroutine(UpdateCellAccount("horse", "0", Player.pID.ToString()));
            StartCoroutine(UpdateCellAccount("horsetime", "0", Player.pID.ToString()));
            return;
        }
        DisplayHorseTime(_timeHorseLeft);
        HorseInfo.text = "Дія: 340 годин\nЦіна: <sprite=1> 50 золота\nБуде діяти ще: \n" + MineHorseInfo;
    }

    void DisplayHorseTime(float timeToDisplay)
    {
        System.TimeSpan remaining = System.TimeSpan.FromSeconds(timeToDisplay);
        MineHorseInfo = remaining.ToString(@"dd\:hh\:mm\:ss");
    }

    private IEnumerator StartErrorsTimer()
    {
        while (_timeErrorsLeft > 0)
        {
            _timeErrorsLeft -= Time.deltaTime;
            UpdateErrorsTimeText();
            yield return null;
        }
    }

    private void UpdateErrorsTimeText()
    {
        if (_timeErrorsLeft < 0)
        {
            _timeErrorsLeft = 0;
            // Закриття повідомлення про помилку
            HorseErrorText.SetActive(false);
            return;
        }
    }
}