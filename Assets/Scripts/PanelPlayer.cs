using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
public class PanelPlayer : MonoBehaviour
{
    private string NickName, SerialCode, nick = "Name", code = "SerialCode", PlayerID, id = "ID", jsonformat;
    [SerializeField] private GameObject PlayerPanel, ErrorText;
    [SerializeField] private Sprite BuyInvSlot;
    [SerializeField] public Image[] Equipments;
    [SerializeField] private Image[] Inventory;

    // Параметри гравця
    [SerializeField] private Text Power, Protection, Dexterity, Skill, Vitability, HP, MaxHP, RegenHP;
    private int pPower, pProtection, pDexterity, pSkill, pVitability, InventoryCount, equipments, bpower = 0, bprotect = 0, bdexterity = 0, bskill = 0, bvitality = 0;

    // Статистика гравця
    [SerializeField] private Text Name, Friends, Level, Expierence, Glory, Clan, BattlesInfo, LootGreenInfo, LootGoldInfo, TravelInfo, WorkInfo, MineInfo, BeetlesInfo;

    // Тренування
    [SerializeField] private Text WPower, WProtection, WDexterity, WSkill, WVitability, wName, wDescription, wUpgrade;
    public ItemJS[] Items;

    public void OpenPanel()
    {
        PlayerPanel.SetActive(true);
        ErrorText.SetActive(false);
        this.NickName = PlayerPrefs.GetString(nick);
        this.SerialCode = PlayerPrefs.GetString(code);
        this.PlayerID = PlayerPrefs.GetString(id);
        StartCoroutine(LoadPlayerStats());
    }

    public void LoadStat()
    {
        this.NickName = PlayerPrefs.GetString(nick);
        this.SerialCode = PlayerPrefs.GetString(code);
        this.PlayerID = PlayerPrefs.GetString(id);
        StartCoroutine(LoadPlayerStats());
    }

    private IEnumerator LoadPlayerStats()
    {
        bpower = 0;
        bprotect = 0;
        bdexterity = 0;
        bskill = 0;
        bvitality = 0;
        string tbpower = "";
        string tbprotect = "";
        string tbdexterity = "";
        string tbskill = "";
        string tbvitality = "";

        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadAccount", "Yes");
        FindDataBase.AddField("PlayerName", this.NickName);
        FindDataBase.AddField("PlayerSerialCode", this.SerialCode);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;
        PlayerInfo Data = JsonUtility.FromJson<PlayerInfo>(jsonformat);
        yield return StartCoroutine(LoadEquipment());

        // Завантаження профілю
        if (equipments > 0)
        {
            for (int i = 0; i < equipments; i++)
            {
                yield return StartCoroutine(LoadItemID(Items[i].id_item.ToString()));
            }
        }

        if (bpower != 0) tbpower = " + " + bpower;
        Power.text = "Сила: " + Data.playerpower.ToString() + tbpower;

        if (bprotect != 0) tbprotect = " + " + bprotect;
        Protection.text = "Захист: " + Data.playerprotection.ToString() + tbprotect;

        if (bdexterity != 0) tbdexterity = " + " + bdexterity;
        Dexterity.text = "Ловкість: " + Data.playerdexterity.ToString() + tbdexterity;

        if (bskill != 0) tbskill = " + " + bskill;
        Skill.text = "Майстерність: " + Data.playerskill.ToString() + tbskill;

        if (bvitality != 0) tbvitality = " + " + bvitality;
        Vitability.text = "Живучість: " + Data.playersurvivability.ToString() + tbvitality;

        HP.text = "Здоров'я: " + Data.playerhp.ToString();
        int mhp = Mathf.FloorToInt(Mathf.Pow((Data.playersurvivability + bvitality), (float)2.2) + 66);
        MaxHP.text = "Макс. Здоров'я: " + mhp.ToString();
        int rhp = Mathf.FloorToInt(mhp / 10);
        RegenHP.text = "Відновлення Здоров'я: " + rhp.ToString();

        Name.text = "Ім'я: " + Data.nickname;
        Level.text = "Рівень: " + Data.playerlvl.ToString();
        Expierence.text = "Досвід: " + Data.playerexpierence.ToString() + " / " + (int)(Mathf.Pow(Data.playerlvl, (float)2.2) + 9);
        Glory.text = "Слава: " + Data.glory.ToString();
        // Clan.text = "Клан: " + Data.playerlvl.ToString();
        BattlesInfo.text = "Перемог/Поразок: " + Data.battleswin.ToString() + " / " + Data.battleslose.ToString();
        LootGreenInfo.text = "Награбовано/Втрачено: " + Data.greenlooted.ToString() + " / " + Data.greenlost.ToString();
        LootGoldInfo.text = "Награбовано/Втрачено: " + Data.goldlooted.ToString() + " / " + Data.goldlost.ToString();
        TravelInfo.text = "Хвилин в походах: " + Data.travelminutes.ToString();
        WorkInfo.text = "Годин на охороні околиць: " + Data.guardhours.ToString();
        MineInfo.text = "Добуто: " + Data.minedgold.ToString();
        BeetlesInfo.text = "Знищено комах: " + Data.beetleswin.ToString();

        // Завантаження тренування
        WPower.text = "Сила: " + Data.playerpower.ToString();
        WProtection.text = "Захист: " + Data.playerprotection.ToString();
        WDexterity.text = "Ловкість: " + Data.playerdexterity.ToString();
        WSkill.text = "Майстерність: " + Data.playerskill.ToString();
        WVitability.text = "Живучість: " + Data.playersurvivability.ToString();
        wName.text = "Сила";
        wDescription.text = "Збільшує завданий ворогові шкоди";
        float upgradepr = Mathf.Pow((Data.playerpower - 4), (float)2.6);
        float upgradeprice = Mathf.FloorToInt(upgradepr);
        wUpgrade.text = "Рівень: " + Data.playerpower.ToString() + "\nЦіна покращення: " + upgradeprice.ToString();

        // Завантаження інвентаря
        InventoryCount = Data.inventory;
        yield return StartCoroutine(LoadInventory());
        www.Dispose();
    }

    public class PlayerInfo
    {
        public int playergreen, playergold, playerdiamonds, playerpower, playerprotection, playerdexterity, playerskill, playersurvivability,
        glory, battleswin, battleslose, greenlooted, greenlost, goldlooted, goldlost, travelminutes, guardhours, minedgold, beetleswin,
        playerhp, playerlvl, playerexpierence, inventory;
        public string nickname, clan;
    }

    private IEnumerator LoadInventory()
    {
        WWWForm FindDataBase = new WWWForm();
        FindDataBase.AddField("OnGameRequest", "Yes");
        FindDataBase.AddField("LoadInventory", "Yes");
        FindDataBase.AddField("PlayerID", this.PlayerID);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/loadaccount.php", FindDataBase);
        yield return www.SendWebRequest();
        jsonformat = www.downloadHandler.text;

        if (jsonformat == "0") InventoryCount = 0;
        else
        {
            ItemJS[] Item = JsonHelper.FromJson<ItemJS>(fixJson(jsonformat));
            InventoryCount = Item.Length;
            /*
            if (InventoryCount != 24)
            {
                InventoryCount += 1;
            }
            for (int i = 0; i < InventoryCount; i++)
            {
                if (i == InventoryCount - 1)
                {
                    Inventory[i].sprite = BuyInvSlot;
                }
                Inventory[i].enabled = true;
            }
            */
        }
        www.Dispose();
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

        if (jsonformat != "0")
        {
            Items = JsonHelper.FromJson<ItemJS>(fixJson(jsonformat));
            equipments = Items.Length;
            for (int i = 0; i < equipments; i++)
            {
                if (Items[i].type_item == 1)
                {
                    Image ImPrewiew = Equipments[0].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
                if (Items[i].type_item == 2)
                {
                    Image ImPrewiew = Equipments[1].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
                if (Items[i].type_item == 3)
                {
                    Image ImPrewiew = Equipments[2].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
                if (Items[i].type_item == 4)
                {
                    Image ImPrewiew = Equipments[3].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
                if (Items[i].type_item == 5)
                {
                    Image ImPrewiew = Equipments[4].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
                if (Items[i].type_item == 6)
                {
                    Image ImPrewiew = Equipments[5].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
                if (Items[i].type_item == 7)
                {
                    Image ImPrewiew = Equipments[6].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
                if (Items[i].type_item == 8)
                {
                    Image ImPrewiew = Equipments[7].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
                if (Items[i].type_item == 9)
                {
                    Image ImPrewiew = Equipments[8].GetComponent<Image>();
                    ImPrewiew.sprite = Resources.Load<Sprite>("Shop/Items/" + Items[i].id_item.ToString());
                }
            }
        }
        else
        {
            equipments = 0;
        }
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
        if (Data.itempower != 0) bpower = bpower + Data.itempower;
        if (Data.itemprotection != 0) bprotect = bprotect + Data.itemprotection;
        if (Data.itemdexterity != 0) bdexterity = bdexterity + Data.itemdexterity;
        if (Data.itemskill != 0) bskill = bskill + Data.itemskill;
        if (Data.itemvitability != 0) bvitality = bvitality + Data.itemvitability;
        www.Dispose();
    }

    public class ItemInfo
    {
        public int itempower, itemprotection, itemdexterity, itemskill, itemvitability;
    }
}