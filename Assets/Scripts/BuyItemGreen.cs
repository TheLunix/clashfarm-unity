using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class BuyItemGreen : MonoBehaviour
{
    [SerializeField] private GameObject Button;
    public Text Description;
    [SerializeField] private Text IDItem;

    private string NickName, SerialCode, PlayerID;
    private int playergreen, PlayerInventory, itemprice, type, CountItems, ItemID, Confirmation;
    private string jsonformat, NameItem;

    private const string NickKey = "Name";
    private const string CodeKey = "SerialCode";
    private const string IDKey = "ID";

    private void Awake()
    {
        NickName = PlayerPrefs.GetString(NickKey);
        SerialCode = PlayerPrefs.GetString(CodeKey);
        PlayerID = PlayerPrefs.GetString(IDKey);
    }

    public void BuyItem()
    {
        ItemID = int.Parse(IDItem.text);
        StartCoroutine(ExecuteBuyItem());
    }

    private IEnumerator ExecuteBuyItem()
    {
        yield return LoadItems();
    }

    private IEnumerator LoadItems()
    {
        yield return LoadAcc();
        yield return LoadItem();
        yield return LoadInventory();

        if (Confirmation == 1)
        {
            if (playergreen < itemprice)
            {
                ShowErrorMessage("Недостаточно зелени!");
            }
            else if (CountItems >= PlayerInventory)
            {
                ShowErrorMessage("Недостаточно места в инвентаре!");
            }
            else
            {
                StartCoroutine(ItemBuy());
            }
        }
        else
        {
            ShowConfirmationMessage();
        }
    }

    private void ShowErrorMessage(string message)
    {
        Description.color = new Color(0.52f, 0.17f, 0.17f);
        Description.text = "Вы не можете это купить\n- " + message;
        Button.SetActive(false);
        Confirmation = 0;
    }

    private void ShowConfirmationMessage()
    {
        Description.color = new Color(0.19f, 0.19f, 0.19f);
        Description.text = "Вы уверены, что хотите купить \"" + NameItem + "\"\nза " + itemprice + " зелени?";
        Confirmation = 1;
    }

    private IEnumerator ItemBuy()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("BuyItem", "Yes");
        FindDataBase.AddField("PlayerID", PlayerID);
        FindDataBase.AddField("ItemID", ItemID.ToString());
        FindDataBase.AddField("ItemType", type.ToString());
        FindDataBase.AddField("Count", "1");
        FindDataBase.AddField("Usability", "0");

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadshopitems.php", FindDataBase);
        yield return www.SendWebRequest();
        Description.text = "Вы купили \"" + NameItem + "\"\nза " + itemprice + " зелени!";
        Button.SetActive(false);
        Confirmation = 0;
        www.Dispose();
    }

    private IEnumerator LoadInventory()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadInventory", "Yes");
        FindDataBase.AddField("PlayerID", PlayerID);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        CountItems = (jsonformat == "0") ? 0 : JsonHelper.FromJson<ItemJS>(fixJson(jsonformat)).Length;
        www.Dispose();
    }

    private IEnumerator LoadItem()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadItemToID", "Yes");
        FindDataBase.AddField("ItemID", ItemID);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadshopitems.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        ItemInfo Data = JsonUtility.FromJson<ItemInfo>(jsonformat);
        ItemID = Data.id;
        type = Data.type;
        itemprice = Data.price;
        NameItem = Data.name;
        www.Dispose();
    }

    private IEnumerator LoadAcc()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadAccount", "Yes");
        FindDataBase.AddField("PlayerName", NickName);
        FindDataBase.AddField("PlayerSerialCode", SerialCode);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Ошибка: " + www.error);
        }
        jsonformat = www.downloadHandler.text;
        PlayerInfo Data = JsonUtility.FromJson<PlayerInfo>(jsonformat);
        playergreen = Data.playergreen;
        PlayerInventory = Data.inventory;
        www.Dispose();
    }

    [Serializable]
    public class ItemInfo
    {
        public int price, id, type;
        public string name;
    }

    public class PlayerInfo
    {
        public int playergreen, inventory;
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }

    [Serializable]
    public class ItemJS
    {
        public string name;
        public int price;
    }

    string fixJson(string value)
    {
        value = "{\"Items\":" + value + "}";
        return value;
    }
}