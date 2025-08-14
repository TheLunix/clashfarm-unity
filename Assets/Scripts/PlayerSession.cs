using System;
using UnityEngine;

[Serializable]
public class PlayerInfo
{
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

    public float playerhp;  // поточний HP з сервера (int — як на сервері)
    public int maxhp;     // нове поле з сервера
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
