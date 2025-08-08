using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class LoadAndUpdateAccount : MonoBehaviour
{
    private float time = 600;
    [SerializeField] private Text timerText, CountBattles, PlayerName, PlayerLvl, PlayerEXP, PlayerHP, PlayerPetHP, CountGreen, CountGold, CountDiamonds;
	[SerializeField] private TextMeshProUGUI HourRewardText;
	[SerializeField] private Slider SliderBattles, SliderPlyerExpierence, SliderPlayerHP, SliderPetHP;
	private string NickName, SerialCode, PLvl, nick = "Name", code = "SerialCode", lvl = "Lvl", PlayerID, id = "ID", jsonformat, result, resultt;
	public int BattleCount, function, differencetime, pExp, MaxHealth, psurv, equipments, bvitality, PetActive, pGold, pID, pLvl, pGreen, pGuardHour, pGuardHours, pMinedGold, pMinedGoldStats, pMaxMinegGold;// getReward;
	public int PetID, PetOwner, PetAvatar, PetPower, PetProtect, PetDexterity, PetSkill, PetVitality, PetKills, PetHP, PetMaxHP;
	public string PetName, pTimeToEndGuard, pTimeToEndMine, pTimeToNextMine, pHorse;
	public bool IsActiveGuard = false, IsActiveMine = false, IsMineToday = false, IsActiveHike = false;
	public float pHP;
	public Image Pet;
	public Sprite[] PetIcons;
	public GameObject PetHPBar, HourRewardButton, RewardIcon;
	System.DateTime momenttime, rewardtime;
	private ItemJS[] Items;
	public SnapSkrolling[] Scroll;
	public PanelPlayer PanelPlayer;
	private IEnumerator RegenCoroutine;
	public PlayerInfo Account;
 
    private float _timeLeft = 0f, _timeRegenHP = 5f;
 
    private IEnumerator StartTimer()
    {
        while (_timeLeft > 0)
        {
            _timeLeft -= Time.deltaTime;
            UpdateTimeText();
            yield return null;
        }
    }
	private IEnumerator FindCountBattle()
    {
		_timeLeft = 0;
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("FindToDataBaseBattleCount", "Yes");
		FindDataBase.AddField("PlayerName", this.NickName);
		FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		result = www.downloadHandler.text;
		www.Dispose();
		CountBattles.text = result+"/12";
		BattleCount = int.Parse(result);
		SliderBattles.value = BattleCount;
		yield return StartCoroutine(FindCountTime());
		if(result != "12")
		{	
			if(momenttime >= System.DateTime.Now)
			{
				int differnsebattles = 12 - (BattleCount);
				CountBattles.text = BattleCount.ToString()+"/12";
				differencetime = (int)(momenttime - System.DateTime.Now).TotalSeconds;
				if(differnsebattles == 0)
				{ _timeLeft = 0; }
				else 
				{ 
					CheckBattles();
					if(differencetime <= 600) _timeLeft = differencetime;
					else _timeLeft = differencetime - ((11- BattleCount)*600);
					StartCoroutine(PlusCountBattle());
					StartCoroutine(StartTimer()); 
				}
			}
			else
			{
				CountBattles.text = BattleCount.ToString()+"/12";
				StartCoroutine(UpdateCountBattle());
			}	
		}
		else { _timeLeft = 0; }
		
    }
	private IEnumerator UpdateCountBattle()
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("UpdateDataBaseBattleCount", "Yes");
		FindDataBase.AddField("PlayerName", this.NickName);
		FindDataBase.AddField("PlayerID", this.PlayerID);
		if(BattleCount != 12)
		{
			if(momenttime <= System.DateTime.Now)
			{
				FindDataBase.AddField("PlayerCombats", 12);
				FindDataBase.AddField("TimeToEndCombat", "0");
			}
			else
			{
				momenttime = System.DateTime.Now;
				momenttime = momenttime.AddSeconds((12-BattleCount)*600);
				string timetoendbattles = momenttime.ToString();
				FindDataBase.AddField("PlayerCombats", BattleCount);
				FindDataBase.AddField("TimeToEndCombat", timetoendbattles);
			}
		}
		else 
		{	
			FindDataBase.AddField("PlayerCombats", 12);
			FindDataBase.AddField("TimeToEndCombat", "0"); 
		}
		FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		result = www.downloadHandler.text;
		BattleCount = int.Parse(result);
		SliderBattles.value = BattleCount;
		CountBattles.text = result+"/12";
		if(BattleCount != 12) { _timeLeft = time; StartCoroutine(StartTimer()); }
		www.Dispose();
    }
	private IEnumerator PlusCountBattle()
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("PlusCountBattle", "Yes");
		FindDataBase.AddField("PlayerName", this.NickName);
		FindDataBase.AddField("PlayerID", this.PlayerID);
		FindDataBase.AddField("PlayerCombats", BattleCount);
		FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		result = www.downloadHandler.text;
		BattleCount = int.Parse(result);
		SliderBattles.value = BattleCount;
		CountBattles.text = result+"/12";
		www.Dispose();
    }
	
	private IEnumerator FindCountTime()
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("FindToDataBaseCountTime", "Yes");
		FindDataBase.AddField("PlayerName", this.NickName);
		FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		resultt = www.downloadHandler.text;
		if(resultt != "0") { momenttime = System.DateTime.Parse(resultt); } 
		else {  momenttime = System.DateTime.Now; momenttime = momenttime.AddSeconds(-1); }
		www.Dispose();
    }
    private void Start()
    {
		SliderBattles.value = 0;
		this.NickName = PlayerPrefs.GetString(nick);
		this.PLvl = PlayerPrefs.GetString(lvl);
       	this.SerialCode = PlayerPrefs.GetString(code);
       	this.PlayerID = PlayerPrefs.GetString(id);
		StartCoroutine(LoadAcc());
		StartCoroutine(FindCountBattle());
    }
	
    public void ReloadInfoBar()
    {
		SliderBattles.value = 0;
		this.NickName = PlayerPrefs.GetString(nick);
		this.PLvl = PlayerPrefs.GetString(lvl);
       	this.SerialCode = PlayerPrefs.GetString(code);
       	this.PlayerID = PlayerPrefs.GetString(id);
		StartCoroutine(LoadAcc());
		StartCoroutine(FindCountBattle());
    }
    private void UpdateTimeText()
    {
		if (_timeLeft < 0)
		{ 
			_timeLeft = 0; 
			BattleCount += 1;		
			SliderBattles.value = BattleCount; 
			StartCoroutine(UpdateCountBattle());
		}
            
 
        float minutes = Mathf.FloorToInt(_timeLeft / 60);
        float seconds = Mathf.FloorToInt(_timeLeft % 60);
       	timerText.text = string.Format("{0:00} : {1:00}", minutes, seconds);
    }
	private void CheckBattles()
	{
		double fff = (double)differencetime / 600;
		int function = Mathf.CeilToInt((float)fff);
		if(differencetime <= 600) BattleCount = 11;
		BattleCount = 12 - function;
	}
	private IEnumerator RegenHP()
    {
        while (_timeRegenHP > 0)
        {
            _timeRegenHP -= Time.deltaTime;
        	yield return StartCoroutine(UpdateTimeRegenHP()); 
            yield return null;
        }
    }
 
    private IEnumerator UpdateTimeRegenHP()
    {
		if (_timeRegenHP < 0)
		{ 
			yield return StartCoroutine(LoadAcc()); 
			_timeRegenHP = 5;	
		}
    }
	private IEnumerator LoadAcc()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadAccount", "Yes");
		FindDataBase.AddField("PlayerName", this.NickName);
		FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

		UnityWebRequest www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/load", FindDataBase);
		yield return www.SendWebRequest();
		if(www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) { Debug.Log("Ошибка: " + www.error); }
		jsonformat = www.downloadHandler.text;
		Account = JsonUtility.FromJson<PlayerInfo>(jsonformat);
		//=====================[Infobar]====================
		pID = Account.id;
		PlayerName.text = Account.nickname;
		PlayerLvl.text = Account.playerlvl.ToString();
		pLvl = Account.playerlvl;
		PlayerEXP.text = Account.playerexpierence.ToString()+"/"+ (int)(Mathf.Pow(Account.playerlvl,(float)2.2)+9);
		psurv = Account.playersurvivability;
		bvitality = 0;
		yield return StartCoroutine(LoadEquipment(pID.ToString()));
		//=====================[Load Profile]====================
		if(equipments > 0)
		{
			for(int i = 0; i < equipments; i++)
			{
				yield return StartCoroutine(LoadItemID(Items[i].id_item.ToString()));
			}
		}
		MaxHealth = Mathf.FloorToInt(Mathf.Pow((Account.playersurvivability+bvitality), (float)2.2)+66);
		yield return StartCoroutine(UpdateCellAccount("maxhp", MaxHealth.ToString()));
		int gethp = Mathf.FloorToInt(Account.playerhp);
		if(gethp > MaxHealth) { gethp = MaxHealth; PlayerHP.text = gethp.ToString()+"/"+MaxHealth.ToString(); yield return StartCoroutine(UpdateCellAccount("playerhp", gethp.ToString())); }
		else { PlayerHP.text = Mathf.FloorToInt(Account.playerhp).ToString()+"/"+MaxHealth.ToString(); }
		yield return StartCoroutine(LoadPet(pID.ToString()));
		PlayerPetHP.text = PetHP.ToString()+"/"+PetMaxHP.ToString();
		pGreen = Account.playergreen;
		CountGold.text = Account.playergold.ToString();
		pGold = Account.playergold;
		CountDiamonds.text = Account.playerdiamonds.ToString();
		pExp = Account.playerexpierence;
		pHP = Account.playerhp;
		pGuardHour = Account.guardhour;
		pGuardHours = Account.guardhours;
		pTimeToEndGuard = Account.timetoendguard; 
		pMinedGold = Account.mining;
		pMinedGoldStats = Account.minedgold;
		pMaxMinegGold = Account.maxminedgold;
		if(pTimeToEndGuard != "0") 
		{
			System.DateTime time = System.DateTime.Parse(pTimeToEndGuard);
			if(System.DateTime.Now < time)
			{
				IsActiveGuard = true;
			}
			else
			{
				int Green = pGreen + (pGuardHour * (Account.playerlvl*50));
				yield return StartCoroutine(UpdateCellAccount("playergreen", Green.ToString()));
				yield return StartCoroutine(UpdateCellAccount("guardhour", "0"));
				int hours = Account.guardhours + pGuardHour;
				yield return StartCoroutine(UpdateCellAccount("guardhours", hours.ToString()));
				yield return StartCoroutine(UpdateCellAccount("timetoendguard", "0"));
				IsActiveGuard = false;
				ReloadInfoBar();
			}
		}
		if(Account.timetoendhike != "0" && Account.hikeactivemin > 0) 
		{
			System.DateTime time = System.DateTime.Parse(Account.timetoendhike);
			if(System.DateTime.Now < time)
			{
				IsActiveHike = true;
			}
			else
			{
				int DiamondValue = 0;
				string DiamondReward = "", ExpReward = "";
				if(Account.hikeactivemin <= 30) DiamondValue = 5;
				if(Account.hikeactivemin > 30 && Account.hikeactivemin <= 60) DiamondValue = 10;
				if(Account.hikeactivemin > 60 && Account.hikeactivemin <= 120) DiamondValue = 15;
				if(Account.hikeactivemin > 120 && Account.hikeactivemin <= 180) DiamondValue = 20;
				if(Account.hikeactivemin > 180 && Account.hikeactivemin <= 240) DiamondValue = 35;
				if(Account.hikeactivemin > 240 && Account.hikeactivemin <= 360) DiamondValue = 50;
				int DiamondChance = UnityEngine.Random.Range(0, 101);
				int TraderChance = UnityEngine.Random.Range(0, 101);
				int ExpChance = UnityEngine.Random.Range(0, 101);
				if(TraderChance <= 10)
				{
					if(DiamondChance <= DiamondValue)
					{
						
						int DiamondRandom =  UnityEngine.Random.Range(0, Account.playerlvl);
						int Diamond = Account.playerdiamonds + DiamondRandom;
						yield return StartCoroutine(UpdateCellAccount("playerdiamonds", Diamond.ToString()));
						DiamondReward = ", <sprite=2> " + DiamondRandom + " алмазов";
						
					}
					if(ExpChance <= 10)
					{
						int Exp = Account.playerexpierence + 1;
						if(Exp == (int)(Mathf.Pow(Account.playerlvl,(float)2.2)+9))
						{
							yield return StartCoroutine(UpdateCellAccount("playerexpierence", "0"));
							int lvl = Account.playerlvl + 1;
							yield return StartCoroutine(UpdateCellAccount("playerlvl", lvl.ToString()));
						}
						else yield return StartCoroutine(UpdateCellAccount("playerexpierence", Exp.ToString()));
						ExpReward = ", <sprite=3> 1 опыта";
					}
					int GreenRandom = (Account.hikeactivemin * (Account.playerlvl*15)) + (UnityEngine.Random.Range(Account.playerlvl*2, Account.playerlvl*Account.hikeactivemin)*5);
					int Green = pGreen + GreenRandom;
					yield return StartCoroutine(UpdateCellAccount("playergreen", Green.ToString()));
					string LastHike = "• Последний поход: \nТы был в походе " + Account.hikeactivemin + " минут и ограбил богатого купца, награда: \n <sprite=0> " + GreenRandom + " зелени" + DiamondReward + ExpReward;
					yield return StartCoroutine(UpdateCellAccount("lasthike", LastHike));
				}
				else
				{
					if(DiamondChance <= DiamondValue)
					{
						
						int DiamondRandom =  UnityEngine.Random.Range(0, 6);
						int Diamond = Account.playerdiamonds + DiamondRandom;
						yield return StartCoroutine(UpdateCellAccount("playerdiamonds", Diamond.ToString()));
						DiamondReward = ", <sprite=2> " + DiamondRandom + " алмазов";
						
					}
					if(ExpChance <= 10)
					{
						int Exp = Account.playerexpierence + 1;
						if(Exp == (int)(Mathf.Pow(Account.playerlvl,(float)2.2)+9))
						{
							yield return StartCoroutine(UpdateCellAccount("playerexpierence", "0"));
							int lvl = Account.playerlvl + 1;
							yield return StartCoroutine(UpdateCellAccount("playerlvl", lvl.ToString()));
						}
						else yield return StartCoroutine(UpdateCellAccount("playerexpierence", Exp.ToString()));
						ExpReward = ", <sprite=3> 1 опыта";
					}
					int GreenRandom = (Account.hikeactivemin * (Account.playerlvl*10)) + UnityEngine.Random.Range(Account.playerlvl*2, Account.playerlvl*Account.hikeactivemin);
					int Green = pGreen + GreenRandom;
					yield return StartCoroutine(UpdateCellAccount("playergreen", Green.ToString()));
					string LastHike = "• Последний поход: \nТы был в походе " + Account.hikeactivemin + " минут и ограбил богатого купца, награда: \n <sprite=0> " + GreenRandom + " зелени" + DiamondReward + ExpReward;
					yield return StartCoroutine(UpdateCellAccount("lasthike", LastHike));
				}
				int hikemin = Account.hikemin - Account.hikeactivemin;
				yield return StartCoroutine(UpdateCellAccount("hikemin", hikemin.ToString()));
				int minutes = Account.hikeminutes + Account.hikeactivemin;
				yield return StartCoroutine(UpdateCellAccount("hikeminutes", minutes.ToString()));
				yield return StartCoroutine(UpdateCellAccount("hikeactivemin", "0"));
				yield return StartCoroutine(UpdateCellAccount("timetoendhike", "0"));
				IsActiveHike = false;
				ReloadInfoBar();
			}
		}
		pTimeToEndMine = Account.timetoendmine;
		pTimeToNextMine = Account.timetonextmine;
		if(Account.ismine == 1) IsMineToday = true;
		if(pTimeToEndMine != "0") 
		{
			System.DateTime time = System.DateTime.Parse(pTimeToEndMine);
			if(System.DateTime.Now < time)
			{
				IsActiveMine = true;
			}
			else
			{
				if(!IsMineToday)
				{
					int Gold = pGold + Account.mining;
					yield return StartCoroutine(UpdateCellAccount("playergold", Gold.ToString()));
					ReloadInfoBar();
					int minedgold = Account.minedgold + Account.mining;
					yield return StartCoroutine(UpdateCellAccount("ismine", "1"));
					yield return StartCoroutine(UpdateCellAccount("minedgold", minedgold.ToString()));
					IsActiveMine = false;
					IsMineToday = true;
				}
			}
		}
		CountGreen.text = Account.playergreen.ToString();
		PetActive = Account.pet;
		if(Account.pet == 1 || Account.pet == 2) { PetHPBar.SetActive(true); }
		Pet.sprite = PetIcons[Account.pet];
		pHorse = Account.horsetime;
		StartCoroutine(RegenHP());
		//==================[Sliders value]=================
		SliderPlyerExpierence.maxValue = (int)(Mathf.Pow(Account.playerlvl,(float)2.2)+9);		
		SliderPlayerHP.maxValue = Mathf.FloorToInt(MaxHealth);	
		SliderPetHP.maxValue = PetMaxHP;
		//==================================================		
		SliderPlyerExpierence.value = Account.playerexpierence;		
		SliderPlayerHP.value = Mathf.FloorToInt(Account.playerhp);	
		SliderPetHP.value = PetHP;
		//==================================================	
		PlayerPrefs.SetString(id, Account.id.ToString());
		PlayerPrefs.SetString(lvl, Account.playerlvl.ToString());
		PlayerPrefs.Save();
		//===================[Hour reward]==================
		if(Account.hourreward == 0)
		{
			HourRewardText.text = "Какие орки в нашем бастионе, "+Account.nickname+", а я уже заждался тебя, тебе тут награда прилетела за активность";
			HourRewardButton.SetActive(true);
		}
		else
		{
			HourRewardText.text = Account.nickname+", ты опять здесь? Что-ж правильно, трудись на благо орды!";
			RewardIcon.SetActive(false);
		}



		//==================================================	
		www.Dispose();
	}
	public class PlayerInfo
	{
		public int id, playerlvl, playerexpierence, playergreen, playergold, playerdiamonds, combats, pet, hourreward,
		playersurvivability, guardhour, guardhours, mining, minedgold, ismine, maxminedgold, horse, hikeminutes, hikemin, hikeactivemin, monkreward;
		public string nickname, serialcode, timetoendguard, timetoendmine, timetonextmine, horsetime, timetoendhike, lasthike;
		public float playerhp;
	}
	private IEnumerator SetRewardNull()
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("SetRewardNull", "Yes");
		FindDataBase.AddField("PlayerID", this.PlayerID);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		//getReward = 0;
		www.Dispose();
    }
	private IEnumerator UpdateCellAccount(string cellname, string value)
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("UpdateCell", cellname);
		FindDataBase.AddField("UpdateValue", value);
		FindDataBase.AddField("PlayerName", this.NickName);
		FindDataBase.AddField("PlayerID", this.PlayerID);
		FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		www.Dispose();
    }
	private IEnumerator LoadEquipment(string id)
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadEquipment", "Yes");
		FindDataBase.AddField("PlayerID", id);

		UnityWebRequest www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/load", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		if(jsonformat != "0")
		{
			Items = JsonHelper.FromJson<ItemJS>(fixJson(jsonformat));
			equipments = Items.Length;
		}
		else equipments = 0;
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
	private IEnumerator LoadItemID(string _ItemID)
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadItemToID", "Yes");
		FindDataBase.AddField("ItemID", _ItemID);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadshopitems.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		ItemInfo Data = JsonUtility.FromJson<ItemInfo>(jsonformat);
		if(Data.itemvitability != 0) bvitality = bvitality + Data.itemvitability;		
		www.Dispose();
	}
	public class ItemInfo
	{
		public int itempower, itemprotection, itemdexterity, itemskill, itemvitability;
	}
	private IEnumerator LoadPet(string PlayerID)
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadPet", "Yes");
		FindDataBase.AddField("PlayerID", PlayerID);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		string jsonformat = www.downloadHandler.text;
		if(jsonformat != "")
		{
			PetInfo Pet = JsonUtility.FromJson<PetInfo>(jsonformat);
			PetID = Pet.id;
			PetOwner = Pet.id_owner;
			PetAvatar = Pet.avatar;
			PetPower = Pet.petpower;
			PetProtect = Pet.petprotect;
			PetDexterity = Pet.petdexterity;
			PetSkill = Pet.petskill;
			PetVitality = Pet.petvitality;
			PetKills = Pet.petkills;
			PetName = Pet.name;
			PetHP = Mathf.FloorToInt(Pet.pethp);
			PetMaxHP =  Mathf.FloorToInt(Mathf.Pow(PetVitality, (float)1.65)-4);
			yield return StartCoroutine(UpdateCellPet("petmaxhp", PetMaxHP.ToString(), PetID.ToString()));
		}
		www.Dispose();
	}
	public class PetInfo
	{
		public int id, id_owner, avatar, petpower, petprotect, petdexterity, petskill, petvitality, petkills;
		public string name;
		public float pethp;
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
}
