using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ShopItemPrice : MonoBehaviour
{
    private string playerID, id = "ID";
    private string jsonFormat;
    [SerializeField] private Text description, itemID;
    [SerializeField] private GameObject button;
    private int _itemID;

    void Start()
    {
        playerID = PlayerPrefs.GetString(id);
        _itemID = int.Parse(itemID.text);
        StartCoroutine(ItemsLoad());
    }

    public void ReloadDescription()
    {
        playerID = PlayerPrefs.GetString(id);
        _itemID = int.Parse(itemID.text);
        StartCoroutine(ItemsLoad());
    }

    private IEnumerator ItemsLoad()
    {
        yield return LoadItems();
    }

    private IEnumerator LoadItems()
    {
        WWWForm findDataBase = new WWWForm();
        findDataBase.AddField("OnGameRequest", "Yes");
        findDataBase.AddField("LoadItemToID", "Yes");
        findDataBase.AddField("ItemID", _itemID);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadshopitems.php", findDataBase);
        yield return www.SendWebRequest();

        jsonFormat = www.downloadHandler.text;
        ItemInfo data = JsonUtility.FromJson<ItemInfo>(jsonFormat);

        string ipower = "", iprotection = "", idexterity = "", iskill = "", ivitability = "";

        if (data.itempower != 0) ipower = "\nСила: +" + data.itempower.ToString();
        if (data.itemprotection != 0) iprotection = "\nЗащита: +" + data.itemprotection.ToString();
        if (data.itemdexterity != 0) idexterity = "\nЛовкость: +" + data.itemdexterity.ToString();
        if (data.itemskill != 0) iskill = "\nМастерство: +" + data.itemskill.ToString();
        if (data.itemvitability != 0) ivitability = "\nЖивучесть: +" + data.itemvitability.ToString();

        description.color = new Color(0.19f, 0.19f, 0.19f);
        description.text = "Уровень: " + data.level + ipower + iprotection + idexterity + iskill + ivitability;

        yield return AvailabilityItem();

        www.Dispose();
    }

    public class ItemInfo
    {
        public int level, price, itempower, itemprotection, itemdexterity, itemskill, itemvitability;
        public string name;
    }

    private IEnumerator AvailabilityItem()
    {
        WWWForm findDataBase = new WWWForm();
        findDataBase.AddField("OnGameRequest", "Yes");
        findDataBase.AddField("AvailabilityItem", "Yes");
        findDataBase.AddField("PlayerID", playerID);
        findDataBase.AddField("ItemID", _itemID);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", findDataBase);
        yield return www.SendWebRequest();

        button.SetActive(www.downloadHandler.text == "0");
        www.Dispose();
    }
}