using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Button = UnityEngine.UI.Button;

public class MessageUsability : MonoBehaviour
{
    private string NickName, SerialCode, nick = "Name", code = "SerialCode", PlayerID, id = "ID",jsonformat, NameItem, OperationSale = "NoSale", EquipmentUse;
	public Button Button1, Button2, Button3, Close;
	public Text Description, InventoryCount, ButtonText1, ButtonText2, ButtonText3, IDItem;
	public GameObject MessageBox, _Button1, _Button2, _Button3;
	private int playergreen, playergold, PlayerInventory, itemprice, type, CountItems, ItemID, Confirmation, IDItemInventory;
	public LoadInventory Inventory;
	public LoadAndUpdateAccount Infobar;
	public PanelPlayer PanelPlayer;
	ItemInfo ShopItem;
	InvItem InventoryItem;
	EquipmentInfo AvabilityEquipent;
	private void Start() 
	{
		this.NickName = PlayerPrefs.GetString(nick);
       	this.SerialCode = PlayerPrefs.GetString(code);
       	this.PlayerID = PlayerPrefs.GetString(id);

		Button1.onClick.AddListener(Button1OnClick);
		Button2.onClick.AddListener(Button2OnClick);
		Button3.onClick.AddListener(Button3OnClick);
		Close.onClick.AddListener(CloseMessageBox);
		
	}
	private void Update() 
	{

		PlayerInventory = int.Parse(InventoryCount.text);
		if(MessageBox.activeSelf == true)
		{
			if(IDItem.text == "BuySlot")
			{
				
				if(OperationSale == "BuySlotError")
				{
					ButtonText3.text = "Закрыть";
					_Button1.SetActive(false); _Button2.SetActive(false); _Button3.SetActive(true); 
					return;
				}
				else if(OperationSale == "BuySlotConfirm")
				{
					ButtonText1.text = "Да";
					ButtonText2.text = "Нет";
					_Button1.SetActive(true); _Button2.SetActive(true); _Button3.SetActive(false); 
					return;
				}
				else
				{	
					ButtonText3.text = "Купить";
					Description.text = "Свободная ячейка! \nВы хотите купить её за \"" +PlayerInventory*50+" золота\"?";
					_Button1.SetActive(false); _Button2.SetActive(false); _Button3.SetActive(true); 
					return;
				}
			}
			else if(IDItem.text != "") 
			{ 
				_Button1.SetActive(true); _Button2.SetActive(true); _Button3.SetActive(false); 
				if(OperationSale == "NoSale")
				{
					ButtonText1.text = "Использовать";
					ButtonText2.text = "Продать";
				}
				if(OperationSale == "Sale1")
				{
					ButtonText1.text = "Да";
					ButtonText2.text = "Нет";
				}
			}
		}
	}
    void Button1OnClick()
    {
        if(OperationSale == "NoSale") 
		{ 
			IDItemInventory = int.Parse(IDItem.text);
			StartCoroutine(ItemUse());
		}
        if(OperationSale == "Sale1") 
		{ 
			StartCoroutine(ItemSell());
		}
        if(OperationSale == "BuySlotConfirm") 
		{ 
			StartCoroutine(BuySlotConfirm());
		}
    }
    void Button2OnClick()
    {
        if(OperationSale == "NoSale") 
		{ 
			IDItemInventory = int.Parse(IDItem.text);
			StartCoroutine(ItemSellPrewiew());
		}
        if(OperationSale == "Sale1") 
		{ 
			MessageBox.SetActive(false);
			OperationSale = "NoSale";
		}
        if(OperationSale == "BuySlotConfirm") 
		{ 
			MessageBox.SetActive(false);
			OperationSale = "NoSale";
		}
    }
    void Button3OnClick()
    {
        if(OperationSale == "NoSale") 
		{ 
			BuySlot();
		}
        if(OperationSale == "BuySlotError") 
		{ 
			MessageBox.SetActive(false);
			OperationSale = "NoSale";
		}
    }
    void CloseMessageBox()
    {
		MessageBox.SetActive(false);
		OperationSale = "NoSale";
    }
	private IEnumerator ItemSellPrewiew()
	{
		
		yield return StartCoroutine(LoadInventoryItem());
		yield return StartCoroutine(LoadItem());
		float itempricetosell = Mathf.FloorToInt((ShopItem.price/100)*25);
		Description.text = "Вы можете продать \"" + ShopItem.name + "\"\nЦена продажи: " +itempricetosell.ToString()+"\nВы уверены что хотите продать данный предмет?";
		OperationSale = "Sale1";
	}
	private IEnumerator ItemSell()
	{
		yield return StartCoroutine(LoadAcc());
		int itempricetosell = (int)Mathf.FloorToInt((ShopItem.price/100)*25);
		playergreen = playergreen + itempricetosell;
		yield return StartCoroutine(DeleteItemFromInventori());
		yield return StartCoroutine(UpdateCellAccount("playergreen", playergreen.ToString()));
		Inventory.ReloadInventory();
		OperationSale = "NoSale";
		MessageBox.SetActive(false);
	}
	
	private IEnumerator DeleteItemFromInventori()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("DeleteFromInventory", "Yes");
		FindDataBase.AddField("ItemID", IDItemInventory);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		www.Dispose();
	}
	
	private IEnumerator LoadInventoryItem()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadInventoryItem", "Yes");
		FindDataBase.AddField("ItemID", IDItemInventory);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		InventoryItem = JsonUtility.FromJson<InvItem>(jsonformat);	
		www.Dispose();
	}
	public class InvItem
	{
		public int id, id_account, id_item;
	}
	
	private IEnumerator LoadItem()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadItemToID", "Yes");
		FindDataBase.AddField("ItemID", InventoryItem.id_item);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms./loadshopitems.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		ShopItem = JsonUtility.FromJson<ItemInfo>(jsonformat);
		www.Dispose();
	}
	public class ItemInfo
	{
		public int price, id, type;
		public string name;
	}
	private IEnumerator LoadAcc()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadAccount", "Yes");
		FindDataBase.AddField("PlayerName", this.NickName);
		FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
		yield return www.SendWebRequest();
		if(www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) { Debug.Log("Ошибка: " + www.error); }
		jsonformat = www.downloadHandler.text;
		PlayerInfo Data = JsonUtility.FromJson<PlayerInfo>(jsonformat);
		//=====================[Infobar]====================
		playergreen = Data.playergreen;
		playergold = Data.playergold;
		PlayerInventory = Data.inventory;
		www.Dispose();
	}
	public class PlayerInfo
	{
		public int playergreen, inventory, playergold;
	}
	private void BuySlot() 
	{
		OperationSale = "BuySlotConfirm";
		Description.text = "Вы можете купить дополнительную ячейку в инвентаре\nЦена покупки: " + PlayerInventory*50 +" золота\nВы уверены что хотите купить дополнительную ячейку?";
	}
	private IEnumerator BuySlotConfirm()
	{
		
		yield return StartCoroutine(LoadAcc());
		if(playergold >= PlayerInventory*50)
		{
			playergold = playergold-(PlayerInventory*50);
			PlayerInventory = PlayerInventory +1;
			yield return StartCoroutine(UpdateCellAccount("inventory", PlayerInventory.ToString()));
			yield return StartCoroutine(UpdateCellAccount("playergold", playergold.ToString()));
			Inventory.ReloadInventory();
			Infobar.ReloadInfoBar();
			OperationSale = "NoSale";
			MessageBox.SetActive(false);
		}
		else
		{
			OperationSale = "BuySlotError";
			Description.text = "Недостаточно золота для покупки!";
		}
	}
	private IEnumerator ItemUse()
	{
		yield return StartCoroutine(LoadInventoryItem()); //get usability item on click
		yield return StartCoroutine(LoadItem()); //get type
		yield return StartCoroutine(LoadEquipmentInfo(ShopItem.type.ToString())); //get equipment availability
		if(EquipmentUse == "Use")
		{
			yield return StartCoroutine(UpdateCellInventory(IDItemInventory.ToString(), "usability", ShopItem.type.ToString()));
			Inventory.ReloadInventory();
			PanelPlayer.LoadStat();
		}
		else if(EquipmentUse == "Replace")
		{
			yield return StartCoroutine(UpdateCellInventory(AvabilityEquipent.id.ToString(), "usability", "0"));
			yield return StartCoroutine(UpdateCellInventory(IDItemInventory.ToString(), "usability", ShopItem.type.ToString()));
			Inventory.ReloadInventory();
			PanelPlayer.LoadStat();
		}
		MessageBox.SetActive(false);
	}
	private IEnumerator LoadEquipmentInfo(string type)
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadEquipmentInfo", "Yes");
		FindDataBase.AddField("AccountID", this.PlayerID);
		FindDataBase.AddField("Type", type);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		if(www.downloadHandler.text == "0")
		{
			EquipmentUse = "Use";
		}
		else
		{
			EquipmentUse = "Replace";
			AvabilityEquipent = JsonUtility.FromJson<EquipmentInfo>(jsonformat);
		}	
		www.Dispose();
	}
	public class EquipmentInfo
	{
		public int id, usability;
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
	private IEnumerator UpdateCellAccount(string cellname, string value)
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("UpdateCell", cellname);
		FindDataBase.AddField("UpdateValue", value);;
		FindDataBase.AddField("PlayerID", this.PlayerID);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		www.Dispose();
    }
	private IEnumerator UpdateCellInventory(string id, string cellname, string value)
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("UpdateInventory", cellname);
		FindDataBase.AddField("UpdateValue", value);;
		FindDataBase.AddField("InventoriID", id);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		www.Dispose();
    }
}

