using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
public class TakeOff : MonoBehaviour
{
	private string NickName, SerialCode, nick = "Name", code = "SerialCode", PlayerID, id = "ID",jsonformat;
	[SerializeField] private string Type;
	private int PlayerInventory, pInventoryCount;
	private ItemJS[] Items;
	public PanelPlayer PanelPlayer;
	public LoadInventory Inventory;
    public void TakeOffEquipment()
	{
		this.NickName = PlayerPrefs.GetString(nick);
       	this.SerialCode = PlayerPrefs.GetString(code);
		this.PlayerID = PlayerPrefs.GetString(id);
		StartCoroutine(LoadEquipmentInfo(Type));
	}
	private IEnumerator LoadEquipmentInfo(string type)
	{
		
		yield return StartCoroutine(LoadAcc());
		yield return StartCoroutine(InventoryLoad());
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadEquipmentInfo", "Yes");
		FindDataBase.AddField("AccountID", this.PlayerID);
		FindDataBase.AddField("Type", type);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		if(jsonformat == "0"){}
		else
		{
			EquipmentInfo AvabilityEquipent = JsonUtility.FromJson<EquipmentInfo>(jsonformat);
			if(pInventoryCount >= PlayerInventory) {}
			else
			{
					yield return StartCoroutine(UpdateCellInventory(AvabilityEquipent.id.ToString(), "usability", "0"));
					Image ImPrewiew = GetComponent<Image>(); ImPrewiew.sprite = Resources.Load<Sprite>("ProfileIcons/"+type);
					Inventory.ReloadInventory();
					PanelPlayer.LoadStat();
			}
		}
		www.Dispose();	
	}
	public class EquipmentInfo
	{
		public int id, usability;
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
		if(www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) { Debug.Log("ќшибка: " + www.error); }
		jsonformat = www.downloadHandler.text;
		PlayerInfo Data = JsonUtility.FromJson<PlayerInfo>(jsonformat);
		//=====================[Infobar]====================
		PlayerInventory = Data.inventory;
		www.Dispose();
	}
	public class PlayerInfo
	{
		public int inventory;
	}
	
	private IEnumerator InventoryLoad()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadInventory", "Yes");
		FindDataBase.AddField("PlayerID", this.PlayerID);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		if(jsonformat == "0") {pInventoryCount = 0; }
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
