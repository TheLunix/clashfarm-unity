using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class GetReward : MonoBehaviour
{
    [SerializeField] private Text CountGreen, CountGold, CountDiamonds;
    [SerializeField] private TextMeshProUGUI HourRewardText;
    [SerializeField] private GameObject HourRewardButton, RewardIcon;
    [SerializeField] private Image RewardImage;
    [SerializeField] private Sprite[] RewardIcons;

    private const string NickKey = "Name";
    private const string CodeKey = "SerialCode";
    private const string IDKey = "ID";

    private string NickName, SerialCode, PlayerID, jsonformat;
    private int randomgreen, randomgold, randomdiamond, imagereward, pgreen, pgold, pdiamond;

    private const int GreenMin = 100;
    private const int GreenMax = 501;
    private const int GoldMin = 1;
    private const int GoldMax = 11;
    private const int DiamondMin = 1;
    private const int DiamondMax = 6;

    public void RewardGet()
    {
        NickName = PlayerPrefs.GetString(NickKey);
        SerialCode = PlayerPrefs.GetString(CodeKey);
        PlayerID = PlayerPrefs.GetString(IDKey);
        StartCoroutine(StartGiveReward());
    }

    private IEnumerator StartGiveReward()
    {
        yield return LoadMoneyInfo();
        int shancereward = Random.Range(1, 101);

        if (shancereward > 0 && shancereward <= 80)
        {
            randomgreen = Random.Range(GreenMin, GreenMax);
            pgreen += randomgreen;
            CountGreen.text = pgreen.ToString();
            imagereward = 0;
            HourRewardText.text = $"Отлично!\nТвоя награда:\n<sprite=0>{randomgreen}";
        }
        else if (shancereward > 80 && shancereward <= 95)
        {
            randomgold = Random.Range(GoldMin, GoldMax);
            pgold += randomgold;
            CountGold.text = pgold.ToString();
            imagereward = 1;
            HourRewardText.text = $"Отлично!\nТвоя награда:\n<sprite=1>{randomgold}";
        }
        else
        {
            randomdiamond = Random.Range(DiamondMin, DiamondMax);
            pdiamond += randomdiamond;
            CountDiamonds.text = pdiamond.ToString();
            imagereward = 2;
            HourRewardText.text = $"Отлично!\nТвоя награда:\n<sprite=2>{randomdiamond}";
        }

        RewardIcon.SetActive(true);
        RewardImage.sprite = RewardIcons[imagereward];
        HourRewardButton.SetActive(false);
        StartCoroutine(SetGiveReward());
    }

    private IEnumerator SetGiveReward()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("SetGiveReward", "Yes");
        FindDataBase.AddField("PlayerID", PlayerID);

        if (randomgreen != 0) FindDataBase.AddField("PlayerGreen", pgreen.ToString());
        if (randomgold != 0) FindDataBase.AddField("PlayerGold", pgold.ToString());
        if (randomdiamond != 0) FindDataBase.AddField("PlayerDiamond", pdiamond.ToString());

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
    }

    private IEnumerator LoadMoneyInfo()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadAccount", "Yes");
        FindDataBase.AddField("PlayerName", NickName);
        FindDataBase.AddField("PlayerSerialCode", SerialCode);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        PlayerInfo Data = JsonUtility.FromJson<PlayerInfo>(jsonformat);

        pgreen = Data.playergreen;
        pgold = Data.playergold;
        pdiamond = Data.playerdiamonds;
        www.Dispose();
    }

    [System.Serializable]
    public class PlayerInfo
    {
        public int playergreen, playergold, playerdiamonds;
    }
}