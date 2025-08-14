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

    public int BattleCount, function, differencetime, pExp, MaxHealth, psurv, equipments, bvitality, PetActive, pGold, pID, pLvl, pGreen, pGuardHour, pGuardHours, pMinedGold, pMinedGoldStats, pMaxMinegGold;
    public int PetID, PetOwner, PetAvatar, PetPower, PetProtect, PetDexterity, PetSkill, PetVitality, PetKills, PetHP, PetMaxHP;
    public string PetName, pTimeToEndGuard, pTimeToEndMine, pTimeToNextMine, pHorse;
    public bool IsActiveGuard = false, IsActiveMine = false, IsMineToday = false, IsActiveHike = false;
    public float pHP;
    public Image Pet;
    public Sprite[] PetIcons;
    public GameObject PetHPBar, HourRewardButton, RewardIcon;
    System.DateTime momenttime, rewardtime;
    public SnapSkrolling[] Scroll;
    public PanelPlayer PanelPlayer;
    private IEnumerator RegenCoroutine;
    public PlayerInfo Account;

    private float _timeLeft = 0f;

    private IEnumerator StartTimer()
    {
        while (_timeLeft > 0)
        {
            _timeLeft -= Time.deltaTime;
            yield return null;
        }
    }

    private void Start()
    {
        if (SliderBattles) SliderBattles.value = 0;
        this.NickName = PlayerPrefs.GetString(nick);
        this.PLvl = PlayerPrefs.GetString(lvl);
        this.SerialCode = PlayerPrefs.GetString(code);
        this.PlayerID = PlayerPrefs.GetString(id);
        StartCoroutine(LoadAcc());
        //StartCoroutine(FindCountBattle());
    }

    private IEnumerator LoadAcc()
    {
        WWWForm form = new WWWForm();
        form.AddField("PlayerName", this.NickName);
        form.AddField("PlayerSerialCode", this.SerialCode);

        using (UnityWebRequest www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/account", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"HTTP Error: {(long)www.responseCode} | {www.error} | URL: {www.url}");
                yield break;
            }

            var json = www.downloadHandler.text;
            PlayerInfo parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<PlayerInfo>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Невірний JSON для account: {e.Message}\nRAW: {json}");
                yield break;
            }
            if (parsed == null)
            {
                Debug.LogError($"Порожній або некоректний JSON. RAW: {json}");
                yield break;
            }

            Account = parsed;

            // ====== Infobar ======
            pID = Account.id;
            if (PlayerName) PlayerName.text = Account.nickname;

            if (PlayerLvl) PlayerLvl.text = Account.playerlvl.ToString();
            pLvl = Account.playerlvl;

            if (PlayerEXP) PlayerEXP.text = Account.playerexpierence + "/" + (int)(Mathf.Pow(Account.playerlvl, 2.2f) + 9);

            psurv = Account.playersurvivability;
            bvitality = 0;
            MaxHealth = Mathf.FloorToInt(Mathf.Pow(Account.playersurvivability + bvitality, 2.2f) + 66);

            int gethp = Mathf.FloorToInt(Account.playerhp);
            if (gethp > MaxHealth) gethp = MaxHealth;
            if (PlayerHP) PlayerHP.text = gethp + "/" + MaxHealth;

            pGreen = Account.playergreen;
            if (CountGreen) CountGreen.text = Account.playergreen.ToString();
            if (CountGold) CountGold.text = Account.playergold.ToString();
            pGold = Account.playergold;
            if (CountDiamonds) CountDiamonds.text = Account.playerdiamonds.ToString();

            pExp = Account.playerexpierence;
            pHP = Account.playerhp;

            pTimeToEndGuard = Account.timetoendguard;
            if (!string.IsNullOrEmpty(Account.timetoendguard) && Account.timetoendguard != "0")
            {
                if (DateTime.TryParse(Account.timetoendguard, out var guardEnd))
                {
                    IsActiveGuard = DateTime.Now < guardEnd;
                }
            }

            // Слайдери
            if (SliderPlyerExpierence)
            {
                SliderPlyerExpierence.maxValue = (int)(Mathf.Pow(Account.playerlvl, 2.2f) + 9);
                SliderPlyerExpierence.value = Account.playerexpierence;
            }
            if (SliderPlayerHP)
            {
                SliderPlayerHP.maxValue = MaxHealth;
                SliderPlayerHP.value = gethp;
            }

            PlayerPrefs.SetString(id, Account.id.ToString());
            PlayerPrefs.SetString(lvl, Account.playerlvl.ToString());
            PlayerPrefs.Save();

            if (Account.hourreward == 0)
            {
                if (HourRewardText) HourRewardText.text = $"Какие орки в нашем бастионе, {Account.nickname}, а я уже заждался тебя, тебе тут награда прилетела за активность";
                if (HourRewardButton) HourRewardButton.SetActive(true);
            }
            else
            {
                if (HourRewardText) HourRewardText.text = $"{Account.nickname}, ты опять здесь? Что-ж правильно, трудись на благо орды!";
                if (RewardIcon) RewardIcon.SetActive(false);
            }
            
        }

        // StartCoroutine(RegenHP()); // за потреби
    }
    private IEnumerator FindCountBattle()
    {
        _timeLeft = 0;
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("PlayerName", this.NickName);
        FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

        UnityWebRequest www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/account", FindDataBase);
        yield return www.SendWebRequest();
        result = www.downloadHandler.text;
        www.Dispose();
        CountBattles.text = result + "/6";
        BattleCount = int.Parse(result);
        SliderBattles.value = BattleCount;
        yield return StartCoroutine(FindCountTime());
        if (result != "6")
        {
            if (momenttime >= System.DateTime.Now)
            {
                int differnsebattles = 6 - (BattleCount);
                CountBattles.text = BattleCount.ToString() + "/6";
                differencetime = (int)(momenttime - System.DateTime.Now).TotalSeconds;
                if (differnsebattles == 0)
                { _timeLeft = 0; }
                else
                {
                    CheckBattles();
                    if (differencetime <= 600) _timeLeft = differencetime;
                    else _timeLeft = differencetime - ((5 - BattleCount) * 600);
                    StartCoroutine(PlusCountBattle());
                    StartCoroutine(StartTimer());
                }
            }
            else
            {
                CountBattles.text = BattleCount.ToString() + "/6";
                StartCoroutine(UpdateCountBattle());
            }
        }
        else { _timeLeft = 0; }

    }

    private void CheckBattles()
    {
        double fff = (double)differencetime / 600;
        int function = Mathf.CeilToInt((float)fff);
        if (differencetime <= 600) BattleCount = 5;
        BattleCount = 6 - function;
    }
    private IEnumerator UpdateCountBattle()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("UpdateDataBaseBattleCount", "Yes");
        FindDataBase.AddField("PlayerName", this.NickName);
        FindDataBase.AddField("PlayerID", this.PlayerID);
        if (BattleCount != 6)
        {
            if (momenttime <= System.DateTime.Now)
            {
                FindDataBase.AddField("PlayerCombats", 6);
                FindDataBase.AddField("TimeToEndCombat", "0");
            }
            else
            {
                momenttime = System.DateTime.Now;
                momenttime = momenttime.AddSeconds((6 - BattleCount) * 600);
                string timetoendbattles = momenttime.ToString();
                FindDataBase.AddField("PlayerCombats", BattleCount);
                FindDataBase.AddField("TimeToEndCombat", timetoendbattles);
            }
        }
        else
        {
            FindDataBase.AddField("PlayerCombats", 6);
            FindDataBase.AddField("TimeToEndCombat", "0");
        }
        FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

        UnityWebRequest www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/account", FindDataBase);
        yield return www.SendWebRequest();
        result = www.downloadHandler.text;
        BattleCount = int.Parse(result);
        SliderBattles.value = BattleCount;
        CountBattles.text = result + "/6";
        if (BattleCount != 6) { _timeLeft = time; StartCoroutine(StartTimer()); }
        www.Dispose();
    }

    private IEnumerator PlusCountBattle()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("PlayerName", this.NickName);
        FindDataBase.AddField("PlayerID", this.PlayerID);

        UnityWebRequest www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/account", FindDataBase);
        yield return www.SendWebRequest();
        result = www.downloadHandler.text;
        BattleCount = int.Parse(result);
        SliderBattles.value = BattleCount;
        CountBattles.text = result + "/6";
        www.Dispose();
    }
    private IEnumerator FindCountTime()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("PlayerName", this.NickName);
        FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

        UnityWebRequest www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/account", FindDataBase);
        yield return www.SendWebRequest();
        resultt = www.downloadHandler.text;
        if (resultt != "0") { momenttime = System.DateTime.Parse(resultt); }
        else { momenttime = System.DateTime.Now; momenttime = momenttime.AddSeconds(-1); }
        www.Dispose();
    }

    [Serializable]
    public class PlayerInfo
    {
        // Поля під JSON з сервера (усі в lower camel / lower snake-подібному стилі, як у DTO)
        public int id;
        public string nickname;
        public string serialcode;

        public int playerlvl;
        public int playerexpierence;
        public int playergreen;
        public int playergold;
        public int playerdiamonds;
        public int playerfraction;
        public int playerpower;
        public int playerprotection;
        public int playerdexterity;
        public int playerskill;
        public int playersurvivability;

        public int combats;
        public int pet;
        public int hourreward;
        
        public int guardhour;
        public int guardhours;
        public int mining;
        public int minedgold;
        public int ismine;
        public int maxminedgold;
        public int horse;
        public int hikeminutes;
        public int hikemin;
        public int hikeactivemin;
        public int monkreward;

        public string timetoendguard;
        public string timetoendmine;
        public string timetonextmine;
        public string horsetime;
        public string timetoendhike;
        public string lasthike;

        public float playerhp;
    }

    public void ReloadInfoBar()
    {
        if (SliderBattles) SliderBattles.value = 0;
        this.NickName = PlayerPrefs.GetString(nick);
        this.PLvl = PlayerPrefs.GetString(lvl);
        this.SerialCode = PlayerPrefs.GetString(code);
        this.PlayerID = PlayerPrefs.GetString(id);
        StartCoroutine(LoadAcc());
    }
    
    public IEnumerator SetCellAccount(string nickname, string serialcode, string cell, string value, Action<bool,string> onDone = null)
    {
        WWWForm form = new WWWForm();
        form.AddField("PlayerName", nickname);
        form.AddField("PlayerSerialCode", serialcode);
        form.AddField("cell", cell);
        form.AddField("value", value);

        using (var www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/setcell", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onDone?.Invoke(false, $"HTTP {www.responseCode}: {www.error}");
                yield break;
            }

            var body = www.downloadHandler.text?.Trim();
            // "0" — ок, "1" — не знайдено гравця, "2" — некоректне значення, "3" — заборонене поле
            bool ok = body == "0";
            onDone?.Invoke(ok, body);
        }
    }

    // GetCellAccount("User3", "ZDM6ATHJSUMABZJ4", "PlayerGold", value => { ... })
    public IEnumerator GetCellAccount(string nickname, string serialcode, string cell, Action<string> onValue)
    {
        WWWForm form = new WWWForm();
        form.AddField("PlayerName", nickname);
        form.AddField("PlayerSerialCode", serialcode);
        form.AddField("cell", cell);

        using (var www = UnityWebRequest.Post("https://api.clashfarm.com/api/player/getcell", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"HTTP {www.responseCode}: {www.error}");
                onValue?.Invoke(null);
                yield break;
            }

            var body = www.downloadHandler.text?.Trim();
            // якщо сервер повернув "1"/"3" — вважай помилкою й вертай null
            if (body == "1" || body == "3")
            {
                onValue?.Invoke(null);
            }
            else
            {
                onValue?.Invoke(body); // наприклад "5000"
            }
        }
    }
}
