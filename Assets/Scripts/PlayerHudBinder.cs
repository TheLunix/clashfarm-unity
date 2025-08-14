using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHudBinder : MonoBehaviour
{
    public TextMeshProUGUI NickText;
    public TextMeshProUGUI LvlText;
    public TextMeshProUGUI ExpText;
    public TextMeshProUGUI HPText;
    public TextMeshProUGUI GreenText;
    public TextMeshProUGUI GoldText;
    public TextMeshProUGUI DiamondsText;
    public TextMeshProUGUI CombatsCount;
    public TextMeshProUGUI CombatsTimer;
    public Slider ExpSlider;
    public Slider HpSlider;
    public int bvitality;
    void OnEnable()
    {
        if (PlayerSession.I != null)
        {
            PlayerSession.I.OnChanged += Refresh;
            Refresh();
        }
    }

    void OnDisable()
    {
        if (PlayerSession.I != null)
            PlayerSession.I.OnChanged -= Refresh;
    }

    void Refresh()
    {
        var d = PlayerSession.I.Data;
        if (NickText) NickText.text = d.nickname;
        if (LvlText) LvlText.text = d.playerlvl.ToString();
        if (ExpText) ExpText.text = d.playerexpierence.ToString()+"/"+Mathf.Floor(Mathf.Pow(d.playerlvl, 2.2f) + 9);
        if (HPText) HPText.text = d.playerhp.ToString()+"/"+Mathf.FloorToInt(Mathf.Pow(d.playersurvivability + bvitality, 2.2f) + 66);
        if (GoldText) GoldText.text = d.playergold.ToString();
        if (GreenText) GreenText.text = d.playergreen.ToString();
        if (DiamondsText) DiamondsText.text = d.playerdiamonds.ToString();
        if (CombatsCount) CombatsCount.text = d.combats.ToString()+"/6";
        if (CombatsTimer) CombatsTimer.text = "00:00";

        if (ExpSlider)
        {
            ExpSlider.maxValue = Mathf.Floor(Mathf.Pow(d.playerlvl, 2.2f) + 9);
            ExpSlider.value = d.playerexpierence;
        }
        if (HpSlider)
        {
            HpSlider.value = d.playerhp;
        }
    }
}
