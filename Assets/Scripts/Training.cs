using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Training : MonoBehaviour
{
    private string NickName, SerialCode, nick = "Name", code = "SerialCode", PlayerID, id = "ID", jsonformat;
    [SerializeField] private GameObject ButtonUpgrade, ErrorText;
    [SerializeField] private Text GreenText, WPower, WProtection, WDexterity, WSkill, WVitability, wName, wDescription, wUpgrade, PlayerHP;
    [SerializeField] private Slider SliderPlayerHP;
    private int Select = 1;
    private int power, protection, dexterity, skill, vitability, php;
    public LoadAndUpdateAccount Player;

    private void Start()
    {
        Select = 1;
        NickName = PlayerPrefs.GetString(nick);
        SerialCode = PlayerPrefs.GetString(code);
        PlayerID = PlayerPrefs.GetString(id);
        StartCoroutine(LoadPlayerStats());
    }

    public void PlayerTraining()
    {
        ErrorText.SetActive(false);
        StartCoroutine(PlayerTrain());
    }

    public IEnumerator PlayerTrain()
    {
        float upgradepr = 0;
        int upgradeprice = 0;

        switch (Select)
        {
            case 1: //power
                upgradepr = Mathf.Pow((power - 4), (float)2.6);
                upgradeprice = Mathf.FloorToInt(upgradepr);

                if (Player.pGreen >= upgradeprice)
                {
                    int differencepgreen = Player.pGreen - upgradeprice;
                    int newstat = power + 1;
                    yield return StartCoroutine(UpdateCellAccount("playergreen", differencepgreen.ToString()));
                    yield return StartCoroutine(UpdateCellAccount("playerpower", newstat.ToString()));
                    yield return StartCoroutine(LoadPlayerStats());
                    UpdateUI("Сила", WPower, power, upgradepr);
                }
                else { ErrorText.SetActive(true); }
                break;

            case 2: //protection
                upgradepr = Mathf.Pow((protection - 4), (float)2.35);
                upgradeprice = Mathf.FloorToInt(upgradepr);

                if (Player.pGreen >= upgradeprice)
                {
                    int differencepgreen = Player.pGreen - upgradeprice;
                    int newstat = protection + 1;
                    yield return StartCoroutine(UpdateCellAccount("playergreen", differencepgreen.ToString()));
                    yield return StartCoroutine(UpdateCellAccount("playerprotection", newstat.ToString()));
                    yield return StartCoroutine(LoadPlayerStats());
                    UpdateUI("Защита", WProtection, protection, upgradepr);
                }
                else { ErrorText.SetActive(true); }
                break;

            case 3: //dexterity
                upgradepr = Mathf.Pow((dexterity - 4), (float)2.3);
                upgradeprice = Mathf.FloorToInt(upgradepr);

                if (Player.pGreen >= upgradeprice)
                {
                    int differencepgreen = Player.pGreen - upgradeprice;
                    int newstat = dexterity + 1;
                    yield return StartCoroutine(UpdateCellAccount("playergreen", differencepgreen.ToString()));
                    yield return StartCoroutine(UpdateCellAccount("playerdexterity", newstat.ToString()));
                    yield return StartCoroutine(LoadPlayerStats());
                    UpdateUI("Ловкость", WDexterity, dexterity, upgradepr);
                }
                else { ErrorText.SetActive(true); }
                break;

            case 4: //skill
                upgradepr = Mathf.Pow((skill - 4), (float)2.5);
                upgradeprice = Mathf.FloorToInt(upgradepr);

                if (Player.pGreen >= upgradeprice)
                {
                    int differencepgreen = Player.pGreen - upgradeprice;
                    int newstat = skill + 1;
                    yield return StartCoroutine(UpdateCellAccount("playergreen", differencepgreen.ToString()));
                    yield return StartCoroutine(UpdateCellAccount("playerskill", newstat.ToString()));
                    yield return StartCoroutine(LoadPlayerStats());
                    UpdateUI("Мастерство", WSkill, skill, upgradepr);
                }
                else { ErrorText.SetActive(true); }
                break;

            case 5: //vitability
                upgradepr = Mathf.Pow((vitability - 4), (float)2.45);
                upgradeprice = Mathf.FloorToInt(upgradepr);

                if (Player.pGreen >= upgradeprice)
                {
                    int differencepgreen = Player.pGreen - upgradeprice;
                    int newstat = vitability + 1;
                    yield return StartCoroutine(UpdateCellAccount("playergreen", differencepgreen.ToString()));
                    yield return StartCoroutine(UpdateCellAccount("playersurvivability", newstat.ToString()));
                    yield return StartCoroutine(LoadPlayerStats());
                    UpdateUI("Живучесть", WVitability, vitability, upgradepr);

                    int maxhp = vitability * 75;
                    SliderPlayerHP.maxValue = maxhp;
                    PlayerHP.text = $"{php}/{maxhp}";
                }
                else { ErrorText.SetActive(true); }
                break;
        }

        Player.ReloadInfoBar();
    }

    private void UpdateUI(string statName, Text statText, int statValue, float upgradepr)
    {
        GreenText.text = Player.pGreen.ToString();
        statText.text = $"{statName}: {statValue}";
        upgradepr = Mathf.Pow((statValue - 4), upgradepr);
        int newUpgradePrice = Mathf.FloorToInt(upgradepr);
        wUpgrade.text = $"Уровень: {statValue}\nЦена улучшения: {newUpgradePrice}";
    }

    private IEnumerator LoadPlayerStats()
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
        UpdateStatsFromData(Data);

        www.Dispose();
    }

    private void UpdateStatsFromData(PlayerInfo data)
    {
        power = data.playerpower;
        protection = data.playerprotection;
        dexterity = data.playerdexterity;
        skill = data.playerskill;
        vitability = data.playersurvivability;
        php = data.playerhp;
    }

    private IEnumerator UpdateCellAccount(string cellname, string value)
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("UpdateCell", cellname);
        FindDataBase.AddField("UpdateValue", value);
        FindDataBase.AddField("PlayerName", NickName);
        FindDataBase.AddField("PlayerID", PlayerID);
        FindDataBase.AddField("PlayerSerialCode", SerialCode);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
    }

    public void ChangeSelection(int selection)
    {
        Select = selection;
        ErrorText.SetActive(false);

        switch (selection)
        {
            case 1: SetUpgradeUI("Сила", WPower, 2.6f); break;
            case 2: SetUpgradeUI("Защита", WProtection, 2.35f); break;
            case 3: SetUpgradeUI("Ловкость", WDexterity, 2.3f); break;
            case 4: SetUpgradeUI("Мастерство", WSkill, 2.5f); break;
            case 5: SetUpgradeUI("Живучесть", WVitability, 2.45f); break;
        }
    }

    private void SetUpgradeUI(string statName, Text statText, float upgradePower)
    {
        wName.text = statName;
        wDescription.text = GetStatDescription(statName);
        float upgradepr = Mathf.Pow((GetStatValue(statName) - 4), upgradePower);
        int upgradeprice = Mathf.FloorToInt(upgradepr);
        wUpgrade.text = $"Уровень: {GetStatValue(statName)}\nЦена улучшения: {upgradeprice}";
    }

    private string GetStatDescription(string statName)
    {
        switch (statName)
        {
            case "Сила": return "Увеличивает урон наносимый противнику";
            case "Защита": return "Уменьшает получаемый урон";
            case "Ловкость": return "Увеличивает возможность уворота от удара";
            case "Мастерство": return "Увеличивает возможность нанести критический удар";
            case "Живучесть": return "Увеличивает максимальное количество здоровья";
            default: return "";
        }
    }

    private int GetStatValue(string statName)
    {
        switch (statName)
        {
            case "Сила": return power;
            case "Защита": return protection;
            case "Ловкость": return dexterity;
            case "Мастерство": return skill;
            case "Живучесть": return vitability;
            default: return 0;
        }
    }

    public class PlayerInfo
    {
        public int playerpower, playerprotection, playerdexterity, playerskill, playersurvivability, playerhp;
    }
}