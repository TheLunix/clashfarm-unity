using System;
using UnityEngine;

[Serializable]
public class PlayerInfo
{
    [Header("Info")]
    public int id;
    public string nickname;
    public string serialcode;
    [Header("Resources")]
    public int playergreen;
    public int playergold;
    public int playerdiamonds;
    public int combats;
    [Header("Stats")]
    public int playerpower;
    public int playerprotection;
    public int playerdexterity;
    public int playerskill;
    public int playersurvivability;
    public float playerhp;  // поточний HP з сервера (int — як на сервері)
    public int maxhp;     // нове поле з сервера
    [Header("Other")]
    public int playerfraction;
    //===============
    public int playerlvl;
    public int playerexpierence;
    //===============
    public int pet;
    //===============
    public int hourreward;
    //===============
    public int guardhour;
    public int guardhours;
    public string timetoendguard;
    //===============
    public int mining;
    public int minedgold;
    public int ismine;
    public int maxminedgold;
    public string timetoendmine;
    public string timetonextmine;
    //===============
    public int horse;
    public string horsetime;
    //===============
    public int hikeminutes;
    public int hikemin;
    public int hikeactivemin;
    public string timetoendhike;
    public string lasthike;
    //===============
    public int monkreward;
}

public class PlayerSession : MonoBehaviour
{
    public static PlayerSession I { get; private set; }

    [Header("Runtime data")]
    public PlayerInfo Data = new PlayerInfo();

    public event Action OnChanged;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Apply(PlayerInfo newData)
    {
        Data = newData ?? new PlayerInfo();
        OnChanged?.Invoke();
    }

    public void Patch(Action<PlayerInfo> patch)
    {
        patch?.Invoke(Data);
        OnChanged?.Invoke();
    }
}
