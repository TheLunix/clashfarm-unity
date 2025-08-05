using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class TrainingPet : MonoBehaviour
{
    private string jsonformat;
    [SerializeField] private GameObject ButtonUpgrade, ErrorText, PanelInfo, PanelTraining;
    [SerializeField] private Text GreenText, WPower, WProtection, WDexterity, WSkill, WVitability, wName, wDescription, wUpgrade, PlayerHP;
    [SerializeField] private Slider SliderPlayerHP;
    public Button CloseButton;
    private int Select = 1;
    private int PetPower, PetProtect, PetDexterity, PetSkill, PetVitality, PetHP, PetMaxHP;
    public LoadAndUpdateAccount Player;

    private void Start()
    {
        Select = 1;
        CloseButton.onClick.AddListener(ClosePanel);
        StartCoroutine(LoadPetStats());
    }

    public void PetTraining()
    {
        ErrorText.SetActive(false);
        StartCoroutine(PetTrain());
    }

    public IEnumerator PetTrain()
    {
        float upgradepr = Mathf.Pow((GetPetStat() - 4), GetUpgradePower());
        int upgradeprice = Mathf.FloorToInt(upgradepr);

        if (Player.pGreen >= upgradeprice)
        {
            int differencepgreen = Player.pGreen - upgradeprice;
            int newstat = GetPetStat() + 1;

            yield return StartCoroutine(UpdateCellAccount("playergreen", differencepgreen.ToString(), Player.pID.ToString()));
            yield return StartCoroutine(UpdateCellPet(GetStatName(), newstat.ToString(), Player.PetID.ToString()));
            yield return StartCoroutine(LoadPetStats());

            GreenText.text = Player.pGreen.ToString();
            UpdateUI(GetStatName(), GetPetStat(), upgradepr);
        }
        else
        {
            ErrorText.SetActive(true);
        }

        Player.ReloadInfoBar();
    }

    private IEnumerator LoadPetStats()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadPet", "Yes");
        FindDataBase.AddField("PlayerID", Player.pID);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();

        jsonformat = www.downloadHandler.text;
        PetInfo Data = JsonUtility.FromJson<PetInfo>(jsonformat);
        UpdateStatsFromData(Data);

        www.Dispose();
    }

    private void UpdateStatsFromData(PetInfo data)
    {
        PetPower = data.petpower;
        PetProtect = data.petprotect;
        PetDexterity = data.petdexterity;
        PetSkill = data.petskill;
        PetVitality = data.petvitality;
        PetHP = Mathf.FloorToInt(data.pethp);
        PetMaxHP = Mathf.FloorToInt(Mathf.Pow(PetVitality, 1.65f) - 4);
        SliderPlayerHP.maxValue = PetMaxHP;
        PlayerHP.text = $"{PetHP}/{PetMaxHP}";
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

    private IEnumerator UpdateCellPet(string cellname, string value, string id)
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("UpdateCellPet", cellname);
        FindDataBase.AddField("UpdateValue", value);
        FindDataBase.AddField("PetID", id);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
        yield return www.SendWebRequest();

        www.Dispose();
    }

    public void ChangeSelection(int selection)
    {
        Select = selection;
        ErrorText.SetActive(false);
        SetUpgradeUI(GetStatName(), GetUpgradePower());
    }

    private void SetUpgradeUI(string statName, float upgradePower)
    {
        wName.text = statName;
        wDescription.text = GetStatDescription(statName);
        float upgradepr = Mathf.Pow((GetPetStat() - 4), upgradePower);
        int upgradeprice = Mathf.FloorToInt(upgradepr);
        wUpgrade.text = $"Уровень: {GetPetStat()}\nЦена улучшения: {upgradeprice}";
    }

    private void UpdateUI(string statName, int statValue, float upgradepr)
    {
        switch (statName)
        {
            case "Сила": WPower.text = $"Сила: {statValue}"; break;
            case "Защита": WProtection.text = $"Защита: {statValue}"; break;
            case "Ловкость": WDexterity.text = $"Ловкость: {statValue}"; break;
            case "Мастерство": WSkill.text = $"Мастерство: {statValue}"; break;
            case "Живучесть": WVitability.text = $"Живучесть: {statValue}"; break;
        }

        upgradepr = Mathf.Pow((statValue - 4), upgradepr);
        int newUpgradePrice = Mathf.FloorToInt(upgradepr);
        wUpgrade.text = $"Уровень: {statValue}\nЦена улучшения: {newUpgradePrice}";
    }

    private string GetStatName()
    {
        switch (Select)
        {
            case 1: return "petpower";
            case 2: return "petprotect";
            case 3: return "petdexterity";
            case 4: return "petskill";
            case 5: return "petvitality";
            default: return "";
        }
    }

    private float GetUpgradePower()
    {
        switch (Select)
        {
            case 1: return 2.6f;
            case 2: return 2.35f;
            case 3: return 2.3f;
            case 4: return 2.5f;
            case 5: return 2.45f;
            default: return 0f;
        }
    }

    private string GetStatDescription(string statName)
    {
        switch (statName)
        {
            case "petpower": return "Увеличивает урон наносимый противнику";
            case "petprotect": return "Уменьшает получаемый урон";
            case "petdexterity": return "Увеличивает возможность уворота от удара";
            case "petskill": return "Увеличивает возможность нанести критический удар";
            case "petvitality": return "Увеличивает максимальное количество здоровья";
            default: return "";
        }
    }

    private int GetPetStat()
    {
        switch (Select)
        {
            case 1: return PetPower;
            case 2: return PetProtect;
            case 3: return PetDexterity;
            case 4: return PetSkill;
            case 5: return PetVitality;
            default: return 0;
        }
    }

    public void ClosePanel()
    {
        PanelInfo.SetActive(true);
        PanelTraining.SetActive(false);
    }

    public class PetInfo
    {
        public int petpower, petprotect, petdexterity, petskill, petvitality;
        public float pethp;
    }
}