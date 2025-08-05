using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PetBuy : MonoBehaviour
{
    public PetPanel PanelPet;
    public LoadAndUpdateAccount Player;
    public Button ButtonBuy, ButtonConfirm, ButtonCancel, FoneMessageBox;
    public Button[] Avatars;
    public GameObject ErrorText, MessageBox, PanelBuy, PanelInfo;
    public Text TextError, Description, MessageText, PetPriceText;

    private int SelectPet = 0, PetPrice = 600;
    private float _timeLeft = 10f;
    private string[] PetName;

    void Start()
    {
        StartCoroutine(GetDiscount());
        ButtonBuy.onClick.AddListener(BuyPet);

        PetName = new string[4];
        Avatars[0].onClick.AddListener(delegate() { PetSelect(1); });
        PetName[0] = "Вовк";
        Avatars[1].onClick.AddListener(delegate() { PetSelect(2); });
        PetName[1] = "Ведмідь";
        Avatars[2].onClick.AddListener(delegate() { PetSelect(3); });
        PetName[2] = "Тигр";
        Avatars[3].onClick.AddListener(delegate() { PetSelect(4); });
        PetName[3] = "Пантера";

        ButtonConfirm.onClick.AddListener(ConfirmBuy);
        ButtonCancel.onClick.AddListener(CloseMessageBox);
        FoneMessageBox.onClick.AddListener(CloseMessageBox);
    }

    private void BuyPet()
    {
        if (SelectPet == 0)
        {
            StartCoroutine(ErrorTimer());
            TextError.text = "Виберіть питомця для покупки!";
        }
        else
        {
            if (Player.pGold < PetPrice)
            {
                StartCoroutine(ErrorTimer());
                TextError.text = "Недостатньо золота для покупки!";
            }
            else
            {
                MessageBox.SetActive(true);
            }
        }
    }

    private IEnumerator ErrorTimer()
    {
        ErrorText.SetActive(true);
        _timeLeft = 10;
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
            ErrorText.SetActive(false);
        }
    }

    private void PetSelect(int Select)
    {
        SelectPet = Select;

        if (Select == 1)
        {
            Description.text = "Одинокі вовки зазвичай не представляють небезпеки. Без своєї стаї ці тварини стають набагато обережнішими.";
        }

        if (Select == 2)
        {
            Description.text = "Ведмеді - це досить молодий вид живих істот. Вони існують приблизно 5 мільйонів років, більше-менше.";
        }

        if (Select == 3)
        {
            Description.text = "За допомогою реву та гучкого ричання тигри спілкуються один з одним на великій відстані. Розлючені тигри ніколи не ричать - вони шиплять.";
        }

        if (Select == 4)
        {
            Description.text = "Чорні пантери проводять більшу частину часу на землі, але вони чудово лазять по деревах.";
        }
    }

    private void ConfirmBuy()
    {
        StartCoroutine(CoroutineConfirmBuy());
    }

    private IEnumerator CoroutineConfirmBuy()
    {
        yield return StartCoroutine(UpdateCellAccount("pet", "2", Player.pID.ToString()));
        int difGold = Player.pGold - PetPrice;
        yield return StartCoroutine(UpdateCellAccount("playergold", difGold.ToString(), Player.pID.ToString()));
        yield return StartCoroutine(CreatePet());
        Player.ReloadInfoBar();
        PanelPet.Panel.SetActive(false);
    }

    private void CloseMessageBox()
    {
        MessageBox.SetActive(false);
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

    private IEnumerator GetDiscount()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("GetServerCell", "Yes");

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();
        string jsonformat = www.downloadHandler.text;
        ServerInfo Data = JsonUtility.FromJson<ServerInfo>(jsonformat);
        int Price = 0;

        if (Data.petdiscount != 1)
        {
            Price = PetPrice - Mathf.FloorToInt(PetPrice * Data.petdiscount);
        }
        else
        {
            Price = PetPrice;
        }

        PetPrice = Price;
        PetPriceText.text = "Ціна: " + Price.ToString() + " золота";
        www.Dispose();
    }

    public class ServerInfo
    {
        public float petdiscount;
    }

    private IEnumerator CreatePet()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("CreatePet", "Yes");
        FindDataBase.AddField("PlayerID", Player.pID.ToString());
        FindDataBase.AddField("PetAvatar", SelectPet.ToString());
        FindDataBase.AddField("PetName", PetName[SelectPet - 1]);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/registeraccount.php", FindDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
    }
}