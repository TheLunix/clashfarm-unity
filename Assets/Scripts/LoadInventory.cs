using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LoadInventory : MonoBehaviour
{
    private string NickName, SerialCode, PlayerLvl, nick = "Name", code = "SerialCode", lvl = "Lvl", jsonformat, PlayerID, id = "ID";
    public int InventoryCount, pInventory, pInventoryCount;
    private float PosX, PosY;
    private GameObject[] Item;
    private Vector2[] ItemPos, ItemScale;
    
    [Range(1, 500)]
    public int ItemOffset;

    [Header("Other Items")]
    public GameObject PrefabItem;
    [SerializeField] private Sprite BuyInvSlot;
    public ItemJS[] Items;

    public void ReloadInventory()
    {
        // Завантаження даних з PlayerPrefs
        this.NickName = PlayerPrefs.GetString(nick);
        this.PlayerLvl = PlayerPrefs.GetString(lvl);
        this.SerialCode = PlayerPrefs.GetString(code);
        this.PlayerID = PlayerPrefs.GetString(id);

        // Очистити існуючі елементи і перезавантажити інвентар
        for(int i = 0; i < InventoryCount; i++)
        {
            Destroy(Item[i]);
        }
        StartCoroutine(ItemsLoad());
    }

    private IEnumerator ItemsLoad()
    {
        yield return StartCoroutine(LoadInventoryCount());
        yield return StartCoroutine(InventoryLoad());
        Item = new GameObject[InventoryCount];
        ItemPos = new Vector2[InventoryCount];
        ItemScale = new Vector2[InventoryCount];
        
        for(int i = 0; i < InventoryCount; i++)
        {
            Item[i] = Instantiate(PrefabItem, transform, false);
            if(i < pInventoryCount)
            {
                GameObject Usability = Item[i].transform.Find("ItemID").gameObject;
                Text IDText = Usability.GetComponent<Text>();
                IDText.text = Items[i].id.ToString();
                
                Image ImPrewiew = Item[i].GetComponent<Image>();
                ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
            }

            GameObject InvCount = Item[i].transform.Find("InventoryCount").gameObject;
            Text InvText = InvCount.GetComponent<Text>();
            InvText.text = pInventory.ToString();
            if(i == 0) continue;

            // Логіка розташування елементів інвентарю
            if(i > 0) { PosX = Item[i - 1].transform.localPosition.x; PosY = Item[i].transform.localPosition.y;}
            if(i >= 6) { PosX = Item[i - 6].transform.localPosition.x - 120f; PosY = Item[i].transform.localPosition.y - 120f;}
            if(i >= 12) { PosX = Item[i - 12].transform.localPosition.x - 120f; PosY = Item[i].transform.localPosition.y - 240f;}
            if(i >= 18) { PosX = Item[i - 18].transform.localPosition.x - 120f; PosY = Item[i].transform.localPosition.y - 360f;}
            Item[i].transform.localPosition = new Vector2(PosX + PrefabItem.GetComponent<RectTransform>().sizeDelta.x + ItemOffset, PosY);
            ItemPos[i] = -Item[i].transform.localPosition;

            if(i == InventoryCount - 1 && pInventory != 24)
            {
                Image ImageBuy = Item[i].GetComponent<Image>();
                ImageBuy.sprite = BuyInvSlot;

                GameObject Usability = Item[i].transform.Find("ItemID").gameObject;
                Text IDText = Usability.GetComponent<Text>();
                IDText.text = "777";
            }
        }
    }

    private IEnumerator LoadInventoryCount()
    {
        // Запит на сервер для завантаження кількості інвентарних слотів
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadAccount", "Yes");
        FindDataBase.AddField("PlayerName", this.NickName);
        FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        PlayerInfo Data = JsonUtility.FromJson<PlayerInfo>(jsonformat);
        InventoryCount = Data.inventory;
        pInventory = Data.inventory;
        if(InventoryCount != 24) InventoryCount = pInventory + 1;
        www.Dispose();
    }

    public class PlayerInfo
    {
        public int inventory;
    }

    private IEnumerator InventoryLoad()
    {
        // Запит на сервер для завантаження інвентарних елементів
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadInventory", "Yes");
        FindDataBase.AddField("PlayerID", this.PlayerID);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        if(jsonformat == "0") { pInventoryCount = 0; }
        else { Items = JsonHelper.FromJson<ItemJS>(fixJson(jsonformat)); pInventoryCount = Items.Length;}
        www.Dispose();
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
        public int id, id_item, type_item, count, usability;
    }

    string fixJson(string value)
    {
        value = "{\"Items\":" + value + "}";
        return value;
    }
}