using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

public class InventorySlotUse : MonoBehaviour
{
    private string NickName, SerialCode, PlayerID, jsonformat, IDItem;
    private int InventoryCount;
    public ItemInfo Data;
    public ItemShopInfo ItemShop;
    public Text ItemID, ICount;

    // Метод, який викликається при кліку на елемент інвентаря
    public void ClickItem()
    {
        // Отримуємо дані гравця
        this.NickName = PlayerPrefs.GetString("Name");
        this.SerialCode = PlayerPrefs.GetString("SerialCode");
        this.PlayerID = PlayerPrefs.GetString("ID");
        IDItem = ItemID.text;
        InventoryCount = int.Parse(ICount.text);

        if (IDItem == "777")
        {
            ShowBuySlotMessageBox();
        }
        else if (IDItem != "")
        {
            StartCoroutine(LoadInventoryItem());
        }
    }

    // Відображення сповіщення для покупки слоту
    private void ShowBuySlotMessageBox()
    {
        GameObject Inventory = transform.parent.gameObject;
        GameObject Inventory1 = Inventory.transform.parent.gameObject;
        GameObject MessageBox = Inventory1.transform.Find("MessageBox").gameObject;
        MessageBox.transform.SetAsLastSibling();
        GameObject BoxInventoryCount = MessageBox.transform.Find("InventoryCount").gameObject;
        Text TextInventoryCount = BoxInventoryCount.GetComponent<Text>();
        TextInventoryCount.text = InventoryCount.ToString();
        GameObject BoxItemID = MessageBox.transform.Find("ItemID").gameObject;
        Text TextItemID = BoxItemID.GetComponent<Text>();
        TextItemID.text = "BuySlot";
        MessageBox.SetActive(true);
    }

    // Завантаження даних елемента інвентаря
    private IEnumerator LoadInventoryItem()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadInventoryItem", "Yes");
        FindDataBase.AddField("ItemID", ItemID.text);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        Data = JsonUtility.FromJson<ItemInfo>(jsonformat);
        yield return StartCoroutine(LoadItemToID());
        www.Dispose();
    }

    public class ItemInfo
    {
        public int id, id_item, type_item, count;
    }

    // Завантаження даних елемента з магазину
    private IEnumerator LoadItemToID()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadItemToID", "Yes");
        FindDataBase.AddField("ItemID", Data.id_item);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadshopitems.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        ItemShop = JsonUtility.FromJson<ItemShopInfo>(jsonformat);
        www.Dispose();

        ShowInventoryItemMessageBox();
    }

    // Відображення сповіщення про обраний елемент інвентаря
    private void ShowInventoryItemMessageBox()
    {
        GameObject Inventory = transform.parent.gameObject;
        GameObject Inventory1 = Inventory.transform.parent.gameObject;
        GameObject MessageBox = Inventory1.transform.Find("MessageBox").gameObject;
        MessageBox.transform.SetAsLastSibling();
        GameObject BoxMessage = MessageBox.transform.Find("boxMessage").gameObject;
        GameObject MessageText = BoxMessage.transform.Find("textMessage").gameObject;
        Text textMessage = MessageText.GetComponent<Text>();
        textMessage.text = "Предмет: " + ItemShop.name + "\nКількість: " + Data.count;
        GameObject BoxInventoryCount = MessageBox.transform.Find("InventoryCount").gameObject;
        Text TextInventoryCount = BoxInventoryCount.GetComponent<Text>();
        TextInventoryCount.text = InventoryCount.ToString();
        GameObject BoxItemID = MessageBox.transform.Find("ItemID").gameObject;
        Text TextItemID = BoxItemID.GetComponent<Text>();
        TextItemID.text = ItemID.text;
        MessageBox.SetActive(true);
    }

    public class ItemShopInfo
    {
        public int id, type, price;
        public string name;
    }
}