using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Button = UnityEngine.UI.Button;
using TMPro;

public class Battle : MonoBehaviour
{
	private string NickName, SerialCode, nick = "Name", code = "SerialCode", PlayerID, id = "ID",jsonformat, logfight, EnemyName, battle_a = "", battle_d = "";
	private int equipments, bpower, bprotect, bdexterity, bskill, bvitality, minb, maxb, _power, _protect, _dexterity, _skill, _vitality, PlayerDamage, EnemyDamage, PlayerPetDamage, EnemyPetDamage;
	private int ebpower, ebprotect, ebdexterity, ebskill, ebvitality, _epower, _eprotect, _edexterity, _eskill, _evitality, EnemyID, EnemyHP, PlayerHP, PlayerRpower, EnemyRpower, PlayerPetPower, EnemyPetPower;
	private int PetID, PetPower, PetProtect, PetDexterity, PetSkill, PetVitality, PetHP, PetMaxHP, PetKills;
	private int ePetPower, ePetProtect, ePetDexterity, ePetSkill, ePetVitality, ePetHP, ePetMaxHP;
	private bool EnemyPet;
	public int BattleFormat, StoreBF;
	public Text[] PlayerCharacteristics, EnemyCharacteristics, ButtonText;
	public Button[] Arena_Button;
	public Image EnemyAvatar;
	public Sprite[] SpriteGnolls;
	public GameObject GnollInfoBar, ResultInfoBar, ObjectErrorText;
	private string[] PlayerStat, EnemyStat;
	private ItemJS[] Items;
	private PlayerInfo Player;
	public LoadAndUpdateAccount Infobar;
	public ArenaButton Arena;
	public Text ErrorText;
	public TextMeshProUGUI BattleInfo;

    void Start()
    {
		this.NickName = PlayerPrefs.GetString(nick);
       	this.SerialCode = PlayerPrefs.GetString(code);
       	this.PlayerID = PlayerPrefs.GetString(id);
		BattleFormat = 0;
		//Buttons
		Arena_Button[0].onClick.AddListener(Button1OnClick);
		Arena_Button[1].onClick.AddListener(Button2OnClick);
		Arena_Button[2].onClick.AddListener(Button3OnClick);
		Arena_Button[3].onClick.AddListener(Button4OnClick);
		Arena_Button[4].onClick.AddListener(Button5OnClick);
		ButtonText[0].text = "Гноллы";
		ButtonText[1].text = "Младше";
		ButtonText[2].text = "Старше";
		ButtonText[3].text = "Ровня";
		ButtonText[4].text = "По уровню";
    }
	public void StartBattle()
	{
		BattleFormat = 0;
		ButtonText[0].text = "Гноллы";
		ButtonText[1].text = "Младше";
		ButtonText[2].text = "Старше";
		ButtonText[3].text = "Ровня";
		ButtonText[4].text = "По уровню";
	}
    void Button1OnClick()
    {		
		ButtonText[0].text = "Сражаться";
		if(BattleFormat == -1)
		{
			BattleFormat = -2;
			BattleInfo.text = logfight;
			ButtonText[0].text = "Итог";
		}
		else if(BattleFormat == -2)
		{
			BattleFormat = -1;
			BattleInfo.text = battle_a;
			ButtonText[0].text = "Детали боя";
		}
        else if(BattleFormat == 0)
		{	
			GnollInfoBar.SetActive(true);
			BattleFormat = 1;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == 1)
		{
			EnemyName = "Гнолл";
			StartCoroutine(LoadBattleInfo());
		}
        else if(BattleFormat == 2)
		{
			EnemyName = "Гнолл охотник";
			StartCoroutine(LoadBattleInfo());
		}
        else if(BattleFormat == 3)
		{
			EnemyName = "Вожак";
			StartCoroutine(LoadBattleInfo());
		}
    }
    void Button2OnClick()
    {
		ButtonText[0].text = "Сражаться";
		if(Infobar.pHP < 30 || Infobar.BattleCount < 1)
		{
			Arena.OpenArena();
		}
        else if(BattleFormat == 1)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 1;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 1 || BattleFormat == -2 && StoreBF == 1)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 1;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == 2)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 2;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 2 || BattleFormat == -2 && StoreBF == 2)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 2;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == 3)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 3;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 3 || BattleFormat == -2 && StoreBF == 3)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 3;
			StartCoroutine(LoadBattle());
		}
    }
    void Button3OnClick()
    {
		ButtonText[0].text = "Сражаться";
		if(Infobar.pHP < 30 || Infobar.BattleCount < 1)
		{
			Arena.OpenArena();
		}
        else if(BattleFormat == 1)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 2;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 1 || BattleFormat == -2 && StoreBF == 1)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 2;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == 2)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 1;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 2 || BattleFormat == -2 && StoreBF == 2)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 1;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == 3)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 1;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 3 || BattleFormat == -2 && StoreBF == 3)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 1;
			StartCoroutine(LoadBattle());
		}
    }
    void Button4OnClick()
    {
		ButtonText[0].text = "Сражаться";
		if(Infobar.pHP < 30 || Infobar.BattleCount < 1)
		{
			Arena.OpenArena();
		}
        else if(BattleFormat == 1)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 3;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 1 || BattleFormat == -2 && StoreBF == 1)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 3;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == 2)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 3;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 2 || BattleFormat == -2 && StoreBF == 2)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 3;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == 3)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 2;
			StartCoroutine(LoadBattle());
		}
        else if(BattleFormat == -1 && StoreBF == 3 || BattleFormat == -2 && StoreBF == 3)
		{
			GnollInfoBar.SetActive(true);
			ResultInfoBar.SetActive(false);
			BattleFormat = 2;
			StartCoroutine(LoadBattle());
		}
    }
    void Button5OnClick()
    {
		ResultInfoBar.SetActive(false);
		ButtonText[0].text = "Гноллы";
		if(BattleFormat == 1 || BattleFormat == 2 || BattleFormat == 3 || BattleFormat == -1 || BattleFormat == -2)
		{
			GnollInfoBar.SetActive(false);
			//PlayerInfoBar.SetActive(false);
			StartBattle();
		}
    }
	private IEnumerator LoadBattle()
	{
		bpower = 0; bprotect = 0; bdexterity = 0; bskill = 0; bvitality = 0;
		ebpower = 0; ebprotect = 0; ebdexterity = 0; ebskill = 0; ebvitality = 0;
		if(BattleFormat == 1)
		{
			minb = -26; maxb = -6;
			yield return StartCoroutine(LoadBattleGnolls());

			ButtonText[0].text = "Сражаться";
			ButtonText[1].text = "Следующий";
			ButtonText[2].text = "Золотые гноллы";
			ButtonText[3].text = "Кристальные гноллы";
			ButtonText[4].text = "Назад";
		}
		else if(BattleFormat == 2)
		{
			minb = -6; maxb = 16;
			yield return StartCoroutine(LoadBattleGnolls());
			ButtonText[2].text = "Обычные гноллы";
			ButtonText[3].text = "Кристальные гноллы";
		}
		else if(BattleFormat == 3)
		{
			minb = 16; maxb = 31;
			yield return StartCoroutine(LoadBattleGnolls());
			ButtonText[2].text = "Обычные гноллы";
			ButtonText[3].text = "Золотые гноллы";
		}
	}
	private IEnumerator LoadBattleInfo()
	{
		PlayerDamage = 0; EnemyDamage = 0; PlayerPetDamage = 0; EnemyPetDamage = 0;
		logfight = "";
		battle_a = ""; battle_d = "";
		int BattlePlayer = 0, BattleEnemy = 0;
		EnemyHP = Mathf.FloorToInt(Mathf.Pow(_evitality, (float)2.2)+66); PlayerHP = Mathf.FloorToInt(Infobar.pHP);
		int RewardBattleGreen = 0, RewardBattleGold = 0, RewardBattleDiamond = 0, exp = 0, pDamagePet = 0, eDamagePet = 0, P_Green = 0;
		for(int i = 0; i <3; i++)
		{
			logfight = logfight + "Раунд " + (i+1) + "\n";
			FightPlayerToEnemy();
			pDamagePet = PlayerPetDamage - ePetHP; if(pDamagePet < 0) pDamagePet = 0;
			if(EnemyHP - (PlayerDamage + pDamagePet) <= 0 ) break;
			if(BattleFormat == 1 && Infobar.PetActive == 1 || BattleFormat == 2 && Infobar.PetActive == 1 ) FightPetPlayerToEnemy();
			if(BattleFormat == 3 && (ePetHP - PlayerPetDamage) <= 0 && (PetHP-EnemyPetDamage) > 0) FightPetPlayerToEnemy();
			if(BattleFormat == 3 && (ePetHP - PlayerPetDamage) > 0 && (PetHP-EnemyPetDamage) > 0) FightPetPlayerToPetEnemy();
			pDamagePet = PlayerPetDamage - ePetHP; if(pDamagePet < 0) pDamagePet = 0;
			if(EnemyHP - (PlayerDamage + pDamagePet) <= 0 ) break;
			FightEnemyToPlayer();
			eDamagePet = EnemyPetDamage - PetHP; if(eDamagePet < 0) eDamagePet = 0;
			if(PlayerHP - (EnemyDamage + eDamagePet) <= 0) break;
			if(BattleFormat == 3 && (PetHP-EnemyPetDamage) <= 0 && (ePetHP - PlayerPetDamage) > 0) FightPetEnemyToPlayer();
			if(BattleFormat == 3 && (PetHP-EnemyPetDamage) > 0 && (ePetHP - PlayerPetDamage) > 0) FightPetEnemyToPetPlayer();
			eDamagePet = EnemyPetDamage - PetHP; if(eDamagePet < 0) eDamagePet = 0;
			if(PlayerHP - (EnemyDamage + eDamagePet) <= 0) break;
		}
		pDamagePet = PlayerPetDamage - ePetHP; if(pDamagePet < 0) pDamagePet = 0;
		EnemyHP = EnemyHP - PlayerDamage - pDamagePet;
		eDamagePet = EnemyPetDamage - PetHP; if(eDamagePet < 0) eDamagePet = 0;
		PlayerHP = PlayerHP - EnemyDamage - eDamagePet;
		ePetHP = ePetHP - pDamagePet;
		if(ePetHP < 0) ePetHP = 0;
		PetHP = PetHP - eDamagePet;
		if(PetHP < 0) PetHP = 0;
		yield return StartCoroutine(UpdateCellPet("pethp", PetHP.ToString(), PetID.ToString())); 
		yield return StartCoroutine(UpdateCellAccount("playerhp", PlayerHP.ToString(), Player.id.ToString())); 
		yield return StartCoroutine(LoadPlayerStats());
		if(EnemyHP <= 0) { BattlePlayer = 1; BattleEnemy = 2; }
		else if(PlayerHP <= 0) { BattlePlayer = 2;  BattleEnemy = 1; }
		else if((PlayerDamage+PlayerPetDamage) > (EnemyDamage+EnemyPetDamage)) { BattlePlayer = 3; BattleEnemy = 4; }
		else if((PlayerDamage+PlayerPetDamage) < (EnemyDamage+EnemyPetDamage)) { BattlePlayer = 4; BattleEnemy = 3; }
		if(BattleFormat == 1 && BattlePlayer == 1 || BattleFormat == 1 && BattlePlayer == 3) 
		{
			RewardBattleGreen = (Player.playerlvl * 100) + UnityEngine.Random.Range(51, 251);
			P_Green = Player.playergreen + RewardBattleGreen;
			if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
		}
		if(BattleFormat == 2 && BattlePlayer == 1 || BattleFormat == 2 && BattlePlayer == 3) 
		{
			RewardBattleGreen = (Player.playerlvl * 150) + UnityEngine.Random.Range(51, 351);
			P_Green = Player.playergreen + RewardBattleGreen;
			if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
			int GoldChance =  UnityEngine.Random.Range(0, 101);
			if(GoldChance <= 50)
			{
				if(Player.goldfromgnoll <= (Player.playerlvl*2) && Player.goldfromgnoll <= 30)
				{
					RewardBattleGold = UnityEngine.Random.Range(0, 3);
				}
			}
		}
		if(BattleFormat == 3 && BattlePlayer == 1 || BattleFormat == 3 && BattlePlayer == 3) 
		{
			RewardBattleGreen = (Player.playerlvl * 200) + UnityEngine.Random.Range(51, 451);
			P_Green = Player.playergreen + RewardBattleGreen;
			if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
			int DiamondChance =  UnityEngine.Random.Range(0, 101);
			if(DiamondChance <= 33)
			{
				if(Player.goldfromgnoll <= Player.playerlvl && Player.goldfromgnoll <= 15)
				{
					RewardBattleDiamond = UnityEngine.Random.Range(0, 3);
				}
			}
		}
		if(BattlePlayer == 2 || BattlePlayer == 4)
		{
			RewardBattleGreen = (int)(Player.playergreen * 0.05);
			P_Green = Player.playergreen - RewardBattleGreen;
			if(Player.goldlostperhour == 0) 
			{
				int GoldChance =  UnityEngine.Random.Range(0, 101);
				if(GoldChance <= 5)
				{
					RewardBattleGold = (int)(Player.playergold * 0.03);
				}
			}
		}
		int P_Combats = Player.combats - 1;
		string[] WinText = new string[4];
		WinText[0] = "<color=green>Вы победили</color> \nПричина: у проигравшего не осталось здоровья \nНаграда: \nЗелень: <sprite=0> "; 
		WinText[1] = "<color=red>Вы проиграли</color> \nПричина: у проигравшего не осталось здоровья \nПотери: \nЗелень: <sprite=0> ";
		WinText[2] = "<color=green>Вы победили</color> \nПричина: победитель нанёс больше урона \nНаграда: \nЗелень: <sprite=0> ";
		WinText[3] = "<color=red>Вы проиграли</color> \nПричина: победитель нанёс больше урона \nПотери: \nЗелень: <sprite=0> ";

		battle_a = WinText[BattlePlayer-1] + RewardBattleGreen;
		if(RewardBattleGold > 0) battle_a = battle_a + "\nЗолото: <sprite=1> "+RewardBattleGold;
		if(RewardBattleDiamond > 0) battle_a = battle_a + "\nАлмазы: <sprite=2> "+RewardBattleDiamond;
		if(exp > 0) battle_a = battle_a + "\nОпыт: <sprite=3> 1";
		battle_a = battle_a + "\n\nНанесенный урон: \n"+
		this.NickName+": "+PlayerDamage+" \n";
		if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
		battle_a = battle_a + EnemyName+": "+EnemyDamage+" \n";
		if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
		battle_a = battle_a + "\n\nОсталось здоровья: \n"+
		this.NickName+": "+PlayerHP+" \n";
		if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PetHP+"\n";
		battle_a = battle_a + EnemyName+": "+EnemyHP+" \n";
		if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+ePetHP+"\n";
		battle_a = battle_a + "\n\nДополнительная информация: \n";
		if(ePetHP <= 0 && BattleFormat > 2)  battle_a = battle_a + "Зверь "+EnemyName+" был убит в бою\n";
		if(PetHP <= 0  && Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+" был убит в бою\n";
		battle_d = WinText[BattleEnemy-1] + RewardBattleGreen;
		if(RewardBattleGold > 0) battle_d = battle_d + "\nЗолото: <sprite=1> "+RewardBattleGold;
		battle_d = battle_d + "\n\nНанесенный урон: \n"+
		this.NickName+": "+PlayerDamage+" \n";
		if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
		battle_d = battle_d + EnemyName+": "+EnemyDamage+" \n";
		if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
		battle_d = battle_d + "\n\nОсталось здоровья: \n"+
		this.NickName+": "+PlayerHP+" \n";
		if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PetHP+"\n";
		battle_d = battle_d + EnemyName+": "+EnemyHP+" \n";
		if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+ePetHP+"\n";
		battle_d = battle_d + "\n\nДополнительная информация: \n";
		if(ePetHP <= 0 && BattleFormat > 2)  
		{
			battle_d = battle_d + "Зверь "+EnemyName+" был убит в бою\n";
			PetKills = PetKills +1;
			yield return StartCoroutine(UpdateCellPet("petkills", PetKills.ToString(), PetID.ToString()));
		}
		if(PetHP <= 0 && Infobar.PetActive == 1) {
			battle_d = battle_d + "Зверь "+this.NickName+" был убит в бою\n";
			yield return StartCoroutine(UpdateCellAccount("pet", "3", Player.id.ToString()));
		}
		yield return StartCoroutine(UpdateCellAccount("playergreen", P_Green.ToString(), Player.id.ToString()));
		if(RewardBattleDiamond > 0) 
		{
			int sumdiamond = Player.playerdiamonds + RewardBattleDiamond;
			yield return StartCoroutine(UpdateCellAccount("playerdiamonds", sumdiamond.ToString(), Player.id.ToString()));
			sumdiamond = Player.diamondfromgnoll + RewardBattleDiamond;
			yield return StartCoroutine(UpdateCellAccount("diamondfromgnoll", sumdiamond.ToString(), Player.id.ToString()));
		}
		yield return StartCoroutine(UpdateCellAccount("combats", P_Combats.ToString(), Player.id.ToString()));
		if(exp > 0)
		{
			if(exp >= (int)(Mathf.Pow(Player.playerlvl,(float)2.2)+9))
			{
				exp = exp - (int)(Mathf.Pow(Player.playerlvl,(float)2.2)+9);
				yield return StartCoroutine(UpdateCellAccount("playerexpierence", exp.ToString(), Player.id.ToString()));
				int lvl = Player.playerlvl + 1;
				yield return StartCoroutine(UpdateCellAccount("playerlvl", lvl.ToString(), Player.id.ToString()));
			}
			else
			{
				yield return StartCoroutine(UpdateCellAccount("playerexpierence", exp.ToString(), Player.id.ToString()));
			}
		}
		int TimeSeconds = 0;
		if(Player.timetoendcombat == "0") TimeSeconds = 600;
		else
		{	
			TimeSeconds = (int)(System.DateTime.Parse(Player.timetoendcombat) - System.DateTime.Now).TotalSeconds;
			TimeSeconds = TimeSeconds + 600;
		}
		System.DateTime Time = System.DateTime.Now.AddSeconds(TimeSeconds);
		yield return StartCoroutine(UpdateCellAccount("timetoendcombat", Time.ToString("dd.MM.yyyy HH:mm:ss"), Player.id.ToString()));
		if(BattlePlayer == 1 || BattlePlayer == 3)
		{
			int battleswin = Player.battleswin +1;
			yield return StartCoroutine(UpdateCellAccount("battleswin", battleswin.ToString(), Player.id.ToString()));
			int greenlooted = Player.greenlooted + RewardBattleGreen;
			yield return StartCoroutine(UpdateCellAccount("greenlooted", greenlooted.ToString(), Player.id.ToString()));
			int goldlooted = Player.goldlooted + RewardBattleGold;
			yield return StartCoroutine(UpdateCellAccount("goldlooted", goldlooted.ToString(), Player.id.ToString()));
			if(RewardBattleGold > 0) 
			{
				int sumgold = Player.playergold + RewardBattleGold;
				yield return StartCoroutine(UpdateCellAccount("playergold", sumgold.ToString(), Player.id.ToString()));
				if(BattleFormat > 0 && BattleFormat < 4)
				{
					sumgold = Player.goldfromgnoll + RewardBattleGold;
					yield return StartCoroutine(UpdateCellAccount("goldfromgnoll", sumgold.ToString(), Player.id.ToString()));
				}
			}
		}
		else
		{
			int battleslose = Player.battleslose +1;
			yield return StartCoroutine(UpdateCellAccount("battleslose", battleslose.ToString(), Player.id.ToString()));
			int greenlost = Player.greenlost + RewardBattleGreen;
			yield return StartCoroutine(UpdateCellAccount("greenlost", greenlost.ToString(), Player.id.ToString()));
			int goldlost = Player.goldlost + RewardBattleGold;
			yield return StartCoroutine(UpdateCellAccount("goldlost", goldlost.ToString(), Player.id.ToString()));
			if(RewardBattleGold > 0) 
			{
				int sumgold = Player.playergold - RewardBattleGold;
				yield return StartCoroutine(UpdateCellAccount("playergold", sumgold.ToString(), Player.id.ToString()));
			}
		}
		Infobar.ReloadInfoBar();
		if(BattleFormat == 1 || BattleFormat == 2 || BattleFormat == 3) EnemyID = BattleFormat * -1;
		yield return StartCoroutine(CreateBattleLog(Player.id.ToString(), EnemyID.ToString(), logfight, battle_a, battle_d));
		ResultBattleInfo();
	}
	/*private IEnumerator LoadBattleInfo()
	{
		PlayerDamage = 0; EnemyDamage = 0; PlayerPetDamage = 0; EnemyPetDamage = 0;
		logfight = "";
		battle_a = ""; battle_d = "";
		EnemyHP = Mathf.FloorToInt(Mathf.Pow(_evitality, (float)2.2)+66); PlayerHP = Mathf.FloorToInt(Infobar.pHP);
		int RewardBattleGreen = 0, RewardBattleGold = 0, RewardBattleDiamond = 0, exp = 0, pDamagePet = 0, eDamagePet = 0;
		for(int i = 0; i <3; i++)
		{
			logfight = logfight + "Раунд " + (i+1) + "\n";
			FightPlayerToEnemy();
			pDamagePet = PlayerPetDamage - ePetHP; if(pDamagePet < 0) pDamagePet = 0;
			if(EnemyHP - (PlayerDamage + pDamagePet) <= 0 ) break;
			if(BattleFormat == 1 || BattleFormat == 2) FightPetPlayerToEnemy();
			if(BattleFormat == 3 && (ePetHP - PlayerPetDamage) <= 0 && (PetHP-EnemyPetDamage) > 0) FightPetPlayerToEnemy();
			if(BattleFormat == 3 && (ePetHP - PlayerPetDamage) > 0 && (PetHP-EnemyPetDamage) > 0) FightPetPlayerToPetEnemy();
			pDamagePet = PlayerPetDamage - ePetHP; if(pDamagePet < 0) pDamagePet = 0;
			if(EnemyHP - (PlayerDamage + pDamagePet) <= 0 ) break;
			FightEnemyToPlayer();
			eDamagePet = EnemyPetDamage - PetHP; if(eDamagePet < 0) eDamagePet = 0;
			if(PlayerHP - (EnemyDamage + eDamagePet) <= 0) break;
			if(BattleFormat == 3 && (PetHP-EnemyPetDamage) <= 0 && (ePetHP - PlayerPetDamage) > 0) FightPetEnemyToPlayer();
			if(BattleFormat == 3 && (PetHP-EnemyPetDamage) > 0 && (ePetHP - PlayerPetDamage) > 0) FightPetEnemyToPetPlayer();
			eDamagePet = EnemyPetDamage - PetHP; if(eDamagePet < 0) eDamagePet = 0;
			if(PlayerHP - (EnemyDamage + eDamagePet) <= 0) break;
		}
		pDamagePet = PlayerPetDamage - ePetHP; if(pDamagePet < 0) pDamagePet = 0;
		EnemyHP = EnemyHP - PlayerDamage - pDamagePet;
		eDamagePet = EnemyPetDamage - PetHP; if(eDamagePet < 0) eDamagePet = 0;
		PlayerHP = PlayerHP - EnemyDamage - eDamagePet;
		ePetHP = ePetHP - PlayerPetDamage;
		if(ePetHP < 0) ePetHP = 0;
		PetHP = PetHP - EnemyPetDamage;
		if(PetHP < 0) PetHP = 0;
		yield return StartCoroutine(UpdateCellPet("pethp", PetHP.ToString(), PetID.ToString())); 
		yield return StartCoroutine(UpdateCellAccount("playerhp", PlayerHP.ToString(), Player.id.ToString())); 
		yield return StartCoroutine(LoadPlayerStats());
		if(EnemyHP <= 0 || PlayerHP <= 0)
		{
			if(EnemyHP <= 0)
			{	
				if(BattleFormat == 1) 
				{
					RewardBattleGreen = (Player.playerlvl * 100) + UnityEngine.Random.Range(51, 251);
					if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
				}
				if(BattleFormat == 2) 
				{
					RewardBattleGreen = (Player.playerlvl * 150) + UnityEngine.Random.Range(51, 351);
					if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
					int GoldChance =  UnityEngine.Random.Range(0, 101);
					if(GoldChance <= 50)
					{
						if(Player.goldfromgnoll <= (Player.playerlvl*2) && Player.goldfromgnoll <= 30)
						{
							RewardBattleGold = UnityEngine.Random.Range(0, 3);
						}
					}
				}
				if(BattleFormat == 3) 
				{
					RewardBattleGreen = (Player.playerlvl * 200) + UnityEngine.Random.Range(51, 451);
					if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
					int DiamondChance =  UnityEngine.Random.Range(0, 101);
					if(DiamondChance <= 33)
					{
						if(Player.goldfromgnoll <= Player.playerlvl && Player.goldfromgnoll <= 15)
						{
							RewardBattleDiamond = UnityEngine.Random.Range(0, 3);
						}
					}
				}
				int P_Green = Player.playergreen + RewardBattleGreen;
				int P_Combats = Player.combats - 1;
				battle_a = "<color=green>Вы победили</color> \nПричина: у проигравшего не осталось здоровья \nНаграда: \nЗелень: "+RewardBattleGreen;
				if(RewardBattleGold > 0) battle_a = battle_a + "\nЗолото: "+RewardBattleGold;
				if(RewardBattleDiamond > 0) battle_a = battle_a + "\nАлмазы: "+RewardBattleDiamond;
				if(exp > 0) battle_a = battle_a + "\nОпыт: 1";
				battle_a = battle_a + "\n\nНанесенный урон: \n"+
				this.NickName+": "+PlayerDamage+" \n";
				if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
				battle_a = battle_a + EnemyName+": "+EnemyDamage+" \n";
				if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
				battle_a = battle_a + "\n\nОсталось здоровья: \n"+
				this.NickName+": "+PlayerHP+" \n";
				if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PetHP+"\n";
				battle_a = battle_a + EnemyName+": "+EnemyHP+" \n";
				if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+ePetHP+"\n";
				battle_a = battle_a + "\n\nДополнительная информация: \n";
				if(ePetHP <= 0 && BattleFormat > 2)  battle_a = battle_a + "Зверь "+EnemyName+" был убит в бою\n";
				if(PetHP <= 0) battle_a = battle_a + "Зверь "+this.NickName+" был убит в бою\n";
				battle_d = "<color=red>Вы проиграли</color> \nПричина: у проигравшего не осталось здоровья \nПотери: \nЗелень: "+RewardBattleGreen;
				if(RewardBattleGold > 0) battle_d = battle_d + "\nЗолото: "+RewardBattleGold;
				if(RewardBattleDiamond > 0) battle_d = battle_d + "\nАлмазы: "+RewardBattleDiamond;
				battle_d = battle_d + "\n\nНанесенный урон: \n"+
				this.NickName+": "+PlayerDamage+" \n";
				if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
				battle_d = battle_d + EnemyName+": "+EnemyDamage+" \n";
				if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
				battle_d = battle_d + "\n\nОсталось здоровья: \n"+
				this.NickName+": "+PlayerHP+" \n";
				if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PetHP+"\n";
				battle_d = battle_d + EnemyName+": "+EnemyHP+" \n";
				if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+ePetHP+"\n";
				battle_d = battle_d + "\n\nДополнительная информация: \n";
				if(ePetHP <= 0 && BattleFormat > 2)  
				{
					battle_d = battle_d + "Зверь "+EnemyName+" был убит в бою\n";
					PetKills = PetKills +1;
					yield return StartCoroutine(UpdateCellPet("petkills", PetKills.ToString(), PetID.ToString()));
				}
				if(PetHP <= 0) {
					battle_d = battle_d + "Зверь "+this.NickName+" был убит в бою\n";
					yield return StartCoroutine(UpdateCellAccount("pet", "3", Player.id.ToString()));
				}
				yield return StartCoroutine(UpdateCellAccount("playergreen", P_Green.ToString(), Player.id.ToString()));
				if(RewardBattleGold > 0) 
				{
					int sumgold = Player.playergold + RewardBattleGold;
					yield return StartCoroutine(UpdateCellAccount("playergold", sumgold.ToString(), Player.id.ToString()));
					sumgold = Player.goldfromgnoll + RewardBattleGold;
					yield return StartCoroutine(UpdateCellAccount("goldfromgnoll", sumgold.ToString(), Player.id.ToString()));
				}
				if(RewardBattleDiamond > 0) 
				{
					int sumdiamond = Player.playerdiamonds + RewardBattleDiamond;
					yield return StartCoroutine(UpdateCellAccount("playerdiamonds", sumdiamond.ToString(), Player.id.ToString()));
					sumdiamond = Player.diamondfromgnoll + RewardBattleDiamond;
					yield return StartCoroutine(UpdateCellAccount("diamondfromgnoll", sumdiamond.ToString(), Player.id.ToString()));
				}
				yield return StartCoroutine(UpdateCellAccount("combats", P_Combats.ToString(), Player.id.ToString()));
				exp = Player.playerexpierence +1;
				yield return StartCoroutine(UpdateCellAccount("playerexpierence", exp.ToString(), Player.id.ToString()));
				if(exp >= (int)(Mathf.Pow(Player.playerlvl,(float)2.2)+9))
				{
					exp = exp - (int)(Mathf.Pow(Player.playerlvl,(float)2.2)+9);
					yield return StartCoroutine(UpdateCellAccount("playerexpierence", exp.ToString(), Player.id.ToString()));
					int lvl = Player.playerlvl + 1;
					yield return StartCoroutine(UpdateCellAccount("playerlvl", lvl.ToString(), Player.id.ToString()));
				}
				int TimeSeconds = 0;
				if(Player.timetoendcombat == "0") TimeSeconds = 600;
				else
				{	
					TimeSeconds = (int)(System.DateTime.Parse(Player.timetoendcombat) - System.DateTime.Now).TotalSeconds;
					TimeSeconds = TimeSeconds + 600;
				}
				System.DateTime Time = System.DateTime.Now.AddSeconds(TimeSeconds);
				yield return StartCoroutine(UpdateCellAccount("timetoendcombat", Time.ToString("dd.MM.yyyy HH:mm:ss"), Player.id.ToString()));
				Infobar.ReloadInfoBar();
			}
			else if(PlayerHP <= 0)
			{	
				RewardBattleGreen = (int)(Player.playergreen * 0.05);
				int P_Green = Player.playergreen - RewardBattleGreen;
				int P_Combats = Player.combats - 1;
				if(Player.goldlostperhour == 0) 
				{
					int GoldChance =  UnityEngine.Random.Range(0, 101);
					if(GoldChance <= 5)
					{
						RewardBattleGold = (int)(Player.playergold * 0.03);
					}
				}
				battle_a = "<color=red>Вы проиграли</color> \nПричина: у проигравшего не осталось здоровья \nПотери: \nЗелень: "+RewardBattleGreen;
				if(RewardBattleGold > 0) battle_a = battle_a + "\nЗолото: "+RewardBattleGold;
				battle_a = battle_a + "\n\nНанесенный урон: \n"+
				this.NickName+": "+PlayerDamage+" \n";
				if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
				battle_a = battle_a + EnemyName+": "+EnemyDamage+" \n";
				if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
				battle_a = battle_a + "\n\nОсталось здоровья: \n"+
				this.NickName+": "+PlayerHP+" \n";
				if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PetHP+"\n";
				battle_a = battle_a + EnemyName+": "+EnemyHP+" \n";
				if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+ePetHP+"\n";
				battle_a = battle_a + "\n\nДополнительная информация: \n";
				if(ePetHP <= 0 && BattleFormat > 2)  battle_a = battle_a + "Зверь "+EnemyName+" был убит в бою\n";
				if(PetHP <= 0) battle_a = battle_a + "Зверь "+this.NickName+" был убит в бою\n";
				battle_d = "<color=green>Вы победили</color> \nПричина: у проигравшего не осталось здоровья \nНаграда: \nЗелень: "+RewardBattleGreen;
				if(RewardBattleGold > 0) battle_d = battle_d + "\nЗолото: "+RewardBattleGold;
				battle_d = battle_d + "\n\nНанесенный урон: \n"+
				this.NickName+": "+PlayerDamage+" \n";
				if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
				battle_d = battle_d + EnemyName+": "+EnemyDamage+" \n";
				if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
				battle_d = battle_d + "\n\nОсталось здоровья: \n"+
				this.NickName+": "+PlayerHP+" \n";
				if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PetHP+"\n";
				battle_d = battle_d + EnemyName+": "+EnemyHP+" \n";
				if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+ePetHP+"\n";
				battle_d = battle_d + "\n\nДополнительная информация: \n";
				if(ePetHP <= 0 && BattleFormat > 2) 
				{
					battle_d = battle_d + "Зверь "+EnemyName+" был убит в бою\n";
					PetKills = PetKills +1;
					yield return StartCoroutine(UpdateCellPet("petkills", PetKills.ToString(), PetID.ToString()));
				}
				if(PetHP <= 0) {
					battle_d = battle_d + "Зверь "+this.NickName+" был убит в бою\n";
					yield return StartCoroutine(UpdateCellAccount("pet", "3", Player.id.ToString()));
				}
				yield return StartCoroutine(UpdateCellAccount("playergreen", P_Green.ToString(), Player.id.ToString()));
				yield return StartCoroutine(UpdateCellAccount("combats", P_Combats.ToString(), Player.id.ToString()));
				if(RewardBattleGold > 0) 
				{
					int diffgold = Player.playergold - RewardBattleGold;
					yield return StartCoroutine(UpdateCellAccount("playergold", diffgold.ToString(), Player.id.ToString()));
					yield return StartCoroutine(UpdateCellAccount("goldlostperhour", "1", Player.id.ToString()));
				}
				int TimeSeconds = 0;
				if(Player.timetoendcombat == "0") TimeSeconds = 600;
				else
				{	
					TimeSeconds = (int)(System.DateTime.Parse(Player.timetoendcombat) - System.DateTime.Now).TotalSeconds;
					TimeSeconds = TimeSeconds + 600;
				}
				System.DateTime Time = System.DateTime.Now.AddSeconds(TimeSeconds);
				yield return StartCoroutine(UpdateCellAccount("timetoendcombat", Time.ToString("dd.MM.yyyy HH:mm:ss"), Player.id.ToString()));
				Infobar.ReloadInfoBar();
			}
		}
		else
		{
			if((PlayerDamage+PlayerPetDamage) > (EnemyDamage+EnemyPetDamage))
			{	
				if(BattleFormat == 1) 
				{
					RewardBattleGreen = (Player.playerlvl * 100) + UnityEngine.Random.Range(51, 251);
					if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
				}
				if(BattleFormat == 2) 
				{
					RewardBattleGreen = (Player.playerlvl * 150) + UnityEngine.Random.Range(51, 351);
					if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
					int GoldChance =  UnityEngine.Random.Range(0, 101);
					if(GoldChance <= 50)
					{
						if(Player.goldfromgnoll <= (Player.playerlvl*2) && Player.goldfromgnoll <= 30)
						{
							RewardBattleGold = UnityEngine.Random.Range(0, 3);
						}
					}
				}
				if(BattleFormat == 3) 
				{
					RewardBattleGreen = (Player.playerlvl * 200) + UnityEngine.Random.Range(51, 451);
					if(Player.playerlvl < 5) exp = Player.playerexpierence +1;
					int DiamondChance =  UnityEngine.Random.Range(0, 101);
					if(DiamondChance <= 33)
					{
						if(Player.goldfromgnoll <= Player.playerlvl && Player.goldfromgnoll <= 15)
						{
							RewardBattleDiamond = UnityEngine.Random.Range(0, 3);
						}
					}
				}
				int P_Green = Player.playergreen + RewardBattleGreen;
				int P_Combats = Player.combats - 1;
				battle_a = "<color=green>Вы победили</color> \nПричина: победитель нанёс больше урона \nНаграда: \nЗелень: "+RewardBattleGreen;
				if(RewardBattleGold > 0) battle_a = battle_a + "\nЗолото: "+RewardBattleGold;
				if(RewardBattleDiamond > 0) battle_a = battle_a + "\nАлмазы: "+RewardBattleDiamond;
				if(exp > 0) battle_a = battle_a + "\nОпыт: 1";
				battle_a = battle_a + "\n\nНанесенный урон: \n"+
				this.NickName+": "+PlayerDamage+" \n";
				if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
				battle_a = battle_a + EnemyName+": "+EnemyDamage+" \n";
				if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
				battle_a = battle_a + "\n\nОсталось здоровья: \n"+
				this.NickName+": "+PlayerHP+" \n";
				if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PetHP+"\n";
				battle_a = battle_a + EnemyName+": "+EnemyHP+" \n";
				if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+ePetHP+"\n";
				battle_a = battle_a + "\n\nДополнительная информация: \n";
				if(ePetHP <= 0 && BattleFormat > 2)  battle_a = battle_a + "Зверь "+EnemyName+" был убит в бою\n";
				if(PetHP <= 0) battle_a = battle_a + "Зверь "+this.NickName+" был убит в бою\n";
				battle_d = "<color=red>Вы проиграли</color> \nПричина: победитель нанёс больше урона \nПотери: \nЗелень: "+RewardBattleGreen;
				if(RewardBattleGold > 0) battle_d = battle_d + "\nЗолото: "+RewardBattleGold;
				if(RewardBattleDiamond > 0) battle_d = battle_d + "\nАлмазы: "+RewardBattleDiamond;
				battle_d = battle_d + "\n\nНанесенный урон: \n"+
				this.NickName+": "+PlayerDamage+" \n";
				if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
				battle_d = battle_d + EnemyName+": "+EnemyDamage+" \n";
				if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
				battle_d = battle_d + "\n\nОсталось здоровья: \n"+
				this.NickName+": "+PlayerHP+" \n";
				if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PetHP+"\n";
				battle_d = battle_d + EnemyName+": "+EnemyHP+" \n";
				if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+ePetHP+"\n";
				battle_d = battle_d + "\n\nДополнительная информация: \n";
				if(ePetHP <= 0 && BattleFormat > 2) 
				{
					battle_d = battle_d + "Зверь "+EnemyName+" был убит в бою\n";
					PetKills = PetKills +1;
					yield return StartCoroutine(UpdateCellPet("petkills", PetKills.ToString(), PetID.ToString()));
				}
				if(PetHP <= 0) {
					battle_d = battle_d + "Зверь "+this.NickName+" был убит в бою\n";
					yield return StartCoroutine(UpdateCellAccount("pet", "3", Player.id.ToString()));
				}
				yield return StartCoroutine(UpdateCellAccount("playergreen", P_Green.ToString(), Player.id.ToString()));
				yield return StartCoroutine(UpdateCellAccount("combats", P_Combats.ToString(), Player.id.ToString()));
				yield return StartCoroutine(UpdateCellAccount("playerexpierence", exp.ToString(), Player.id.ToString()));
				if(exp >= (int)(Mathf.Pow(Player.playerlvl,(float)2.2)+9))
				{
					exp = exp - (int)(Mathf.Pow(Player.playerlvl,(float)2.2)+9);
					yield return StartCoroutine(UpdateCellAccount("playerexpierence", exp.ToString(), Player.id.ToString()));
					int lvl = Player.playerlvl + 1;
					yield return StartCoroutine(UpdateCellAccount("playerlvl", lvl.ToString(), Player.id.ToString()));
				}
				if(RewardBattleGold > 0) 
				{
					int sumgold = Player.playergold + RewardBattleGold;
					yield return StartCoroutine(UpdateCellAccount("playergold", sumgold.ToString(), Player.id.ToString()));
					sumgold = Player.goldfromgnoll + RewardBattleGold;
					yield return StartCoroutine(UpdateCellAccount("goldfromgnoll", sumgold.ToString(), Player.id.ToString()));
				}
				if(RewardBattleDiamond > 0) 
				{
					int sumdiamond = Player.playerdiamonds + RewardBattleDiamond;
					yield return StartCoroutine(UpdateCellAccount("playerdiamonds", sumdiamond.ToString(), Player.id.ToString()));
					sumdiamond = Player.diamondfromgnoll + RewardBattleDiamond;
					yield return StartCoroutine(UpdateCellAccount("diamondfromgnoll", sumdiamond.ToString(), Player.id.ToString()));
				}
				int TimeSeconds = 0;
				if(Player.timetoendcombat == "0") TimeSeconds = 600;
				else
				{	
					TimeSeconds = (int)(System.DateTime.Parse(Player.timetoendcombat) - System.DateTime.Now).TotalSeconds;
					TimeSeconds = TimeSeconds + 600;
				}
				System.DateTime Time = System.DateTime.Now.AddSeconds(TimeSeconds);
				yield return StartCoroutine(UpdateCellAccount("timetoendcombat", Time.ToString("dd.MM.yyyy HH:mm:ss"), Player.id.ToString()));
				Infobar.ReloadInfoBar();
			}
			else if((PlayerDamage+PlayerPetDamage) < (EnemyDamage+EnemyPetDamage))
			{	
				RewardBattleGreen = (int)(Player.playergreen * 0.05);
				int P_Green = Player.playergreen - RewardBattleGreen;
				int P_Combats = Player.combats - 1;
				if(Player.goldlostperhour == 0) 
				{
					int GoldChance =  UnityEngine.Random.Range(0, 101);
					if(GoldChance <= 5)
					{
						RewardBattleGold = (int)(Player.playergold * 0.03);
					}
				}
				battle_a = "<color=red>Вы проиграли</color> \nПричина: победитель нанёс больше урона \nПотери: \nЗелень: "+RewardBattleGreen;
				if(RewardBattleGold > 0) battle_a = battle_a + "\nЗолото: "+RewardBattleGold;
				battle_a = battle_a + "\n\nНанесенный урон: \n"+
				this.NickName+": "+PlayerDamage+" \n";
				if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
				battle_a = battle_a + EnemyName+": "+EnemyDamage+" \n";
				if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
				battle_a = battle_a + "\n\nОсталось здоровья: \n"+
				this.NickName+": "+PlayerHP+" \n";
				if(Infobar.PetActive == 1) battle_a = battle_a + "Зверь "+this.NickName+": "+PetHP+"\n";
				battle_a = battle_a + EnemyName+": "+EnemyHP+" \n";
				if(EnemyPet == true) battle_a = battle_a + "Зверь "+EnemyName+": "+ePetHP+"\n";
				battle_a = battle_a + "\n\nДополнительная информация: \n";
				if(ePetHP <= 0 && BattleFormat > 2)  battle_a = battle_a + "Зверь "+EnemyName+" был убит в бою\n";
				if(PetHP <= 0) battle_a = battle_a + "Зверь "+this.NickName+" был убит в бою\n";
				battle_d = "<color=green>Вы победили</color> \nПричина: победитель нанёс больше урона \nНаграда: \nЗелень: "+RewardBattleGreen;
				if(RewardBattleGold > 0) battle_d = battle_d + "\nЗолото: "+RewardBattleGold;
				battle_d = battle_d + "\n\nНанесенный урон: \n"+
				this.NickName+": "+PlayerDamage+" \n";
				if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PlayerPetDamage+"\n";
				battle_d = battle_d + EnemyName+": "+EnemyDamage+" \n";
				if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+EnemyPetDamage+"\n";
				battle_d = battle_d + "\n\nОсталось здоровья: \n"+
				this.NickName+": "+PlayerHP+" \n";
				if(Infobar.PetActive == 1) battle_d = battle_d + "Зверь "+this.NickName+": "+PetHP+"\n";
				battle_d = battle_d + EnemyName+": "+EnemyHP+" \n";
				if(EnemyPet == true) battle_d = battle_d + "Зверь "+EnemyName+": "+ePetHP+"\n";
				battle_d = battle_d + "\n\nДополнительная информация: \n";
				if(ePetHP <= 0 && BattleFormat > 2) 
				{
					battle_d = battle_d + "Зверь "+EnemyName+" был убит в бою\n";
					PetKills = PetKills +1;
					yield return StartCoroutine(UpdateCellPet("petkills", PetKills.ToString(), PetID.ToString()));
				}
				if(PetHP <= 0) {
					battle_d = battle_d + "Зверь "+this.NickName+" был убит в бою\n";
					yield return StartCoroutine(UpdateCellAccount("pet", "3", Player.id.ToString()));
				}
				yield return StartCoroutine(UpdateCellAccount("playergreen", P_Green.ToString(), Player.id.ToString()));
				yield return StartCoroutine(UpdateCellAccount("combats", P_Combats.ToString(), Player.id.ToString()));
				if(RewardBattleGold > 0) 
				{
					int diffgold = Player.playergold - RewardBattleGold;
					yield return StartCoroutine(UpdateCellAccount("playergold", diffgold.ToString(), Player.id.ToString()));
					yield return StartCoroutine(UpdateCellAccount("goldlostperhour", "1", Player.id.ToString()));
				}
				int TimeSeconds = 0;
				if(Player.timetoendcombat == "0") TimeSeconds = 600;
				else
				{	
					TimeSeconds = (int)(System.DateTime.Parse(Player.timetoendcombat) - System.DateTime.Now).TotalSeconds;
					TimeSeconds = TimeSeconds + 600;
				}
				System.DateTime Time = System.DateTime.Now.AddSeconds(TimeSeconds);
				yield return StartCoroutine(UpdateCellAccount("timetoendcombat", Time.ToString("dd.MM.yyyy HH:mm:ss"), Player.id.ToString()));
				Infobar.ReloadInfoBar();
			}
		}
		if(BattleFormat == 1 || BattleFormat == 2 || BattleFormat == 3) EnemyID = BattleFormat * -1;
		yield return StartCoroutine(CreateBattleLog(Player.id.ToString(), EnemyID.ToString(), logfight, battle_a, battle_d));
		ResultBattleInfo();
	}*/
	private void ResultBattleInfo()
	{
		GnollInfoBar.SetActive(false);
		ResultInfoBar.SetActive(true);
		ButtonText[0].text = "Детали боя";
		StoreBF = BattleFormat;
		BattleFormat = -1;
		BattleInfo.text = battle_a;
	}
	private void FightPlayerToEnemy()
	{
		//Attack Player
		PlayerRpower = _power + (_power - _epower) + UnityEngine.Random.Range(0, (_power - _epower));
		double PlayerCritical = PlayerRpower * (double)(((_skill - (_epower/10))/100)+1);
		double PlayerEvasion = (_dexterity/ _eskill) + Math.Pow((_dexterity/4), 0.8);
		int PlayerBlock = (_eprotect -_power)/6;
		float RandEvasion = UnityEngine.Random.Range(0f, 101f), RandBlock = UnityEngine.Random.Range(0f, 101f), RandCritical = UnityEngine.Random.Range(0f, 101f);
		int DamagePet = PlayerPetDamage-ePetHP; if(DamagePet < 0) DamagePet = 0;
		//Start Fight Player Round 1
		if(RandEvasion > PlayerEvasion)
		{
			if(RandBlock > PlayerBlock)
			{
				if(RandCritical <= 15)
				{
					if((int)(PlayerRpower+(PlayerCritical-(_eprotect/6))) > 0)
					{
						if((EnemyHP - (PlayerDamage + DamagePet)) - (int)(PlayerRpower+(PlayerCritical-(_eprotect/6))) <= 0)
						{
							int HP = EnemyHP - (PlayerDamage + DamagePet);
							PlayerDamage = PlayerDamage + HP;
							logfight = logfight + this.NickName + " нанёс критический удар, ударил c силой " + (int)PlayerCritical+ " и нанес " +  HP + " урона \n";
						}
						else
						{
							PlayerDamage = PlayerDamage + (int)(PlayerRpower+(PlayerCritical-(_eprotect/6)));
							logfight = logfight + this.NickName + " нанёс критический удар, ударил c силой " + (int)PlayerCritical+ " и нанес " +  (int)(PlayerRpower+(PlayerCritical-(_eprotect/6))) + " урона \n";
						}
					}
					else logfight = logfight + this.NickName + " попытался нанести удар но "+EnemyName+" угадал замысел и поставил блок\n";
				}
				else
				{
					if((int)(PlayerRpower-(_eprotect/6)) > 0)
					{
						if((EnemyHP - (PlayerDamage + DamagePet)) - (int)(PlayerRpower-(_eprotect/6)) <= 0)
						{
							int HP = EnemyHP - (PlayerDamage + DamagePet);
							PlayerDamage = PlayerDamage + HP;
							logfight = logfight + this.NickName + " ударил c силой " +PlayerRpower+ " и нанес " +  HP + " урона \n";
						}
						else
						{
							PlayerDamage = PlayerDamage +  (int)(PlayerRpower-(_eprotect/6));
							logfight = logfight + this.NickName + " ударил c силой " +PlayerRpower+ " и нанес " +  (int)(PlayerRpower-(_eprotect/6)) + " урона \n";
						}
						
					}
					else logfight = logfight + this.NickName + " попытался нанести удар но "+EnemyName+" угадал замысел и поставил блок\n";
				}
			}
			else 
			{
				logfight = logfight + this.NickName + " попытался нанести удар но "+EnemyName+" угадал замысел и поставил блок \n";
			}
		}
		else 
		{
			logfight = logfight + this.NickName + " попытался нанести удар но "+EnemyName+" удачно увернулся \n";
		}
	}
	private void FightEnemyToPlayer()
	{
		
		//Attack Player
		EnemyRpower = _epower + (_epower - _power) + UnityEngine.Random.Range(0, (_epower - _power));
		double EnemyCritical = EnemyRpower * (double)(((_eskill - (_power/10))/100)+1);
		double EnemyEvasion = (_edexterity/ _skill) + Math.Pow((_edexterity/4), 0.8);
		int EnemyBlock = (_protect -_epower)/6;
		float RandEvasion = UnityEngine.Random.Range(0f, 101f), RandBlock = UnityEngine.Random.Range(0f, 101f), RandCritical = UnityEngine.Random.Range(0f, 101f);
		int DamagePet = EnemyPetDamage-PetHP; if(DamagePet < 0) DamagePet = 0;
		//Start Fight Player Round 1
		if(RandEvasion > EnemyEvasion)
		{
			if(RandBlock > EnemyBlock)
			{
				if(RandCritical <= 15)
				{
					if((int)(EnemyRpower+(EnemyCritical-(_protect/6))) > 0)
					{
						if((PlayerHP - (EnemyDamage + DamagePet)) - (int)(EnemyRpower+(EnemyCritical-(_protect/6))) <= 0)
						{
							int HP = PlayerHP - (EnemyDamage + DamagePet);
							EnemyDamage = EnemyDamage + HP;
							logfight = logfight + EnemyName + " нанёс критический удар, ударил c силой " + (int)EnemyCritical+ " и нанес " +  HP + " урона \n";
						}
						else
						{
							EnemyDamage = EnemyDamage + (int)(EnemyRpower+(EnemyCritical-(_protect/6)));
							logfight = logfight + EnemyName + " нанёс критический удар, ударил c силой " + (int)EnemyCritical+ " и нанес " +  (int)(EnemyRpower+(EnemyCritical-(_protect/6))) + " урона \n";
						}
					}
					else logfight = logfight + EnemyName + " попытался нанести удар но "+this.NickName+" угадал замысел и поставил блок\n";
				}
				else
				{
					if((int)(EnemyRpower-(_protect/6)) > 0)
					{
						if((PlayerHP - (EnemyDamage + DamagePet)) - (int)(EnemyRpower-(_protect/6)) <= 0)
						{
							int HP = PlayerHP - (EnemyDamage + DamagePet);
							EnemyDamage = EnemyDamage + HP;
							logfight = logfight + EnemyName + " ударил c силой " +EnemyRpower+ " и нанес " +  HP + " урона \n";
						}
						else
						{
							EnemyDamage = EnemyDamage +  (int)(EnemyRpower-(_protect/6));
							logfight = logfight + EnemyName + " ударил c силой " +EnemyRpower+ " и нанес " +  (int)(EnemyRpower-(_protect/6)) + " урона \n";
						}
						
					}
					else logfight = logfight + EnemyName + " попытался нанести удар но "+this.NickName+" угадал замысел и поставил блок\n";
				}
			}
			else 
			{
				logfight = logfight + EnemyName + " попытался нанести удар но "+this.NickName+" угадал замысел и поставил блок \n";
			}
		}
		else 
		{
			logfight = logfight + EnemyName + " попытался нанести удар но "+this.NickName+" удачно увернулся \n";
		}
	}
	private void FightPetPlayerToEnemy()
	{
		//Attack Player
		PlayerPetPower = PetPower;
		if(EnemyHP - (PlayerPetDamage+PlayerDamage+PlayerPetPower) <= 0)
		{
			int Damage = EnemyHP - PlayerDamage - PlayerPetDamage;
			PlayerPetDamage = PlayerPetDamage + Damage;
			logfight = logfight + "Зверь " + this.NickName + " ударил " + EnemyName + " и нанес " +  Damage + " урона \n";
		}
		else
		{
			PlayerPetDamage = PlayerPetDamage +  PlayerPetPower;
			logfight = logfight + "Зверь " + this.NickName + " ударил " + EnemyName + " и нанес " +  PlayerPetPower + " урона \n";
		}
	}
	private void FightPetEnemyToPlayer()
	{
		//Attack Player
		EnemyPetPower = ePetPower;
		if(PlayerHP - (EnemyPetDamage + EnemyDamage + EnemyPetPower) <= 0)
		{
			int Damage = PlayerHP - EnemyDamage - EnemyPetDamage;
			EnemyPetDamage = EnemyPetDamage + Damage;
			logfight = logfight + "Зверь " + EnemyName + " ударил " + this.NickName + " и нанес " +  Damage + " урона \n";
		}
		else
		{
			EnemyPetDamage = EnemyPetDamage +  EnemyPetPower;
			logfight = logfight + "Зверь " + EnemyName + " ударил " + this.NickName + " и нанес " +  EnemyPetPower + " урона \n";
		}
	}
	private void FightPetPlayerToPetEnemy()
	{
		//Attack PlayerPet
		PlayerPetPower = PetPower + (PetPower - ePetPower) + UnityEngine.Random.Range(0, (PetPower - ePetPower));
		double PlayerPetCritical = PlayerPetPower * (double)(((PetSkill - (ePetPower/10))/100)+1);
		double PlayerPetEvasion = (PetDexterity/ ePetSkill) + Math.Pow((PetDexterity/4), 0.8);
		int PlayerPetBlock = (ePetProtect - PetPower)/6;
		float RandEvasion = UnityEngine.Random.Range(0f, 101f), RandBlock = UnityEngine.Random.Range(0f, 101f), RandCritical = UnityEngine.Random.Range(0f, 101f);
		
		//Start Fight PlayerPet Round
		if(RandEvasion > PlayerPetEvasion)
		{
			if(RandBlock > PlayerPetBlock)
			{
				if(RandCritical <= 15)
				{
					if((int)(PlayerPetPower+(PlayerPetCritical-(ePetProtect/6))) > 0)
					{
						if(((ePetHP - PlayerPetDamage) - (int)(PlayerPetPower+(PlayerPetCritical-(ePetProtect/6)))) <= 0)
						{
							int HP = ePetHP - PlayerPetDamage;
							PlayerPetDamage = PlayerPetDamage + HP;
							logfight = logfight + "Зверь " + this.NickName + " нанёс критический удар зверю "+ EnemyName +", ударил c силой " + (int)PlayerPetCritical+ " и нанес " +  HP + " урона \n";
						}
						else
						{
							PlayerPetDamage = PlayerPetDamage + (int)(PlayerPetPower+(PlayerPetCritical-(ePetProtect/6)));
							logfight = logfight + "Зверь " + this.NickName + " нанёс критический удар зверю "+ EnemyName +", ударил c силой " + (int)PlayerPetCritical+ " и нанес " +  (int)(PlayerPetPower+(PlayerPetCritical-(ePetProtect/6))) + " урона \n";
						}
					}
					else logfight = logfight + "Зверь " + this.NickName + " попытался нанести удар но зверь "+EnemyName+" угадал замысел и удачно увернулся\n";
				}
				else
				{
					if((int)(PlayerPetPower-(ePetProtect/6)) > 0)
					{
						if((ePetHP - PlayerPetDamage) - (int)(PlayerPetPower-(ePetProtect/6)) <= 0)
						{
							int HP = ePetHP - PlayerPetDamage;
							PlayerPetDamage = PlayerPetDamage + HP;
							logfight = logfight + "Зверь " + this.NickName + " ударил зверя "+ EnemyName +" c силой " +PlayerPetPower+ " и нанес " +  HP + " урона \n";
						}
						else
						{
							PlayerPetDamage = PlayerPetDamage +  (int)(PlayerPetPower-(ePetProtect/6));
							logfight = logfight + "Зверь " + this.NickName + " ударил зверя "+ EnemyName +" c силой " +PlayerPetPower+ " и нанес " +  (int)(PlayerPetPower-(ePetProtect/6)) + " урона \n";
						}
						
					}
					else logfight = logfight + "Зверь " + this.NickName + " попытался нанести удар но зверь "+EnemyName+" угадал замысел и удачно увернулся\n";
				}
			}
			else 
			{
				logfight = logfight + "Зверь " + this.NickName + " попытался нанести удар но зверь "+EnemyName+" угадал замысел и удачно увернулся \n";
			}
		}
		else 
		{
			logfight = logfight + "Зверь " + this.NickName + " попытался нанести удар но зверь "+EnemyName+" удачно увернулся \n";
		}
	}
	private void FightPetEnemyToPetPlayer()
	{
		//Attack EnemyPet
		EnemyPetPower = ePetPower + (ePetPower - PetPower) + UnityEngine.Random.Range(0, (ePetPower - PetPower));
		double EnemyPetCritical = EnemyPetPower * (double)(((ePetSkill - (PetPower/10))/100)+1);
		double EnemyPetEvasion = (ePetDexterity/ PetSkill) + Math.Pow((ePetDexterity/4), 0.8);
		int EnemyPetBlock = (PetProtect - ePetPower)/6;
		float RandEvasion = UnityEngine.Random.Range(0f, 101f), RandBlock = UnityEngine.Random.Range(0f, 101f), RandCritical = UnityEngine.Random.Range(0f, 101f);
		
		//Start Fight EnemyPet Round
		if(RandEvasion > EnemyPetEvasion)
		{
			if(RandBlock > EnemyPetBlock)
			{
				if(RandCritical <= 15)
				{
					if((int)(EnemyPetPower+(EnemyPetCritical-(PetProtect/6))) > 0)
					{
						if(((PetHP - EnemyPetDamage) - (int)(EnemyPetPower+(EnemyPetCritical-(ePetProtect/6)))) <= 0)
						{
							int HP = ePetHP - EnemyPetDamage;
							EnemyPetDamage = EnemyPetDamage + HP;
							logfight = logfight + "Зверь " + EnemyName + " нанёс критический удар зверю "+ this.NickName +", ударил c силой " + (int)EnemyPetCritical+ " и нанес " +  HP + " урона \n";
						}
						else
						{
							EnemyPetDamage = EnemyPetDamage + (int)(EnemyPetPower+(EnemyPetCritical-(PetProtect/6)));
							logfight = logfight + "Зверь " + EnemyName + " нанёс критический удар зверю "+ this.NickName +", ударил c силой " + (int)EnemyPetCritical+ " и нанес " +  (int)(EnemyPetPower+(EnemyPetCritical-(PetProtect/6))) + " урона \n";
						}
					}
					else logfight = logfight + "Зверь " + EnemyName + " попытался нанести удар но зверь "+this.NickName+" угадал замысел и удачно увернулся\n";
				}
				else
				{
					if((int)(EnemyPetPower-(PetProtect/6)) > 0)
					{
						if((PetHP - EnemyPetDamage) - (int)(EnemyPetPower-(PetProtect/6)) <= 0)
						{
							int HP = PetHP - EnemyPetDamage;
							EnemyPetDamage = EnemyPetDamage + HP;
							logfight = logfight + "Зверь " + EnemyName + " ударил зверя "+ this.NickName +" c силой " +EnemyPetPower+ " и нанес " +  HP + " урона \n";
						}
						else
						{
							EnemyPetDamage = EnemyPetDamage +  (int)(EnemyPetPower-(PetProtect/6));
							logfight = logfight + "Зверь " + EnemyName + " ударил зверя "+ this.NickName +" c силой " +EnemyPetPower+ " и нанес " +  (int)(EnemyPetPower-(PetProtect/6)) + " урона \n";
						}
						
					}
					else logfight = logfight + "Зверь " + EnemyName + " попытался нанести удар но зверь "+this.NickName+" угадал замысел и удачно увернулся\n";
				}
			}
			else 
			{
				logfight = logfight + "Зверь " + EnemyName + " попытался нанести удар но зверь "+this.NickName+" угадал замысел и удачно увернулся \n";
			}
		}
		else 
		{
			logfight = logfight + "Зверь " + EnemyName + " попытался нанести удар но зверь "+this.NickName+" удачно увернулся \n";
		}
	}
	private IEnumerator LoadBattleGnolls()
	{
		yield return StartCoroutine(LoadPlayerStats());
		if(Infobar.PetActive == 1) yield return StartCoroutine(LoadPlayerPetStats());
		yield return StartCoroutine(LoadGnollStats());
	}
	private IEnumerator LoadPlayerStats()
	{
		string tbpower = "", tbprotect = "", tbdexterity = "", tbskill = "", tbvitality = "";
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadAccount", "Yes");
		FindDataBase.AddField("PlayerName", this.NickName);
		FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		Player = JsonUtility.FromJson<PlayerInfo>(jsonformat);
		yield return StartCoroutine(LoadEquipment());
		//=====================[Load Profile]====================
		if(equipments > 0)
		{
			for(int i = 0; i < equipments; i++)
			{
				yield return StartCoroutine(LoadItemID(Items[i].id_item.ToString()));
			}
		}
		if(bpower != 0) { tbpower = " + " + bpower; }
		_power = Player.playerpower + bpower;
		PlayerCharacteristics[0].text = Player.playerpower.ToString() + tbpower;
		if(bprotect != 0) { tbprotect = " + " + bprotect;}
		_protect = Player.playerprotection + bprotect; 
		PlayerCharacteristics[1].text = Player.playerprotection.ToString() + tbprotect;
		if(bdexterity != 0) { tbdexterity = " + " + bdexterity;}
		_dexterity = Player.playerdexterity + bdexterity;
		PlayerCharacteristics[2].text = Player.playerdexterity.ToString() + tbdexterity;
		if(bskill != 0) { tbskill = " + " + bskill;}
		_skill = Player.playerskill + bskill;
		PlayerCharacteristics[3].text = Player.playerskill.ToString() + tbskill;
		if(bvitality != 0) tbvitality = " + " + bvitality;
		PlayerCharacteristics[4].text = Player.playersurvivability.ToString() + tbvitality;
		www.Dispose();
	}
	public class PlayerInfo
	{
		public int id, combats, playergreen, playergold, playerdiamonds, playerpower, playerprotection, playerdexterity, playerskill, playersurvivability,
		glory, battleswin, battleslose, greenlooted, greenlost, goldlooted, goldlost, travelminutes, workhour, mining, beetleswin,
		playerhp, playerlvl, playerexpierence, inventory, goldfromgnoll, diamondfromgnoll, goldlostperhour;
		public string nickname, clan, timetoendcombat;
	}
	private IEnumerator LoadPlayerPetStats()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadPet", "Yes");
		FindDataBase.AddField("PlayerID", Infobar.pID);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		PetInfo Data = JsonUtility.FromJson<PetInfo>(jsonformat);
		//=======================[Load Pet]======================
		ePetHP = 0;
		EnemyPet = false;
		PetID = Data.id;
		PetPower= Data.petpower;
		PetProtect = Data.petprotect;
		PetDexterity = Data.petdexterity;
		PetSkill = Data.petskill;
		PetVitality = Data.petvitality;
		PetKills = Data.petkills;
		PetHP = Mathf.FloorToInt(Data.pethp);
		PetMaxHP =  Mathf.FloorToInt(Mathf.Pow(PetVitality, 1.65f)-4);
		if(BattleFormat == 3)
		{
			EnemyPet = true;
			ePetPower= Data.petpower;
			ePetProtect = Data.petprotect;
			ePetDexterity = Data.petdexterity;
			ePetSkill = Data.petskill;
			ePetVitality = Data.petvitality;
			ePetMaxHP =  Mathf.FloorToInt(Mathf.Pow(PetVitality, 1.65f)-4);
			ePetHP = PetMaxHP;
		}
		www.Dispose();
	}
	public class PetInfo
	{
		public int id, petpower, petprotect, petdexterity, petskill, petvitality, petkills;
		public float pethp;
	}
	private IEnumerator LoadGnollStats()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadGnollEquipment", "Yes");
		FindDataBase.AddField("PlayerLvl", Player.playerlvl);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		GnollInfo Data = JsonUtility.FromJson<GnollInfo>(jsonformat);
		//=====================[Load Profile]====================
		if(Data.weapon != 0) yield return StartCoroutine(LoadItemForGnoll(Data.weapon.ToString()));
		if(Data.armor != 0) yield return StartCoroutine(LoadItemForGnoll(Data.armor.ToString()));
		if(Data.helmet != 0) yield return StartCoroutine(LoadItemForGnoll(Data.helmet.ToString()));
		if(Data.boot != 0) yield return StartCoroutine(LoadItemForGnoll(Data.boot.ToString()));
		if(Data.glove != 0) yield return StartCoroutine(LoadItemForGnoll(Data.glove.ToString()));
		if(Data.pant != 0) yield return StartCoroutine(LoadItemForGnoll(Data.pant.ToString()));
		if(Data.shield != 0) yield return StartCoroutine(LoadItemForGnoll(Data.shield.ToString()));

		int epower = Player.playerpower + UnityEngine.Random.Range(minb, maxb);
		if(epower < 5) epower = 5;
		_epower = epower + ebpower;
		int eprotect = Player.playerprotection + UnityEngine.Random.Range(minb, maxb); 
		if(eprotect < 5) eprotect = 5;
		_eprotect = eprotect + ebprotect;
		int edexterity = Player.playerdexterity + UnityEngine.Random.Range(minb, maxb); 
		if(edexterity < 5) edexterity = 5;
		_edexterity = edexterity + ebdexterity;
		int eskill = Player.playerskill + UnityEngine.Random.Range(minb, maxb); 
		if(eskill < 5) eskill = 5;
		_eskill = eskill + ebskill;
		int evitality = Player.playersurvivability + UnityEngine.Random.Range(minb, maxb);
		if(evitality < 5) evitality = 5;
		_evitality = evitality + ebvitality;
		EnemyCharacteristics[0].text = epower.ToString();
		EnemyCharacteristics[1].text = eprotect.ToString();
		EnemyCharacteristics[2].text = edexterity.ToString();
		EnemyCharacteristics[3].text = eskill.ToString();
		EnemyCharacteristics[4].text = evitality.ToString();

		EnemyAvatar.sprite = SpriteGnolls[BattleFormat-1];
		www.Dispose();
	}
	public class GnollInfo
	{
		public int weapon, armor, helmet, boot, glove, pant, shield;
	}
	private IEnumerator LoadEquipment()
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadEquipment", "Yes");
		FindDataBase.AddField("PlayerID", this.PlayerID);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
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
		if(Data.itempower != 0) bpower = bpower + Data.itempower;
		if(Data.itemprotection != 0) bprotect = bprotect + Data.itemprotection;
		if(Data.itemdexterity != 0) bdexterity = bdexterity + Data.itemdexterity;
		if(Data.itemskill != 0) bskill = bskill + Data.itemskill;
		if(Data.itemvitability != 0) bvitality = bvitality + Data.itemvitability;		
		www.Dispose();
	}
	public class ItemInfo
	{
		public int itempower, itemprotection, itemdexterity, itemskill, itemvitability;
	}
	private IEnumerator LoadItemForGnoll(string _ItemID)
	{
		WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("LoadItemToID", "Yes");
		FindDataBase.AddField("ItemID", _ItemID);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadshopitems.php", FindDataBase);
		yield return www.SendWebRequest();
		jsonformat = www.downloadHandler.text;
		ItemInfo GnollItem = JsonUtility.FromJson<ItemInfo>(jsonformat);
		if(GnollItem.itempower != 0) ebpower = ebpower + GnollItem.itempower;
		if(GnollItem.itemprotection != 0) ebprotect = ebprotect + GnollItem.itemprotection;
		if(GnollItem.itemdexterity != 0) ebdexterity = ebdexterity + GnollItem.itemdexterity;
		if(GnollItem.itemskill != 0) ebskill = ebskill + GnollItem.itemskill;
		if(GnollItem.itemvitability != 0) ebvitality = ebvitality + GnollItem.itemvitability;		
		www.Dispose();
	}
	private IEnumerator UpdateCellAccount(string cellname, string value, string id)
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("UpdateCell", cellname);
		FindDataBase.AddField("UpdateValue", value);;
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
	private IEnumerator CreateBattleLog(string id_attacker, string id_defender, string log, string log_battle_attacker, string log_battle_defender)
    {
        WWWForm FindDataBase = new WWWForm();
		FindDataBase.AddField("OnGameRequest", "Yes");
		FindDataBase.AddField("CreateBattleLog", "Yes");
		FindDataBase.AddField("ID_Attacker", id_attacker);
		FindDataBase.AddField("ID_Defender", id_defender);
		FindDataBase.AddField("Log", log);
		FindDataBase.AddField("Log_Battle_Attacker", log_battle_attacker);
		FindDataBase.AddField("Log_Battle_Defender", log_battle_defender);

		UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", FindDataBase);
		yield return www.SendWebRequest();
		www.Dispose();
    }
}
