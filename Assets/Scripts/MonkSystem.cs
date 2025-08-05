using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class MonkSystem : MonoBehaviour
{
    public LoadAndUpdateAccount Player;
    public GameObject Button;
    public TextMeshProUGUI MonkInfo;

    public void GetReward() { StartCoroutine(_GetReward()); }
    public IEnumerator _GetReward()
    {
        int chance = Random.Range(0, 101);

        if (chance <= 75) // Зелень
        {
            int reward = Random.Range(Player.Account.playerlvl * 5, Player.Account.playerlvl * 50);
            int green = Player.Account.playergreen + reward;
            yield return StartCoroutine(UpdateCellAccount("playergreen", green.ToString(), Player.Account.id.ToString()));
            yield return StartCoroutine(UpdateCellAccount("monkreward", "1", Player.Account.id.ToString()));
            MonkInfo.text = "Ви осмислили мудрість монаха і отримали: <sprite=0> " + reward + " зелені";
        }
        else if (chance > 75 && chance <= 95) // Золото
        {
            int reward = Random.Range(0, Player.Account.playerlvl);
            int gold = Player.Account.playergold + reward;
            yield return StartCoroutine(UpdateCellAccount("playergold", gold.ToString(), Player.Account.id.ToString()));
            yield return StartCoroutine(UpdateCellAccount("monkreward", "1", Player.Account.id.ToString()));
            MonkInfo.text = "Ви осмислили мудрість монаха і отримали: <sprite=1> " + reward + " золота";
        }
        else // Алмази
        {
            int reward = Random.Range(0, 11);
            int diamonds = Player.Account.playerdiamonds + reward;
            yield return StartCoroutine(UpdateCellAccount("playerdiamonds", diamonds.ToString(), Player.Account.id.ToString()));
            yield return StartCoroutine(UpdateCellAccount("monkreward", "1", Player.Account.id.ToString()));
            MonkInfo.text = "Ви осмислили мудрість монаха і отримали: <sprite=3> " + reward + " алмазів";
        }

        Player.ReloadInfoBar();
        Button.SetActive(false);
    }

    private IEnumerator UpdateCellAccount(string cellname, string value, string id)
    {
        WWWForm findDataBase = new WWWForm();
        findDataBase.AddField("OnGameRequest", "Yes");
        findDataBase.AddField("UpdateCell", cellname);
        findDataBase.AddField("UpdateValue", value);
        findDataBase.AddField("PlayerID", id);

        UnityWebRequest www = UnityWebRequest.Post("http://clashoffarms/getcelldatabase.php", findDataBase);
        yield return www.SendWebRequest();
        www.Dispose();
    }
}